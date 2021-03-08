using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimSpawners
{
    class PawnSelectionWindow : Window
    {
        private static readonly Vector2 WINDOW_SIZE = new Vector2(500f, 500f);
        private static readonly float PAWN_ROW_HEIGHT = 30f;
        private float scrollViewHeight;
        private Vector2 scrollPos;
        private List<UniversalSpawner> spawners;
        private string searchKeyword;

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
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect);

            // get all selected sapwners
            spawners = Find.Selector.SelectedObjects.OfType<UniversalSpawner>().ToList();
            if (GenList.NullOrEmpty(spawners))
            {
                Close(true);
                return;
            }

            // search bar
            Rect searchBarRect = list.GetRect(PAWN_ROW_HEIGHT);
            searchKeyword = Widgets.TextField(searchBarRect, searchKeyword);

            list.GapLine();
            int yOffset = 10;

            // setup scrolling menu
            Rect outRect = new Rect(5f, PAWN_ROW_HEIGHT + yOffset + 5f, WINDOW_SIZE.x - 30, WINDOW_SIZE.y - yOffset - 30f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, scrollViewHeight);
            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect, true);

            // draw each entry
            float currY = 0;
            foreach (PawnKindDef pawnKind in DefDatabase<PawnKindDef>.AllDefsListForReading)
            {
                if (ShouldDrawPawnRow(currY, scrollPos.y, outRect.height))
                {
                    if (searchKeyword.NullOrEmpty() || (pawnKind.label.IndexOf(searchKeyword, StringComparison.InvariantCultureIgnoreCase) >= 0))
                    if (ShouldDrawPawnRow(currY, scrollPos.y, outRect.height))
                    {
                        DrawPawnRow(pawnKind, currY, viewRect.width);
                        currY += PAWN_ROW_HEIGHT;
                    }
                }
                else
                {
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
            if ((currentY + PAWN_ROW_HEIGHT - scrollY < 0) || (currentY - PAWN_ROW_HEIGHT - scrollY - viewHeight > 0))
            {
                return false;
            }
            else
            {
                return true;
            }
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
                Rect iconRect = new Rect(0, currentY, 30, PAWN_ROW_HEIGHT);
                pawnKind.DrawColouredIcon(iconRect);
            }

            // pawn kind name and point cost
            Rect labelRect = new Rect(60, currentY, width, PAWN_ROW_HEIGHT);
            Widgets.Label(labelRect, $"{pawnKind.label} ({pawnKind.combatPower} points)");

            // button for selecting a new pawn kind
            Rect selectButtonRect = new Rect(350, currentY, 100, PAWN_ROW_HEIGHT);
            if (Widgets.ButtonText(selectButtonRect, "RimSpawners_PawnSelectionButtonUnselected".Translate()))
            {
                if (spawners != null)
                {
                    foreach (UniversalSpawner spawner in spawners)
                    {
                        spawner.SetChosenKind(pawnKind);
                        Close(true);
                    }
                }
            }
        }
    }
}
