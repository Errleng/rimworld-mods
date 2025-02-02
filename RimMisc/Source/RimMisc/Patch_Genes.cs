using HarmonyLib;
using RimWorld;
using System;
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
                                try
                                {
                                    // Need to make sure to remove all genes. Sometimes they are not removed, just marked as overridden?
                                    var endogeneDefs = pawn.genes.Endogenes.Select(x => x.def).ToList();
                                    Log.Message($"{pawn.LabelShortCap} has {endogeneDefs.Count} endogenes: {string.Join(", ", endogeneDefs.Select(x => x.LabelCap))}");
                                    foreach (var gene in pawn.genes.GenesListForReading)
                                    {
                                        pawn.genes.RemoveGene(gene);
                                    }
                                    Log.Message($"{pawn.LabelShortCap} has {pawn.genes.GenesListForReading.Count} remaining genes: {string.Join(", ", pawn.genes.GenesListForReading.Select(x => x.Label))}");
                                    foreach (var geneDef in endogeneDefs)
                                    {
                                        pawn.genes.AddGene(geneDef, false);
                                    }
                                    Log.Message($"{pawn.LabelShortCap} has {pawn.genes.GenesListForReading.Count} added endogenes: {string.Join(", ", pawn.genes.GenesListForReading.Select(x => x.Label))}");

                                    GeneUtility.ImplantXenogermItem(pawn, __instance);
                                    Log.Message($"Implanted xenogerm {__instance.xenotypeName} for {pawn.LabelShortCap}. Their xenotype is {pawn.genes.xenotypeName}.");
                                }
                                catch (NullReferenceException ex)
                                {
                                    Log.Error($"Could not implant xenogerm {__instance.xenotypeName} for {pawn.LabelShortCap}:\n{ex.ToString()}");
                                }
                            }
                        }
                        else
                        {
                            Log.Message($"Skipping xenogerm implantation for {pawn.LabelShortCap} because condition is false: {pawn.IsQuestLodger()} && {pawn.genes != null} && ({pawn.IsColonistPlayerControlled} || {pawn.IsPrisonerOfColony} || {pawn.IsSlaveOfColony} || ({pawn.IsColonyMutant} && {pawn.IsGhoul})");
                        }
                    }
                }
            });
            __result = list;
        }
    }
}
