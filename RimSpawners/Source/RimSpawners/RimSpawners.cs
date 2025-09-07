using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimSpawners
{
    [StaticConstructorOnStartup]
    internal class Loader
    {
        static Loader()
        {
            RimSpawners.settings.ApplySettings();
        }
    }

    internal class RimSpawners : Mod
    {
        private static readonly float SEARCH_RESULT_ROW_HEIGHT = 30f;
        private static readonly float SCROLLBAR_WIDTH = 20f;
        private static readonly float ICON_WIDTH = 30f;
        private static readonly float LABEL_WIDTH = 200f;
        private static readonly float BUTTON_WIDTH = 60f;

        public static readonly string modName = "RimSpawners";
        public static RimSpawnersSettings settings;
        private Vector2 scrollPos = new Vector2(0, 0);
        private Vector2 weaponScrollPos = new Vector2(0, 0);
        private Vector2 apparelScrollPos = new Vector2(0, 0);
        private Vector2 selectedWeaponScrollPos = new Vector2(0, 0);
        private Vector2 selectedApparelScrollPos = new Vector2(0, 0);
        private float weaponScrollHeight;
        private float apparelScrollHeight;
        private float selectedWeaponScrollHeight;
        private float selectedApparelScrollHeight;
        private string weaponSearchKeyword = "";
        private string apparelSearchKeyword = "";

        public static FactionDef spawnedPawnFactionDef;
        public static Faction spawnedPawnFaction;
        public static System.Random rng;

        public RimSpawners(ModContentPack content) : base(content)
        {
            settings = GetSettings<RimSpawnersSettings>();
            var harmony = new Harmony("com.rimspawners.rimworld.mod");
            harmony.PatchAll();
            rng = new System.Random();
            Log.Message("RimSpawners loaded");
        }

        public static void LogMessage(string message)
        {
            Log.Message($"[{modName}] {message}");
        }

        public static void LogError(string message)
        {
            Log.Error($"[{modName}] {message}");
        }

        private void TextFieldNumericLabeled(Listing_Standard listingStandard, string label, ref float value, float min = RimSpawnersSettings.MIN_VALUE, float max = RimSpawnersSettings.MAX_VALUE)
        {
            string buffer = null;
            listingStandard.TextFieldNumericLabeled(label, ref value, ref buffer, min, max);
        }

        private void DrawSelectedItemsSection(string title, HashSet<string> selectedItems, ref Vector2 scrollPos, ref float scrollHeight, float sectionHeight, Rect sectionRect)
        {
            GUI.BeginGroup(sectionRect);
            var titleRect = new Rect(0, 0, LABEL_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            Widgets.Label(titleRect, title);

            var outRect = new Rect(0, SEARCH_RESULT_ROW_HEIGHT, sectionRect.width, sectionHeight - SEARCH_RESULT_ROW_HEIGHT);
            var viewRect = new Rect(0, 0, sectionRect.width - SCROLLBAR_WIDTH, scrollHeight);
            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);

            float currY = 0;
            foreach (var defName in selectedItems.ToList())
            {
                var def = DefDatabase<ThingDef>.GetNamed(defName);
                var rowRect = new Rect(0, currY, viewRect.width, SEARCH_RESULT_ROW_HEIGHT);
                
                var iconRect = new Rect(0, currY, ICON_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
                var labelRect = new Rect(ICON_WIDTH, currY, LABEL_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
                var buttonRect = new Rect(sectionRect.width - BUTTON_WIDTH - SCROLLBAR_WIDTH, currY, BUTTON_WIDTH, SEARCH_RESULT_ROW_HEIGHT);

                Widgets.ThingIcon(iconRect, def);
                Widgets.Label(labelRect, def.label);
                
                if (Widgets.ButtonText(buttonRect, "Remove"))
                {
                    selectedItems.Remove(defName);
                }

                currY += SEARCH_RESULT_ROW_HEIGHT;
            }

            scrollHeight = currY;
            Widgets.EndScrollView();
            GUI.EndGroup();
        }

        private void DrawItemSelectSection(string title, string filter, HashSet<string> selectedItems, string searchKeyword, ref Vector2 scrollPos, ref float scrollHeight, float sectionHeight, Rect sectionRect, ref string newSearchKeyword)
        {
            GUI.BeginGroup(sectionRect);
            var titleRect = new Rect(0, 0, LABEL_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            Widgets.Label(titleRect, title);

            var searchRect = new Rect(0, SEARCH_RESULT_ROW_HEIGHT, sectionRect.width - SCROLLBAR_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
            newSearchKeyword = Widgets.TextField(searchRect, searchKeyword);

            var outRect = new Rect(0, searchRect.y + searchRect.height, sectionRect.width, sectionHeight - (searchRect.y + searchRect.height));
            var viewRect = new Rect(0, 0, sectionRect.width - SCROLLBAR_WIDTH, scrollHeight);
            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);

            float currY = 0;
            string trimmedSearch = searchKeyword?.Trim() ?? "";
            var items = DefDatabase<ThingDef>.AllDefs.Where(d => 
                !selectedItems.Contains(d.defName) && 
                d.IsWeapon == (filter == "Weapons") && 
                (filter != "Weapons" || d.equipmentType == EquipmentType.Primary) &&
                (filter != "Apparel" || d.IsApparel) &&
                (string.IsNullOrEmpty(trimmedSearch) || d.label.IndexOf(trimmedSearch, System.StringComparison.OrdinalIgnoreCase) >= 0));

            foreach (var def in items)
            {
                var rowRect = new Rect(0, currY, viewRect.width, SEARCH_RESULT_ROW_HEIGHT);
                
                var iconRect = new Rect(0, currY, ICON_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
                var labelRect = new Rect(ICON_WIDTH, currY, LABEL_WIDTH, SEARCH_RESULT_ROW_HEIGHT);
                var buttonRect = new Rect(sectionRect.width - BUTTON_WIDTH - SCROLLBAR_WIDTH, currY, BUTTON_WIDTH, SEARCH_RESULT_ROW_HEIGHT);

                Widgets.ThingIcon(iconRect, def);
                Widgets.Label(labelRect, def.label);
                
                if (Widgets.ButtonText(buttonRect, "Add"))
                {
                    selectedItems.Add(def.defName);
                }

                currY += SEARCH_RESULT_ROW_HEIGHT;
            }

            scrollHeight = currY;
            Widgets.EndScrollView();
            GUI.EndGroup();
        }

        public override void WriteSettings()
        {
            settings.ApplySettings();
            base.WriteSettings();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listingStandard = new Listing_Standard();
            var listHeight = (settings.hediffCapMods.Count + settings.hediffStatOffsets.Count) * 46;
            Rect listingRect = new Rect(inRect.x, inRect.y, inRect.width - 40, inRect.height + 20 + listHeight);
            listingStandard.Begin(listingRect);

            var outRect = new Rect(0, 0, inRect.width, inRect.height - 20);
            var viewRect = new Rect(0, 0, inRect.width, listingRect.height);
            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);

            // Core settings
            TextFieldNumericLabeled(listingStandard, "RimSpawners_SettingsMatterSiphonPointsPerSecond".Translate(), ref settings.matterSiphonPointsPerSecond);
            TextFieldNumericLabeled(listingStandard, "RimSpawners_SettingsControlNodePointsStored".Translate(), ref settings.controlNodePointsStored);

            listingStandard.GapLine();

            // Pawn behavior settings
            listingStandard.CheckboxLabeled("RimSpawners_SettingsCachePawns".Translate(), ref settings.cachePawns);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsUseAllyFaction".Translate(), ref settings.useAllyFaction);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsMaxSkills".Translate(), ref settings.maxSkills);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsDisableNeeds".Translate(), ref settings.disableNeeds);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsDisableCorpses".Translate(), ref settings.disableCorpses);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsDoNotAttackFleeing".Translate(), ref settings.doNotAttackFleeing);

            // Spawn behavior settings
            listingStandard.CheckboxLabeled("RimSpawners_SettingsSpawnOnlyOnThreat".Translate(), ref settings.spawnOnlyOnThreat);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsCrossMap".Translate(), ref settings.crossMap);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsGroupPawnkinds".Translate(), ref settings.groupPawnkinds);

            // Combat behavior settings
            listingStandard.CheckboxLabeled("RimSpawners_SettingsDoNotDamagePlayerBuildings".Translate(), ref settings.doNotDamagePlayerBuildings);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsDoNotDamageFriendlies".Translate(), ref settings.doNotDamageFriendlies);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsMassivelyDamageEnemyBuildings".Translate(), ref settings.massivelyDamageEnemyBuildings);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsRandomizeLoadouts".Translate(), ref settings.randomizeLoadouts);

            listingStandard.GapLine();

            // Weapons and Apparel Section
            float sectionHeight = 200f;
            
            // Selected Weapons Section
            var selectedWeaponsRect = new Rect(0, listingStandard.CurHeight + 10f, inRect.width / 2 - 20, sectionHeight);
            DrawSelectedItemsSection("Selected Weapons", settings.selectedWeapons, ref selectedWeaponScrollPos, ref selectedWeaponScrollHeight, sectionHeight, selectedWeaponsRect);

            // Selected Apparel Section
            var selectedApparelRect = new Rect(inRect.width / 2, listingStandard.CurHeight + 10f, inRect.width / 2 - 20, sectionHeight);
            DrawSelectedItemsSection("Selected Apparel", settings.selectedApparel, ref selectedApparelScrollPos, ref selectedApparelScrollHeight, sectionHeight, selectedApparelRect);

            listingStandard.Gap(sectionHeight + 20f);

            // Weapon Selection Section
            var weaponSelectRect = new Rect(0, listingStandard.CurHeight + 10f, inRect.width / 2 - 20, sectionHeight);
            DrawItemSelectSection("Available Weapons", "Weapons", settings.selectedWeapons, weaponSearchKeyword, ref weaponScrollPos, ref weaponScrollHeight, sectionHeight, weaponSelectRect, ref weaponSearchKeyword);

            // Apparel Selection Section
            var apparelSelectRect = new Rect(inRect.width / 2, listingStandard.CurHeight + 10f, inRect.width / 2 - 20, sectionHeight);
            DrawItemSelectSection("Available Apparel", "Apparel", settings.selectedApparel, apparelSearchKeyword, ref apparelScrollPos, ref apparelScrollHeight, sectionHeight, apparelSelectRect, ref apparelSearchKeyword);

            listingStandard.Gap(sectionHeight + 20f);

            // Rest of settings...
            listingStandard.GapLine();
            foreach (var key in settings.hediffCapMods.Keys.OrderBy(x => x))
            {
                var val = settings.hediffCapMods[key];
                float offset = val.offset;
                bool enabled = val.enabled;
                listingStandard.CheckboxLabeled("RimSpawners_SettingsHediffCapMod".Translate(key), ref enabled);
                TextFieldNumericLabeled(listingStandard, "", ref offset, -10000);
                settings.hediffCapMods[key].enabled = enabled;
                settings.hediffCapMods[key].offset = offset;
            }

            listingStandard.GapLine();

            foreach (var key in settings.hediffStatOffsets.Keys.OrderBy(x => x))
            {
                var val = settings.hediffStatOffsets[key];
                float offset = val.offset;
                bool enabled = val.enabled;
                listingStandard.CheckboxLabeled("RimSpawners_SettingsHediffStatOffset".Translate(key), ref enabled);
                TextFieldNumericLabeled(listingStandard, "", ref offset, -10000);
                settings.hediffStatOffsets[key].enabled = enabled;
                settings.hediffStatOffsets[key].offset = offset;
            }

            Widgets.EndScrollView();
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "RimSpawners";
        }
    }

    public static class ObjectExtension
    {
        public static string ToStringNullable(this object value)
        {
            return (value ?? "Null").ToString();
        }

        public static string ToStringNullable(this List<string> stringList)
        {
            if (stringList != null)
            {
                return string.Join(", ", stringList);
            }
            return "Null";
        }
    }
}