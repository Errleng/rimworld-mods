﻿using UnityEngine;
using Verse;

namespace RimSpawners
{
    class RimSpawners : Mod
    {
        private RimSpawnersSettings settings;
        public RimSpawners(ModContentPack content) : base(content)
        {
            settings = GetSettings<RimSpawnersSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.Label("RimSpawners_SettingsMaximumPoints".Translate(settings.maxSpawnerPoints));
            settings.maxSpawnerPoints = listingStandard.Slider(settings.maxSpawnerPoints, 10f, 2000f);

            listingStandard.GapLine();
            if (listingStandard.RadioButton_NewTemp("RimSpawners_SettingsScaledSpawnTimeButton".Translate(), settings.spawnTime.Equals(SpawnTimeSetting.Scaled)))
            {
                settings.spawnTime = SpawnTimeSetting.Scaled;
            }
            if (listingStandard.RadioButton_NewTemp("RimSpawners_SettingsFixedSpawnTimeButton".Translate(), settings.spawnTime.Equals(SpawnTimeSetting.Fixed)))
            {
                settings.spawnTime = SpawnTimeSetting.Fixed;
            }
            listingStandard.GapLine();

            listingStandard.Label("RimSpawners_SettingsScaledSpawnTimePointsPerSecond".Translate(settings.spawnTimePointsPerSecond));
            settings.spawnTimePointsPerSecond = listingStandard.Slider(settings.spawnTimePointsPerSecond, 0.01f, 50f);
            listingStandard.Label("RimSpawners_SettingsFixedSpawnTimeSeconds".Translate(settings.spawnTimeSecondsPerSpawn));
            settings.spawnTimeSecondsPerSpawn = listingStandard.Slider(settings.spawnTimeSecondsPerSpawn, 1f, 600f);

            listingStandard.CheckboxLabeled("RimSpawners_SettingsUseAllyFaction".Translate(), ref settings.useAllyFaction);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsDisableNeeds".Translate(), ref settings.disableNeeds);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsDisableCorpses".Translate(), ref settings.disableCorpses);

            listingStandard.CheckboxLabeled("RimSpawners_SettingsSpawnOnThreats".Translate(), ref settings.spawnOnlyOnThreat);
            listingStandard.Label("RimSpawners_SettingsSpawnOnThreatsSpeedMultiplier".Translate(settings.spawnOnThreatSpeedMultiplier));
            settings.spawnOnThreatSpeedMultiplier = listingStandard.Slider(settings.spawnOnThreatSpeedMultiplier, 0.01f, 10f);

            listingStandard.End();

            settings.ApplySettings();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "RimSpawners".Translate();
        }
    }

    public static class ObjectExtension
    {
        public static string ToStringNullable(this object value)
        {
            return (value ?? "Null").ToString();
        }
    }
}
