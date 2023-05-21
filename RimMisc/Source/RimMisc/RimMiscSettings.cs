using HarmonyLib;
using RimWorld;
using System;
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
            Scribe_Collections.Look(ref condenserItems, "condenserItems", LookMode.Deep);
            base.ExposeData();
        }

        public void ApplySettings()
        {
            foreach (var item in condenserItems.ToList())
            {
                if (item.ThingDef == null)
                {
                    condenserItems.Remove(item);
                    Log.Warning("RimMisc_ItemDoesNotExist".Translate(item.thingDefName));
                }
            }

            var condenserDef = DefDatabase<ThingDef>.GetNamed(RimMisc.CondenserDefName);
            if (condenserDef != null)
            {
                condenserDef.recipes = RimMisc.Settings.condenserItems.Select(item => item.CreateRecipe()).ToList();
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

            if (patchBuildingHp)
            {
                var count = 0;

                Predicate<ThingDef> isValidBuilding = delegate (ThingDef def)
                {
                    return def.IsBuildingArtificial &&
                    (def.building.buildingTags.Contains("Production") || def.IsWorkTable);
                };

                foreach (var def in DefDatabase<ThingDef>.AllDefs)
                {
                    if (isValidBuilding(def))
                    {
                        def.SetStatBaseValue(StatDefOf.MaxHitPoints, 100000);
                    }
                }

                Log.Message($"Patched HP for {count} building defs");

                if (Find.CurrentMap != null)
                {
                    foreach (var building in Find.CurrentMap.listerBuildings.allBuildingsColonist)
                    {
                        if (isValidBuilding(building.def))
                        {
                            building.HitPoints = building.MaxHitPoints;
                        }
                    }
                }
            }
        }
    }
}