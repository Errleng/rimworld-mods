﻿using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Rimternet
{

    public class MainTabWindow_ModHelp : MainTabWindow, IHelpDefView
    {

        #region Instance Data

        protected static List<ModCategory> CachedHelpCategories;
        public HelpDef SelectedHelpDef;

        public const float WindowMargin = 6f; // 15 is way too much.
        public const float EntryHeight = 30f;
        public const float EntryIndent = 15f;
        public const float ParagraphMargin = 8f;
        public const float LineHeigthOffset = 6f; // CalcSize overestimates required height by roughly this much.

        protected Rect SelectionRect;
        protected Rect DisplayRect;
        protected static Vector2 ArrowImageSize = new Vector2(10f, 10f);

        protected Vector2 SelectionScrollPos = default(Vector2);
        protected Vector2 DisplayScrollPos = default(Vector2);

        public const float MinWidth = 800f;
        public const float MinHeight = 600f;
        public const float MinListWidth = 200f;
        public float ContentHeight = 9999f;
        public float SelectionHeight = 9999f;

        private static string _filterString = "";
        private string _lastFilterString = "";
        private int _lastFilterTick;
        private bool _filtered;
        private bool _jump;

        private MainButton_HelpMenuDef TabDef
        {
            get
            {
                return def as MainButton_HelpMenuDef;
            }
        }

        #endregion

        #region Constructor

        public MainTabWindow_ModHelp()
        {
            layer = WindowLayer.GameUI;
            soundAppear = null;
            soundClose = null;
            doCloseButton = false;
            doCloseX = true;
            closeOnCancel = true;
            forcePause = true;
        }

        #endregion

        #region Positioning overrides

        public override MainTabWindowAnchor Anchor
        {
            get
            {
                return MainTabWindowAnchor.Right;
            }
        }

        public override Vector2 RequestedTabSize
        {
            get
            {
                if (TabDef != null)
                {
                    return new Vector2(TabDef.windowSize.x > MinWidth ? TabDef.windowSize.x : MinWidth, TabDef.windowSize.y > MinHeight ? TabDef.windowSize.y : MinHeight);
                }
                return new Vector2(MinWidth, MinHeight);
            }
        }

        #endregion

        #region Category Cache Object

        public class ModCategory
        {
            readonly List<HelpCategoryDef> _helpCategories = new List<HelpCategoryDef>();

            public readonly string ModName;

            public bool Expanded;

            public ModCategory(string modName)
            {
                ModName = modName;
            }

            public List<HelpCategoryDef> HelpCategories
            {
                get
                {
                    return _helpCategories.OrderBy(a => a.label).ToList();
                }
            }

            public bool ShouldDraw
            {
                get;
                set;
            }

            public bool MatchesFilter(string filter)
            {
                return (
                    (filter == "") ||
                    (ModName.ToUpper().Contains(filter.ToUpper()))
                );
            }

            public bool ThisOrAnyChildMatchesFilter(string filter)
            {
                return (
                    (MatchesFilter(filter)) ||
                    (HelpCategories.Any(hc => hc.ThisOrAnyChildMatchesFilter(filter)))
                );
            }

            public void Filter(string filter)
            {
                ShouldDraw = ThisOrAnyChildMatchesFilter(filter);
                Expanded = (
                    (filter != "") &&
                    (ThisOrAnyChildMatchesFilter(filter))
                );

                foreach (HelpCategoryDef hc in HelpCategories)
                {
                    hc.Filter(filter, MatchesFilter(filter));
                }
            }

            public void AddCategory(HelpCategoryDef def)
            {
                _helpCategories.AddUnique(def);
            }
        }

        #endregion

        #region Category Cache Control

        public override void PreOpen()
        {
            base.PreOpen();

            // Set whether the window forces a pause
            // Not entirely sure why force pause warrants an xml setting? - Fluffy.
            if (TabDef != null)
            {
                forcePause = TabDef.pauseGame;
            }

            // Build the help system
            Recache();

            // set initial Filter
            Filter();
        }

        public static void Recache()
        {
            CachedHelpCategories = new List<ModCategory>();
            foreach (var helpCategory in DefDatabase<HelpCategoryDef>.AllDefs)
            {
                // parent modcategory does not exist, create it.
                if (CachedHelpCategories.All(t => t.ModName != helpCategory.ModName))
                {
                    var mCat = new ModCategory(helpCategory.ModName);
                    mCat.AddCategory(helpCategory);
                    CachedHelpCategories.Add(mCat);
                }
                // add to existing modcategory
                else
                {
                    var mCat = CachedHelpCategories.Find(t => t.ModName == helpCategory.ModName);
                    mCat.AddCategory(helpCategory);
                }
            }
        }

        #endregion

        #region Filter

        private void _filterUpdate()
        {
            // filter after a short delay.
            // Log.Message(_filterString + " | " + _lastFilterTick + " | " + _filtered);
            if (_filterString != _lastFilterString)
            {
                _lastFilterString = _filterString;
                _lastFilterTick = 0;
                _filtered = false;
            }
            else if (!_filtered)
            {
                if (_lastFilterTick > 60 * 5 && _filterString.Length > 1)
                {
                    Filter();
                }
                _lastFilterTick++;
            }
        }

        public void Filter()
        {
            foreach (ModCategory mc in CachedHelpCategories)
            {
                mc.Filter(_filterString);
            }
            _filtered = true;
        }

        public void ResetFilter()
        {
            _filterString = "";
            _lastFilterString = "";
            Filter();
        }

        #endregion

        #region OTab Rendering

        public override void DoWindowContents(Rect rect)
        {
            Text.Font = GameFont.Small;

            GUI.BeginGroup(rect);

            float selectionWidth = TabDef != null ? (TabDef.listWidth >= MinListWidth ? TabDef.listWidth : MinListWidth) : MinListWidth;
            SelectionRect = new Rect(0f, 0f, selectionWidth, rect.height);
            DisplayRect = new Rect(
                SelectionRect.width + WindowMargin, 0f,
                rect.width - SelectionRect.width - WindowMargin, rect.height
            );

            DrawSelectionArea(SelectionRect);
            DrawDisplayArea(DisplayRect);

            GUI.EndGroup();
        }

        void DrawDisplayArea(Rect rect)
        {
            Widgets.DrawMenuSection(rect);

            if (SelectedHelpDef == null)
            {
                return;
            }

            Text.Font = GameFont.Medium;
            Text.WordWrap = false;
            float titleWidth = Text.CalcSize(SelectedHelpDef.LabelCap).x;
            var titleRect = new Rect(rect.xMin + WindowMargin, rect.yMin + WindowMargin, titleWidth, 60f);

            if (SelectedHelpDef.keyDef is ResearchProjectDef)
            {
                var research = SelectedHelpDef.keyDef as ResearchProjectDef;
                var researchRect = new Rect(rect.xMin + WindowMargin, rect.yMin + WindowMargin + 5f, 90f, 50f);

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;

                if (research.IsFinished)
                {
                    Widgets.DrawMenuSection(researchRect);
                    Widgets.Label(researchRect, ResourceBank.String.Finished);
                }
                else if (research == Find.ResearchManager.GetProject())
                {
                    Widgets.DrawMenuSection(researchRect);
                    Widgets.Label(researchRect, ResourceBank.String.InProgress);
                }
                else if (!research.CanStartNow)
                {
                    Widgets.DrawMenuSection(researchRect);
                    Widgets.Label(researchRect, ResourceBank.String.Locked);
                }
                else if (Widgets.ButtonText(researchRect, ResourceBank.String.Research, true, false, true))
                {
                    SoundDef.Named("ResearchStart").PlayOneShotOnCamera(null);
                    Find.ResearchManager.SetCurrentProject(research);
                    TutorSystem.Notify_Event("StartResearchProject");
                }

                titleRect.x += 100f;
            }
            else if (
                (SelectedHelpDef.keyDef != null) &&
                (SelectedHelpDef.keyDef.IconTexture() != null)
            )
            {
                var iconRect = new Rect(titleRect.xMin + WindowMargin, rect.yMin + WindowMargin, 60f - 2 * WindowMargin, 60f - 2 * WindowMargin);
                titleRect.x += 60f;
                SelectedHelpDef.keyDef.DrawColouredIcon(iconRect);
            }

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(titleRect, SelectedHelpDef.LabelCap);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;

            Rect outRect = rect.ContractedBy(WindowMargin);
            outRect.yMin += 60f;
            Rect viewRect = outRect;
            viewRect.width -= 16f;
            viewRect.height = ContentHeight;

            GUI.BeginGroup(outRect);
            Widgets.BeginScrollView(outRect.AtZero(), ref DisplayScrollPos, viewRect.AtZero());

            Vector2 cur = Vector2.zero;

            HelpDetailSectionHelper.DrawText(ref cur, viewRect.width, SelectedHelpDef.description);

            cur.y += ParagraphMargin;

            foreach (HelpDetailSection section in SelectedHelpDef.HelpDetailSections)
            {
                section.Draw(ref cur, viewRect.width, this);
            }

            ContentHeight = cur.y;

            Widgets.EndScrollView();
            GUI.EndGroup();
        }

        void DrawSelectionArea(Rect rect)
        {
            Widgets.DrawMenuSection(rect);

            _filterUpdate();
            Rect filterRect = new Rect(rect.xMin + WindowMargin, rect.yMin + WindowMargin, rect.width - 3 * WindowMargin - 30f, 30f);
            Rect clearRect = new Rect(filterRect.xMax + WindowMargin + 3f, rect.yMin + WindowMargin + 3f, 24f, 24f);
            _filterString = Widgets.TextField(filterRect, _filterString);
            if (_filterString != "")
            {
                if (Widgets.ButtonImage(clearRect, Widgets.CheckboxOffTex))
                {
                    _filterString = "";
                    Filter();
                }
            }

            Rect outRect = rect;
            outRect.yMin += 40f;
            outRect.xMax -= 2f; // some spacing around the scrollbar

            float viewWidth = SelectionHeight > outRect.height ? outRect.width - 16f : outRect.width;
            var viewRect = new Rect(0f, 0f, viewWidth, SelectionHeight);

            GUI.BeginGroup(outRect);
            Widgets.BeginScrollView(outRect.AtZero(), ref SelectionScrollPos, viewRect);

            if (CachedHelpCategories.Count(mc => mc.ShouldDraw) < 1)
            {
                Rect messageRect = outRect.AtZero();
                Widgets.Label(messageRect, "NoHelpDefs".Translate());
            }
            else
            {
                Vector2 cur = Vector2.zero;

                // This works fine for the current artificial three levels of helpdefs. 
                // Can easily be adapted by giving each category a list of subcategories, 
                // and migrating the responsibility for drawing them and the helpdefs to DrawCatEntry().
                // Would also require some minor adaptations to the filter methods, but nothing major.
                // - Fluffy.
                foreach (ModCategory mc in CachedHelpCategories.Where(mc => mc.ShouldDraw))
                {
                    DrawModEntry(ref cur, 0, viewRect, mc);

                    cur.x += EntryIndent;
                    if (mc.Expanded)
                    {
                        foreach (HelpCategoryDef hc in mc.HelpCategories.Where(hc => hc.ShouldDraw))
                        {
                            DrawCatEntry(ref cur, 1, viewRect, hc);

                            if (hc.Expanded)
                            {
                                foreach (HelpDef hd in hc.HelpDefs.Where(hd => hd.ShouldDraw))
                                {
                                    DrawHelpEntry(ref cur, 1, viewRect, hd);
                                }
                            }
                        }
                    }
                }

                SelectionHeight = cur.y;
            }

            Widgets.EndScrollView();
            GUI.EndGroup();
        }

        #endregion

        #region list rect helper

        public enum State
        {
            Expanded,
            Closed,
            Leaf
        }

        /// <summary>
        /// Generic method for drawing the squares. 
        /// </summary>
        /// <param name="cur">Current x,y vector</param>
        /// <param name="nestLevel">Level of nesting for indentation</param>
        /// <param name="view">Size of viewing area (assumed vertically scrollable)</param>
        /// <param name="label">Label to show</param>
        /// <param name="state">State of collapsing icon to show</param>
        /// <param name="selected">For leaf entries, is this entry selected?</param>
        /// <returns></returns>
        public bool DrawEntry(ref Vector2 cur, int nestLevel, Rect view, string label, State state, bool selected = false)
        {
            cur.x = nestLevel * EntryIndent;
            float iconOffset = ArrowImageSize.x + 2 * WindowMargin;
            float width = view.width - cur.x - iconOffset - WindowMargin;
            float height = EntryHeight;

            if (Text.CalcHeight(label, width) > EntryHeight)
            {
                Text.Font = GameFont.Tiny;
                float height2 = Text.CalcHeight(label, width);
                height = Mathf.Max(height, height2);
            }

            if (state != State.Leaf)
            {
                Rect iconRect = new Rect(cur.x + WindowMargin, cur.y + height / 2 - ArrowImageSize.y / 2, ArrowImageSize.x, ArrowImageSize.y);
                GUI.DrawTexture(iconRect, state == State.Expanded ? ResourceBank.Icon.HelpMenuArrowDown : ResourceBank.Icon.HelpMenuArrowRight);
            }

            Text.Anchor = TextAnchor.MiddleLeft;
            Rect labelRect = new Rect(cur.x + iconOffset, cur.y, width, height);
            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            // full viewRect width for overlay and button
            Rect buttonRect = view;
            buttonRect.yMin = cur.y;
            cur.y += height;
            buttonRect.yMax = cur.y;
            GUI.color = Color.grey;
            Widgets.DrawLineHorizontal(view.xMin, cur.y, view.width);
            GUI.color = Color.white;
            if (selected)
            {
                Widgets.DrawHighlightSelected(buttonRect);
            }
            else
            {
                Widgets.DrawHighlightIfMouseover(buttonRect);
            }
            return Widgets.ButtonInvisible(buttonRect);
        }

        public void DrawModEntry(ref Vector2 cur, int nestLevel, Rect view, ModCategory mc)
        {
            State curState = mc.Expanded ? State.Expanded : State.Closed;
            if (DrawEntry(ref cur, nestLevel, view, mc.ModName, curState))
            {
                mc.Expanded = !mc.Expanded;
            }
        }

        public void DrawCatEntry(ref Vector2 cur, int nestLevel, Rect view, HelpCategoryDef catDef)
        {
            State curState = catDef.Expanded ? State.Expanded : State.Closed;
            if (DrawEntry(ref cur, nestLevel, view, catDef.LabelCap, curState))
            {
                catDef.Expanded = !catDef.Expanded;
            }
        }

        public void DrawHelpEntry(ref Vector2 cur, int nestLevel, Rect view, HelpDef helpDef)
        {
            bool selected = SelectedHelpDef == helpDef;
            if (selected && _jump)
            {
                SelectionScrollPos.y = cur.y;
                _jump = false;
            }
            if (DrawEntry(ref cur, nestLevel, view, helpDef.LabelCap, State.Leaf, selected))
            {
                SelectedHelpDef = helpDef;
            }
        }

        public void JumpTo(Def def)
        {
            JumpTo(def.GetHelpDef());
        }

        public void JumpTo(HelpDef helpDef)
        {
            Find.MainTabsRoot.SetCurrentTab(this.def);
            ResetFilter();
            _jump = true;
            SelectedHelpDef = helpDef;
            HelpCategoryDef cat = DefDatabase<HelpCategoryDef>.AllDefsListForReading.First(hc => hc.HelpDefs.Contains(helpDef));
            cat.Expanded = true;
            ModCategory mod = CachedHelpCategories.First(mc => mc.HelpCategories.Contains(cat));
            mod.Expanded = true;
        }

        public bool Accept(HelpDef def)
        {
            return true;
        }

        public IHelpDefView SecondaryView(HelpDef def)
        {
            return null;
        }

        #endregion

    }

}
