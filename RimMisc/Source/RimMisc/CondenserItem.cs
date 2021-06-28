using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimMisc
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
                    label = "RimMisc_RecipeCondense".Translate(ThingDef.label),
                    jobString = "RimMisc_CondenseJobString".Translate(ThingDef.label),
                    ingredients = new List<IngredientCount>(),
                    defaultIngredientFilter = new ThingFilter(),
                    effectWorking = DefDatabase<EffecterDef>.GetNamed("Research"),
                    workAmount = work,
                    workSkill = DefDatabase<SkillDef>.GetNamed("Intellectual"),
                    workSpeedStat = DefDatabase<StatDef>.GetNamed("ResearchSpeed"),
                    workSkillLearnFactor = 0.5f,
                    soundWorking = DefDatabase<SoundDef>.GetNamed("Interact_Research"),
                    products = new List<ThingDefCountClass> { new ThingDefCountClass(thing, yield) }
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
            work = ThingDef.BaseMarketValue * yield * GenTicks.TicksPerRealSecond;
        }
    }
}