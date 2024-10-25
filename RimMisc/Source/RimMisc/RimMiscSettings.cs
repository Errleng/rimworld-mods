using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimMisc
{
    public class RimMiscSettings : ModSettings
    {
        public bool autoCloseLetters;
        public float autoCloseLettersSeconds;
        public bool defaultDoUntil;
        public float defaultIngredientRadius;
        public bool disableEnemyUninstall;
        public bool killDownedPawns;
        public bool patchBuildingHp;
        public bool preventSkyfallDestruction;
        public bool preventRoofCollapse;
        public bool constructEvenIfNotEnough;
        public bool changeAreaOnThreat;
        public bool myMiscStuff;
        public List<CondenserItem> condenserItems = new List<CondenserItem>();

        public override void ExposeData()
        {
            Scribe_Values.Look(ref defaultDoUntil, "defaultDoUntil");
            Scribe_Values.Look(ref defaultIngredientRadius, "defaultIngredientRadius");
            Scribe_Values.Look(ref autoCloseLetters, "autoCloseLetters");
            Scribe_Values.Look(ref autoCloseLettersSeconds, "autoCloseLettersSeconds", 10f);
            Scribe_Values.Look(ref disableEnemyUninstall, "disableEnemyUninstall");
            Scribe_Values.Look(ref killDownedPawns, "killDownedPawns");
            Scribe_Values.Look(ref patchBuildingHp, "patchBuildingHp");
            Scribe_Values.Look(ref preventSkyfallDestruction, "preventSkyfallDestruction");
            Scribe_Values.Look(ref preventRoofCollapse, "preventRoofCollapse");
            Scribe_Values.Look(ref constructEvenIfNotEnough, "constructEvenIfNotEnough");
            Scribe_Values.Look(ref changeAreaOnThreat, "changeAreaOnThreat");
            Scribe_Values.Look(ref myMiscStuff, "myMiscStuff");
            Scribe_Collections.Look(ref condenserItems, "condenserItems", LookMode.Deep);
            base.ExposeData();
        }

        public List<CondenserItem> GetRealCondenserItems()
        {
            var realCondenserItems = new List<CondenserItem>();
            foreach (var item in condenserItems.ToList())
            {
                if (item == null || item.ThingDef == null)
                {
                    Log.Warning("RimMisc_ItemDoesNotExist".Translate(item.thingDefName));
                    continue;
                }
                realCondenserItems.Add(item);
            }
            return realCondenserItems;
        }

        public void ApplySettings()
        {
            var realCondenserItems = GetRealCondenserItems();
            var condenserDef = DefDatabase<ThingDef>.GetNamed(RimMisc.CondenserDefName);
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
                AccessTools.FieldRefAccess<ThingDef, List<RecipeDef>>(condenserDef, "allRecipesCached") = null;
            }

            if (disableEnemyUninstall)
            {
                foreach (var thing in DefDatabase<ThingDef>.AllDefsListForReading)
                {
                    if (thing.stealable && thing.Minifiable)
                    {
                        thing.stealable = false;
                    }
                }
            }
        }
    }
}