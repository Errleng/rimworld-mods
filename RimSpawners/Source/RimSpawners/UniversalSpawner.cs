using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace RimSpawners
{
    class UniversalSpawner : Building
    {
        static readonly RimSpawnersSettings settings = LoadedModManager.GetMod<RimSpawners>().GetSettings<RimSpawnersSettings>();
        static readonly int threatCheckTicks = GenTicks.SecondsToTicks(10);
        static readonly int threatOverDestroyPawnTicks = GenTicks.SecondsToTicks(300);

        private CompUniversalSpawnerPawn cups;

        public bool ThreatActive { get; set; }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            cups = GetComp<CompUniversalSpawnerPawn>();
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }

        public override void Tick()
        {
            base.Tick();

            if (this.IsHashIntervalTick(threatCheckTicks))
            {
                if (settings.spawnOnlyOnThreat)
                {
                    bool isThreatOnMap = ParentHolder is Map &&
                        GenHostility.AnyHostileActiveThreatTo(MapHeld, Faction, false)
                        //|| Map.listerThings.ThingsOfDef(ThingDefOf.Tornado).Any()
                        //|| Map.listerThings.ThingsOfDef(ThingDefOf.DropPodIncoming).Any()
                        ;

                    if (isThreatOnMap)
                    {
                        // only spawn all pawns when the threat is first detected
                        if (!ThreatActive)
                        {
                            cups.SpawnPawnsUntilPoints(settings.maxSpawnerPoints);
                        }
                        ThreatActive = true;
                        cups.Dormant = false;
                    }
                    else
                    {
                        ThreatActive = false;
                        cups.Dormant = true;
                    }
                }
            }

            if (this.IsHashIntervalTick(threatOverDestroyPawnTicks))
            {
                if (settings.spawnOnlyOnThreat && !ThreatActive)
                {
                    RemoveAllSpawnedPawns();
                }
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);
            RemoveAllSpawnedPawns();
        }

        public void RemoveAllSpawnedPawns()
        {
            Log.Message("Spawner is destroying all spawned pawns");

            for (int i = cups.spawnedPawns.Count - 1; i >= 0; i--)
            {
                cups.spawnedPawns[i].Destroy();
                cups.spawnedPawns.RemoveAt(i);
            }
        }

        //public List<PawnKindDef> GetPawnKindsToSpawn()
        //{
        //    CompProperties_UniversalSpawnerPawn comp = def.GetCompProperties<CompProperties_UniversalSpawnerPawn>();
        //    if (comp.spawnablePawnKinds == null)
        //    {
        //        return new List<PawnKindDef>();
        //    }
        //    else
        //    {
        //        return comp.spawnablePawnKinds;
        //    }
        //}

        //public void SetPawnKindsToSpawn(List<PawnKindDef> newPawnKindsToSpawn)
        //{
        //    CompProperties_UniversalSpawnerPawn comp = def.GetCompProperties<CompProperties_UniversalSpawnerPawn>();
        //    comp.spawnablePawnKinds = newPawnKindsToSpawn;
        //    Log.Message($"Set spawner pawn kinds to {string.Join(", ", comp.spawnablePawnKinds)}");
        //}

        public PawnKindDef GetChosenKind()
        {
            return cups.ChosenKind;
        }

        public void SetChosenKind(PawnKindDef newChosenKind)
        {
            cups.ChosenKind = newChosenKind;
            // recalculate spawn time
            cups.CalculateNextPawnSpawnTick();
        }
    }
}
