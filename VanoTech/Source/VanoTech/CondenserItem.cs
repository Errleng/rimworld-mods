using RimWorld;
using System.Collections.Generic;
using Verse;

namespace VanoTech
{
    public class CondenserItem : IExposable
    {
        public string thingDefName;
        public float work;
        public int yield;

        public CondenserItem()
        {
        }

        public CondenserItem(string thingDefName, float work, int yield)
        {
            this.thingDefName = thingDefName;
            this.work = work;
            this.yield = yield;
        }

        public ThingDef ThingDef => DefDatabase<ThingDef>.GetNamed(thingDefName);

        public void ExposeData()
        {
            Scribe_Values.Look(ref thingDefName, "thingDefName");
            Scribe_Values.Look(ref work, "work");
            Scribe_Values.Look(ref yield, "yield");
        }

        public RecipeDef CreateRecipe()
        {
            var thing = ThingDef;
            var recipeDefName = $"Condense_{thing.defName}";
            var recipe = DefDatabase<RecipeDef>.GetNamed(recipeDefName, false);
            if (recipe == null)
            {
                recipe = new RecipeDef
                {
                    defName = recipeDefName,
                    label = "VanoTech_RecipeCondense".Translate(ThingDef.label),
                    jobString = "VanoTech_CondenseJobString".Translate(ThingDef.label),
                    ingredients = new List<IngredientCount>(),
                    defaultIngredientFilter = new ThingFilter(),
                    effectWorking = EffecterDefOf.Research,
                    workAmount = work,
                    workSkill = SkillDefOf.Intellectual,
                    workSpeedStat = StatDefOf.DeepDrillingSpeed,
                    workSkillLearnFactor = 1f,
                    soundWorking = DefDatabase<SoundDef>.GetNamed("Interact_Research"),
                    products = new List<ThingDefCountClass> { new ThingDefCountClass(thing, yield) },
                    unfinishedThingDef = DefDatabase<ThingDef>.GetNamed(VanoTech.UnfinishedCondenserThingDefName)
                };
            }
            else
            {
                recipe.workAmount = work;
                recipe.products = new List<ThingDefCountClass> { new ThingDefCountClass(thing, yield) };
            }

            return recipe;
        }

        public void CalculateWorkAmount()
        {
            var additionalWorkFactor = 2;
            work = ThingDef.BaseMarketValue * yield * GenTicks.TicksPerRealSecond * additionalWorkFactor;
        }
    }
}
