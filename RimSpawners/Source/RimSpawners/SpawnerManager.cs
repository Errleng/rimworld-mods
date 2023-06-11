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
        private static readonly int UPDATE_POINTS_INTERVAL = GenTicks.SecondsToTicks(5);
        private static readonly int LONG_UPDATE_INTERVAL = GenTicks.SecondsToTicks(60);
        private static readonly int SPAWN_INTERVAL = GenTicks.SecondsToTicks(15);

        private readonly RimSpawnersSettings settings = LoadedModManager.GetMod<RimSpawners>().GetSettings<RimSpawnersSettings>();
        private int SpawnedPawnPoints => (int)spawnedPawns.Select(x => x.kindDef.combatPower).Sum();
        private List<TargetInfo> spawnLocations = new List<TargetInfo>();

        private bool dormant;

        public bool dropNearEnemy;
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
            Scribe_Values.Look(ref dropNearEnemy, "dropNearEnemy");
            Scribe_Values.Look(ref active, "active");

            Scribe_Values.Look(ref points, "points");
            Scribe_Values.Look(ref maxPoints, "maxPoints");
            Scribe_Collections.Look(ref pawnsToSpawn, "pawnsToSpawn", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref spawnedPawns, "spawnedPawns", LookMode.Reference, Array.Empty<object>());
            Scribe_Collections.Look(ref spawnQueue, "spawnQueue", LookMode.Deep);

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
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            var ticks = Find.TickManager.TicksGame;

            if (ticks % UPDATE_POINTS_INTERVAL == 0)
            {
                points = Math.Min(points + (int)(pointsPerSecond * GenTicks.TicksToSeconds(SPAWN_INTERVAL)), maxPoints);
            }

            if (ticks % SPAWN_INTERVAL == 0)
            {
                points = Math.Min(points + (int)(pointsPerSecond * GenTicks.TicksToSeconds(SPAWN_INTERVAL)), maxPoints);

                spawnedPawns.RemoveAll(x => x == null || !x.Spawned);

                if (settings.spawnOnlyOnThreat)
                {
                    if (Utils.OtherMapsHostile(new List<Map>(), Faction.OfPlayer))
                    {
                        dormant = false;
                        SpawnRound();
                    }
                    else
                    {
                        dormant = true;
                        RemoveAllSpawnedPawns();
                    }
                }
                else
                {
                    dormant = false;
                    SpawnRound();
                }
            }

            if (ticks % LONG_UPDATE_INTERVAL == 0)
            {
                CalculateCache();
            }
        }

        private void SpawnRound()
        {
            if (!active || dormant)
            {
                return;
            }

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
                            return;
                        }
                        Pawn pawn;
                        if (TrySpawnPawn(kind, out pawn))
                        {
                            if (pawn.caller != null)
                            {
                                pawn.caller.DoCall();
                            }
                            points -= (int)kind.combatPower;
                            --entry.count;
                        }
                        else
                        {
                            return;
                        }
                    }
                    spawnQueue.Pop();
                }

                GenerateQueue();

                if (spawnQueue.Count == 0)
                {
                    break;
                }
            }
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
                if (entry.Value.count > 0)
                {
                    spawnQueue.Add(new SpawnPawnInfo(entry.Value));
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
                queueStr.Add($"{entry.GetKindLabel()} x{entry.count}");
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
                , string.Join(", ", queueStr));
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
            foreach (var pawn in spawnedPawns)
            {
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

            spawnedPawns.Clear();
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
            Log.Message($"Recycling pawn {pawn.kindDef.defName} {pawn.Name}");
            cachedPawns[pawn.kindDef.defName].Add(pawn);
        }

        private bool TrySpawnPawn(PawnKindDef kind, out Pawn pawn)
        {
            pawn = GenerateNewPawn(kind);

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

            AddCustomCompToPawn(pawn);

            spawnedPawns.Add(pawn);

            var faction = pawn.Faction;

            TargetInfo dropInfo = null;
            if (spawnLocations.Count > 0)
            {
                var hostileSpawnLocations = spawnLocations.Where(x => GenHostility.AnyHostileActiveThreatTo(x.Map, faction)).ToList();
                if (hostileSpawnLocations.Count > 0)
                {
                    dropInfo = hostileSpawnLocations.RandomElement();
                }
            }
            if (dropInfo == null)
            {
                dropInfo = Utils.FindDropCenter(dropNearEnemy);
            }

            if (dropInfo.Map == null || dropInfo.Cell == IntVec3.Invalid)
            {
                Log.Error($"Could not find drop pod location for spawning {kind.defName}");
                return false;
            }

            //Log.Message($"Dropping {pawn} near {dropInfo.Item1}, {dropInfo.Item2}");

            DropPodUtility.DropThingsNear(dropInfo.Cell,
                dropInfo.Map,
                Gen.YieldSingle<Thing>(pawn),
                60,
                false,
                false,
                false);

            SendMessage(dropInfo.Cell, dropInfo.Map, kind);

            // setup pawn lord and AI
            var lord = LordMaker.MakeNewLord(faction,
                new LordJob_AssaultColony(
                    Faction.OfAncientsHostile,
                    false,
                    false,
                    false,
            false,
                    false,
                    false,
                    false),
                dropInfo.Map);
            lord.AddPawn(pawn);

            return true;
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

            ResurrectionUtility.Resurrect(cachedPawn);
            PawnGenerator.RedressPawn(cachedPawn, request);
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
    }
}
