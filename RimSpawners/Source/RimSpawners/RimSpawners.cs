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
        public static readonly string modName = "RimSpawners";
        public static RimSpawnersSettings settings;
        private Vector2 scrollPos = new Vector2(0, 0);
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

            TextFieldNumericLabeled(listingStandard, "RimSpawners_SettingsMaximumPoints".Translate(), ref settings.maxSpawnerPoints);

            listingStandard.GapLine();
            if (listingStandard.RadioButton("RimSpawners_SettingsScaledSpawnTimeButton".Translate(), settings.spawnTime.Equals(SpawnTimeSetting.Scaled)))
            {
                settings.spawnTime = SpawnTimeSetting.Scaled;
            }

            if (listingStandard.RadioButton("RimSpawners_SettingsFixedSpawnTimeButton".Translate(), settings.spawnTime.Equals(SpawnTimeSetting.Fixed)))
            {
                settings.spawnTime = SpawnTimeSetting.Fixed;
            }

            listingStandard.GapLine();

            TextFieldNumericLabeled(listingStandard, "RimSpawners_SettingsScaledSpawnTimePointsPerSecond".Translate(), ref settings.spawnTimePointsPerSecond);
            TextFieldNumericLabeled(listingStandard, "RimSpawners_SettingsFixedSpawnTimeSeconds".Translate(), ref settings.spawnTimeSecondsPerSpawn);

            listingStandard.CheckboxLabeled("RimSpawners_SettingsCachePawns".Translate(), ref settings.cachePawns);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsUseAllyFaction".Translate(), ref settings.useAllyFaction);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsMaxSkills".Translate(), ref settings.maxSkills);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsDisableNeeds".Translate(), ref settings.disableNeeds);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsDisableCorpses".Translate(), ref settings.disableCorpses);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsDoNotAttackFleeing".Translate(), ref settings.doNotAttackFleeing);

            listingStandard.CheckboxLabeled("RimSpawners_SettingsSpawnOnThreats".Translate(), ref settings.spawnOnlyOnThreat);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsCrossMap".Translate(), ref settings.crossMap);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsGroupSpawns".Translate(), ref settings.groupPawnkinds);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsDoNotDamagePlayerBuildings".Translate(), ref settings.doNotDamagePlayerBuildings);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsDoNotDamageFriendlies".Translate(), ref settings.doNotDamageFriendlies);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsMassivelyDamageEnemyBuildings".Translate(), ref settings.massivelyDamageEnemyBuildings);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsRandomizeLoadouts".Translate(), ref settings.randomizeLoadouts);

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