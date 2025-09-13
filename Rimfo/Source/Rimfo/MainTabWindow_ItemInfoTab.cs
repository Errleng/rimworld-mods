using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;

namespace Rimfo
{
    internal class MainTabWindow_ItemInfoTab : MainTabWindow
    {
        private enum ItemType
        {
            Stuff,
            RangedWeapons,
            RangedWeaponsCraftable,
            MeleeWeapons,
            MeleeWeaponsCraftable,
            Apparel,
            ApparelCraftable,
            Ingestible,
        }

        private static readonly float WINDOW_WIDTH = 1200;
        private static readonly float WINDOW_HEIGHT = 800;
        private static readonly float MARGIN_SIZE = 50;
        private static readonly float LABEL_HEIGHT = 50;
        private static readonly float LABEL_WIDTH = 60;

        private static readonly Dictionary<string, Func<ThingDef, string>> STUFF_INFOS = new Dictionary<string, Func<ThingDef, string>> {
                { "Name", x => x.LabelCap.ToString() },
                { "Categories", x => string.Join(", ", x.stuffProps.categories) },
                { "Market value", x => RoundNum(x.GetStatValueAbstract(StatDefOf.MarketValue)).ToString() },
                { "Mass", x => RoundNum(x.GetStatValueAbstract(StatDefOf.Mass)).ToStringMass()},
                { "Max HP", x => x.stuffProps.statFactors.GetStatFactorFromList(StatDefOf.MaxHitPoints).ToStringPercent()},
                { "Flammability", x => x.stuffProps.statFactors.GetStatFactorFromList(StatDefOf.Flammability).ToStringPercent()},
                { "Beauty", x => x.stuffProps.statFactors.GetStatFactorFromList(StatDefOf.Beauty).ToStringPercent()},
                { "Armor - sharp", x => RoundNum(x.GetStatValueAbstract(StatDefOf.StuffPower_Armor_Sharp)).ToStringPercent() },
                { "Armor - blunt", x => RoundNum(x.GetStatValueAbstract(StatDefOf.StuffPower_Armor_Blunt)).ToStringPercent()},
                { "Armor - heat", x => RoundNum(x.GetStatValueAbstract(StatDefOf.StuffPower_Armor_Heat)).ToStringPercent()},
                { "Insulation - cold", x => RoundNum(x.GetStatValueAbstract(StatDefOf.StuffPower_Insulation_Cold)).ToStringTemperatureOffset()},
                { "Insulation - heat", x => RoundNum(x.GetStatValueAbstract(StatDefOf.StuffPower_Insulation_Heat)).ToStringTemperatureOffset()},
                { "Sharp damage", x => RoundNum(x.GetStatValueAbstract(StatDefOf.SharpDamageMultiplier)).ToStringPercent()},
                { "Blunt damage", x => RoundNum(x.GetStatValueAbstract(StatDefOf.BluntDamageMultiplier)).ToStringPercent()},
                { "Melee cooldown", x => x.stuffProps.statFactors.GetStatFactorFromList(StatDefOf.MeleeWeapon_CooldownMultiplier).ToStringPercent()},
                { "Door speed", x => x.stuffProps.statFactors.GetStatFactorFromList(StatDefOf.DoorOpenSpeed).ToStringPercent()},
            };
        private static readonly Dictionary<string, Func<ThingDef, string>> RANGED_WEAPON_INFOS = new Dictionary<string, Func<ThingDef, string>> {
                { "Name", x => x.LabelCap.ToString() },
                { "Market value", x => RoundNum(x.GetStatValueAbstract(StatDefOf.MarketValue, GenStuff.DefaultStuffFor(x))).ToString() },
                { "Damage", x =>  DebugOutputGeneralMethod("damage", new Type[]{ typeof(ThingDef) }, x).ToString()},
                { "Armor penetration", x => RoundNum((float) DebugOutputGeneralMethod("armorPenetration", new Type[] { typeof(ThingDef) }, x)).ToStringPercent()},
                { "Range", x => x.Verbs[0].range.ToString()},
                { "Burst", x => DebugOutputGeneralMethod("burstShots", new Type[]{ typeof(ThingDef) }, x).ToString()},
                { "Cooldown", x => DebugOutputGeneralMethod("cooldown", new Type[]{ typeof(ThingDef) }, x).ToString()},
                { "Warmup", x => DebugOutputGeneralMethod("warmup", new Type[]{ typeof(ThingDef) }, x).ToString()},
                { "Full Cycle", x =>  RoundNum((float)DebugOutputGeneralMethod("fullcycle", new Type[]{ typeof(ThingDef) }, x)).ToString()},
                { "DPS", x =>  RoundNum((float)DebugOutputGeneralMethod("dpsMissless", new Type[]{ typeof(ThingDef) }, x)).ToString()},
                { "Stopping power", x => DebugOutputGeneralMethod("stoppingPower", new Type[]{ typeof(ThingDef) }, x).ToString()},
                { "Accuracy touch", x => RoundNum((float)DebugOutputGeneralMethod("accTouch", new Type[]{ typeof(ThingDef) }, x)).ToStringPercent()},
                { "Accuracy short", x => RoundNum((float)DebugOutputGeneralMethod("accShort", new Type[]{ typeof(ThingDef) }, x)).ToStringPercent()},
                { "Accuracy medium", x => RoundNum((float) DebugOutputGeneralMethod("accMed", new Type[] { typeof(ThingDef) }, x)).ToStringPercent()},
                { "Accuracy long", x => RoundNum((float) DebugOutputGeneralMethod("accLong", new Type[] { typeof(ThingDef) }, x)).ToStringPercent()},
                //{ "Projectile speed", x => x.projectile != null ? x.projectile.speed.ToString() : ""},
                { "Forced miss radius", x => x.Verbs[0].ForcedMissRadius.ToString()},
            };
        private static readonly Dictionary<string, Func<ThingDef, string>> MELEE_WEAPON_INFOS = new Dictionary<string, Func<ThingDef, string>> {
                { "Name", x => x.LabelCap.ToString() },
                { "Market value", x => RoundNum(x.GetStatValueAbstract(StatDefOf.MarketValue, GenStuff.DefaultStuffFor(x))).ToString() },
                { "DPS", x => RoundNum(x.GetStatValueAbstract(StatDefOf.MeleeWeapon_AverageDPS, GenStuff.DefaultStuffFor(x))).ToString()},
                { "Armor Penetration", x => RoundNum(x.GetStatValueAbstract(RimfoDefOf.MeleeWeapon_AverageArmorPenetration, GenStuff.DefaultStuffFor(x))).ToStringPercent() },
            };
        private static readonly Dictionary<string, Func<ThingDef, string>> APPAREL_INFOS = new Dictionary<string, Func<ThingDef, string>> {
                { "Name", x => x.LabelCap.ToString() },
                { "Layer", x => string.Join(", ", x.apparel.layers.Select((ApparelLayerDef l) => l.LabelCap)) },
                { "Market value", x => RoundNum(x.GetStatValueAbstract(StatDefOf.MarketValue, GenStuff.DefaultStuffFor(x))).ToString() },
                { "Armor - sharp", x => RoundNum(x.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp, GenStuff.DefaultStuffFor(x))).ToStringPercent() },
                { "Armor - blunt", x => RoundNum(x.GetStatValueAbstract(StatDefOf.ArmorRating_Blunt, GenStuff.DefaultStuffFor(x))).ToStringPercent()},
                { "Armor - heat", x => RoundNum(x.GetStatValueAbstract(StatDefOf.ArmorRating_Heat, GenStuff.DefaultStuffFor(x))).ToStringPercent()},
                { "Insulation - cold", x => RoundNum(x.GetStatValueAbstract(StatDefOf.Insulation_Cold, GenStuff.DefaultStuffFor(x))).ToStringTemperatureOffset()},
                { "Insulation - heat", x => RoundNum(x.GetStatValueAbstract(StatDefOf.Insulation_Heat, GenStuff.DefaultStuffFor(x))).ToStringTemperatureOffset()},
                { "Consciousness", x => RoundNum(Utils.GetApparelHediffCapacityOffset(x, PawnCapacityDefOf.Consciousness)).ToString()},
                { "Manipulation", x => RoundNum(Utils.GetApparelHediffCapacityOffset(x, PawnCapacityDefOf.Manipulation)).ToString()},
                { "Moving", x => RoundNum(Utils.GetApparelHediffCapacityOffset(x, PawnCapacityDefOf.Moving)).ToString() },
                { "Global work speed", x => x.equippedStatOffsets?.Find(y => y.stat.Equals(StatDefOf.WorkSpeedGlobal))?.ValueToStringAsOffset ?? "0"},
                { "General labor speed", x => x.equippedStatOffsets?.Find(y => y.stat.Equals(StatDefOf.GeneralLaborSpeed))?.ValueToStringAsOffset ?? "0"},
                { "Move speed", x => x.equippedStatOffsets?.Find(y => y.stat.Equals(StatDefOf.MoveSpeed))?.ValueToStringAsOffset ?? "0"},
                { "Mental break threshold", x => x.equippedStatOffsets?.Find(y => y.stat.Equals(StatDefOf.MentalBreakThreshold))?.ValueToStringAsOffset ?? "0"},
            };
        private static readonly Dictionary<string, Func<ThingDef, string>> INGESTIBLE_INFOS = new Dictionary<string, Func<ThingDef, string>> {
                { "Name", x => x.LabelCap.ToString() },
                { "Mood", x => RoundNum(Utils.GetIngestibleMoodOffset(x.ingestible)).ToString()},
                { "Consciousness", x => RoundNum(Utils.GetIngestibleHediffCapacityOffset(x.ingestible, PawnCapacityDefOf.Consciousness)).ToString() },
                { "Manipulation", x => RoundNum(Utils.GetIngestibleHediffCapacityOffset(x.ingestible, PawnCapacityDefOf.Manipulation)).ToString() },
                { "Moving", x => RoundNum(Utils.GetIngestibleHediffCapacityOffset(x.ingestible, PawnCapacityDefOf.Moving)).ToString() },
                { "Global work speed", x => RoundNum(Utils.GetIngestibleHediffStatOffset(x.ingestible, StatDefOf.WorkSpeedGlobal)).ToString() },
                { "General labor speed", x => RoundNum(Utils.GetIngestibleHediffStatOffset(x.ingestible, StatDefOf.GeneralLaborSpeed)).ToString() },
                { "Move speed", x => RoundNum(Utils.GetIngestibleHediffStatOffset(x.ingestible, StatDefOf.MoveSpeed)).ToString() },
                { "Mental break threshold", x => RoundNum(Utils.GetIngestibleHediffStatOffset(x.ingestible, StatDefOf.MentalBreakThreshold)).ToString() },
            };

        private float scrollHeight;
        private float scrollWidth;
        private Vector2 scrollPosition;

        private List<ThingDef> stuff;
        private List<ThingDef> rangedWeapons;
        private List<ThingDef> rangedWeaponsCraftable;
        private List<ThingDef> meleeWeapons;
        private List<ThingDef> meleeWeaponsCraftable;
        private List<ThingDef> apparel;
        private List<ThingDef> apparelCraftable;
        private List<ThingDef> ingestible;


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

        private static float RoundNum(float num)
        {
            return (float)Math.Round(num, 3);
        }

        private void UpdateItemLists()
        {
            Func<ThingDef, bool> isCraftable = (ThingDef x) => DefDatabase<RecipeDef>.AllDefs.Any(r => r.products.Any(p => p.thingDef == x));
            stuff = DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.IsStuff).OrderBy(x => x.label).ToList();
            rangedWeapons = DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.IsRangedWeapon).OrderBy(x => x.label).ToList();
            rangedWeaponsCraftable = rangedWeapons.Where(x => isCraftable(x)).ToList();
            meleeWeapons = DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.IsMeleeWeapon).OrderBy(x => x.label).ToList();
            meleeWeaponsCraftable = meleeWeapons.Where(x => isCraftable(x)).ToList();
            apparel = DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.IsApparel).OrderBy(x => x.label).ToList();
            apparelCraftable = apparel.Where(x => isCraftable(x)).ToList();
            ingestible = DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.IsIngestible).OrderBy(x => x.label).ToList();
        }

        private void DrawInfos(Rect outRect, Rect rowRect, Dictionary<string, Func<ThingDef, string>> columnInfos, ref List<ThingDef> items)
        {
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
                bool visible = rowRect.y >= scrollPosition.y && rowRect.y + rowRect.height <= scrollPosition.y + outRect.height;
                if (visible)
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

                if (Widgets.ButtonText(rowRect, "Rimfo_ItemTab_ItemTypeButton".Translate(itemTypeToDisplay)))
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
                        DrawInfos(outRect, rowRect, STUFF_INFOS, ref stuff);
                        break;
                    case ItemType.RangedWeapons:
                        DrawInfos(outRect, rowRect, RANGED_WEAPON_INFOS, ref rangedWeapons);
                        break;
                    case ItemType.RangedWeaponsCraftable:
                        DrawInfos(outRect, rowRect, RANGED_WEAPON_INFOS, ref rangedWeaponsCraftable);
                        break;
                    case ItemType.MeleeWeapons:
                        DrawInfos(outRect, rowRect, MELEE_WEAPON_INFOS, ref meleeWeapons);
                        break;
                    case ItemType.MeleeWeaponsCraftable:
                        DrawInfos(outRect, rowRect, MELEE_WEAPON_INFOS, ref meleeWeaponsCraftable);
                        break;
                    case ItemType.Apparel:
                        DrawInfos(outRect, rowRect, APPAREL_INFOS, ref apparel);
                        break;
                    case ItemType.ApparelCraftable:
                        DrawInfos(outRect, rowRect, APPAREL_INFOS, ref apparelCraftable);
                        break;
                    case ItemType.Ingestible:
                        DrawInfos(outRect, rowRect, INGESTIBLE_INFOS, ref ingestible);
                        break;
                }

                Widgets.EndScrollView();

                GUI.EndGroup();
            }
        }
    }
}