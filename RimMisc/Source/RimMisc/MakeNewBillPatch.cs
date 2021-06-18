using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimMisc
{
    [HarmonyPatch(typeof(BillUtility), "MakeNewBill")]
    class MakeNewBillPatch
    {
        static void Postfix(ref Bill __result)
        {
            if (RimMisc.Settings.defaultDoUntil)
            {
                if (__result is Bill_Production billProduction && billProduction.recipe.WorkerCounter.CanCountProducts(billProduction))
                {
                    billProduction.repeatMode = BillRepeatModeDefOf.TargetCount;
                }
            }
        }
    }
}
