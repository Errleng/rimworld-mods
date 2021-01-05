using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            listingStandard.Label($"Maximum points: {settings.maxSpawnerPoints} days");
            // A year is four quadrums and a quadrum is fifteen days
            settings.maxSpawnerPoints = listingStandard.Slider(settings.maxSpawnerPoints, 200f, 2000f);
            listingStandard.Label($"Spawn interval: {settings.daysToSpawn} days");
            // A year is four quadrums and a quadrum is fifteen days
            settings.daysToSpawn = listingStandard.Slider(settings.daysToSpawn, 0.01f, 15f);
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
