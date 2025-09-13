using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace VanoTech
{
    [StaticConstructorOnStartup]
    public class Loader
    {
        static Loader()
        {
            VanoTech.Settings.ApplySettings();
        }
    }

    public class VanoTech : Mod
    {
        private static readonly float SEARCH_RESULT_ROW_HEIGHT = 30f;
        private static readonly float CONDENSER_ITEM_ROW_HEIGHT = 30f;
        private static readonly float CONDENSER_ITEM_ICON_WIDTH = 30f;
        private static readonly float CONDENSER_ITEM_LABEL_WIDTH = 200f;
        private static readonly float CONDENSER_ITEM_FIELD_WIDTH = 100f;
        private static readonly float ITEM_PADDING = 10f;
        private static readonly float BUTTON_WIDTH = 60f;
        private static readonly float SCROLLBAR_WIDTH = 20f;
        private static readonly float MIN_WORK = 1;
        private static readonly float MAX_WORK = 1000000;
        private static readonly int MIN_YIELD = 1;
        private static readonly int MAX_YIELD = 1000;
        public static readonly string UnfinishedCondenserThingDefName = "UnfinishedCondenserThing";
        public static readonly string CondenserDefName = "VanometricCondenser";

        public static VanoTechSettings Settings;
        private float condenserItemScrollHeight;
        private Vector2 condenserItemScrollPos;
        private float condenserItemSelectScrollHeight;
        private Vector2 condenserItemSelectScrollPos;
        
        private string searchKeyword;

        public VanoTech(ModContentPack content) : base(content)
        {
            Settings = GetSettings<VanoTechSettings>();
            Log.Message("VanoTech loaded");
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var condenserItemsRect = inRect.Rounded();
            var condenserItemsSelectRect = inRect.BottomPart(0.3f).Rounded();
            var condenserItemsScrollRect = new Rect(condenserItemsRect)
            {
                height = condenserItemsSelectRect.y - condenserItemsRect.y,
                y = condenserItemsRect.y + 10
            };
            var condenserItemsSelectScrollRect = new Rect(condenserItemsSelectRect)
            {
                y = condenserItemsSelectRect.y + 1
            };

            DrawSelectedCondenserItems(condenserItemsScrollRect);
            DrawItemSelect(condenserItemsSelectScrollRect);

            Settings.ApplySettings();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "VanoTech";
        }
        
        public override void WriteSettings()
        {
            Settings.ApplySettings();
            base.WriteSettings();
        }
        
        private void DrawSelectedCondenserItems(Rect scrollSectionRect)
        {
            GUI.BeginGroup(scrollSectionRect);
            var sectionLabelRect = new Rect(0, 0, CONDENSER_ITEM_LABEL_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            var workLabelRect = new Rect(CONDENSER_ITEM_ICON_WIDTH + CONDENSER_ITEM_LABEL_WIDTH, 0, CONDENSER_ITEM_FIELD_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            var yieldLabelRect = new Rect(workLabelRect.x + workLabelRect.width, 0, CONDENSER_ITEM_FIELD_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            Widgets.Label(sectionLabelRect, "VanoTech_CondenserItemsSection".Translate());
            Widgets.Label(workLabelRect, "VanoTech_WorkColumn".Translate());
            Widgets.Label(yieldLabelRect, "VanoTech_YieldColumn".Translate());

            var condenserItems = Settings.GetRealCondenserItems();

            var outRect = new Rect(0, workLabelRect.height, scrollSectionRect.width, scrollSectionRect.height);
            outRect.height -= outRect.y;
            var viewRect = new Rect(0, workLabelRect.height, scrollSectionRect.width - SCROLLBAR_WIDTH, condenserItemScrollHeight);
            Widgets.BeginScrollView(outRect, ref condenserItemScrollPos, viewRect);

            // draw each entry
            var currentY = outRect.y;
            foreach (var item in condenserItems)
            {
                if (item == null)
                    continue;

                try
                {
                    DrawCondenserItemRow(item, scrollSectionRect, currentY);
                    currentY += CONDENSER_ITEM_ROW_HEIGHT;
                }
                catch
                {
                    currentY += CONDENSER_ITEM_ROW_HEIGHT;
                    continue;
                }
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
            if (Widgets.ButtonText(removeButtonRect, "VanoTech_CondenserItemRemoveButton".Translate()))
            {
                Settings.condenserItems.Remove(item);
            }

            var calculateWorkButtonRect = new Rect(removeButtonRect.x - ITEM_PADDING, currentY, BUTTON_WIDTH * 2, SEARCH_RESULT_ROW_HEIGHT);
            calculateWorkButtonRect.x -= calculateWorkButtonRect.width;
            if (Widgets.ButtonText(calculateWorkButtonRect, "VanoTech_CondenserItemCalculateWorkButton".Translate()))
            {
                item.CalculateWorkAmount();
            }
        }

        private void DrawItemSelect(Rect scrollSectionRect)
        {
            GUI.BeginGroup(scrollSectionRect);
            var labelRect = new Rect(0, 0, scrollSectionRect.width, SEARCH_RESULT_ROW_HEIGHT);
            Widgets.Label(labelRect, "VanoTech_CondenserItemsSelectSection".Translate());

            var searchBarRect = new Rect(0, labelRect.height, scrollSectionRect.width, SEARCH_RESULT_ROW_HEIGHT);
            searchKeyword = Widgets.TextField(searchBarRect, searchKeyword);

            // setup scrolling menu
            var outRect = new Rect(0, searchBarRect.y + searchBarRect.height, scrollSectionRect.width, scrollSectionRect.height);
            outRect.height -= outRect.y;
            var viewRect = new Rect(0, searchBarRect.y + searchBarRect.height, scrollSectionRect.width - SCROLLBAR_WIDTH, condenserItemSelectScrollHeight);
            Widgets.BeginScrollView(outRect, ref condenserItemSelectScrollPos, viewRect);

            // filter out items already in list
            var condenserItemThingDefNames = Settings.GetRealCondenserItems().Select(item => item.thingDefName).ToHashSet();
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

            // button for selecting a new item
            var addButtonRect = new Rect(sectionRect.width - BUTTON_WIDTH - SCROLLBAR_WIDTH, currentY, BUTTON_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            if (Widgets.ButtonText(addButtonRect, "VanoTech_CondenserItemAddButton".Translate()))
            {
                var item = new CondenserItem(thing.defName, MIN_WORK, MIN_YIELD);
                item.CalculateWorkAmount();
                Settings.condenserItems.Add(item);
            }
        }
    }
}
