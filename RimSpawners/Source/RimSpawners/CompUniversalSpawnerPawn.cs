﻿using RimWorld;
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
    class CompUniversalSpawnerPawn : ThingComp
    {
        static readonly RimSpawnersSettings Settings = LoadedModManager.GetMod<RimSpawners>().GetSettings<RimSpawnersSettings>();

        private CompProperties_UniversalSpawnerPawn Props => (CompProperties_UniversalSpawnerPawn)props;

        public bool Dormant { get => dormant; set => dormant = value; }
        public bool Paused { get => paused; set => paused = value; }

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

        public float SpawnUntilFullSpeedMultiplier { set => spawnUntilFullSpeedMultiplier = value; }

        public Lord Lord => FindLordToJoin(parent, Props.lordJob, Props.shouldJoinParentLord, null);

        private float SpawnedPawnsPoints
        {
            get
            {
                FilterOutUnspawnedPawns();
                float num = 0f;
                for (int i = 0; i < spawnedPawns.Count; i++)
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
        }

        public static Lord FindLordToJoin(Thing spawner, Type lordJobType, bool shouldTryJoinParentLord, Func<Thing, List<Pawn>> spawnedPawnSelector = null)
        {
            if (spawner.Spawned)
            {
                if (shouldTryJoinParentLord)
                {
                    Building building = spawner as Building;
                    Lord lord = (building != null) ? building.GetLord() : null;
                    if (lord != null)
                    {
                        return lord;
                    }
                }
                if (spawnedPawnSelector == null)
                {
                    spawnedPawnSelector = delegate (Thing s)
                    {
                        CompUniversalSpawnerPawn cusp = s.TryGetComp<CompUniversalSpawnerPawn>();
                        if (cusp != null)
                        {
                            return cusp.spawnedPawns;
                        }
                        return null;
                    };
                }
                Predicate<Pawn> hasJob = delegate (Pawn x)
                {
                    Lord lord2 = x.GetLord();
                    return lord2 != null && lord2.LordJob.GetType() == lordJobType;
                };
                Pawn foundPawn = null;
                RegionTraverser.BreadthFirstTraverse(spawner.GetRegion(RegionType.Set_Passable), (Region from, Region to) => true, delegate (Region r)
                {
                    List<Thing> list = r.ListerThings.ThingsOfDef(spawner.def);
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].Faction == spawner.Faction)
                        {
                            List<Pawn> list2 = spawnedPawnSelector(list[i]);
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
                }, 40, RegionType.Set_Passable);
                if (foundPawn != null)
                {
                    return foundPawn.GetLord();
                }
            }
            return null;
        }

        public static Lord CreateNewLord(Thing byThing, bool aggressive, float defendRadius, Type lordJobType)
        {
            IntVec3 invalid;
            if (!CellFinder.TryFindRandomCellNear(byThing.Position, byThing.Map, 5, (IntVec3 c) => c.Standable(byThing.Map) && byThing.Map.reachability.CanReach(c, byThing, PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)), out invalid, -1))
            {
                Log.Error("Found no place for mechanoids to defend " + byThing, false);
                invalid = IntVec3.Invalid;
            }
            return LordMaker.MakeNewLord(byThing.Faction, Activator.CreateInstance(lordJobType, new object[]
            {
                new SpawnedPawnParams
                {
                    aggressive = aggressive,
                    defendRadius = defendRadius,
                    defSpot = invalid,
                    spawnerThing = byThing
                }
            }) as LordJob, byThing.Map, null);
        }

        private void SpawnInitialPawns()
        {
            int num = 0;
            Pawn pawn;
            while (num < Props.initialPawnsCount && TrySpawnPawn(out pawn))
            {
                num++;
            }
            SpawnPawnsUntilPoints(Props.initialPawnsPoints);
            CalculateNextPawnSpawnTick();
        }

        public void SpawnPawnsUntilPoints(float points)
        {
            int num = 0;
            while (SpawnedPawnsPoints < points)
            {
                num++;
                if (num > 1000)
                {
                    Log.Error("Too many iterations.", false);
                    break;
                }
                Pawn pawn;
                if (!TrySpawnPawn(out pawn))
                {
                    break;
                }
            }
            CalculateNextPawnSpawnTick();
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
                        float secondsToNextSpawn = chosenKind.combatPower / Settings.spawnTimePointsPerSecond;
                        float ticksToNextSpawn = GenTicks.SecondsToTicks(secondsToNextSpawn);
                        CalculateNextPawnSpawnTick(ticksToNextSpawn);
                        break;
                    }
                case SpawnTimeSetting.Fixed:
                    {
                        float ticksToNextSpawn = GenTicks.SecondsToTicks(Props.pawnSpawnIntervalSeconds);
                        CalculateNextPawnSpawnTick(ticksToNextSpawn);
                        break;
                    }
                default:
                    throw new InvalidEnumArgumentException("RimSpawners: spawn time setting enum must have value");
            }
        }

        public void CalculateNextPawnSpawnTick(float delayTicks)
        {
            //float existingPawnSlowdown = GenMath.LerpDouble(0f, 5f, 1f, 0.5f, (float)spawnedPawns.Count);
            //if (Find.Storyteller.difficultyValues.enemyReproductionRateFactor > 0f)
            //{
            //    nextPawnSpawnTick = Find.TickManager.TicksGame + (int)(delayTicks / (existingPawnSlowdown * Find.Storyteller.difficultyValues.enemyReproductionRateFactor));
            //    return;
            //}

            delayTicks /= spawnUntilFullSpeedMultiplier;

            nextPawnSpawnTick = Find.TickManager.TicksGame + (int)delayTicks;
        }

        private void FilterOutUnspawnedPawns()
        {
            for (int i = spawnedPawns.Count - 1; i >= 0; i--)
            {
                if (!spawnedPawns[i].Spawned)
                {
                    spawnedPawns.RemoveAt(i);
                }
            }
        }

        private PawnKindDef RandomPawnKindDef()
        {
            float curPoints = SpawnedPawnsPoints;
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

            bool spawningHumanlike = chosenKind.RaceProps.Humanlike;
            if (spawningHumanlike && pawn.Faction.IsPlayer)
            {
                pawn.playerSettings.hostilityResponse = HostilityResponseMode.Attack;
            }

            AddCustomCompToPawn(pawn);

            // spawn pawn on map
            spawnedPawns.Add(pawn);
            GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(parent.Position, parent.Map, Props.pawnSpawnRadius, null), parent.Map, WipeMode.Vanish);

            // setup pawn lord and AI
            Lord lord = Lord;
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
            RimSpawnersPawnComp existingSpawnedPawnComp = pawn.GetComp<RimSpawnersPawnComp>();
            if (existingSpawnedPawnComp == null)
            {
                RimSpawnersPawnComp spawnedPawnComp = new RimSpawnersPawnComp();
                CompProperties_RimSpawnersPawn spawnedPawnCompProps = new CompProperties_RimSpawnersPawn(this);
                spawnedPawnComp.parent = pawn;
                pawn.AllComps.Add(spawnedPawnComp);
                spawnedPawnComp.Initialize(spawnedPawnCompProps);
            }
        }

        private Pawn GenerateNewPawn()
        {
            bool spawningHumanlike = chosenKind.RaceProps.Humanlike;
            int maxLifeStageIndex = chosenKind.lifeStages.Count - 1;
            // account for humanlikes, which do not have lifeStages
            if ((maxLifeStageIndex < 0) && spawningHumanlike)
            {
                maxLifeStageIndex = chosenKind.RaceProps.lifeStageAges.Count - 1;
            }

            float pawnMinAge = chosenKind.RaceProps.lifeStageAges[maxLifeStageIndex].minAge;

            Faction pawnFaction = parent.Faction;
            if (pawnFaction.IsPlayer && Settings.useAllyFaction)
            {
                pawnFaction = Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamed("RimSpawnersFriendlyFaction", true));
            }

            PawnGenerationRequest request = new PawnGenerationRequest(
                chosenKind,
                pawnFaction,
                PawnGenerationContext.NonPlayer,
                -1,
                true,
                false,
                false,
                false,
                false,
                true,
                1f,
                false,
                true,
                false,
                false,
                false,
                false,
                false,
                false,
                0f,
                null,
                1f,
                null,
                null,
                null,
                null,
                null,
                pawnMinAge,
                pawnMinAge);

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
            Pawn cachedPawn = cachedPawns[0];
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
            Log.Message("Universal spawner comp is destroying all spawned pawns");

            foreach (Pawn pawn in spawnedPawns)
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
            Log.Message("Universal spawner comp is destroying all cached pawns");
            foreach (Pawn cachedPawn in cachedPawns)
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
            foreach (Pawn pawn in spawnedPawns)
            {
                AddCustomCompToPawn(pawn);

                //// following works to remove needs with ShouldHaveNeeds patch
                //if (Settings.disableNeeds)
                //{
                //    pawn.needs.AddOrRemoveNeedsAsAppropriate();
                //}
            }
        }

        public override void CompTick()
        {
            if (Active && parent.Spawned && nextPawnSpawnTick == -1)
            {
                SpawnInitialPawns();
            }
            if (parent.Spawned)
            {
                FilterOutUnspawnedPawns();
                if (Active && Find.TickManager.TicksGame >= nextPawnSpawnTick)
                {
                    if (SpawnedPawnsPoints >= Props.maxSpawnedPawnsPoints)
                    {
                        spawnUntilFullSpeedMultiplier = 1f;
                    }

                    Pawn pawn;

                    if ((Props.maxSpawnedPawnsPoints < 0f || SpawnedPawnsPoints < Props.maxSpawnedPawnsPoints) && Find.Storyteller.difficultyValues.enemyReproductionRateFactor > 0f && TrySpawnPawn(out pawn) && pawn.caller != null)
                    {
                        pawn.caller.DoCall();
                    }

                    CalculateNextPawnSpawnTick();
                }
            }
        }

        public void SendMessage()
        {
            if (!Props.spawnMessageKey.NullOrEmpty() && MessagesRepeatAvoider.MessageShowAllowed(Props.spawnMessageKey, 0.1f))
            {
                Messages.Message(Props.spawnMessageKey.Translate(chosenKind.LabelCap), parent, MessageTypeDefOf.PositiveEvent, true);
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
                    action = delegate ()
                    {
                        Pawn pawn;
                        TrySpawnPawn(out pawn);
                    }
                };
            }
            yield break;
        }

        public override string CompInspectStringExtra()
        {
            if (chosenKind == null)
            {
                return "RimSpawners_UniversalAssemblerInspectNoneChosen".Translate();
            }

            string text;

            string chosenKindName = chosenKind.LabelCap;

            if (SpawnedPawnsPoints < Props.maxSpawnedPawnsPoints)
            {
                text = "RimSpawners_UniversalAssemblerInspectChosen".Translate(chosenKindName, SpawnedPawnsPoints, Props.maxSpawnedPawnsPoints);

                if (!Paused && !Dormant)
                {
                    int ticksToNextSpawn = nextPawnSpawnTick - Find.TickManager.TicksGame;
                    text += "RimSpawners_UniversalAssemblerInspectNextSpawn".Translate(ticksToNextSpawn.ToStringSecondsFromTicks());
                }
            }
            else
            {
                text = "RimSpawners_UniversalAssemblerInspectLimit".Translate(chosenKindName, Props.maxSpawnedPawnsPoints);
            }

            if (Paused)
            {
                text += "RimSpawners_UniversalAssemblerInspectPaused".Translate();
            }
            else if (Dormant)
            {
                text += "RimSpawners_UniversalAssemblerInspectDormant".Translate();
            }
            return text;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextPawnSpawnTick, "nextPawnSpawnTick", 0, false);
            Scribe_Values.Look(ref pawnsLeftToSpawn, "pawnsLeftToSpawn", -1, false);
            Scribe_Collections.Look(ref spawnedPawns, "spawnedPawns", LookMode.Reference, Array.Empty<object>());
            Scribe_Values.Look(ref aggressive, "aggressive", false, false);
            Scribe_Values.Look(ref canSpawnPawns, "canSpawnPawns", true, false);
            Scribe_Values.Look(ref dormant, "dormant", false, false);
            Scribe_Defs.Look(ref chosenKind, "chosenKind");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                spawnedPawns.RemoveAll((Pawn x) => x == null);
                if (pawnsLeftToSpawn == -1 && Props.maxPawnsToSpawn != IntRange.zero)
                {
                    pawnsLeftToSpawn = Props.maxPawnsToSpawn.RandomInRange;
                }
            }
        }

        public int nextPawnSpawnTick = -1;

        public int pawnsLeftToSpawn = -1;

        public List<Pawn> spawnedPawns = new List<Pawn>();
        public List<Pawn> cachedPawns;

        public bool aggressive = true;

        public bool canSpawnPawns = true;

        private bool dormant;
        private bool paused;

        private PawnKindDef chosenKind;

        private float spawnUntilFullSpeedMultiplier = 1f;
    }
}
