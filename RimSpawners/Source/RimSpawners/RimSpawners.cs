using UnityEngine;
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

            // A year is four quadrums and a quadrum is fifteen days
            listingStandard.Label($"RimSpawners_SettingsMaximumPoints".Translate(settings.maxSpawnerPoints));
            settings.maxSpawnerPoints = listingStandard.Slider(settings.maxSpawnerPoints, 10f, 2000f);
            listingStandard.Label($"RimSpawners_SettingsSpawnInterval".Translate(settings.daysToSpawn));
            // A year is four quadrums and a quadrum is fifteen days
            settings.daysToSpawn = listingStandard.Slider(settings.daysToSpawn, 0.01f, 15f);

            listingStandard.CheckboxLabeled("RimSpawners_SettingsDisableNeeds".Translate(), ref settings.disableNeeds);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsDisableCorpses".Translate(), ref settings.disableCorpses);
            listingStandard.CheckboxLabeled("RimSpawners_SettingsSpawnOnThreats".Translate(), ref settings.spawnOnlyOnThreat);
            listingStandard.CheckboxLabeled($"RimSpawners_SettingsScaleSpawnTime".Translate(), ref settings.scaleSpawnIntervals);
            listingStandard.Label("RimSpawners_SettingsScaleSpawnTime".Translate(settings.pointsPerSecond));
            settings.pointsPerSecond = listingStandard.Slider(settings.pointsPerSecond, 0.01f, 50f);

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
