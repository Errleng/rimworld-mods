using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using UnityEngine;
using Verse;

namespace RimSpawners
{
    internal class ThingSelectorUI
    {
        private Vector2 selectedScrollPos = Vector2.zero;
        private Vector2 availableScrollPos = Vector2.zero;
        private float selectedScrollHeight;
        private float availableScrollHeight;
        private string searchKeyword = "";

        private readonly string title;
        private readonly HashSet<string> selectedDefs;
        private readonly System.Func<ThingDef, bool> filter;
        private readonly float sectionHeight;
        private readonly float iconWidth;
        private readonly float labelWidth;
        private readonly float buttonWidth;
        private readonly float rowHeight;

        public ThingSelectorUI(
            string title,
            HashSet<string> selectedDefs,
            System.Func<ThingDef, bool> filter,
            float sectionHeight = 200f,
            float iconWidth = 30f,
            float labelWidth = 200f,
            float buttonWidth = 60f,
            float rowHeight = 30f)
        {
            this.title = title;
            this.selectedDefs = selectedDefs;
            this.filter = filter;
            this.sectionHeight = sectionHeight;
            this.iconWidth = iconWidth;
            this.labelWidth = labelWidth;
            this.buttonWidth = buttonWidth;
            this.rowHeight = rowHeight;
            Log.Message($"Initialized ThingSelectorUI for {title} with {selectedDefs.Count} selected items: {string.Join(", ", selectedDefs)}");
        }

        private void DrawSelectedItems(Rect sectionRect)
        {
            GUI.BeginGroup(sectionRect);
            var titleRect = new Rect(0, 0, labelWidth, rowHeight);
            Widgets.Label(titleRect, $"Selected {title.Translate()}");

            var outRect = new Rect(0, rowHeight, sectionRect.width - GenUI.ScrollBarWidth, sectionHeight - rowHeight);
            var viewRect = new Rect(0, 0, outRect.width - 50, selectedScrollHeight);
            Widgets.BeginScrollView(outRect, ref selectedScrollPos, viewRect);

            float currY = 0;
            foreach (var defName in selectedDefs.ToList())
            {
                var def = DefDatabase<ThingDef>.GetNamed(defName);
                var rowRect = new Rect(0, currY, viewRect.width, rowHeight);

                var iconRect = new Rect(0, currY, iconWidth, rowHeight);
                var labelRect = new Rect(iconWidth, currY, labelWidth, rowHeight);
                var buttonRect = new Rect(outRect.width - buttonWidth - GenUI.ScrollBarWidth - 10, currY, buttonWidth, rowHeight);

                Widgets.ThingIcon(iconRect, def);
                Widgets.Label(labelRect, def.label);

                if (Widgets.ButtonText(buttonRect, "RimSpawners_SettingsRemove".Translate()))
                {
                    selectedDefs.Remove(defName);
                }

                currY += rowHeight;
            }

            selectedScrollHeight = currY;
            Widgets.EndScrollView();
            GUI.EndGroup();
        }

        private void DrawAvailableItems(Rect sectionRect)
        {
            GUI.BeginGroup(sectionRect);
            var titleRect = new Rect(0, 0, labelWidth, rowHeight);
            Widgets.Label(titleRect, $"Available {title.Translate()}");

            var searchRect = new Rect(0, rowHeight, sectionRect.width - GenUI.ScrollBarWidth, rowHeight);
            searchKeyword = Widgets.TextField(searchRect, searchKeyword);

            var outRect = new Rect(0, searchRect.y + searchRect.height, sectionRect.width - GenUI.ScrollBarWidth - 10, sectionHeight - (searchRect.y + searchRect.height));
            var viewRect = new Rect(0, 0, outRect.width - 50, availableScrollHeight);
            Widgets.BeginScrollView(outRect, ref availableScrollPos, viewRect);

            float currY = 0;
            string trimmedSearch = searchKeyword?.Trim() ?? "";
            var items = DefDatabase<ThingDef>.AllDefs.Where(d =>
                !selectedDefs.Contains(d.defName) &&
                filter(d) &&
                (string.IsNullOrEmpty(trimmedSearch) || d.label.IndexOf(trimmedSearch, System.StringComparison.OrdinalIgnoreCase) >= 0));

            foreach (var def in items)
            {
                var rowRect = new Rect(0, currY, viewRect.width, rowHeight);

                var iconRect = new Rect(0, currY, iconWidth, rowHeight);
                var labelRect = new Rect(iconWidth, currY, labelWidth, rowHeight);
                var buttonRect = new Rect(outRect.width - buttonWidth - GenUI.ScrollBarWidth, currY, buttonWidth, rowHeight);

                Widgets.ThingIcon(iconRect, def);
                Widgets.Label(labelRect, def.label);

                if (Widgets.ButtonText(buttonRect, "RimSpawners_SettingsAdd".Translate()))
                {
                    selectedDefs.Add(def.defName);
                }

                currY += rowHeight;
            }

            availableScrollHeight = currY;
            Widgets.EndScrollView();
            GUI.EndGroup();
        }

        public void Draw(Rect inRect, Listing_Standard listingStandard)
        {
            Log.Message($"listing standard height 1: {listingStandard.CurHeight}");
            var selectedWeaponsRect = new Rect(0, listingStandard.CurHeight + 10f, inRect.width - 50, sectionHeight);
            DrawSelectedItems(selectedWeaponsRect);
            listingStandard.Gap(sectionHeight + 20f);

            Log.Message($"listing standard height 2: {listingStandard.CurHeight}");
            var availableWeaponsRect = new Rect(0, listingStandard.CurHeight + 10f, inRect.width - 50, sectionHeight);
            DrawAvailableItems(availableWeaponsRect);
        }
    }
}