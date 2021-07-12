using HarmonyLib;
using RimWorld;

namespace RimMisc
{
    [HarmonyPatch(typeof(BillUtility), "MakeNewBill")]
    class Patch_MakeNewBill
    {
        private static void Postfix(ref Bill __result)
        {
            if (__result is Bill_Production billProduction)
            {
                if (RimMisc.Settings.defaultDoUntil && billProduction.recipe.WorkerCounter.CanCountProducts(billProduction))
                {
                    billProduction.repeatMode = BillRepeatModeDefOf.TargetCount;
                    billProduction.targetCount = 1;
                }

                if (RimMisc.Settings.defaultIngredientRadius > 0)
                {
                    billProduction.ingredientSearchRadius = RimMisc.Settings.defaultIngredientRadius;
                }
            }
        }
    }
}