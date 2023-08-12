using RimWorld;
using System.Linq;
using Verse;

namespace RimMisc
{
    internal class Utils
    {
        private static float GetHediffCapacityOffset(HediffDef hediff, PawnCapacityDef capacity)
        {
            float modifier = 0;
            // only apply first stage / min severity
            if (hediff.stages == null || hediff.stages.Count == 0)
            {
                return 0;
            }
            var stage = hediff.stages[0];
            if (stage.capMods == null)
            {
                return 0;
            }
            foreach (var capMod in stage.capMods)
            {
                if (capMod.capacity == capacity)
                {
                    modifier += capMod.offset;
                }
            }
            return modifier;
        }

        private static float GetHediffStatOffset(HediffDef hediff, StatDef stat)
        {
            float modifier = 0;
            // only apply first stage / min severity
            if (hediff.stages == null || hediff.stages.Count == 0)
            {
                return 0;
            }
            var stage = hediff.stages[0];
            if (stage.statOffsets == null)
            {
                return 0;
            }
            foreach (var statMod in stage.statOffsets)
            {
                if (statMod.stat == stat)
                {
                    modifier += statMod.value;
                }
            }
            return modifier;
        }

        public static float GetIngestibleHediffCapacityOffset(IngestibleProperties ingestible, PawnCapacityDef capacity)
        {
            if (ingestible.outcomeDoers == null)
            {
                return 0;
            }
            float modifier = 0;
            foreach (var outcome in ingestible.outcomeDoers)
            {
                if (outcome is IngestionOutcomeDoer_GiveHediff outcomeHediff)
                {
                    modifier += GetHediffCapacityOffset(outcomeHediff.hediffDef, capacity);
                }
            }
            return modifier;
        }

        public static float GetIngestibleHediffStatOffset(IngestibleProperties ingestible, StatDef stat)
        {
            if (ingestible.outcomeDoers == null)
            {
                return 0;
            }
            float modifier = 0;
            foreach (var outcome in ingestible.outcomeDoers)
            {
                if (outcome is IngestionOutcomeDoer_GiveHediff outcomeHediff)
                {
                    modifier += GetHediffStatOffset(outcomeHediff.hediffDef, stat);
                }
            }
            return modifier;
        }

        public static float GetApparelHediffCapacityOffset(ThingDef apparel, PawnCapacityDef capacity)
        {
            var apparelCauseHediff = (CompProperties_CauseHediff_Apparel)apparel.CompDefFor<CompCauseHediff_Apparel>();
            if (apparelCauseHediff == null)
            {
                return 0;
            }
            float modifier = GetHediffCapacityOffset(apparelCauseHediff.hediff, capacity);
            return modifier;
        }

        public static float GetApparelHediffStatOffset(ThingDef apparel, StatDef stat)
        {
            var apparelCauseHediff = (CompProperties_CauseHediff_Apparel)apparel.CompDefFor<CompCauseHediff_Apparel>();
            if (apparelCauseHediff == null)
            {
                return 0;
            }
            float modifier = GetHediffStatOffset(apparelCauseHediff.hediff, stat);
            return modifier;
        }

        public static float GetIngestibleMoodOffset(IngestibleProperties ingestible)
        {
            float mood = 0;
            if (ingestible.tasteThought != null)
            {
                var stages = ingestible.tasteThought.stages;
                if (stages.Count > 0)
                {
                    mood += stages[0].baseMoodEffect;
                }
            }
            if (ingestible.outcomeDoers != null)
            {
                foreach (var outcome in ingestible.outcomeDoers)
                {
                    if (outcome is IngestionOutcomeDoer_GiveHediff outcomeHediff)
                    {
                        var hediffThoughts = DefDatabase<ThoughtDef>.AllDefsListForReading.Where(x => x.hediff == outcomeHediff.hediffDef).ToList();
                        foreach (var thought in hediffThoughts)
                        {
                            if (thought.stages == null || thought.stages.Count == 0)
                            {
                                continue;
                            }
                            var stage = thought.stages[0];
                            mood += stage.baseMoodEffect;
                        }
                    }
                }
            }
            return mood;
        }
    }
}
