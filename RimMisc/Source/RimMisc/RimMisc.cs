using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace RimMisc
{
    public class RimMisc : Mod
    {
        private static readonly float SEARCH_RESULT_ROW_HEIGHT = 30f;
        private static readonly float CONDENSER_ITEM_ROW_HEIGHT = 30f;
        private static readonly float CONDENSER_ITEM_FIELD_WIDTH = 100f;
        private static readonly Vector2 WINDOW_SIZE = new Vector2(900f, 900f);

        private float scrollViewHeight;
        private string searchKeyword;
        private Vector2 scrollPos;

        public static RimMiscSettings Settings;
        public RimMisc(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RimMiscSettings>();
            Harmony harmony = new Harmony("com.rimmisc.rimworld.mod");
            harmony.PatchAll();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.CheckboxLabeled("DefaultDoUntil".Translate(), ref Settings.defaultDoUntil);
            listingStandard.CheckboxLabeled("AutoCloseLetters".Translate(), ref Settings.autoCloseLetters);

            listingStandard.Label("AutoCloseLettersSeconds".Translate(Settings.autoCloseLettersSeconds));
            Settings.autoCloseLettersSeconds = listingStandard.Slider(Settings.autoCloseLettersSeconds, 1, 600);

            DrawSelectedCondenserItems(listingStandard);
            DrawItemSelect(listingStandard);

            Widgets.EndScrollView();

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "RimMisc".Translate();
        }

        private void DrawSelectedCondenserItems(Listing_Standard listingStandard)
        {
            foreach (var item in Settings.condenserItems)
            {
                DrawCondenserItemRow(item, listingStandard);
            }
        }

        private void DrawCondenserItemRow(CondenserItem item, Listing_Standard listingStandard)
        {
        }

        private void DrawItemSelect(Listing_Standard listingStandard)
        {
            Rect searchBarRect = listingStandard.GetRect(SEARCH_RESULT_ROW_HEIGHT);
            searchKeyword = Widgets.TextField(searchBarRect, searchKeyword);

            listingStandard.GapLine();
            int yOffset = 10;

            // setup scrolling menu
            Rect outRect = new Rect(5f, SEARCH_RESULT_ROW_HEIGHT + yOffset + 5f, WINDOW_SIZE.x - 30, WINDOW_SIZE.y - yOffset - 30f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, scrollViewHeight);
            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);

            // draw each entry
            float currY = 0;
            var thingList = DefDatabase<ThingDef>.AllDefs.Where(d => d.category == ThingCategory.Item);
            foreach (ThingDef thing in thingList)
            {
                if (searchKeyword.NullOrEmpty() || (thing.label.IndexOf(searchKeyword, StringComparison.InvariantCultureIgnoreCase) >= 0))
                {
                    if (ShouldDrawSearchResultsRow(currY, scrollPos.y, outRect.height))
                    {
                        DrawSearchResultsRow(thing, currY, viewRect.width);
                    }
                    currY += SEARCH_RESULT_ROW_HEIGHT;
                }
            }
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

        private void DrawSearchResultsRow(ThingDef thing, float currentY, float width)
        {
            Rect iconRect = new Rect(0, currentY, 30, SEARCH_RESULT_ROW_HEIGHT);
            Widgets.ThingIcon(iconRect, thing);

            Rect labelRect = new Rect(60, currentY, width, SEARCH_RESULT_ROW_HEIGHT);
            Widgets.Label(labelRect, thing.label);

            // button for selecting a new pawn kind
            Rect selectButtonRect = new Rect(350, currentY, 100, SEARCH_RESULT_ROW_HEIGHT);
            if (Widgets.ButtonText(selectButtonRect, "RimMisc_CondenserItemSelectButton".Translate()))
            {

            }
        }
    }
}
