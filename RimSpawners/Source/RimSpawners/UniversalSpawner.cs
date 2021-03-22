using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimSpawners
{
    class UniversalSpawner : Building
    {
        static readonly RimSpawnersSettings Settings = LoadedModManager.GetMod<RimSpawners>().GetSettings<RimSpawnersSettings>();
        static readonly int THREAT_CHECK_TICKS = GenTicks.SecondsToTicks(10);
        static readonly int THREAT_OVER_DESTROY_PAWNS_TICKS = GenTicks.SecondsToTicks(300);

        private CompUniversalSpawnerPawn cusp;

        public bool ThreatActive { get; set; }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            cusp = GetComp<CompUniversalSpawnerPawn>();
            Log.Message($"CompUniversalSpawnerPawn is {cusp.ToStringNullable()}");
        }

        public override void Tick()
        {
            base.Tick();

            if (this.IsHashIntervalTick(THREAT_CHECK_TICKS))
            {
                if (Settings.spawnOnlyOnThreat)
                {
                    bool isThreatOnMap = ParentHolder is Map &&
                        GenHostility.AnyHostileActiveThreatTo(MapHeld, Faction)
                        //|| Map.listerThings.ThingsOfDef(ThingDefOf.Tornado).Any()
                        //|| Map.listerThings.ThingsOfDef(ThingDefOf.DropPodIncoming).Any()
                        ;

                    if (isThreatOnMap)
                    {
                        // only spawn all pawns when the threat is first detected
                        if (!ThreatActive)
                        {
                            //cusp.SpawnPawnsUntilPoints(Settings.maxSpawnerPoints);
                            cusp.SpawnUntilFullSpeedMultiplier = Settings.spawnOnThreatSpeedMultiplier;
                        }
                        ThreatActive = true;
                        cusp.Dormant = false;
                    }
                    else
                    {
                        ThreatActive = false;
                        cusp.Dormant = true;
                    }
                }
                else
                {
                    ThreatActive = false;
                    cusp.Dormant = false;
                }

            }

            if (this.IsHashIntervalTick(THREAT_OVER_DESTROY_PAWNS_TICKS))
            {
                if (Settings.spawnOnlyOnThreat && !ThreatActive)
                {
                    cusp.RemoveAllSpawnedPawns();
                }
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);
            cusp.RemoveAllSpawnedPawns();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo baseGizmo in base.GetGizmos())
            {
                yield return baseGizmo;
            }
            yield return new Command_Action()
            {
                defaultLabel = "RimSpawners_KillSwitch".Translate(),
                defaultDesc = "RimSpawners_KillSwitchDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Detonate"),
                action = RemoveAllSpawnedPawns
            };
            yield return new Command_Action()
            {
                defaultLabel = "RimSpawners_Reset".Translate(),
                defaultDesc = "RimSpawners_ResetDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TryReconnect"),
                action = () =>
                {
                    SetChosenKind(null);
                }
            };
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
            return cusp.ChosenKind;
        }

        public void SetChosenKind(PawnKindDef newChosenKind)
        {
            cusp.ChosenKind = newChosenKind;
        }

        public void RemoveAllSpawnedPawns()
        {
            cusp.RemoveAllSpawnedPawns();
        }
    }
}
