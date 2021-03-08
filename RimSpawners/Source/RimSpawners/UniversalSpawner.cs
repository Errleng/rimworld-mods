﻿using RimWorld;
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

        public override void ExposeData()
        {
            base.ExposeData();

            // add custom ThingComp to all pawns when loading a save
            CompSpawnerPawn cps = GetComp<CompSpawnerPawn>();
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

            CompSpawnerPawn cps = GetComp<CompSpawnerPawn>();
            Type cpsType = typeof(CompSpawnerPawn);
            PropertyInfo spawnedPawnsPointsProperty = cpsType.GetProperty("SpawnedPawnsPoints", BindingFlags.Instance | BindingFlags.NonPublic);
            float currentPoints = (float)spawnedPawnsPointsProperty.GetValue(cps);
            return base.GetInspectString() + $"\n{currentPoints}/{comp.maxSpawnedPawnsPoints} points";
        }

        public void SetPawnKindsToSpawn(List<PawnKindDef> newPawnKindsToSpawn)
        {
            CompProperties_SpawnerPawn comp = def.GetCompProperties<CompProperties_SpawnerPawn>();
            comp.spawnablePawnKinds = newPawnKindsToSpawn;
            Log.Message($"Set spawner pawn kinds to {string.Join(", ", comp.spawnablePawnKinds)}");
        }

        public PawnKindDef GetChosenKind()
        {
            CompSpawnerPawn cps = GetComp<CompSpawnerPawn>();
            Type cpsType = typeof(CompSpawnerPawn);
            FieldInfo chosenKindField = cpsType.GetField("chosenKind", BindingFlags.Instance | BindingFlags.NonPublic);
            return (PawnKindDef)(chosenKindField.GetValue(cps));
        }

        public void SetChosenKind(PawnKindDef newChosenKind)
        {
            CompSpawnerPawn cps = GetComp<CompSpawnerPawn>();
            Type cpsType = typeof(CompSpawnerPawn);
            FieldInfo chosenKindField = cpsType.GetField("chosenKind", BindingFlags.NonPublic | BindingFlags.Instance);
            chosenKindField.SetValue(cps, newChosenKind);

            Log.Message($"Set spawner chosen pawn kind to {GetChosenKind().defName} with point cost {newChosenKind.combatPower}");
        }

        public void ResetCompSpawnerPawn()
        {
            Log.Message($"Resetting spawner: destroying all its spawned pawns");

            CompSpawnerPawn cps = GetComp<CompSpawnerPawn>();
            foreach (Pawn pawn in cps.spawnedPawns)
            {
                pawn.Destroy();
            }
        }
    }
}
