﻿using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimSpawners
{
    class VanometricFabricator : Building
    {
        static readonly RimSpawnersSettings Settings = LoadedModManager.GetMod<RimSpawners>().GetSettings<RimSpawnersSettings>();
        static readonly int THREAT_CHECK_TICKS = GenTicks.SecondsToTicks(10);
        static readonly int THREAT_OVER_DESTROY_PAWNS_TICKS = GenTicks.SecondsToTicks(300);

        private CompVanometricFabricatorPawn cusp;

        public bool ThreatActive { get; set; }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            cusp = GetComp<CompVanometricFabricatorPawn>();
            Log.Message($"CompVanometricFabricatorPawn is {cusp.ToStringNullable()}");
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
            yield return new Command_Toggle()
            {
                defaultLabel = "RimSpawners_Pause".Translate(),
                defaultDesc = "RimSpawners_PauseDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt"),
                isActive = () => cusp.Paused,
                toggleAction = () =>
                {
                    cusp.Paused = !cusp.Paused;
                }
            };
            yield return new Command_Toggle()
            {
                defaultLabel = "RimSpawners_DropPodToggle".Translate(),
                defaultDesc = "RimSpawners_DropPodToggleDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/SelectAllTransporters"),
                isActive = () => cusp.SpawnInDropPods,
                toggleAction = () =>
                {
                    cusp.SpawnInDropPods = !cusp.SpawnInDropPods;
                    if (!cusp.SpawnInDropPods)
                    {
                        cusp.dropSpot = IntVec3.Invalid;
                    }
                }
            };
            yield return new Command_Action()
            {
                defaultLabel = "RimSpawners_DropSpotSelect".Translate(),
                defaultDesc = "RimSpawners_DropSpotSelectDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("Things/Special/DropPod"),
                action = () =>
                {
                    TargetingParameters targetParams = TargetingParameters.ForDropPodsDestination();
                    Find.Targeter.BeginTargeting(targetParams, delegate (LocalTargetInfo target)
                    {
                        List<VanometricFabricator> spawners = Find.Selector.SelectedObjects.OfType<VanometricFabricator>().ToList();
                        foreach (VanometricFabricator spawner in spawners)
                        {
                            spawner.cusp.dropSpot = target.Cell;
                        }
                    });
                }
            };
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

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            if (cusp.dropSpot != IntVec3.Invalid)
            {
                Vector3 vector = cusp.dropSpot.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays);
                Graphics.DrawMesh(MeshPool.plane10, vector, Quaternion.identity, GenDraw.InteractionCellMaterial, 0);
            }
        }

        //public List<PawnKindDef> GetPawnKindsToSpawn()
        //{
        //    CompProperties_VanometricFabricatorPawn comp = def.GetCompProperties<CompProperties_VanometricFabricatorPawn>();
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
        //    CompProperties_VanometricFabricatorPawn comp = def.GetCompProperties<CompProperties_VanometricFabricatorPawn>();
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