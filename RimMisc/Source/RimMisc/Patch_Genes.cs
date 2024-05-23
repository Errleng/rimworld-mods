using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimMisc
{
    [HarmonyPatch(typeof(Xenogerm))]
    class Patch_Xenogerm
    {
        static readonly CachedTexture implantAllTexture = new CachedTexture("UI/Gizmos/ImplantGenes");

        [HarmonyPatch("GetGizmos")]
        [HarmonyPostfix]
        static void Postfix(Xenogerm __instance, ref IEnumerable<Gizmo> __result)
        {
            var list = __result.ToList();
            list.Add(new Command_Action
            {
                defaultLabel = "ImplantXenogermAll".Translate(),
                defaultDesc = "ImplantXenogermAllDesc".Translate(),
                icon = implantAllTexture.Texture,
                action = delegate
                {
                    foreach (Pawn pawn in __instance.Map.mapPawns.FreeColonistsSpawned)
                    {
                        if (!pawn.IsQuestLodger() && pawn.genes != null && (pawn.IsColonistPlayerControlled || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony || (pawn.IsColonyMutant && pawn.IsGhoul)))
                        {
                            int metabolism = GeneUtility.MetabolismAfterImplanting(pawn, __instance.GeneSet);
                            if (metabolism < GeneTuning.BiostatRange.TrueMin)
                            {
                                Messages.Message(pawn.LabelShortCap + ": " + "ResultingMetTooLow".Translate() + " (" + metabolism + ")", MessageTypeDefOf.RejectInput);
                            }
                            else if (__instance.PawnIdeoDisallowsImplanting(pawn))
                            {
                                Messages.Message(pawn.LabelShortCap + ": " + "IdeoligionForbids".Translate(), MessageTypeDefOf.RejectInput);
                            }
                            else
                            {
                                GeneUtility.ImplantXenogermItem(pawn, __instance);
                            }
                        }
                    }
                }
            });
            __result = list;
        }
    }
}
