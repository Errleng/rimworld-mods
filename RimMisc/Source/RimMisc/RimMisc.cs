using HarmonyLib;
using RimWorld;
using RocketMan;
using Soyuz;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimMisc
{
    [StaticConstructorOnStartup]
    public class Loader
    {
        static Loader()
        {
            RimMisc.Settings.ApplySettings();
            AddComps();
        }

        public static void AddComps()
        {
            var thingsWithComps = DefDatabase<ThingDef>.AllDefs.Where(def => typeof(ThingWithComps).IsAssignableFrom(def.thingClass) && def.destroyable).ToList();
            foreach (var thingDef in thingsWithComps)
            {
                thingDef.comps.Add(new CompProperties(typeof(CompMeleeAttackable)));
            }
        }
    }

    public class RimMisc : Mod
    {
        private static readonly float SEARCH_RESULT_ROW_HEIGHT = 30f;
        private static readonly float CONDENSER_ITEM_ROW_HEIGHT = 30f;
        private static readonly float CONDENSER_ITEM_ICON_WIDTH = 30f;
        private static readonly float CONDENSER_ITEM_LABEL_WIDTH = 200f;
        private static readonly float CONDENSER_ITEM_FIELD_WIDTH = 100f;
        private static readonly float ITEM_PADDING = 10f;
        private static readonly float BUTTON_WIDTH = 60f;
        private static readonly float SCROLLBAR_WIDTH = 20;
        private static readonly float MIN_AUTOCLOSE_SECONDS = RimMiscWorldComponent.AUTO_CLOSE_LETTERS_CHECK_TICKS.TicksToSeconds();
        private static readonly float MAX_AUTOCLOSE_SECONDS = 600;
        private static readonly int MIN_WORK = 1;
        private static readonly int MAX_WORK = 1000000;
        private static readonly int DEFAULT_WORK = 6000;
        private static readonly int MIN_YIELD = 1;
        private static readonly int MAX_YIELD = 1000;
        public static readonly string UnfinishedCondenserThingDefName = "UnfinishedCondenserThing";
        public static readonly string CondenserDefName = "VanometricCondenser";

        public static RimMiscSettings Settings;
        private float condenserItemScrollHeight;
        private Vector2 condenserItemScrollPos;
        private float condenserItemSelectScrollHeight;
        private Vector2 condenserItemSelectScrollPos;

        private string searchKeyword;

        public RimMisc(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RimMiscSettings>();
            var harmony = new Harmony("com.rimmisc.rimworld.mod");
            harmony.PatchAll();
            Log.Message("RimMisc loaded");
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var settingsRect = inRect.TopPart(0.5f).Rounded();
            var condenserItemsRect = inRect.BottomPart(0.5f).Rounded();
            var condenserItemsSelectRect = inRect.BottomPart(0.3f).Rounded();
            var condenserItemsScrollRect = new Rect(condenserItemsRect)
            {
                height = condenserItemsSelectRect.y - condenserItemsRect.y,
                y = condenserItemsRect.y + 10
            };
            var condenserItemsSelectScrollRect = new Rect(condenserItemsSelectRect)
            {
                y = condenserItemsSelectRect.y + 10
            };

            var listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            var settingsSection = listingStandard.BeginSection(settingsRect.height);

            settingsSection.CheckboxLabeled("RimMisc_DefaultDoUntil".Translate(), ref Settings.defaultDoUntil);
            settingsSection.CheckboxLabeled("RimMisc_AutoCloseLetters".Translate(), ref Settings.autoCloseLetters);
            settingsSection.CheckboxLabeled("RimMisc_DisableEnemyUninstall".Translate(), ref Settings.disableEnemyUninstall);
            settingsSection.CheckboxLabeled("RimMisc_KillDownedPawns".Translate(), ref Settings.killDownedPawns);
            settingsSection.CheckboxLabeled("RimMisc_PatchBuildingHp".Translate(), ref Settings.patchBuildingHp);
            if (settingsSection.ButtonText("RimMisc_EnableRocketmanTimeDilation".Translate()))
            {
                EnableRocketmanRaces();
            }

            settingsSection.Label("RimMisc_AutoCloseLettersSeconds".Translate(Settings.autoCloseLettersSeconds));
            Settings.autoCloseLettersSeconds = settingsSection.Slider(Settings.autoCloseLettersSeconds, MIN_AUTOCLOSE_SECONDS, MAX_AUTOCLOSE_SECONDS);
            settingsSection.Label("RimMisc_DefaultIngredientRadius".Translate(Settings.defaultIngredientRadius));
            Settings.defaultIngredientRadius = settingsSection.Slider(Settings.defaultIngredientRadius, 0, Bill.MaxIngredientSearchRadius);

            settingsSection.EndSection(settingsSection);
            listingStandard.End();

            DrawSelectedCondenserItems(condenserItemsScrollRect);
            DrawItemSelect(condenserItemsSelectScrollRect);
            base.DoSettingsWindowContents(inRect);
        }

        public override void WriteSettings()
        {
            Settings.ApplySettings();
            base.WriteSettings();
        }

        public override string SettingsCategory()
        {
            return "RimMisc";
        }

        private void DrawSelectedCondenserItems(Rect scrollSectionRect)
        {
            GUI.BeginGroup(scrollSectionRect);
            var sectionLabelRect = new Rect(0, 0, CONDENSER_ITEM_LABEL_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            var workLabelRect = new Rect(CONDENSER_ITEM_ICON_WIDTH + CONDENSER_ITEM_LABEL_WIDTH, 0, CONDENSER_ITEM_FIELD_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            var yieldLabelRect = new Rect(workLabelRect.x + workLabelRect.width, 0, CONDENSER_ITEM_FIELD_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            Widgets.Label(sectionLabelRect, "RimMisc_CondenserItemsSection".Translate());
            Widgets.Label(workLabelRect, "RimMisc_WorkColumn".Translate());
            Widgets.Label(yieldLabelRect, "RimMisc_YieldColumn".Translate());

            Settings.condenserItems = Settings.condenserItems.Where(item => item != null).ToList();

            var outRect = new Rect(0, workLabelRect.height, scrollSectionRect.width, scrollSectionRect.height);
            outRect.height -= outRect.y;
            var viewRect = new Rect(0, workLabelRect.height, scrollSectionRect.width - SCROLLBAR_WIDTH, condenserItemScrollHeight);
            Widgets.BeginScrollView(outRect, ref condenserItemScrollPos, viewRect);

            // draw each entry
            var currentY = outRect.y;
            var immutableCondenserItems = new List<CondenserItem>(Settings.condenserItems);
            foreach (var item in immutableCondenserItems)
            {
                DrawCondenserItemRow(item, scrollSectionRect, currentY);
                currentY += CONDENSER_ITEM_ROW_HEIGHT;
            }

            condenserItemScrollHeight = currentY;

            Widgets.EndScrollView();
            GUI.EndGroup();
        }

        private void DrawCondenserItemRow(CondenserItem item, Rect sectionRect, float currentY)
        {
            string workFieldString = null;
            string yieldFieldString = null;

            var iconRect = new Rect(0, currentY, CONDENSER_ITEM_ICON_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            var labelRect = new Rect(iconRect.width, currentY, CONDENSER_ITEM_LABEL_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            var fieldRect1 = new Rect(labelRect.x + labelRect.width, currentY, CONDENSER_ITEM_FIELD_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            var fieldRect2 = new Rect(fieldRect1.x + fieldRect1.width + ITEM_PADDING, currentY, CONDENSER_ITEM_FIELD_WIDTH, SEARCH_RESULT_ROW_HEIGHT);

            Widgets.ThingIcon(iconRect, item.ThingDef);
            Widgets.Label(labelRect, item.ThingDef.label);
            Widgets.TextFieldNumeric(fieldRect1, ref item.work, ref workFieldString, MIN_WORK, MAX_WORK);
            Widgets.TextFieldNumeric(fieldRect2, ref item.yield, ref yieldFieldString, MIN_YIELD, MAX_YIELD);

            var removeButtonRect = new Rect(sectionRect.width - BUTTON_WIDTH - SCROLLBAR_WIDTH, currentY, BUTTON_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            if (Widgets.ButtonText(removeButtonRect, "RimMisc_CondenserItemRemoveButton".Translate()))
            {
                Settings.condenserItems.Remove(item);
            }

            var calculateWorkButtonRect = new Rect(removeButtonRect.x - ITEM_PADDING, currentY, BUTTON_WIDTH * 2, SEARCH_RESULT_ROW_HEIGHT);
            calculateWorkButtonRect.x -= calculateWorkButtonRect.width;
            if (Widgets.ButtonText(calculateWorkButtonRect, "RimMisc_CondenserItemCalculateWorkButton".Translate()))
            {
                item.CalculateWorkAmount();
            }
        }

        private void DrawItemSelect(Rect scrollSectionRect)
        {
            GUI.BeginGroup(scrollSectionRect);
            var labelRect = new Rect(0, 0, scrollSectionRect.width, SEARCH_RESULT_ROW_HEIGHT);
            Widgets.Label(labelRect, "RimMisc_CondenserItemsSelectSection".Translate());

            var searchBarRect = new Rect(0, labelRect.height, scrollSectionRect.width, SEARCH_RESULT_ROW_HEIGHT);
            searchKeyword = Widgets.TextField(searchBarRect, searchKeyword);

            // setup scrolling menu
            var outRect = new Rect(0, searchBarRect.y + searchBarRect.height, scrollSectionRect.width, scrollSectionRect.height);
            outRect.height -= outRect.y;
            var viewRect = new Rect(0, searchBarRect.y + searchBarRect.height, scrollSectionRect.width - SCROLLBAR_WIDTH, condenserItemSelectScrollHeight);
            Widgets.BeginScrollView(outRect, ref condenserItemSelectScrollPos, viewRect);

            // filter out items already in list
            var condenserItemThingDefNames = Settings.condenserItems.Select(item => item.thingDefName).ToHashSet();
            var thingList = DefDatabase<ThingDef>.AllDefs.Where(d => !condenserItemThingDefNames.Contains(d.defName) &&
            (d.category == ThingCategory.Item || d.category == ThingCategory.Building));

            // draw each entry
            var currY = outRect.y;
            foreach (var thing in thingList)
            {
                if (searchKeyword.NullOrEmpty() || thing.label.IndexOf(searchKeyword, StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    if (ShouldDrawSearchResultsRow(currY, condenserItemSelectScrollPos.y, outRect.height))
                    {
                        DrawSearchResultsRow(thing, scrollSectionRect, currY);
                    }

                    currY += SEARCH_RESULT_ROW_HEIGHT;
                }
            }

            condenserItemSelectScrollHeight = currY;

            Widgets.EndScrollView();
            GUI.EndGroup();
        }

        private bool ShouldDrawSearchResultsRow(float currentY, float scrollY, float viewHeight)
        {
            if (currentY + SEARCH_RESULT_ROW_HEIGHT - scrollY < 0 || currentY - SEARCH_RESULT_ROW_HEIGHT - scrollY - viewHeight > 0)
            {
                return false;
            }

            return true;
        }

        private void DrawSearchResultsRow(ThingDef thing, Rect sectionRect, float currentY)
        {
            var iconRect = new Rect(0, currentY, CONDENSER_ITEM_ICON_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            Widgets.ThingIcon(iconRect, thing);

            var labelRect = new Rect(CONDENSER_ITEM_ICON_WIDTH, currentY, CONDENSER_ITEM_LABEL_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            Widgets.Label(labelRect, thing.label);

            // button for selecting a new pawn kind
            var addButtonRect = new Rect(sectionRect.width - BUTTON_WIDTH - SCROLLBAR_WIDTH, currentY, BUTTON_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            if (Widgets.ButtonText(addButtonRect, "RimMisc_CondenserItemAddButton".Translate()))
            {
                var item = new CondenserItem(thing.defName, MIN_WORK, MIN_YIELD);
                item.CalculateWorkAmount();
                Settings.condenserItems.Add(item);
            }
        }

        private void EnableRocketmanRaces()
        {
            if (ModsConfig.IsActive("Krkr.RocketMan"))
            {
                foreach (var raceSettings in Context.Settings.AllRaceSettings)
                {
                    var hasCustomThingClass = IgnoreMeDatabase.ShouldIgnore(raceSettings.def);
                    if (raceSettings.enabled || raceSettings.isFastMoving || hasCustomThingClass)
                    {
                        continue;
                    }
                    Context.DilationEnabled[(int)raceSettings.def.index] = true;
                    raceSettings.enabled = true;
                    raceSettings.Prepare(true);
                    Log.Message($"Enable time dilation for {(string)raceSettings.def.LabelCap ?? raceSettings.def.defName}");
                }
            }
        }
    }
}