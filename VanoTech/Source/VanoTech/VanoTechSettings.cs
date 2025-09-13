using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace VanoTech
{
    public class VanoTechSettings : ModSettings
    {
        public List<CondenserItem> condenserItems = new List<CondenserItem>();

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref condenserItems, "condenserItems", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && condenserItems == null)
                condenserItems = new List<CondenserItem>();
        }

        public List<CondenserItem> GetRealCondenserItems()
        {
            var realCondenserItems = new List<CondenserItem>();
            foreach (var item in condenserItems.ToList())
            {
                if (item == null || item.ThingDef == null)
                {
                    Log.Warning("VanoTech_ItemDoesNotExist".Translate(item.thingDefName));
                    continue;
                }
                realCondenserItems.Add(item);
            }
            return realCondenserItems;
        }

        public void ApplySettings()
        {
            var realCondenserItems = GetRealCondenserItems();
            ThingDef condenserDef = DefDatabase<ThingDef>.GetNamed(VanoTech.CondenserDefName);
            if (condenserDef != null)
            {
                condenserDef.recipes = realCondenserItems.Select(item => item.CreateRecipe()).ToList();
                condenserDef.recipes.ForEach(recipe =>
                {
                    if (DefDatabase<RecipeDef>.GetNamed(recipe.defName, false) == null)
                    {
                        recipe.PostLoad();
                        DefDatabase<RecipeDef>.Add(recipe);
                    }
                });
                // Clear recipe cache to ensure new recipes are recognized
                typeof(ThingDef).GetField("allRecipesCached", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(condenserDef, null);
            }
        }
    }
}
