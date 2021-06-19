using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace RimMisc
{
    public class CondenserItem : IExposable
    {
        public string thingDefName;
        public int work;
        public int yield;

        public ThingDef ThingDef
        {
            get => DefDatabase<ThingDef>.GetNamed(thingDefName);
        }

        public CondenserItem()
        {

        }

        public CondenserItem(string thingDefName, int work, int @yield)
        {
            this.thingDefName = thingDefName;
            this.work = work;
            this.yield = yield;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref thingDefName, "thingDefName");
            Scribe_Values.Look(ref work, "work");
            Scribe_Values.Look(ref yield, "yield");
        }

        public RecipeDef CreateRecipe()
        {
            ThingDef thing = ThingDef;
            string recipeDefName = $"Condense_{thing.defName}";
            RecipeDef recipe = DefDatabase<RecipeDef>.GetNamed(recipeDefName, false);
            if (recipe == null)
            {
                recipe = new RecipeDef
                {
                    defName = recipeDefName,
                    label = $"RimMisc_RecipeCondense".Translate(thingDefName),
                    jobString = "RimMisc_CondenseJobString".Translate(thingDefName),
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
    }
}
