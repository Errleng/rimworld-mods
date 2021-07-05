using HarmonyLib;
using RimWorld;

namespace RimMisc
{
    [HarmonyPatch(typeof(BillUtility), "MakeNewBill")]
    class Patch_MakeNewBill
    {
        private static void Postfix(ref Bill __result)
        {
            if (RimMisc.Settings.defaultDoUntil)
                if (__result is Bill_Production billProduction && billProduction.recipe.WorkerCounter.CanCountProducts(billProduction))
                {
                    billProduction.repeatMode = BillRepeatModeDefOf.TargetCount;
                    billProduction.targetCount = 1;
                }
        }
    }
}