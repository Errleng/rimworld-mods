using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;

namespace RimMisc
{
    internal class MainTabWindow_ItemInfoTab : MainTabWindow
    {
        private enum ItemType
        {
            Stuff,
            RangedWeapons,
            MeleeWeapons,
            Apparel
        }

        private static readonly float WINDOW_WIDTH = 1200;
        private static readonly float WINDOW_HEIGHT = 800;
        private static readonly float MARGIN_SIZE = 50;
        private static readonly float LABEL_HEIGHT = 50;
        private static readonly float LABEL_WIDTH = 60;

        private static readonly Dictionary<string, Func<ThingDef, string>> STUFF_INFOS = new Dictionary<string, Func<ThingDef, string>> {
                { "Name", x => x.LabelCap.ToString() },
                { "Categories", x => string.Join(", ", x.stuffProps.categories) },
                { "Market value", x => x.GetStatValueAbstract(StatDefOf.MarketValue).ToString() },
                { "Mass", x => x.GetStatValueAbstract(StatDefOf.Mass).ToStringMass()},
                { "Max HP", x => x.stuffProps.statFactors.GetStatFactorFromList(StatDefOf.MaxHitPoints).ToStringPercent()},
                { "Flammability", x => x.stuffProps.statFactors.GetStatFactorFromList(StatDefOf.Flammability).ToStringPercent()},
                { "Beauty", x => x.stuffProps.statFactors.GetStatFactorFromList(StatDefOf.Beauty).ToStringPercent()},
                { "Armor - sharp", x => x.GetStatValueAbstract(StatDefOf.StuffPower_Armor_Sharp).ToStringPercent() },
                { "Armor - blunt", x => x.GetStatValueAbstract(StatDefOf.StuffPower_Armor_Blunt).ToStringPercent()},
                { "Armor - heat", x => x.GetStatValueAbstract(StatDefOf.StuffPower_Armor_Heat).ToStringPercent()},
                { "Insulation - cold", x => x.GetStatValueAbstract(StatDefOf.StuffPower_Insulation_Cold).ToStringTemperatureOffset()},
                { "Insulation - heat", x => x.GetStatValueAbstract(StatDefOf.StuffPower_Insulation_Heat).ToStringTemperatureOffset()},
                { "Sharp damage", x => x.GetStatValueAbstract(StatDefOf.SharpDamageMultiplier).ToStringPercent()},
                { "Blunt damage", x => x.GetStatValueAbstract(StatDefOf.BluntDamageMultiplier).ToStringPercent()},
                { "Melee cooldown", x => x.stuffProps.statFactors.GetStatFactorFromList(StatDefOf.MeleeWeapon_CooldownMultiplier).ToStringPercent()},
                { "Door speed", x => x.stuffProps.statFactors.GetStatFactorFromList(StatDefOf.DoorOpenSpeed).ToStringPercent()},
            };
        private static readonly Dictionary<string, Func<ThingDef, string>> RANGED_WEAPON_INFOS = new Dictionary<string, Func<ThingDef, string>> {
                { "Name", x => x.LabelCap.ToString() },
                { "Market value", x => x.GetStatValueAbstract(StatDefOf.MarketValue).ToString() },
                { "Damage", x =>  DebugOutputGeneralMethod("damage", new Type[]{ typeof(ThingDef) }, x).ToString()},
                { "Armor penetration", x => ((float)DebugOutputGeneralMethod("armorPenetration", new Type[]{ typeof(ThingDef) }, x)).ToStringPercent()},
                { "Range", x => x.Verbs[0].range.ToString()},
                { "Burst", x => DebugOutputGeneralMethod("burstShots", new Type[]{ typeof(ThingDef) }, x).ToString()},
                { "Cooldown", x => DebugOutputGeneralMethod("cooldown", new Type[]{ typeof(ThingDef) }, x).ToString()},
                { "Warmup", x => DebugOutputGeneralMethod("warmup", new Type[]{ typeof(ThingDef) }, x).ToString()},
                { "Full Cycle", x =>  Math.Round((float)DebugOutputGeneralMethod("fullcycle", new Type[]{ typeof(ThingDef) }, x), 2).ToString()},
                { "DPS", x =>  Math.Round((float)DebugOutputGeneralMethod("dpsMissless", new Type[]{ typeof(ThingDef) }, x), 2).ToString()},
                { "Stopping power", x => DebugOutputGeneralMethod("stoppingPower", new Type[]{ typeof(ThingDef) }, x).ToString()},
                { "Accuracy touch", x => ((float)DebugOutputGeneralMethod("accTouch", new Type[]{ typeof(ThingDef) }, x)).ToStringPercent()},
                { "Accuracy short", x =>((float)DebugOutputGeneralMethod("accShort", new Type[]{ typeof(ThingDef) }, x)).ToStringPercent()},
                { "Accuracy medium", x => ((float)DebugOutputGeneralMethod("accMed", new Type[]{ typeof(ThingDef) }, x)).ToStringPercent()},
                { "Accuracy long", x => ((float) DebugOutputGeneralMethod("accLong", new Type[]{ typeof(ThingDef) }, x)).ToStringPercent()},
                //{ "Projectile speed", x => x.projectile != null ? x.projectile.speed.ToString() : ""},
                { "Forced miss radius", x => x.Verbs[0].ForcedMissRadius.ToString()},
            };
        private static readonly Dictionary<string, Func<ThingDef, string>> MELEE_WEAPON_INFOS = new Dictionary<string, Func<ThingDef, string>> {
                { "Name", x => x.LabelCap.ToString() },
                { "Market value", x => x.GetStatValueAbstract(StatDefOf.MarketValue).ToString() },
                { "DPS", x => x.GetStatValueAbstract(StatDefOf.MeleeWeapon_AverageDPS).ToString()},
                { "Armor Penetration", x => x.GetStatValueAbstract(StatDefOf.MeleeWeapon_AverageArmorPenetration).ToStringPercent() },
            };
        private static readonly Dictionary<string, Func<ThingDef, string>> APPAREL_INFOS = new Dictionary<string, Func<ThingDef, string>> {
                { "Name", x => x.LabelCap.ToString() },
                { "Layer", x => string.Join(", ", x.apparel.layers.Select((ApparelLayerDef l) => l.ToString())) },
                { "Market value", x => x.GetStatValueAbstract(StatDefOf.MarketValue).ToString() },
                { "Armor - sharp", x => x.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp).ToStringPercent() },
                { "Armor - blunt", x => x.GetStatValueAbstract(StatDefOf.ArmorRating_Blunt).ToStringPercent()},
                { "Armor - heat", x => x.GetStatValueAbstract(StatDefOf.ArmorRating_Heat).ToStringPercent()},
                { "Insulation - cold", x => x.GetStatValueAbstract(StatDefOf.Insulation_Cold).ToStringTemperatureOffset()},
                { "Insulation - heat", x => x.GetStatValueAbstract(StatDefOf.Insulation_Heat).ToStringTemperatureOffset()},
                { "Global work speed", x => x.equippedStatOffsets?.Find(y => y.stat.Equals(StatDefOf.WorkSpeedGlobal))?.ValueToStringAsOffset ?? "0"},
                { "Move speed", x => x.equippedStatOffsets?.Find(y => y.stat.Equals(StatDefOf.MoveSpeed))?.ValueToStringAsOffset ?? "0"},
                { "General labor speed", x => x.equippedStatOffsets?.Find(y => y.stat.Equals(StatDefOf.GeneralLaborSpeed))?.ValueToStringAsOffset ?? "0"},
                { "Mental break threshold", x => x.equippedStatOffsets?.Find(y => y.stat.Equals(StatDefOf.MentalBreakThreshold))?.ValueToStringAsOffset ?? "0"},
            };

        private float scrollHeight;
        private float scrollWidth;
        private Vector2 scrollPosition;

        private List<ThingDef> stuff;
        private List<ThingDef> rangedWeapons;
        private List<ThingDef> meleeWeapons;
        private List<ThingDef> apparel;


        private ItemType itemTypeToDisplay;
        private string sortColumn;
        private bool sortAsc;

        public MainTabWindow_ItemInfoTab()
        {
            draggable = false;
            resizeable = false;
            sortColumn = "";
            sortAsc = true;
            scrollHeight = WINDOW_HEIGHT;
            scrollWidth = WINDOW_WIDTH;
            UpdateItemLists();
        }

        public override Vector2 RequestedTabSize => new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);

        private static object DebugOutputGeneralMethod(string methodName, Type[] argTypes, params object[] args)
        {
            return Traverse.Create(typeof(DebugOutputsGeneral)).Method(methodName, argTypes).GetValue(args);
        }

        private void UpdateItemLists()
        {
            stuff = DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.IsStuff).OrderBy(x => x.label).ToList();
            rangedWeapons = DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.IsRangedWeapon).OrderBy(x => x.label).ToList();
            meleeWeapons = DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.IsMeleeWeapon).OrderBy(x => x.label).ToList();
            apparel = DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.IsApparel).OrderBy(x => x.label).ToList();
        }

        private void DrawInfos(Rect rowRect, Dictionary<string, Func<ThingDef, string>> columnInfos, ref List<ThingDef> items)
        {
            // columns headers
            var columnNameRect = new Rect(rowRect) { width = LABEL_WIDTH };
            columnNameRect.x += LABEL_WIDTH;
            columnNameRect.height *= 2;
            foreach (var name in columnInfos.Keys)
            {
                if (sortColumn == name)
                {
                    Widgets.Label(columnNameRect, (sortAsc ? "+ " : "- ") + name);
                }
                else
                {
                    Widgets.Label(columnNameRect, name);
                }

                if (Mouse.IsOver(columnNameRect))
                {
                    GUI.DrawTexture(columnNameRect, TexUI.HighlightTex);
                }
                if (Widgets.ButtonInvisible(columnNameRect))
                {
                    if (sortColumn == name)
                    {
                        sortAsc = !sortAsc;
                    }
                    else
                    {
                        sortAsc = true;
                    }
                    sortColumn = name;

                    IOrderedEnumerable<ThingDef> sortedItems = null;
                    if (sortAsc)
                    {
                        sortedItems = items.OrderBy(x => columnInfos[sortColumn](x), new SemiNumericComparer());
                    }
                    else
                    {
                        sortedItems = items.OrderByDescending(x => columnInfos[sortColumn](x), new SemiNumericComparer());
                    }
                    sortedItems = sortedItems.ThenBy(x => x.label);
                    items = sortedItems.ToList();
                }
                columnNameRect.x += LABEL_WIDTH;
            }
            rowRect.y += columnNameRect.height;
            float currHeight = rowRect.y;

            for (int i = 0; i < items.Count; i++)
            {
                var itemDef = items[i];

                var iconRect = new Rect(rowRect.x, rowRect.y, 30, 30);
                Widgets.ThingIcon(iconRect, itemDef);
                Widgets.InfoCardButton(rowRect.x + iconRect.width, rowRect.y, itemDef, GenStuff.DefaultStuffFor(itemDef));

                var infoRect = new Rect(rowRect) { width = LABEL_WIDTH };
                infoRect.x += LABEL_WIDTH;
                foreach (var info in columnInfos)
                {
                    try
                    {
                        Widgets.Label(infoRect, info.Value(itemDef));
                        infoRect.x += LABEL_WIDTH;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error occurred while getting item {itemDef.defName} info {info.Key}");
                        throw ex;
                    }
                }
                rowRect.y += LABEL_HEIGHT;
                currHeight += LABEL_HEIGHT;
            }

            scrollHeight = currHeight;
            scrollWidth = 2 * MARGIN_SIZE + LABEL_WIDTH * (columnInfos.Count + 2);
        }

        public override void DoWindowContents(Rect inRect)
        {
            var map = Find.CurrentMap;

            if (map != null)
            {
                GUI.BeginGroup(inRect);

                var rowRect = new Rect(MARGIN_SIZE, MARGIN_SIZE, WINDOW_WIDTH - MARGIN_SIZE, LABEL_HEIGHT);
                Text.Font = GameFont.Medium;

                if (Widgets.ButtonText(rowRect, "RimMisc_ItemTab_ItemTypeButton".Translate(itemTypeToDisplay)))
                {
                    var itemTypeOptions = new List<FloatMenuOption>();
                    foreach (ItemType itemType in Enum.GetValues(typeof(ItemType)))
                    {
                        itemTypeOptions.Add(new FloatMenuOption(itemType.ToString(),
                            () =>
                            {
                                itemTypeToDisplay = itemType;
                                UpdateItemLists();
                                scrollPosition = new Vector2(0, 0);
                            }));
                    }

                    Find.WindowStack.Add(new FloatMenu(itemTypeOptions));
                }

                rowRect.y += LABEL_HEIGHT;

                var outRect = new Rect(0, rowRect.y, inRect.width - MARGIN_SIZE, inRect.height - rowRect.y);
                var viewRect = new Rect(0, 0, scrollWidth, scrollHeight);
                Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

                Text.Font = GameFont.Small;
                switch (itemTypeToDisplay)
                {
                    case ItemType.Stuff:
                        DrawInfos(rowRect, STUFF_INFOS, ref stuff);
                        break;
                    case ItemType.RangedWeapons:
                        DrawInfos(rowRect, RANGED_WEAPON_INFOS, ref rangedWeapons);
                        break;
                    case ItemType.MeleeWeapons:
                        DrawInfos(rowRect, MELEE_WEAPON_INFOS, ref meleeWeapons);
                        break;
                    case ItemType.Apparel:
                        DrawInfos(rowRect, APPAREL_INFOS, ref apparel);
                        break;
                }

                Widgets.EndScrollView();

                GUI.EndGroup();
            }
        }
    }

    class SemiNumericComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            var regex = new Regex(@"^([-+]?[0-9]*\.?[0-9]+)");

            // run the regex on both strings
            var xRegexResult = regex.Match(x);
            var yRegexResult = regex.Match(y);

            // check if they are both numbers
            if (xRegexResult.Success && yRegexResult.Success)
            {
                return float.Parse(xRegexResult.Groups[1].Value).CompareTo(float.Parse(yRegexResult.Groups[1].Value));
            }

            // otherwise return as string comparison
            return x.CompareTo(y);
        }
    }
}