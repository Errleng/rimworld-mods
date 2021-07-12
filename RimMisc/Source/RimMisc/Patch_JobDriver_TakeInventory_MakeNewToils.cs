using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimMisc
{
    [HarmonyPatch(typeof(JobDriver_TakeInventory), "MakeNewToils")]
    internal class Patch_JobDriver_TakeInventory_MakeNewToils
    {
        private static bool Prefix(JobDriver_TakeInventory __instance)
        {
            if (RimMisc.Settings.disableEnemyUninstall)
            {
                var pawn = __instance.pawn;
                var thing = pawn.CurJob.GetTarget(TargetIndex.A).Thing;
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