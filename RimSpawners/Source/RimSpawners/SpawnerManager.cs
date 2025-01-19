using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace RimSpawners
{
    internal class SpawnerManager : WorldComponent
    {
        private static readonly int LONG_UPDATE_INTERVAL = GenTicks.SecondsToTicks(60);
        private static readonly int SPAWN_INTERVAL = GenTicks.SecondsToTicks(15);

        private readonly RimSpawnersSettings settings = LoadedModManager.GetMod<RimSpawners>().GetSettings<RimSpawnersSettings>();
        private int SpawnedPawnPoints => (int)spawnedPawns.Select(x => x.kindDef.combatPower).Sum();
        private List<TargetInfo> spawnLocations = new List<TargetInfo>();
        private List<ThingStuffPair> allWeapons = null;
        private List<ThingStuffPair> meleeWeapons = null;
        private List<ThingStuffPair> rangedWeapons = null;

        private bool dormant;

        public bool useDropPod = true;
        public bool spawnNearEnemy;
        public bool spawnAllAtOnce;
        public bool active = true;

        public int points;
        public int maxPoints;
        public int pointsPerSecond;
        public Dictionary<string, SpawnPawnInfo> pawnsToSpawn = new Dictionary<string, SpawnPawnInfo>();
        public List<Pawn> spawnedPawns = new List<Pawn>();
        Dictionary<string, List<Pawn>> cachedPawns = new Dictionary<string, List<Pawn>>();
        public List<SpawnPawnInfo> spawnQueue = new List<SpawnPawnInfo>();


        public SpawnerManager(World world) : base(world)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref useDropPod, "useDropPod");
            Scribe_Values.Look(ref spawnNearEnemy, "spawnNearEnemy");
            Scribe_Values.Look(ref spawnAllAtOnce, "spawnAllAtOnce");
            Scribe_Values.Look(ref active, "active");

            Scribe_Values.Look(ref points, "points");
            Scribe_Values.Look(ref maxPoints, "maxPoints");
            Scribe_Collections.Look(ref pawnsToSpawn, "pawnsToSpawn", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref spawnedPawns, "spawnedPawns", LookMode.Reference, Array.Empty<object>());
            Scribe_Collections.Look(ref spawnQueue, "spawnQueue", LookMode.Deep);

            var nullPawns = spawnedPawns.Where(x => x == null).ToList();
            if (nullPawns.Count > 0)
            {
                Log.Error($"Spawner manager is tracking {nullPawns.Count}/{spawnedPawns.Count} null spawned pawns. Spawned pawns list: {string.Join(", ", spawnedPawns.Select(x => x == null ? "Null" : x.LabelCap))}");
            }
            spawnedPawns.RemoveAll(x => x == null);

            cachedPawns.Clear();
            foreach (var pawnKind in DefDatabase<PawnKindDef>.AllDefsListForReading)
            {
                if (!pawnsToSpawn.ContainsKey(pawnKind.defName))
                {
                    pawnsToSpawn.Add(pawnKind.defName, new SpawnPawnInfo(pawnKind.defName, pawnKind.LabelCap));
                }
                cachedPawns.Add(pawnKind.defName, new List<Pawn>());
            }

            foreach (var pawn in spawnedPawns)
            {
                AddCustomCompToPawn(pawn);
            }

            Predicate<ThingDef> isWeapon = (ThingDef td) => td.equipmentType == EquipmentType.Primary && !td.weaponTags.NullOrEmpty();
            allWeapons = ThingStuffPair.AllWith(isWeapon);
            meleeWeapons = allWeapons.Where(x => x.thing.IsMeleeWeapon).ToList();
            rangedWeapons = allWeapons.Where(x => !x.thing.IsMeleeWeapon).ToList();
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            var ticks = Find.TickManager.TicksGame;

            if (ticks % SPAWN_INTERVAL == 0)
            {
                points = points + (int)(pointsPerSecond * GenTicks.TicksToSeconds(SPAWN_INTERVAL));

                spawnedPawns.RemoveAll(x => x == null || !x.Spawned);

                // Create dictionary of (map, hostiles)
                var mapsToHostilePawns = new Dictionary<Map, List<Pawn>>();
                foreach (var map in Find.Maps)
                {
                    var activeHostilePawns = Utils.GetActiveHostilesOnMap(map);
                    if (activeHostilePawns.Count > 0 || GenHostility.AnyHostileActiveThreatTo(map, Faction.OfPlayer))
                    {
                        mapsToHostilePawns.Add(map, activeHostilePawns);
                    }
                }

                if (settings.spawnOnlyOnThreat)
                {
                    // Remove all pawns on maps without threats
                    var spawnedPawnsOnMapsWithoutHostiles = spawnedPawns
                        .Where(x => !mapsToHostilePawns.ContainsKey(x.Map))
                        .Select(x => x.ThingID)
                        .ToHashSet();
                    RemoveSpawnedPawns(spawnedPawnsOnMapsWithoutHostiles);

                    if (mapsToHostilePawns.Count == 0)
                    {
                        // If there are no maps with hostiles, then go to sleep
                        dormant = true;
                    }
                    else
                    {
                        dormant = false;
                        SpawnRound(mapsToHostilePawns);
                    }
                }
                else
                {
                    dormant = false;
                    if (mapsToHostilePawns.Count == 0)
                    {
                        // If there are no maps with hostiles, then spawn on every map
                        SpawnRound(Find.Maps.ToDictionary(x => x, x => new List<Pawn>()));
                    }
                    else
                    {
                        SpawnRound(mapsToHostilePawns);
                    }
                }

                points = Math.Min(points, maxPoints);
            }

            if (ticks % LONG_UPDATE_INTERVAL == 0)
            {
                CalculateCache();
            }
        }

        private void SpawnRound(Dictionary<Map, List<Pawn>> mapsToHostilePawns)
        {
            if (!active || dormant)
            {
                return;
            }

            if (spawnAllAtOnce && points < maxPoints)
            {
                return;
            }

            // Save excess points
            int excessPoints = Math.Max(0, points - maxPoints);
            // Cap the points to use in spawning
            points = Math.Min(points, maxPoints);
            // Spawn pawns on maps with hostile pawns
            PlacePawns(GeneratePawns(), mapsToHostilePawns);

            // Restore excess points
            points += excessPoints;
        }

        private List<Pawn> GeneratePawns()
        {
            var generatedPawns = new List<Pawn>();
            while (points > 0)
            {
                while (spawnQueue.Count > 0)
                {
                    var entry = spawnQueue.Last();
                    var kind = DefDatabase<PawnKindDef>.GetNamed(entry.pawnKindDefName);

                    while (entry.count > 0)
                    {
                        if (points < kind.combatPower || SpawnedPawnPoints > maxPoints)
                        {
                            return generatedPawns;
                        }
                        Pawn pawn = GeneratePawn(kind);
                        if (pawn.caller != null)
                        {
                            pawn.caller.DoCall();
                        }
                        points -= (int)kind.combatPower;
                        --entry.count;
                        generatedPawns.Add(pawn);
                    }
                    spawnQueue.Pop();
                }

                GenerateQueue();

                if (spawnQueue.Count == 0)
                {
                    break;
                }
            }
            return generatedPawns;
        }

        public void CalculateCache()
        {
            var newPointsPerSecond = 0;
            var newMaxPoints = 0;
            spawnLocations.Clear();

            List<Map> maps = Find.Maps;
            bool foundCoreFab = false;
            foreach (var map in maps)
            {
                foreach (Building building in map.listerBuildings.allBuildingsColonist)
                {
                    if (building.def == RimSpawnersDefOf.CoreFabricator)
                    {
                        foundCoreFab = true;
                    }
                    else if (building.def == RimSpawnersDefOf.MatterSiphon)
                    {
                        newPointsPerSecond += building.TryGetComp<CompPointGenerator>().PointsPerSecond;
                    }
                    else if (building.def == RimSpawnersDefOf.ControlNode)
                    {
                        newMaxPoints += building.TryGetComp<CompPointStorage>().PointsStored;
                    }
                    else if (building.def == RimSpawnersDefOf.SpawnMarker)
                    {
                        spawnLocations.Add(new TargetInfo(building.Position, building.Map));
                    }
                }
            }

            if (!foundCoreFab)
            {
                active = false;
            }

            pointsPerSecond = newPointsPerSecond;
            maxPoints = newMaxPoints;
        }

        public void Reset()
        {
            RemoveAllSpawnedPawns();
            spawnQueue.Clear();
            foreach (var entry in pawnsToSpawn)
            {
                pawnsToSpawn[entry.Key].count = 0;
            }
        }

        public void GenerateQueue()
        {
            spawnQueue.Clear();
            foreach (var entry in pawnsToSpawn)
            {
                if (entry.Value.count == 0)
                {
                    continue;
                }
                if (settings.groupPawnkinds)
                {
                    spawnQueue.Add(new SpawnPawnInfo(entry.Value));
                }
                else
                {
                    for (int i = 0; i < entry.Value.count; i++)
                    {
                        var spawnInfo = new SpawnPawnInfo(entry.Value);
                        spawnInfo.count = 1;
                        spawnQueue.Add(spawnInfo);
                    }
                }
            }
            spawnQueue.Shuffle();
        }

        public string GetInspectString()
        {
            var queueStr = new List<string>();
            for (int i = spawnQueue.Count - 1; i >= 0; i--)
            {
                var entry = spawnQueue[i];
                var spawnInfoLabel = entry.GetKindLabel();
                if (settings.groupPawnkinds)
                {
                    spawnInfoLabel += $" x{entry.count}";
                }
                queueStr.Add(spawnInfoLabel);
            }
            var timeToNextSpawn = Math.Round(GenTicks.TicksToSeconds(SPAWN_INTERVAL - Find.TickManager.TicksGame % SPAWN_INTERVAL));
            return "RimSpawners_SpawnerManagerInspectString"
                .Translate(
                points
                , maxPoints
                , pointsPerSecond
                , spawnedPawns.Count
                , SpawnedPawnPoints >= maxPoints ? "all" : SpawnedPawnPoints.ToString()
                , timeToNextSpawn
                , string.Join(", ", queueStr),
                active ? "" : "PAUSED").Trim();
        }

        public string[] GetSpawnedPawnCounts()
        {
            var pawnCounts = new List<string>();
            for (int i = spawnQueue.Count - 1; i >= 0; i--)
            {
                var entry = spawnQueue[i];
                if (entry.count > 0)
                {
                    pawnCounts.Add($"{entry.GetKindLabel()} x{entry.count}");
                }
            }
            return pawnCounts.ToArray();
        }

        public void RemoveAllSpawnedPawns()
        {
            RemoveSpawnedPawns(spawnedPawns.Select(x => x.ThingID).ToHashSet());
        }

        public void RemoveSpawnedPawns(HashSet<string> pawnIds)
        {
            foreach (var pawn in spawnedPawns)
            {
                if (!pawnIds.Contains(pawn.ThingID))
                {
                    continue;
                }
                if (settings.cachePawns)
                {
                    // make it like the pawn never existed
                    pawn.SetFaction(null);
                    pawn.relations?.ClearAllRelations();

                    // destroy everything they owned
                    pawn.inventory?.DestroyAll();
                    pawn.apparel?.DestroyAll();
                    pawn.equipment?.DestroyAllEquipment();

                    pawn.Kill(null);
                    pawn.Corpse?.Destroy();
                }
                else
                {
                    pawn.Destroy();
                }
            }
            spawnedPawns.RemoveAll(pawn => pawnIds.Contains(pawn.ThingID));
        }

        public void ClearCachedPawns()
        {
            foreach (var entry in cachedPawns)
            {
                foreach (var cachedPawn in entry.Value)
                {
                    if (cachedPawn != null && !cachedPawn.Destroyed)
                    {
                        cachedPawn.Destroy();
                    }
                }
                entry.Value.Clear();
            }
        }

        public void RecyclePawn(Pawn pawn)
        {
            //Log.Message($"Recycling pawn {pawn.kindDef.defName} {pawn.Name}");
            cachedPawns[pawn.kindDef.defName].Add(pawn);
        }

        private Pawn GeneratePawn(PawnKindDef kind)
        {
            Pawn pawn = GenerateNewPawn(kind);

            var spawningHumanlike = kind.RaceProps.Humanlike;
            if (spawningHumanlike)
            {
                if (pawn.Faction.IsPlayer)
                {
                    pawn.playerSettings.hostilityResponse = HostilityResponseMode.Attack;
                }

                if (settings.maxSkills)
                {
                    foreach (var skillRecord in pawn.skills.skills)
                    {
                        if (!skillRecord.TotallyDisabled)
                        {
                            skillRecord.Level = SkillRecord.MaxLevel;
                            skillRecord.passion = Passion.Major;
                        }
                    }
                }

            }

            if (settings.randomizeLoadouts)
            {
                RandomizeLoadout(pawn);
            }

            AddCustomCompToPawn(pawn);

            spawnedPawns.Add(pawn);

            return pawn;
        }

        private void PlacePawns(List<Pawn> pawns, Dictionary<Map, List<Pawn>> mapsToHostilePawns)
        {
            // Create dictionary of (map, spawn marker locations)
            var mapSpawnLocations = new Dictionary<Map, List<TargetInfo>>();
            foreach (var spawnLocation in spawnLocations)
            {
                if (!mapSpawnLocations.ContainsKey(spawnLocation.Map))
                {
                    mapSpawnLocations.Add(spawnLocation.Map, new List<TargetInfo>());
                }
                mapSpawnLocations[spawnLocation.Map].Add(spawnLocation);
            }

            // Spawn pawns Round Robin on each map with hostiles
            var mapsWithHostiles = new List<Map>(mapsToHostilePawns.Keys);
            for (int i = 0; i < pawns.Count; i++)
            {
                var map = mapsWithHostiles[i % mapsToHostilePawns.Count];
                TargetInfo spawnInfo = null;

                if (mapSpawnLocations.ContainsKey(map))
                {
                    // Prioritize the spawn locations on the map
                    spawnInfo = mapSpawnLocations[map].RandomElement();
                }
                else
                {
                    // Choose where to spawn on the map
                    spawnInfo = FindSpawnLocation(mapsToHostilePawns, map);
                }

                var pawn = pawns[i];
                if (spawnInfo.Map == null || spawnInfo.Cell == IntVec3.Invalid)
                {
                    Log.Error($"Could not find location for spawning {pawn.kindDef.defName} on map {map}");
                    continue;
                }

                //Log.Message($"Dropping {pawn} near {dropInfo.Item1}, {dropInfo.Item2}");

                if (useDropPod)
                {
                    DropPodUtility.DropThingsNear(spawnInfo.Cell,
                    spawnInfo.Map,
                    Gen.YieldSingle<Thing>(pawn),
                    60,
                    false,
                    false,
                    false);
                }
                else
                {
                    GenSpawn.Spawn(pawn, spawnInfo.Cell, spawnInfo.Map);
                }

                SendMessage(spawnInfo.Cell, spawnInfo.Map, pawn.kindDef);

                // setup pawn lord and AI
                var lord = LordMaker.MakeNewLord(
                    pawn.Faction,
                    new LordJob_SearchAndDestroy(),
                    spawnInfo.Map);
                lord.AddPawn(pawn);
            }
        }

        private TargetInfo FindSpawnLocation(Dictionary<Map, List<Pawn>> mapsToHostilePawns, Map map)
        {
            var spawnCenter = IntVec3.Invalid;
            var hostiles = mapsToHostilePawns[map];
            if (spawnNearEnemy && hostiles.Count > 0)
            {
                // Pick a random enemy
                var target = hostiles.RandomElement();
                var targetCell = target.Position;
                if (useDropPod)
                {
                    DropCellFinder.TryFindDropSpotNear(targetCell, map, out spawnCenter, true, false, false);
                }
                else
                {
                    spawnCenter = CellFinder.RandomClosewalkCellNear(targetCell, map, 20);
                }
            }
            else
            {
                // Do not spawn near enemy OR there are no hostiles on the map
                if (useDropPod)
                {
                    spawnCenter = DropCellFinder.FindRaidDropCenterDistant(map);
                }
                else
                {
                    spawnCenter = CellFinder.RandomCell(map);
                }
            }

            return new TargetInfo(spawnCenter, map);
        }

        void SendMessage(IntVec3 pos, Map map, PawnKindDef kind)
        {
            if (MessagesRepeatAvoider.MessageShowAllowed("RimSpawners_VanometricFabricatorSpawnMessage", 0.1f))
            {
                Messages.Message("RimSpawners_VanometricFabricatorSpawnMessage".Translate(kind.LabelCap.ToString() ?? kind.defName), new TargetInfo(pos, map), MessageTypeDefOf.SilentInput);
            }
        }

        private void AddCustomCompToPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                Log.Error($"Tried to add RimSpawners spawned pawn comp to a null pawn");
                return;
            }
            var existingSpawnedPawnComp = pawn.GetComp<RimSpawnersPawnComp>();
            if (existingSpawnedPawnComp == null)
            {
                var spawnedPawnComp = new RimSpawnersPawnComp();
                var spawnedPawnCompProps = new CompProperties_RimSpawnersPawn(RecyclePawn);
                spawnedPawnComp.parent = pawn;
                pawn.AllComps.Add(spawnedPawnComp);
                spawnedPawnComp.Initialize(spawnedPawnCompProps);
            }
        }

        private Pawn GenerateNewPawn(PawnKindDef kind)
        {
            var spawningHumanlike = kind.RaceProps.Humanlike;
            var maxLifeStageIndex = kind.lifeStages.Count - 1;
            // account for humanlikes, which do not have lifeStages
            if (maxLifeStageIndex < 0 && spawningHumanlike)
            {
                maxLifeStageIndex = kind.RaceProps.lifeStageAges.Count - 1;
            }

            var pawnMinAge = kind.RaceProps.lifeStageAges[maxLifeStageIndex].minAge;

            var pawnFaction = Faction.OfPlayer;
            if (pawnFaction.IsPlayer && settings.useAllyFaction)
            {
                var spawnedPawnFaction = Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamed("RimSpawnersFriendlyFaction", true));
                if (spawnedPawnFaction != null)
                {
                    pawnFaction = spawnedPawnFaction;
                }
            }

            var request = new PawnGenerationRequest(
                kind,
                pawnFaction,
                PawnGenerationContext.NonPlayer,
                -1,
                true,
                false,
                false,
                false,
                true,
                0f,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                0f,
                0f,
                null,
                0f,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                false,
                false,
                false,
                false,
                null,
                null,
                null,
                null,
                null,
                0f,
                DevelopmentalStage.Adult,
                null,
                null,
                null,
                false);

            if (ModLister.RoyaltyInstalled)
            {
                // disable royalty titles because
                //   the pawn may use permits or psychic powers (too powerful) and
                //   on death, the pawn's title may be inherited by a colonist
                // this also prevents empire pawns from having psychic powers
                request.ForbidAnyTitle = true;
            }

            Pawn pawn;
            if (spawningHumanlike && settings.cachePawns && cachedPawns[kind.defName].Count > 0)
            {
                Log.Message("Trying to use cached pawn");
                pawn = GetCachedPawn(request);
            }
            else
            {
                Log.Message("Generating new pawn");
                pawn = PawnGenerator.GeneratePawn(request);
            }

            return pawn;
        }

        private Pawn GetCachedPawn(PawnGenerationRequest request)
        {
            // take the first pawn in the dead pawn queue
            var kind = request.KindDef;
            var cachedPawn = cachedPawns[kind.defName][0];
            cachedPawns[kind.defName].RemoveAt(0);

            if (cachedPawn.Discarded)
            {
                Log.Message("Cached pawn is discarded. Generating a new pawn.");
                return PawnGenerator.GeneratePawn(request);
            }

            cachedPawn.Corpse?.Destroy();

            ResurrectionUtility.TryResurrect(cachedPawn);
            PawnGenerator.RedressPawn(cachedPawn, request);

            if (settings.randomizeLoadouts)
            {
                RandomizeLoadout(cachedPawn);
            }

            Log.Message($"Using cached pawn {cachedPawn.Name}");

            if (cachedPawn.Dead)
            {
                Log.Warning("Cached pawn dead after resurrection??");
                cachedPawn.health.Notify_Resurrected();
                if (cachedPawn.Dead)
                {
                    Log.Warning("Cached pawn still dead after resurrection??");
                }
            }

            return cachedPawn;
        }

        private void RandomizeLoadout(Pawn pawn)
        {
            if (!pawn.RaceProps.ToolUser)
            {
                Log.Message($"Cannot randomize loadout for {pawn} because it is not a tool user");
                return;
            }
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                Log.Message($"Cannot randomize loadout for {pawn} because it is incapable of manipulation");
                return;
            }
            if (pawn.WorkTagIsDisabled(WorkTags.Violent))
            {
                Log.Message($"Cannot randomize loadout for {pawn} because it is incapable of violence");
                return;
            }

            pawn.equipment.DestroyAllEquipment();

            // Let's give a bias for ranged weapons, maybe 80/20 ranged/melee split
            var weaponPool = rangedWeapons;
            if (RimSpawners.rng.Next(100) < 20)
            {
                weaponPool = meleeWeapons;
            }

            var thingStuffPair = weaponPool.RandomElement();
            var thingWithComps = (ThingWithComps)ThingMaker.MakeThing(thingStuffPair.thing, thingStuffPair.stuff);
            PawnGenerator.PostProcessGeneratedGear(thingWithComps, pawn);
            CompEquippable compEquippable = thingWithComps.TryGetComp<CompEquippable>();
            if (compEquippable != null)
            {
                if (pawn.kindDef.weaponStyleDef != null)
                {
                    compEquippable.parent.StyleDef = pawn.kindDef.weaponStyleDef;
                }
                else if (pawn.Ideo != null)
                {
                    compEquippable.parent.StyleDef = pawn.Ideo.GetStyleFor(thingWithComps.def);
                }
            }

            pawn.equipment.AddEquipment(thingWithComps);
        }
    }
}
