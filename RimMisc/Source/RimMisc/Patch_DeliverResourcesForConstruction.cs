using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;

namespace RimMisc
{
    class Patch_DeliverResourcesForConstruction
    {
        [HarmonyPatch(typeof(ItemAvailability), "ThingsAvailableAnywhere")]
        public static class Patch_ItemAvailability
        {
            public static bool Prefix(ThingDef need, int amount, Pawn pawn, ref bool __result)
            {
                if (RimMisc.Settings.constructEvenIfNotEnough)
                {
                    // If there is anything on the map that matches the resource, then deliver it
                    List<Thing> list = pawn.Map.listerThings.ThingsOfDef(need);
                    __result = list.Any(t => !t.IsForbidden(pawn));
                    return false;
                }
                return true;
            }
        }

        //This is just to change a break into a continue
        //Honestly is a vanilla bug
        //Would only deliver resource #2 once there's enough resource #1 
        //Though resource #1 doesn't care if there's enough #2
        [HarmonyPatch(typeof(WorkGiver_ConstructDeliverResources), "ResourceDeliverJobFor")]
        public static class BreakToContinue_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                bool found = false;
                List<CodeInstruction> instList = instructions.ToList();
                for (int i = 0; i < instList.Count(); i++)
                {
                    CodeInstruction inst = instList[i];
                    if (inst.opcode == OpCodes.Beq && instList[i - 4].opcode == OpCodes.Ldsfld)
                    {
                        inst.opcode = OpCodes.Br;
                        found = true;
                    }
                    yield return inst;
                }
                if (found == false)
                {
                    Log.Error($"RimMisc deliver resources transpiler failed");
                }
            }
        }
    }
}
