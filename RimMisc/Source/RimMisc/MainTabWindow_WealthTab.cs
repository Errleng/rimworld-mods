using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimMisc
{
    internal class MainTabWindow_WealthTab : MainTabWindow
    {
        private static readonly double FLOATING_POINT_TOLERANCE = 0.01;
        private static readonly float WINDOW_WIDTH = 1200;
        private static readonly float WINDOW_HEIGHT = 800;
        private static readonly float MARGIN_SIZE = 50;
        private static readonly float LABEL_HEIGHT = 25;
        private static readonly float UPDATE_INTERVAL_TICKS = GenTicks.SecondsToTicks(10);
        private List<WealthRecord> buildingWealths;
        private List<WealthRecord> creatureWealths;
        private Dictionary<TerrainDef, TerrainWealthRecord> floorWealths;
        private List<WealthRecord> itemWealths;
        private float lastUpdateTick;
        private float scrollHeight;

        private Vector2 scrollPosition;

        private WealthType wealthTypeToDraw;

        public MainTabWindow_WealthTab()
        {
            draggable = true;
            resizeable = false;
            //forcePause = true;
            scrollHeight = WINDOW_HEIGHT;
        }

        public override Vector2 RequestedTabSize => new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);

        private bool IsThingItem(IThingHolder thingHolder)
        {
            if (thingHolder is PassingShip || thingHolder is MapComponent)
            {
                return false;
            }

            var pawn = thingHolder as Pawn;
            return (pawn == null || pawn.Faction == Faction.OfPlayer) && (pawn == null || !pawn.IsQuestLodger());
        }

        private void CalculateItemWealth()
        {
            itemWealths = new List<WealthRecord>();

            var map = Find.CurrentMap;
            var items = new List<Thing>();
            ThingOwnerUtility.GetAllThingsRecursively(map, ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), items, false, IsThingItem);
            //Log.Message($"Initial items: {string.Join(",", items.Select(item => item.Label))}");

            foreach (var item in items)
            {
                if (item.SpawnedOrAnyParentSpawned && item.MarketValue > 0 && !item.PositionHeld.Fogged(map))
                {
                    if (itemWealths.Count == 0)
                    {
                        itemWealths.Add(new WealthRecord(item));
                    }
                    else
                    {
                        var foundRecord = false;
                        foreach (var wealthRecord in itemWealths)
                        {
                            if (foundRecord)
                            {
                                break;
                            }

                            var uniqueItem = wealthRecord.things[0];
                            if (item.GetCustomLabelNoCount(false) == uniqueItem.GetCustomLabelNoCount(false) && Math.Abs(item.MarketValue - uniqueItem.MarketValue) < FLOATING_POINT_TOLERANCE || item.CanStackWith(uniqueItem))
                            {
                                //Log.Message($"Check {item.Label}, {uniqueItem.Label}, {item.CanStackWith(uniqueItem)}, {(itemCounts.ContainsKey(uniqueItem.LabelNoCount) ? itemCounts[uniqueItem.LabelNoCount].ToString() : "None")}");
                                wealthRecord.AddThing(item);
                                foundRecord = true;
                            }
                        }

                        if (!foundRecord)
                            //Log.Message($"Add {item.Label}, {itemCounts.ContainsKey(item.LabelNoCount)}");
                        {
                            itemWealths.Add(new WealthRecord(item));
                        }
                    }
                }
            }

            Log.Message($"Total item wealth: {itemWealths.Sum(record => record.totalWealth)}");
        }

        private void CalculateBuildingWealth()
        {
            buildingWealths = new List<WealthRecord>();

            var map = Find.CurrentMap;
            var buildings = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);

            foreach (var building in buildings)
            {
                if (building.GetStatValue(StatDefOf.MarketValueIgnoreHp) > 0 && building.Faction == Faction.OfPlayer)
                {
                    if (buildingWealths.Count == 0)
                    {
                        buildingWealths.Add(new WealthRecord(building));
                    }
                    else
                    {
                        var foundRecord = false;
                        foreach (var wealthRecord in buildingWealths)
                        {
                            if (foundRecord)
                            {
                                break;
                            }

                            var existingBuilding = wealthRecord.things[0];
                            if (building.def == existingBuilding.def)
                            {
                                wealthRecord.AddThing(building);
                                foundRecord = true;
                            }
                        }

                        if (!foundRecord)
                        {
                            buildingWealths.Add(new WealthRecord(building));
                        }
                    }
                }
            }

            Log.Message($"Total building wealth: {buildingWealths.Sum(record => record.totalWealth)}");
        }

        private void CalculateFloorWealth()
        {
            floorWealths = new Dictionary<TerrainDef, TerrainWealthRecord>();

            var map = Find.CurrentMap;

            var maxIndex = -1;
            var allDefsListForReading = DefDatabase<TerrainDef>.AllDefsListForReading;
            foreach (var terrainDef in allDefsListForReading)
            {
                maxIndex = Mathf.Max(maxIndex, terrainDef.index);
            }

            var terrainMarketValue = new float[maxIndex + 1];
            foreach (var terrainDef in allDefsListForReading)
            {
                terrainMarketValue[terrainDef.index] = terrainDef.GetStatValueAbstract(StatDefOf.MarketValue);
            }

            var topGrid = map.terrainGrid.topGrid;
            var fogGrid = map.fogGrid.fogGrid;
            var size = map.Size;
            var mapSize = size.x * size.z;

            for (var i = 0; i < mapSize; i++)
            {
                if (!fogGrid[i])
                {
                    var terrainDef = topGrid[i];
                    var terrainWealth = terrainMarketValue[terrainDef.index];
                    if (terrainWealth > 0)
                    {
                        if (floorWealths.ContainsKey(terrainDef))
                        {
                            floorWealths[terrainDef].count++;
                            floorWealths[terrainDef].wealth = terrainWealth;
                        }
                        else
                        {
                            floorWealths[terrainDef] = new TerrainWealthRecord(terrainDef);
                        }
                    }
                }
            }

            Log.Message($"Total floor wealth: {floorWealths.Sum(terrainWealth => terrainWealth.Value.count * terrainWealth.Value.wealth)}");
        }

        private void CalculateCreatureWealth()
        {
            creatureWealths = new List<WealthRecord>();

            var map = Find.CurrentMap;
            foreach (var pawn in map.mapPawns.PawnsInFaction(Faction.OfPlayer))
            {
                creatureWealths.Add(new WealthRecord(pawn));
            }

            Log.Message($"Total pawn wealth: {creatureWealths.Sum(record => record.totalWealth)}");
        }

        private void DrawItemWealth(Rect rowRect)
        {
            float currHeight = 0;
            var sortedItemWealths = itemWealths.OrderByDescending(record => record.totalWealth).ToList();
            var labelRect = new Rect(rowRect) {y = 0};
            foreach (var record in sortedItemWealths)
            {
                var item = record.things[0];
                Widgets.Label(labelRect, "RimMisc_WealthTab_ItemRow".Translate(item.LabelCapNoCount, record.count, item.MarketValue, record.totalWealth));
                labelRect.y += LABEL_HEIGHT;
                currHeight += LABEL_HEIGHT;
            }

            scrollHeight = currHeight;
        }

        private void DrawBuildingWealth(Rect rowRect)
        {
            float currHeight = 0;
            var sortedBuildingWealths = buildingWealths.OrderByDescending(record => record.totalWealth).ToList();
            var labelRect = new Rect(rowRect) {y = 0};
            foreach (var record in sortedBuildingWealths)
            {
                var building = record.things[0];
                Widgets.Label(labelRect, "RimMisc_WealthTab_ItemRow".Translate(building.LabelCapNoCount, record.count, building.GetStatValue(StatDefOf.MarketValueIgnoreHp), record.totalWealth));
                labelRect.y += LABEL_HEIGHT;
                currHeight += LABEL_HEIGHT;
            }

            scrollHeight = currHeight;
        }

        private void DrawFloorWealth(Rect rowRect)
        {
            float currHeight = 0;
            var sortedFloorWealths = floorWealths.OrderByDescending(floorWealth => floorWealth.Value.count * floorWealth.Value.wealth).ToList();
            var labelRect = new Rect(rowRect) {y = 0};
            foreach (var floorWealth in sortedFloorWealths)
            {
                var terrain = floorWealth.Key;
                var record = floorWealth.Value;
                Widgets.Label(labelRect, "RimMisc_WealthTab_ItemRow".Translate(terrain.LabelCap, record.count, record.wealth, record.count * record.wealth));
                labelRect.y += LABEL_HEIGHT;
                currHeight += LABEL_HEIGHT;
            }

            scrollHeight = currHeight;
        }

        private void DrawCreatureWealth(Rect rowRect)
        {
            float currHeight = 0;
            var sortedCreatureWealths = creatureWealths.OrderByDescending(record => record.totalWealth).ToList();
            var labelRect = new Rect(rowRect) {y = 0};
            foreach (var record in sortedCreatureWealths)
            {
                var pawn = record.things[0];
                Widgets.Label(labelRect, "RimMisc_WealthTab_ItemRow".Translate(pawn.LabelCapNoCount, record.count, pawn.MarketValue, record.totalWealth));
                labelRect.y += LABEL_HEIGHT;
                currHeight += LABEL_HEIGHT;
            }

            scrollHeight = currHeight;
        }

        private void CalculateWealthForType(WealthType wealthType)
        {
            switch (wealthType)
            {
                case WealthType.Items:
                    CalculateItemWealth();
                    break;
                case WealthType.Buildings:
                    CalculateBuildingWealth();
                    break;
                case WealthType.Floors:
                    CalculateFloorWealth();
                    break;
                case WealthType.Creatures:
                    CalculateCreatureWealth();
                    break;
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            var map = Find.CurrentMap;

            if (map != null)
            {
                var wealthWatcher = map.wealthWatcher;

                GUI.BeginGroup(inRect);

                var rowRect = new Rect(MARGIN_SIZE, MARGIN_SIZE, WINDOW_WIDTH - MARGIN_SIZE, LABEL_HEIGHT);
                Text.Font = GameFont.Medium;
                Widgets.Label(rowRect, "RimMisc_WealthTab_Title".Translate(wealthWatcher.WealthItems, wealthWatcher.WealthBuildings, wealthWatcher.WealthFloorsOnly, wealthWatcher.WealthPawns, wealthWatcher.WealthTotal));
                rowRect.y += LABEL_HEIGHT;

                if (Widgets.ButtonText(rowRect, "RimMisc_WealthTab_WealthTypeButton".Translate(wealthTypeToDraw)))
                {
                    var wealthTypeOptions = new List<FloatMenuOption>();
                    foreach (WealthType wealthType in Enum.GetValues(typeof(WealthType)))
                    {
                        wealthTypeOptions.Add(new FloatMenuOption(wealthType.ToString(),
                            () =>
                            {
                                wealthTypeToDraw = wealthType;
                                CalculateWealthForType(wealthTypeToDraw);
                                scrollPosition = new Vector2(0, 0);
                            }));
                    }

                    Find.WindowStack.Add(new FloatMenu(wealthTypeOptions));
                }

                rowRect.y += LABEL_HEIGHT;

                if (Find.TickManager.TicksGame - lastUpdateTick > UPDATE_INTERVAL_TICKS)
                {
                    CalculateWealthForType(wealthTypeToDraw);
                    lastUpdateTick = Find.TickManager.TicksGame;
                }

                var outRect = new Rect(0, rowRect.y, inRect.width - MARGIN_SIZE, inRect.height - rowRect.y);
                var viewRect = new Rect(0, 0, inRect.width - MARGIN_SIZE, scrollHeight);
                Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

                Text.Font = GameFont.Small;
                switch (wealthTypeToDraw)
                {
                    case WealthType.Items:
                        DrawItemWealth(rowRect);
                        break;
                    case WealthType.Buildings:
                        DrawBuildingWealth(rowRect);
                        break;
                    case WealthType.Floors:
                        DrawFloorWealth(rowRect);
                        break;
                    case WealthType.Creatures:
                        DrawCreatureWealth(rowRect);
                        break;
                }

                Widgets.EndScrollView();

                GUI.EndGroup();
            }
        }

        private enum WealthType
        {
            Items,
            Buildings,
            Floors,
            Creatures
        }
    }

    internal class WealthRecord
    {
        public int count;
        public List<Thing> things;
        public float totalWealth;

        public WealthRecord(Thing thing)
        {
            things = new List<Thing>();
            AddThing(thing);
        }

        public void AddThing(Thing newThing)
        {
            things.Add(newThing);

            switch (newThing.def.category)
            {
                case ThingCategory.Item:
                    count += newThing.stackCount;
                    totalWealth += newThing.MarketValue * newThing.stackCount;
                    break;
                case ThingCategory.Building:
                    count++;
                    totalWealth += newThing.GetStatValue(StatDefOf.MarketValueIgnoreHp);
                    break;
                case ThingCategory.Pawn:
                    count++;
                    totalWealth += newThing.MarketValue;
                    break;
            }
        }
    }

    internal class TerrainWealthRecord
    {
        public int count;
        public TerrainDef terrain;
        public float wealth;

        public TerrainWealthRecord(TerrainDef terrain)
        {
            this.terrain = terrain;
        }
    }
}