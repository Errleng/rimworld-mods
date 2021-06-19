using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
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
        }
    }
    public class RimMisc : Mod
    {
        private static readonly float SEARCH_RESULT_ROW_HEIGHT = 30f;
        private static readonly float CONDENSER_ITEM_ROW_HEIGHT = 30f;
        private static readonly float CONDENSER_ITEM_ICON_WIDTH = 30f;
        private static readonly float CONDENSER_ITEM_LABEL_WIDTH = 200f;
        private static readonly float CONDENSER_ITEM_FIELD_WIDTH = 100f;
        private static readonly float BUTTON_WIDTH = 60f;
        private static readonly float SCROLLBAR_WIDTH = 20;
        private static readonly int MIN_WORK = 1;
        private static readonly int MAX_WORK = 10000;
        private static readonly int DEFAULT_WORK = 6000;
        private static readonly int MIN_YIELD = 1;
        private static readonly int MAX_YIELD = 1000;

        private string searchKeyword;
        private float condenserItemScrollHeight;
        private float condenserItemSelectScrollHeight;
        private Vector2 condenserItemScrollPos;
        private Vector2 condenserItemSelectScrollPos;

        public static RimMiscSettings Settings;
        public RimMisc(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RimMiscSettings>();
            Harmony harmony = new Harmony("com.rimmisc.rimworld.mod");
            harmony.PatchAll();
            Log.Message("RimMisc loaded");
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect settingsRect = inRect.TopPart(0.25f).Rounded();
            Rect condenserItemsRect = inRect.BottomPart(0.5f).Rounded();
            Rect condenserItemsSelectRect = inRect.BottomPart(0.3f).Rounded();
            Rect condenserItemsScrollRect = new Rect(condenserItemsRect)
            {
                height = condenserItemsSelectRect.y - condenserItemsRect.y,
                y = condenserItemsRect.y + 10,
            };
            Rect condenserItemsSelectScrollRect = new Rect(condenserItemsSelectRect)
            {
                y = condenserItemsSelectRect.y + 10,
            };

            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            Listing_Standard settingsSection = listingStandard.BeginSection_NewTemp(settingsRect.height);

            settingsSection.CheckboxLabeled("RimMisc_DefaultDoUntil".Translate(), ref Settings.defaultDoUntil);
            settingsSection.CheckboxLabeled("RimMisc_AutoCloseLetters".Translate(), ref Settings.autoCloseLetters);

            settingsSection.Label("RimMisc_AutoCloseLettersSeconds".Translate(Settings.autoCloseLettersSeconds));
            Settings.autoCloseLettersSeconds = settingsSection.Slider(Settings.autoCloseLettersSeconds, 10, 600);

            settingsSection.EndSection(settingsSection);
            listingStandard.End();

            DrawSelectedCondenserItems(condenserItemsScrollRect);
            DrawItemSelect(condenserItemsSelectScrollRect);

            Settings.ApplySettings();

            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "RimMisc".Translate();
        }

        private void DrawSelectedCondenserItems(Rect scrollSectionRect)
        {
            GUI.BeginGroup(scrollSectionRect);
            Rect sectionLabelRect = new Rect(0, 0, CONDENSER_ITEM_LABEL_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            Rect workLabelRect = new Rect(CONDENSER_ITEM_ICON_WIDTH + CONDENSER_ITEM_LABEL_WIDTH, 0, CONDENSER_ITEM_FIELD_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            Rect yieldLabelRect = new Rect(workLabelRect.x + workLabelRect.width, 0, CONDENSER_ITEM_FIELD_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            Widgets.Label(sectionLabelRect, "RimMisc_CondenserItemsSection".Translate());
            Widgets.Label(workLabelRect, "RimMisc_WorkColumn".Translate());
            Widgets.Label(yieldLabelRect, "RimMisc_YieldColumn".Translate());

            Settings.condenserItems = Settings.condenserItems.Where(item => item != null).ToList();

            Rect outRect = new Rect(0, workLabelRect.height, scrollSectionRect.width, scrollSectionRect.height);
            outRect.height -= outRect.y;
            Rect viewRect = new Rect(0, workLabelRect.height, scrollSectionRect.width - SCROLLBAR_WIDTH, condenserItemScrollHeight);
            Widgets.BeginScrollView(outRect, ref condenserItemScrollPos, viewRect);

            // draw each entry
            float currentY = outRect.y;
            List<CondenserItem> immutableCondenserItems = new List<CondenserItem>(Settings.condenserItems);
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

            Rect iconRect = new Rect(0, currentY, CONDENSER_ITEM_ICON_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            Rect labelRect = new Rect(iconRect.width, currentY, CONDENSER_ITEM_LABEL_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            Rect fieldRect1 = new Rect(labelRect.x + labelRect.width, currentY, CONDENSER_ITEM_FIELD_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            Rect fieldRect2 = new Rect(fieldRect1.x + fieldRect1.width, currentY, CONDENSER_ITEM_FIELD_WIDTH, SEARCH_RESULT_ROW_HEIGHT);

            Widgets.ThingIcon(iconRect, item.ThingDef);
            Widgets.Label(labelRect, item.ThingDef.label);
            Widgets.TextFieldNumeric(fieldRect1, ref item.work, ref workFieldString, MIN_WORK, MAX_WORK);
            Widgets.TextFieldNumeric(fieldRect2, ref item.yield, ref yieldFieldString, MIN_YIELD, MAX_YIELD);

            Rect removeButtonRect = new Rect(sectionRect.width - BUTTON_WIDTH - SCROLLBAR_WIDTH, currentY, BUTTON_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            if (Widgets.ButtonText(removeButtonRect, "RimMisc_CondenserItemRemoveButton".Translate()))
            {
                Settings.condenserItems.Remove(item);
            }
        }

        private void DrawItemSelect(Rect scrollSectionRect)
        {
            GUI.BeginGroup(scrollSectionRect);
            Rect labelRect = new Rect(0, 0, scrollSectionRect.width, SEARCH_RESULT_ROW_HEIGHT);
            Widgets.Label(labelRect, "RimMisc_CondenserItemsSelectSection".Translate());

            Rect searchBarRect = new Rect(0, labelRect.height, scrollSectionRect.width, SEARCH_RESULT_ROW_HEIGHT);
            searchKeyword = Widgets.TextField(searchBarRect, searchKeyword);

            // setup scrolling menu
            Rect outRect = new Rect(0, searchBarRect.y + searchBarRect.height, scrollSectionRect.width, scrollSectionRect.height);
            outRect.height -= outRect.y;
            Rect viewRect = new Rect(0, searchBarRect.y + searchBarRect.height, scrollSectionRect.width - SCROLLBAR_WIDTH, condenserItemSelectScrollHeight);
            Widgets.BeginScrollView(outRect, ref condenserItemSelectScrollPos, viewRect);

            // filter out items already in list
            HashSet<string> condenserItemThingDefNames = Settings.condenserItems.Select(item => item.thingDefName).ToHashSet();
            var thingList = DefDatabase<ThingDef>.AllDefs.Where(d => !condenserItemThingDefNames.Contains(d.defName) && d.category == ThingCategory.Item);

            // draw each entry
            float currY = outRect.y;
            foreach (ThingDef thing in thingList)
            {
                if (searchKeyword.NullOrEmpty() || (thing.label.IndexOf(searchKeyword, StringComparison.InvariantCultureIgnoreCase) >= 0))
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
            if ((currentY + SEARCH_RESULT_ROW_HEIGHT - scrollY < 0) || (currentY - SEARCH_RESULT_ROW_HEIGHT - scrollY - viewHeight > 0))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private void DrawSearchResultsRow(ThingDef thing, Rect sectionRect, float currentY)
        {
            Rect iconRect = new Rect(0, currentY, CONDENSER_ITEM_ICON_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            Widgets.ThingIcon(iconRect, thing);

            Rect labelRect = new Rect(CONDENSER_ITEM_ICON_WIDTH, currentY, CONDENSER_ITEM_LABEL_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            Widgets.Label(labelRect, thing.label);

            // button for selecting a new pawn kind
            Rect addButtonRect = new Rect(sectionRect.width - BUTTON_WIDTH - SCROLLBAR_WIDTH, currentY, BUTTON_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            if (Widgets.ButtonText(addButtonRect, "RimMisc_CondenserItemAddButton".Translate()))
            {
                Settings.condenserItems.Add(new CondenserItem(thing.defName, DEFAULT_WORK, MIN_YIELD));
            }
        }
    }
}
