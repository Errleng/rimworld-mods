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
    class PawnSelectionITab : ITab
    {
        private static readonly Vector2 WINDOW_SIZE = new Vector2(500f, 500f);
        private static readonly float PAWN_ROW_HEIGHT = 30f;
        private float scrollViewHeight;
        private Vector2 scrollPos;
        private UniversalSpawner spawner => SelThing as UniversalSpawner;

        public PawnSelectionITab()
        {
            size = WINDOW_SIZE;
            labelKey = "RimSpawners_PawnSelectionTabName".Translate();
        }

        protected override void FillTab()
        {
            Listing_Standard list = new Listing_Standard();
            Rect inRect = new Rect(0f, 0f, WINDOW_SIZE.x, WINDOW_SIZE.y).ContractedBy(10f);
            list.Begin(inRect);

            // show currently selected pawn kind
            Rect textRect = list.GetRect(PAWN_ROW_HEIGHT);

            PawnKindDef pawnKindToSpawn = spawner.GetChosenKind();
            if (pawnKindToSpawn != null)
            {
                Widgets.TextArea(textRect, $"Currently selected: {pawnKindToSpawn}", true);
            }
            else
            {
                Widgets.TextArea(textRect, $"No pawns selected", true);
            }

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
                if ((pawnKindToSpawn == null) || !pawnKindToSpawn.Equals(pawnKind))
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

            // pawn kind name
            Rect labelRect = new Rect(60, currentY, width, PAWN_ROW_HEIGHT);
            Widgets.Label(labelRect, pawnKind.label);

            // button for selecting a new pawn kind
            Rect selectButtonRect = new Rect(350, currentY, 100, PAWN_ROW_HEIGHT);
            if (Widgets.ButtonText(selectButtonRect, "RimSpawners_PawnSelectionButtonUnselected".Translate()))
            {
                spawner.SetChosenKind(pawnKind);
            }
        }
    }
}
