using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System.Reflection;

namespace Pathing
{
    [StaticConstructorOnStartup]
    public class Patcher
    {
        static Patcher()
        {
            var harmony = new Harmony("com.pathing.rimworld.mod");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
            Log.Message("Pathing mod loaded");
        }
    }

    [HarmonyPatch(typeof(Pawn_PathFollower), "PatherTick")]
    class PatchPawn_PathFollower
    {
        private static IntVec3 lastPos;

        static void Postfix(Pawn_PathFollower __instance, Pawn ___pawn)
        {
            if (___pawn.IsColonistPlayerControlled && __instance.Moving)
            {
                if (__instance.nextCellCostLeft > 0f)
                {
                    __instance.nextCellCostLeft = 0;
                }

                if (!___pawn.Position.Equals(lastPos))
                {
                    lastPos = ___pawn.Position;
                    __instance.PatherTick();
                } else
                {
                    lastPos = ___pawn.Position;
                }
            }
        }
    }

    [HarmonyPatch(typeof(StatExtension), "GetStatValue")]
    class PatchGetStatValue
    {
        static void Postfix(Thing thing, StatDef stat, bool applyPostProcess, ref float __result)
        {
            Pawn pawn = thing as Pawn;
            if ((pawn != null) && (pawn.IsColonistPlayerControlled))
            {
                if (stat.Equals(StatDefOf.WorkSpeedGlobal))
                {
                    __result *= 10;
                } else if (stat.Equals(StatDefOf.GlobalLearningFactor))
                {
                    __result *= 100;
                } else if (stat.Equals(StatDefOf.CarryingCapacity))
                {
                    __result *= 10;
                } else if (stat.Equals(StatDefOf.MoveSpeed))
                {
                    __result *= 100;
                }
            }
        }
    }
}