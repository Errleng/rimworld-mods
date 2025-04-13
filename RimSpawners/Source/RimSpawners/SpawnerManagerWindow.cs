using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimSpawners
{
    internal class SpawnerManagerWindow : Window
    {
        private static readonly Vector2 WINDOW_SIZE = new Vector2(800f, 500f);
        private static readonly float ROW_HEIGHT = 30f;

        private SpawnerManager spawnerManager;
        private string searchKeyword;
        private float scrollViewHeight;
        private Vector2 scrollPos;
        private List<PawnKindDef> pawnKindDefs;

        public SpawnerManagerWindow()
        {
            spawnerManager = Find.World.GetComponent<SpawnerManager>();

            // Display pawn kinds in order of mod name, then in order of name
            pawnKindDefs = DefDatabase<PawnKindDef>.AllDefsListForReading
                .OrderBy(x => getModName(x))
                .ThenBy(x => getName(x))
                .ToList();

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

            list.GapLine();

            // search bar
            var searchBarRect = list.GetRect(ROW_HEIGHT);
            searchKeyword = Widgets.TextField(searchBarRect, searchKeyword);
            list.GapLine();

            // setup scrolling menu
            var yOffset = 30;
            var outRect = new Rect(5f, ROW_HEIGHT + yOffset + 5f, WINDOW_SIZE.x - 50, WINDOW_SIZE.y - yOffset - 30f);
            var viewRect = new Rect(0f, 0f, outRect.width + 50, scrollViewHeight + 10);
            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);

            float currY = 0;

            // Draw a list of all pawn kinds to be selected to spawn
            string prevMod = "";
            foreach (var pawnKind in pawnKindDefs)
            {
                var textToSearch = getName(pawnKind);
                // Filter out rows that do not match the search 
                if (searchKeyword.NullOrEmpty() || textToSearch.IndexOf(searchKeyword, StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    // Draw section header if new section started
                    if (getModName(pawnKind) != prevMod)
                    {
                        currY += ROW_HEIGHT;
                        if (ShouldDrawRow(currY, scrollPos.y, outRect.height))
                        {
                            var labelRect = new Rect(140, currY, viewRect.width, ROW_HEIGHT);
                            Widgets.Label(labelRect, getModName(pawnKind));
                        }
                        currY += ROW_HEIGHT * 2;
                    }

                    // Draw pawn kind row
                    if (ShouldDrawRow(currY, scrollPos.y, outRect.height))
                    {
                        DrawRow(pawnKind, currY, viewRect.width);
                    }
                    currY += ROW_HEIGHT;

                    prevMod = getModName(pawnKind);
                }
            }

            currY += 10;

            // Draw a list of the currently selected pawns to spawn
            foreach (var entry in spawnerManager.pawnsToSpawn)
            {
                if (entry.Value.count == 0)
                {
                    continue;
                }
                var text = $"{entry.Value.GetKindLabel()} x{entry.Value.count}";
                var labelRect = new Rect(60, currY, viewRect.width, ROW_HEIGHT);
                if (ShouldDrawRow(currY, scrollPos.y, outRect.height))
                {
                    Widgets.Label(labelRect, "RimSpawners_SpawnerManagerPawnCount".Translate(text));
                }
                currY += ROW_HEIGHT;
            }

            if (Event.current.type == EventType.Layout)
            {
                scrollViewHeight = currY + ROW_HEIGHT;
            }

            Widgets.EndScrollView();

            list.End();
        }

        private bool ShouldDrawRow(float currentY, float scrollY, float viewHeight)
        {
            if (currentY + ROW_HEIGHT - scrollY < 0 || currentY - ROW_HEIGHT - scrollY - viewHeight > 0)
            {
                return false;
            }

            return true;
        }

        private void DrawRow(PawnKindDef pawnKind, float currentY, float width)
        {
            // button for selecting a new pawn kind
            var spawnCountButtonRect = new Rect(0, currentY, 50, ROW_HEIGHT);

            if (!spawnerManager.pawnsToSpawn.ContainsKey(pawnKind.defName))
            {
                Log.Error($"Spawner manager could not find kind {pawnKind.defName}. It contains {spawnerManager.pawnsToSpawn.Count} kinds.");
                return;
            }

            var info = spawnerManager.pawnsToSpawn[pawnKind.defName];
            string buffer = null;
            Widgets.TextFieldNumeric(spawnCountButtonRect, ref info.count, ref buffer, 0, 1000);

            // pawn kind image
            if (pawnKind.IconTexture() != null)
            {
                var iconRect = new Rect(80, currentY, 30, ROW_HEIGHT);
                pawnKind.DrawColouredIcon(iconRect);
            }

            // pawn kind name and point cost
            var labelRect = new Rect(140, currentY, width, ROW_HEIGHT);

            var label = getName(pawnKind);
            label = label.Substring(0, Math.Min(50, label.Length));

            Widgets.Label(labelRect, "RimSpawners_PawnSelectionListEntry".Translate(label, pawnKind.combatPower));

            var tip = new TipSignal("RimSpawners_PawnSelectionToolTip".Translate(
                getModName(pawnKind),
                pawnKind.defName,
                pawnKind.weaponTags.ToStringNullable(),
                pawnKind.apparelTags.ToStringNullable(),
                (pawnKind.apparelRequired?.Select(thing => thing.LabelCap.ToString()).ToList()).ToStringNullable(),
                pawnKind.apparelDisallowTags.ToStringNullable(),
                pawnKind.techHediffsTags.ToStringNullable()));
            tip.delay = 0.1f;

            TooltipHandler.TipRegion(labelRect, tip);
        }

        private string getName(PawnKindDef pawnKind)
        {
            return pawnKind.LabelCap.ToString() ?? pawnKind.defName;
        }

        private string getModName(PawnKindDef pawnKind)
        {
            return pawnKind.modContentPack?.Name ?? "Unknown";
        }
    }
}
