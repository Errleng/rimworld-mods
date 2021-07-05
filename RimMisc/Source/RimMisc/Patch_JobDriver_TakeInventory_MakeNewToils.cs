using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimMisc
{
    [HarmonyPatch(typeof(JobDriver_TakeInventory), "MakeNewToils")]
    class Patch_JobDriver_TakeInventory_MakeNewToils
    {
        private static bool Prefix(JobDriver_TakeInventory __instance)
        {
            if (RimMisc.Settings.disableEnemyUninstall)
            {
                var pawn = __instance.pawn;
                var thing = pawn.CurJob.GetTarget(TargetIndex.A).Thing;
                Log.Message($"Pawn {pawn.Name} TakeInventory {TargetIndex.A} thing {thing}");
                if (!__instance.pawn.IsColonistPlayerControlled && thing.def.category == ThingCategory.Building && thing.Faction == Faction.OfPlayer)
                {
                    pawn.jobs.curDriver.ReadyForNextToil();
                    return false;
                }
            }

            return true;
        }
    }
}
