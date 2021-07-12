using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace RimMisc
{
    public class RimMiscSettings : ModSettings
    {
        public bool autoCloseLetters;
        public float autoCloseLettersSeconds;
        public bool defaultDoUntil;
        public bool disableEnemyUninstall;
        public float defaultIngredientRadius;
        public List<CondenserItem> condenserItems = new List<CondenserItem>();

        public override void ExposeData()
        {
            Scribe_Values.Look(ref defaultDoUntil, "defaultDoUntil");
            Scribe_Values.Look(ref defaultIngredientRadius, "defaultIngredientRadius");
            Scribe_Values.Look(ref autoCloseLetters, "autoCloseLetters");
            Scribe_Values.Look(ref autoCloseLettersSeconds, "autoCloseLettersSeconds", 10f);
            Scribe_Values.Look(ref disableEnemyUninstall, "disableEnemyUninstall", false);
            Scribe_Collections.Look(ref condenserItems, "condenserItems", LookMode.Deep);
            base.ExposeData();
        }

        public void ApplySettings()
        {
            foreach (var item in condenserItems.ToList())
                if (item.ThingDef == null)
                {
                    condenserItems.Remove(item);
                    Log.Warning("RimMisc_ItemDoesNotExist".Translate(item.thingDefName));
                }

            var condenserDef = DefDatabase<ThingDef>.GetNamed("VanometricCondenser");
            if (condenserDef != null)
            {
                condenserDef.recipes = RimMisc.Settings.condenserItems.Select(item => item.CreateRecipe()).ToList();
                condenserDef.recipes.ForEach(recipe =>
                {
                    if (DefDatabase<RecipeDef>.GetNamed(recipe.defName, false) == null) DefDatabase<RecipeDef>.Add(recipe);
                });
                AccessTools.FieldRefAccess<ThingDef, List<RecipeDef>>(condenserDef, "allRecipesCached") = null;
            }
        }
    }
}