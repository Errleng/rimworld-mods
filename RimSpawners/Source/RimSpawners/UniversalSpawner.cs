using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Verse;
using Verse.AI.Group;

namespace RimSpawners
{
    class UniversalSpawner : Building
    {
        static readonly RimSpawnersSettings settings = LoadedModManager.GetMod<RimSpawners>().GetSettings<RimSpawnersSettings>();
        static readonly int threatCheckTicks = GenTicks.SecondsToTicks(10);
        static readonly int threatOverDestroyPawnTicks = GenTicks.SecondsToTicks(300);

        private CompSpawnerPawn cps;

        public bool ThreatActive { get; set; }

        public override void ExposeData()
        {
            base.ExposeData();

            cps = GetComp<CompSpawnerPawn>();

            // add custom ThingComp to all pawns when loading a save
            foreach (Pawn pawn in cps.spawnedPawns)
            {
                RimSpawnersPawnComp customThingComp = new RimSpawnersPawnComp();
                RimSpawnersPawnCompProperties customThingCompProps = new RimSpawnersPawnCompProperties();
                customThingComp.parent = pawn;
                pawn.AllComps.Add(customThingComp);
                customThingComp.Initialize(customThingCompProps);
            }
        }

        public override string GetInspectString()
        {
            CompProperties_SpawnerPawn comp = def.GetCompProperties<CompProperties_SpawnerPawn>();

            Type cpsType = typeof(CompSpawnerPawn);
            PropertyInfo spawnedPawnsPointsProperty = cpsType.GetProperty("SpawnedPawnsPoints", BindingFlags.Instance | BindingFlags.NonPublic);
            float currentPoints = (float)spawnedPawnsPointsProperty.GetValue(cps);

            string inspectStringAppend = "";
            if (GetChosenKind() != null)
            {
                inspectStringAppend += "\n";
                inspectStringAppend += $"{currentPoints}/{comp.maxSpawnedPawnsPoints} points";
                inspectStringAppend += "\n";
                if (settings.spawnOnlyOnThreat && !ThreatActive)
                {
                    inspectStringAppend += $"Dormant until a threat is detected";
                }
                else
                {
                    inspectStringAppend += $"Next spawn in {GenTicks.ToStringSecondsFromTicks(cps.nextPawnSpawnTick - Find.TickManager.TicksGame)}";
                }
            }
            return base.GetInspectString() + inspectStringAppend;
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
                            Log.Message($"Spawning pawns in response to threat");
                            cps.SpawnPawnsUntilPoints(settings.maxSpawnerPoints);
                            ThreatActive = true;
                        }
                    }
                    else if (ThreatActive)
                    {
                        Log.Message($"Threat is over");
                        ThreatActive = false;
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

        private void RemoveAllSpawnedPawns()
        {
            Log.Message("Spawner is destroying all spawned pawns");

            for (int i = cps.spawnedPawns.Count - 1; i >= 0; i--)
            {
                cps.spawnedPawns[i].Destroy();
                cps.spawnedPawns.RemoveAt(i);
            }
        }

        public List<PawnKindDef> GetPawnKindsToSpawn()
        {
            CompProperties_SpawnerPawn comp = def.GetCompProperties<CompProperties_SpawnerPawn>();
            if (comp.spawnablePawnKinds == null)
            {
                return new List<PawnKindDef>();
            }
            else
            {
                return comp.spawnablePawnKinds;
            }
        }

        public void SetPawnKindsToSpawn(List<PawnKindDef> newPawnKindsToSpawn)
        {
            CompProperties_SpawnerPawn comp = def.GetCompProperties<CompProperties_SpawnerPawn>();
            comp.spawnablePawnKinds = newPawnKindsToSpawn;
            Log.Message($"Set spawner pawn kinds to {string.Join(", ", comp.spawnablePawnKinds)}");
        }

        public PawnKindDef GetChosenKind()
        {
            Type cpsType = typeof(CompSpawnerPawn);
            FieldInfo chosenKindField = cpsType.GetField("chosenKind", BindingFlags.Instance | BindingFlags.NonPublic);
            return (PawnKindDef)(chosenKindField.GetValue(cps));
        }

        public void SetChosenKind(PawnKindDef newChosenKind)
        {
            Type cpsType = typeof(CompSpawnerPawn);
            FieldInfo chosenKindField = cpsType.GetField("chosenKind", BindingFlags.NonPublic | BindingFlags.Instance);
            chosenKindField.SetValue(cps, newChosenKind);

            Log.Message($"Set spawner chosen pawn kind to {GetChosenKind().defName} with point cost {newChosenKind.combatPower}");
        }

        public void ResetCompSpawnerPawn()
        {
            Log.Message($"Resetting spawner");

            RemoveAllSpawnedPawns();
        }
    }
}
