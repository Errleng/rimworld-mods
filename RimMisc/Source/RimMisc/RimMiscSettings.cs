using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Verse;

namespace RimMisc
{
    public class RimMiscSettings : ModSettings
    {
        public bool defaultDoUntil;
        public bool autoCloseLetters;
        public float autoCloseLettersSeconds;
        public List<CondenserItem> condenserItems = new List<CondenserItem>();

        public override void ExposeData()
        {
            Scribe_Values.Look(ref defaultDoUntil, "defaultDoUntil");
            Scribe_Values.Look(ref autoCloseLetters, "autoCloseLetters");
            Scribe_Values.Look(ref autoCloseLettersSeconds, "autoCloseLettersSeconds", 10f);
            Scribe_Collections.Look(ref condenserItems, "condenserItems", LookMode.Deep);
            base.ExposeData();
        }

        public void ApplySettings()
        {
            ThingDef condenserDef = DefDatabase<ThingDef>.GetNamed("VanometricCondenser");
            if (condenserDef != null)
            {
                condenserDef.recipes = RimMisc.Settings.condenserItems.Select(item => item.CreateRecipe()).ToList();
                AccessTools.FieldRefAccess<ThingDef, List<RecipeDef>>(condenserDef, "allRecipesCached") = null;
            }
        }
    }
}
