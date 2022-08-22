using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimSpawners
{
    internal class RimSpawners : Mod
    {
        private readonly RimSpawnersSettings settings;
        public static FactionDef spawnedPawnFactionDef;
        public static Faction spawnedPawnFaction;

        public RimSpawners(ModContentPack content) : base(content)
        {
            settings = GetSettings<RimSpawnersSettings>();
        }

        private void TextFieldNumericLabeled(Listing_Standard listingStandard, string label, ref float value, float min = RimSpawnersSettings.MIN_VALUE, float max = RimSpawnersSettings.MAX_VALUE)
        {
            string buffer = null;
            listingStandard.TextFieldNumericLabeled(label, ref value, ref buffer, min, max);
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

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
            TextFieldNumericLabeled(listingStandard, "RimSpawners_SettingsSpawnOnThreatsSpeedMultiplier".Translate(), ref settings.spawnOnThreatSpeedMultiplier);
            TextFieldNumericLabeled(listingStandard, "RimSpawners_SettingsDropPodMinDist".Translate(), ref settings.dropPodMinDist);

            listingStandard.End();

            settings.ApplySettings();
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