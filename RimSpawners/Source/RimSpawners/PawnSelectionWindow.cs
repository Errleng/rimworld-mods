using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimSpawners
{
    internal class PawnSelectionWindow : Window
    {
        private static readonly Vector2 WINDOW_SIZE = new Vector2(500f, 500f);
        private static readonly float PAWN_ROW_HEIGHT = 30f;
        private static string searchKeyword;
        private static float scrollViewHeight;
        private static Vector2 scrollPos;
        private List<VanometricFabricator> spawners;

        public PawnSelectionWindow()
        {
            forcePause = false;
            absorbInputAroundWindow = false;
            closeOnCancel = true;
            soundAppear = SoundDefOf.CommsWindow_Open;
            soundClose = SoundDefOf.CommsWindow_Close;
            doCloseButton = false;
            doCloseX = true;
            draggable = true;
            drawShadow = true;
            preventCameraMotion = false;
            onlyOneOfTypeAllowed = true;
            resizeable = true;
        }

        public override Vector2 InitialSize => WINDOW_SIZE;

        public override void DoWindowContents(Rect inRect)
        {
            var list = new Listing_Standard();
            list.Begin(inRect);

            // get all selected sapwners
            spawners = Find.Selector.SelectedObjects.OfType<VanometricFabricator>().ToList();
            if (spawners.NullOrEmpty())
            {
                Close();
                return;
            }

            // search bar
            var searchBarRect = list.GetRect(PAWN_ROW_HEIGHT);
            searchKeyword = Widgets.TextField(searchBarRect, searchKeyword);

            list.GapLine();
            var yOffset = 10;

            // setup scrolling menu!
            var outRect = new Rect(5f, PAWN_ROW_HEIGHT + yOffset + 5f, WINDOW_SIZE.x - 30, WINDOW_SIZE.y - yOffset - 30f);
            var viewRect = new Rect(0f, 0f, outRect.width - 16f, scrollViewHeight);
            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);

            // draw each entry
            float currY = 0;
            foreach (var pawnKind in DefDatabase<PawnKindDef>.AllDefsListForReading)
            {
                var textToSearch = pawnKind.label ?? pawnKind.defName;
                if (searchKeyword.NullOrEmpty() || textToSearch.IndexOf(searchKeyword, StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    if (ShouldDrawPawnRow(currY, scrollPos.y, outRect.height))
                    {
                        DrawPawnRow(pawnKind, currY, viewRect.width);
                    }

                    currY += PAWN_ROW_HEIGHT;
                }
            }

            if (Event.current.type == EventType.Layout)
            {
                scrollViewHeight = currY + PAWN_ROW_HEIGHT;
            }

            Widgets.EndScrollView();

            list.End();
        }

        private bool ShouldDrawPawnRow(float currentY, float scrollY, float viewHeight)
        {
            if (currentY + PAWN_ROW_HEIGHT - scrollY < 0 || currentY - PAWN_ROW_HEIGHT - scrollY - viewHeight > 0)
            {
                return false;
            }

            return true;
        }

        private void DrawPawnRow(PawnKindDef pawnKind, float currentY, float width)
        {
            //List<PawnKindDef> pawnKindsToSpawn = spawner.getPawnKindsToSpawn();
            //// skip already selected pawn kind
            //if (pawnKindsToSpawn.Contains(pawnKind))
            //{
            //    return;
            //}

            // pawn kind image
            if (pawnKind.IconTexture() != null)
            {
                var iconRect = new Rect(0, currentY, 30, PAWN_ROW_HEIGHT);
                pawnKind.DrawColouredIcon(iconRect);
            }

            // pawn kind name and point cost
            var labelRect = new Rect(60, currentY, width, PAWN_ROW_HEIGHT);

            string label = pawnKind.LabelCap;
            if (label == null)
            {
                label = pawnKind.defName;
            }

            Widgets.Label(labelRect, "RimSpawners_PawnSelectionListEntry".Translate(label, pawnKind.combatPower));

            var tip = new TipSignal("RimSpawners_PawnSelectionToolTip".Translate(
                pawnKind.weaponTags.ToStringNullable(),
                pawnKind.apparelTags.ToStringNullable(),
                (pawnKind.apparelRequired?.Select(thing => thing.LabelCap.ToString()).ToList()).ToStringNullable(),
                pawnKind.apparelDisallowTags.ToStringNullable(),
                pawnKind.techHediffsTags.ToStringNullable()));
            tip.delay = 0.1f;

            TooltipHandler.TipRegion(labelRect, tip);

            // button for selecting a new pawn kind
            var selectButtonRect = new Rect(350, currentY, 100, PAWN_ROW_HEIGHT);
            if (Widgets.ButtonText(selectButtonRect, "RimSpawners_PawnSelectionButtonUnselected".Translate()))
            {
                if (spawners != null)
                {
                    foreach (var spawner in spawners)
                    {
                        spawner.SetChosenKind(pawnKind);
                        Close();
                    }
                }
            }
        }
    }
}