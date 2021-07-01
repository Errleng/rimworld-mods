using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimMisc
{
    //[StaticConstructorOnStartup]
    class MainTabWindow_WealthTab : MainTabWindow
    {

        enum WealthType
        {
            Items,
            Buildings,
            Floors,
            Creatures
        }

        private static readonly float WINDOW_WIDTH = 1200;
        private static readonly float WINDOW_HEIGHT = 800;
        private static readonly float MARGIN_SIZE = 50;
        private static readonly float LABEL_HEIGHT = 25;
        private static readonly float UPDATE_INTERVAL_TICKS = GenTicks.SecondsToTicks(10);

        private Vector2 scrollPosition;
        private float scrollHeight;
        private float lastUpdateTick;
        private Dictionary<string, int> itemCounts;
        private HashSet<Thing> uniqueItems;

        public MainTabWindow_WealthTab()
        {
            draggable = true;
            resizeable = false;
            //forcePause = true;
            scrollHeight = WINDOW_HEIGHT;
        }

        private bool IsThingItem(IThingHolder thingHolder)
        {
            if (thingHolder is PassingShip || thingHolder is MapComponent)
            {
                return false;
            }
            Pawn pawn = thingHolder as Pawn;
            return (pawn == null || pawn.Faction == Faction.OfPlayer) && (pawn == null || !pawn.IsQuestLodger());
        }

        private void CalculateItemWealth()
        {
            var map = Find.CurrentMap;
            List<Thing> items = new List<Thing>();
            ThingOwnerUtility.GetAllThingsRecursively(map, ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), items, false, IsThingItem);
            //Log.Message($"Initial items: {string.Join(",", items.Select(item => item.Label))}");

            itemCounts = new Dictionary<string, int>();
            uniqueItems = new HashSet<Thing>();
            foreach (var item in items)
            {
                if (item.SpawnedOrAnyParentSpawned && item.MarketValue > 0 && !item.PositionHeld.Fogged(map))
                {
                    if (itemCounts.Count == 0)
                    {
                        uniqueItems.Add(item);
                        itemCounts[item.LabelNoCount] = item.stackCount;
                    }
                    else
                    {
                        bool foundItem = false;
                        foreach (var uniqueItem in uniqueItems)
                        {
                            if (foundItem)
                            {
                                break;
                            }

                            if (item.LabelNoCount == uniqueItem.LabelNoCount && (item == uniqueItem || item.CanStackWith(uniqueItem)))
                            {
                                //Log.Message($"Check {item.Label}, {uniqueItem.Label}, {item.CanStackWith(uniqueItem)}, {(itemCounts.ContainsKey(uniqueItem.LabelNoCount) ? itemCounts[uniqueItem.LabelNoCount].ToString() : "None")}");
                                itemCounts[uniqueItem.LabelNoCount] += item.stackCount;
                                foundItem = true;
                            }
                        }

                        if (!foundItem)
                        {
                            //Log.Message($"Add {item.Label}, {itemCounts.ContainsKey(item.LabelNoCount)}");
                            uniqueItems.Add(item);
                            itemCounts[item.LabelNoCount] = item.stackCount;
                        }
                    }
                }
            }
            Log.Message($"Total wealth: {uniqueItems.Sum(item => itemCounts[item.LabelNoCount] * item.MarketValue)}");
        }

        private void DrawItemWealth(Rect inRect, Rect rowRect)
        {
            Rect outRect = new Rect(0, rowRect.y, inRect.width, inRect.height);
            Rect viewRect = new Rect(0, 0, inRect.width, scrollHeight);

            GUI.BeginGroup(viewRect);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            float currHeight = 0;
            var sortedItems = uniqueItems.OrderByDescending(item => itemCounts[item.LabelNoCount] * item.MarketValue).ToList();
            foreach (var item in sortedItems)
            {
                var itemCount = itemCounts[item.LabelNoCount];
                var wealth = itemCount * item.MarketValue;
                Widgets.Label(rowRect, "RimMisc_WealthTab_ItemRow".Translate(item.LabelCapNoCount, itemCount, item.MarketValue, wealth));
                rowRect.y += LABEL_HEIGHT;
                currHeight += LABEL_HEIGHT;
            }
            scrollHeight = currHeight;

            Widgets.EndScrollView();
            GUI.EndGroup();
        }

        public override Vector2 RequestedTabSize
        {
            get
            {
                return new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            var map = Find.CurrentMap;

            if (map != null)
            {
                var wealthWatcher = map.wealthWatcher;

                GUI.BeginGroup(inRect);

                var rowRect = new Rect(MARGIN_SIZE, MARGIN_SIZE, WINDOW_WIDTH, LABEL_HEIGHT);
                Text.Font = GameFont.Medium;
                Widgets.Label(rowRect, "RimMisc_WealthTab_Title".Translate(wealthWatcher.WealthItems, wealthWatcher.WealthBuildings, wealthWatcher.WealthFloorsOnly, wealthWatcher.WealthPawns, wealthWatcher.WealthTotal));
                rowRect.y += LABEL_HEIGHT * 2;

                if (Find.TickManager.TicksGame - lastUpdateTick > UPDATE_INTERVAL_TICKS)
                {
                    CalculateItemWealth();
                    //CalculateBuildingWealth();
                    //CalculateFloorWealth();
                    //CalculateCreatureWealth();
                    lastUpdateTick = Find.TickManager.TicksGame;
                }

                Text.Font = GameFont.Small;
                DrawItemWealth(inRect, rowRect);

                GUI.EndGroup();
            }

            base.DoWindowContents(inRect);
        }
    }
}
