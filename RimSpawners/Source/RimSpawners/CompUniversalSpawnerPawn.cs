using RimWorld;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimSpawners
{
    internal class CompVanometricFabricatorPawn : ThingComp
    {
        private static readonly RimSpawnersSettings Settings = LoadedModManager.GetMod<RimSpawners>().GetSettings<RimSpawnersSettings>();

        public bool aggressive = true;
        public List<Pawn> cachedPawns;

        public bool canSpawnPawns = true;

        private PawnKindDef chosenKind;

        private bool dormant;
        public TargetInfo dropSpotTarget;

        public int nextPawnSpawnTick = -1;
        private bool paused;

        public int pawnsLeftToSpawn = -1;
        private bool spawnAllAtOnce;

        public List<Pawn> spawnedPawns = new List<Pawn>();
        private bool spawnInDropPods;
        private bool spawnInDropPodsNearEnemy;

        private float spawnUntilFullSpeedMultiplier = 1f;

        private CompProperties_VanometricFabricatorPawn Props => (CompProperties_VanometricFabricatorPawn)props;

        public bool Dormant
        {
            get => dormant;
            set => dormant = value;
        }

        public bool Paused
        {
            get => paused;
            set => paused = value;
        }

        public bool SpawnInDropPods
        {
            get => spawnInDropPods;
            set => spawnInDropPods = value;
        }

        public bool SpawnInDropPodsNearEnemy
        {
            get => spawnInDropPodsNearEnemy;
            set => spawnInDropPodsNearEnemy = value;
        }

        public bool SpawnAllAtOnce
        {
            get => spawnAllAtOnce;
            set => spawnAllAtOnce = value;
        }

        public PawnKindDef ChosenKind
        {
            get => chosenKind;
            set
            {
                chosenKind = value;
                ClearCachedPawns();
                CalculateNextPawnSpawnTick();
            }
        }

        public float SpawnUntilFullSpeedMultiplier
        {
            set => spawnUntilFullSpeedMultiplier = value;
        }

        public Lord Lord => FindLordToJoin(parent, Props.lordJob, Props.shouldJoinParentLord);

        private float SpawnedPawnsPoints
        {
            get
            {
                FilterOutUnspawnedPawns();
                var num = 0f;
                for (var i = 0; i < spawnedPawns.Count; i++)
                {
                    num += spawnedPawns[i].kindDef.combatPower;
                }

                return num;
            }
        }

        public bool Active => pawnsLeftToSpawn != 0 && !Dormant && !Paused;

        public override void Initialize(CompProperties initialProps)
        {
            base.Initialize(initialProps);
            if (chosenKind == null)
            {
                chosenKind = RandomPawnKindDef();
            }

            if (Props.maxPawnsToSpawn != IntRange.zero)
            {
                pawnsLeftToSpawn = Props.maxPawnsToSpawn.RandomInRange;
            }

            dropSpotTarget = new TargetInfo(IntVec3.Invalid, null, true);
        }

        public static Lord FindLordToJoin(Thing spawner,
            Type lordJobType,
            bool shouldTryJoinParentLord,
            Func<Thing, List<Pawn>> spawnedPawnSelector = null)
        {
            if (spawner.Spawned)
            {
                if (shouldTryJoinParentLord)
                {
                    var building = spawner as Building;
                    var lord = building != null ? building.GetLord() : null;
                    if (lord != null)
                    {
                        return lord;
                    }
                }

                if (spawnedPawnSelector == null)
                {
                    spawnedPawnSelector = delegate (Thing s)
                    {
                        var cusp = s.TryGetComp<CompVanometricFabricatorPawn>();
                        if (cusp != null)
                        {
                            return cusp.spawnedPawns;
                        }

                        return null;
                    };
                }

                Predicate<Pawn> hasJob = delegate (Pawn x)
                {
                    var lord2 = x.GetLord();
                    return lord2 != null && lord2.LordJob.GetType() == lordJobType;
                };
                Pawn foundPawn = null;
                RegionTraverser.BreadthFirstTraverse(spawner.GetRegion(),
                    (from,
                        to) => true,
                    delegate (Region r)
                    {
                        var list = r.ListerThings.ThingsOfDef(spawner.def);
                        for (var i = 0; i < list.Count; i++)
                        {
                            if (list[i].Faction == spawner.Faction)
                            {
                                var list2 = spawnedPawnSelector(list[i]);
                                if (list2 != null)
                                {
                                    foundPawn = list2.Find(hasJob);
                                }

                                if (foundPawn != null)
                                {
                                    return true;
                                }
                            }
                        }

                        return false;
                    },
                    40);
                if (foundPawn != null)
                {
                    return foundPawn.GetLord();
                }
            }

            return null;
        }

        public static Lord CreateNewLord(Thing byThing,
            bool aggressive,
            float defendRadius,
            Type lordJobType)
        {
            IntVec3 invalid;
            if (!CellFinder.TryFindRandomCellNear(byThing.Position, byThing.Map, 5, c => c.Standable(byThing.Map) && byThing.Map.reachability.CanReach(c, byThing, PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors)), out invalid))
            {
                Log.Error("Found no place for mechanoids to defend " + byThing);
                invalid = IntVec3.Invalid;
            }

            return LordMaker.MakeNewLord(byThing.Faction,
                Activator.CreateInstance(lordJobType,
                    new SpawnedPawnParams
                    {
                        aggressive = aggressive,
                        defendRadius = defendRadius,
                        defSpot = invalid,
                        spawnerThing = byThing
                    }) as LordJob,
                byThing.Map);
        }

        private void SpawnInitialPawns()
        {
            var num = 0;
            Pawn pawn;
            while (num < Props.initialPawnsCount && TrySpawnPawn(out pawn))
            {
                num++;
            }

            SpawnPawnsUntilPoints(Props.initialPawnsPoints);
        }

        public void SpawnPawnsUntilPoints(float points)
        {
            CalculateNextPawnSpawnTick();
            var num = 0;
            while (SpawnedPawnsPoints < points)
            {
                num++;
                if (num > 1000)
                {
                    Log.Error("Too many iterations.");
                    break;
                }

                Pawn pawn;
                if (!TrySpawnPawn(out pawn))
                {
                    break;
                }
            }
        }

        public void CalculateNextPawnSpawnTick()
        {
            if (chosenKind == null)
            {
                return;
            }

            switch (Settings.spawnTime)
            {
                case SpawnTimeSetting.Scaled:
                    {
                        var secondsToNextSpawn = chosenKind.combatPower / Settings.spawnTimePointsPerSecond;
                        float ticksToNextSpawn = secondsToNextSpawn.SecondsToTicks();
                        CalculateNextPawnSpawnTick(ticksToNextSpawn);
                        break;
                    }
                case SpawnTimeSetting.Fixed:
                    {
                        float ticksToNextSpawn = Props.pawnSpawnIntervalSeconds.SecondsToTicks();
                        CalculateNextPawnSpawnTick(ticksToNextSpawn);
                        break;
                    }
                default:
                    throw new InvalidEnumArgumentException("RimSpawners: spawn time setting enum must have value");
            }
        }

        public void CalculateNextPawnSpawnTick(float delayTicks)
        {
            if (SpawnAllAtOnce)
            {
                if (SpawnedPawnsPoints < Props.maxSpawnedPawnsPoints)
                {
                    var remainingSpawns = (int)Math.Ceiling((Props.maxSpawnedPawnsPoints - SpawnedPawnsPoints) / chosenKind.combatPower);
                    delayTicks *= remainingSpawns;
                }
            }

            delayTicks /= spawnUntilFullSpeedMultiplier;

            nextPawnSpawnTick = Find.TickManager.TicksGame + (int)delayTicks;
        }

        private void FilterOutUnspawnedPawns()
        {
            for (var i = spawnedPawns.Count - 1; i >= 0; i--)
            {
                var pawn = spawnedPawns[i];
                if (SpawnInDropPods)
                {
                    if (!pawn.Spawned && !ThingOwnerUtility.AnyParentIs<ActiveDropPodInfo>(pawn))
                    {
                        spawnedPawns.RemoveAt(i);
                    }
                }
                else if (!pawn.Spawned)
                {
                    spawnedPawns.RemoveAt(i);
                }
            }
        }

        private PawnKindDef RandomPawnKindDef()
        {
            var curPoints = SpawnedPawnsPoints;
            IEnumerable<PawnKindDef> source = Props.spawnablePawnKinds;
            if (Props.maxSpawnedPawnsPoints > -1f)
            {
                source = from x in source
                         where curPoints + x.combatPower <= Props.maxSpawnedPawnsPoints
                         select x;
            }

            PawnKindDef result;
            if (source.TryRandomElement(out result))
            {
                return result;
            }
            return null;
        }

        private IntVec3 GetTargetCellFromHostile(Pawn target)
        {
            if (SpawnInDropPodsNearEnemy)
            {
                return target.Position;
            }
            return DropCellFinder.FindRaidDropCenterDistant(target.Map);
        }

        private Tuple<Map, IntVec3> FindDropCenter()
        {
            var dropCenter = IntVec3.Invalid;
            var targetCell = dropSpotTarget.Cell;
            var targetMap = dropSpotTarget.Map;

            // if no target specified, then try finding automatically on current map
            if (dropSpotTarget.Cell == IntVec3.Invalid)
            {
                targetMap = parent.Map;
                var target = FindRandomActiveHostile(targetMap);
                if (target != null)
                {
                    targetCell = GetTargetCellFromHostile(target);
                }
            }

            // if there are no threats on current map or on target map, then spawn on another map
            if (FindRandomActiveHostile(targetMap) == null && FindRandomActiveHostile(parent.Map) == null && Settings.crossMap)
            {
                var maps = Find.Maps;
                foreach (var map in maps)
                {
                    var target = FindRandomActiveHostile(map);
                    if (target != null)
                    {
                        targetCell = GetTargetCellFromHostile(target);
                        targetMap = map;
                        break;
                    }
                }
            }

            if (targetCell != IntVec3.Invalid)
            {
                DropCellFinder.TryFindDropSpotNear(targetCell, targetMap, out dropCenter, true, false, false);
            }

            if (dropCenter == IntVec3.Invalid)
            {
                dropCenter = DropCellFinder.FindRaidDropCenterDistant(targetMap);
            }

            return new Tuple<Map, IntVec3>(targetMap, dropCenter);
        }

        private bool TrySpawnPawn(out Pawn pawn)
        {
            if (!canSpawnPawns)
            {
                pawn = null;
                return false;
            }

            if (!Props.chooseSingleTypeToSpawn)
            {
                chosenKind = RandomPawnKindDef();
            }

            if (chosenKind == null)
            {
                pawn = null;
                return false;
            }

            pawn = GenerateNewPawn();

            var spawningHumanlike = chosenKind.RaceProps.Humanlike;
            if (spawningHumanlike)
            {
                if (pawn.Faction.IsPlayer)
                {
                    pawn.playerSettings.hostilityResponse = HostilityResponseMode.Attack;
                }

                if (Settings.maxSkills)
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

            var dropPodSuccess = false;
            if (SpawnInDropPods)
            {
                var dropInfo = FindDropCenter();

                DropPodUtility.DropThingsNear(dropInfo.Item2,
                    dropInfo.Item1,
                    Gen.YieldSingle<Thing>(pawn),
                    60,
                    false,
                    false,
                    false);
                dropPodSuccess = true;
            }

            if (!SpawnInDropPods || !dropPodSuccess)
            {
                GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(parent.Position, parent.Map, Props.pawnSpawnRadius), parent.Map);
            }

            // setup pawn lord and AI
            var lord = Lord;
            if (lord == null)
            {
                lord = CreateNewLord(parent, aggressive, Props.defendRadius, Props.lordJob);
            }

            lord.AddPawn(pawn);
            if (Props.spawnSound != null)
            {
                Props.spawnSound.PlayOneShot(parent);
            }

            if (pawnsLeftToSpawn > 0)
            {
                pawnsLeftToSpawn--;
            }

            SendMessage();
            return true;
        }

        private void AddCustomCompToPawn(Pawn pawn)
        {
            var existingSpawnedPawnComp = pawn.GetComp<RimSpawnersPawnComp>();
            if (existingSpawnedPawnComp == null)
            {
                var spawnedPawnComp = new RimSpawnersPawnComp();
                var spawnedPawnCompProps = new CompProperties_RimSpawnersPawn(this);
                spawnedPawnComp.parent = pawn;
                pawn.AllComps.Add(spawnedPawnComp);
                spawnedPawnComp.Initialize(spawnedPawnCompProps);
            }
        }

        private Pawn GenerateNewPawn()
        {
            var spawningHumanlike = chosenKind.RaceProps.Humanlike;
            var maxLifeStageIndex = chosenKind.lifeStages.Count - 1;
            // account for humanlikes, which do not have lifeStages
            if (maxLifeStageIndex < 0 && spawningHumanlike)
            {
                maxLifeStageIndex = chosenKind.RaceProps.lifeStageAges.Count - 1;
            }

            var pawnMinAge = chosenKind.RaceProps.lifeStageAges[maxLifeStageIndex].minAge;

            var pawnFaction = parent.Faction;
            if (pawnFaction.IsPlayer && Settings.useAllyFaction)
            {
                var spawnedPawnFaction = Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamed("RimSpawnersFriendlyFaction", false));
                if (spawnedPawnFaction != null)
                {
                    pawnFaction = spawnedPawnFaction;
                }
            }

            var request = new PawnGenerationRequest(
                chosenKind,
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
            if (spawningHumanlike && Settings.cachePawns && cachedPawns.Count > 0)
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
            var cachedPawn = cachedPawns[0];
            cachedPawns.RemoveAt(0);

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
                Log.Message("Cached pawn dead after resurrection??");
                cachedPawn.health.Notify_Resurrected();
                if (cachedPawn.Dead)
                {
                    Log.Message("Cached pawn still dead after resurrection??");
                }
            }

            return cachedPawn;
        }

        public void RemoveAllSpawnedPawns()
        {
            Log.Message("Vanometric fabricator comp is destroying all spawned pawns");

            foreach (var pawn in spawnedPawns)
            {
                if (Settings.cachePawns)
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
            Log.Message("Vanometric fabricator comp is destroying all cached pawns");
            foreach (var cachedPawn in cachedPawns)
            {
                if (cachedPawn != null && !cachedPawn.Destroyed)
                {
                    cachedPawn.Destroy();
                }
            }

            cachedPawns.Clear();
        }

        public void RecyclePawn(Pawn pawn)
        {
            if (pawn.kindDef.Equals(chosenKind))
            {
                Log.Message($"Recycling pawn {pawn.kindDef.LabelCap} {pawn.Name}");
                // add dead pawns to cached pawns
                cachedPawns.Add(pawn);
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            cachedPawns = new List<Pawn>();

            if (!respawningAfterLoad && Active && nextPawnSpawnTick == -1)
            {
                SpawnInitialPawns();
            }

            // add custom ThingComp to all pawns after loading a save
            foreach (var pawn in spawnedPawns)
            {
                AddCustomCompToPawn(pawn);

                //// following works to remove needs with ShouldHaveNeeds patch
                //if (Settings.disableNeeds)
                //{
                //    pawn.needs.AddOrRemoveNeedsAsAppropriate();
                //}
            }
        }

        private Pawn FindRandomActiveHostile(Map map)
        {
            Pawn hostilePawn = null;
            var hostilePawns = new List<Pawn>();

            var pawnsOnMap = map.mapPawns.AllPawnsSpawned;
            foreach (var pawn in pawnsOnMap)
            {
                if (pawn.HostileTo(Faction.OfPlayer) && !pawn.Downed)
                {
                    var dormantComp = pawn.GetComp<CompCanBeDormant>();
                    if (dormantComp == null || dormantComp.Awake)
                    {
                        hostilePawns.Add(pawn);
                    }
                }
            }

            if (hostilePawns.Count > 0)
            {
                hostilePawn = hostilePawns[Rand.Range(0, hostilePawns.Count)];
            }

            return hostilePawn;
        }

        public override void CompTick()
        {
            if (Active && parent.Spawned && nextPawnSpawnTick == -1)
            {
                SpawnInitialPawns();
            }

            if (parent.Spawned)
            {
                if (SpawnedPawnsPoints >= Props.maxSpawnedPawnsPoints)
                {
                    spawnUntilFullSpeedMultiplier = 1f;
                }

                if (Active && Find.TickManager.TicksGame >= nextPawnSpawnTick && SpawnedPawnsPoints < Props.maxSpawnedPawnsPoints)
                {
                    FilterOutUnspawnedPawns();

                    Pawn pawn;

                    if (SpawnAllAtOnce)
                    {
                        SpawnPawnsUntilPoints(Props.maxSpawnedPawnsPoints);
                    }
                    else
                    {
                        if ((Props.maxSpawnedPawnsPoints < 0f || SpawnedPawnsPoints < Props.maxSpawnedPawnsPoints) && TrySpawnPawn(out pawn) && pawn.caller != null)
                        {
                            pawn.caller.DoCall();
                        }

                        CalculateNextPawnSpawnTick();
                    }
                }
            }
        }

        public void SendMessage()
        {
            if (!Props.spawnMessageKey.NullOrEmpty() && MessagesRepeatAvoider.MessageShowAllowed(Props.spawnMessageKey, 0.1f))
            {
                Messages.Message(Props.spawnMessageKey.Translate(chosenKind.LabelCap), parent, MessageTypeDefOf.SilentInput);
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEBUG: Spawn pawn",
                    icon = TexCommand.ReleaseAnimals,
                    action = delegate
                    {
                        Pawn pawn;
                        TrySpawnPawn(out pawn);
                    }
                };
            }
        }

        public override string CompInspectStringExtra()
        {
            if (chosenKind == null)
            {
                return "RimSpawners_VanometricFabricatorInspectNoneChosen".Translate();
            }

            string text;

            string chosenKindName = chosenKind.LabelCap;

            if (SpawnedPawnsPoints < Props.maxSpawnedPawnsPoints)
            {
                text = "RimSpawners_VanometricFabricatorInspectChosen".Translate(chosenKindName, SpawnedPawnsPoints, Props.maxSpawnedPawnsPoints);

                if (!Paused && !Dormant)
                {
                    var ticksToNextSpawn = nextPawnSpawnTick - Find.TickManager.TicksGame;
                    text += "RimSpawners_VanometricFabricatorInspectNextSpawn".Translate(ticksToNextSpawn.ToStringSecondsFromTicks());
                }
            }
            else
            {
                text = "RimSpawners_VanometricFabricatorInspectLimit".Translate(chosenKindName, Props.maxSpawnedPawnsPoints);
            }

            if (Paused)
            {
                text += "RimSpawners_VanometricFabricatorInspectPaused".Translate();
            }
            else if (Dormant)
            {
                text += "RimSpawners_VanometricFabricatorInspectDormant".Translate();
            }

            if (Prefs.DevMode)
            {
                var ticksToNextSpawn = nextPawnSpawnTick - Find.TickManager.TicksGame;
                text += "RimSpawners_VanometricFabricatorInspectDebug".Translate(ticksToNextSpawn);
            }

            return text;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextPawnSpawnTick, "nextPawnSpawnTick");
            Scribe_Values.Look(ref pawnsLeftToSpawn, "pawnsLeftToSpawn", -1);
            Scribe_Collections.Look(ref spawnedPawns, "spawnedPawns", LookMode.Reference, Array.Empty<object>());
            Scribe_Values.Look(ref aggressive, "aggressive");
            Scribe_Values.Look(ref canSpawnPawns, "canSpawnPawns", true);
            Scribe_Values.Look(ref dormant, "dormant");
            Scribe_Values.Look(ref paused, "paused");
            Scribe_Values.Look(ref spawnInDropPods, "spawnInDropPods");
            Scribe_Values.Look(ref spawnInDropPodsNearEnemy, "spawnInDropPodsNearEnemy");
            Scribe_Values.Look(ref spawnAllAtOnce, "spawnAllAtOnce");
            Scribe_Defs.Look(ref chosenKind, "chosenKind");
            Scribe_TargetInfo.Look(ref dropSpotTarget, "dropSpotTarget");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                spawnedPawns.RemoveAll(x => x == null);
                if (pawnsLeftToSpawn == -1 && Props.maxPawnsToSpawn != IntRange.zero)
                {
                    pawnsLeftToSpawn = Props.maxPawnsToSpawn.RandomInRange;
                }
            }
        }
    }
}