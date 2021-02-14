using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Verse;

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
            } else
            {
                return comp.spawnablePawnKinds;
            }
        }

        public void SetPawnKindsToSpawn(List<PawnKindDef> newPawnKindsToSpawn)
        {
            CompProperties_SpawnerPawn comp = def.GetCompProperties<CompProperties_SpawnerPawn>();
            comp.spawnablePawnKinds = newPawnKindsToSpawn;
            Log.Message($"Set universal spawner pawn kinds to {string.Join(", ", comp.spawnablePawnKinds)}");
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
            Log.Message($"Set universal spawner chosen pawn kind to {GetChosenKind().defName}");
        }
    }
}
