using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HighDensityHydroCustom
{
    class HighDensityHydroSettings : ModSettings
    {
        public static readonly float MIN_FERTILITY = 0.01f;
        public static readonly float MAX_FERTILITY = 10f;
        public static readonly int MIN_CAPACITY = 1;
        public static readonly int MAX_CAPACITY = 500;

        public float smallBayFertility;
        public int smallBayCapacity;
        public float mediumBayFertility;
        public int mediumBayCapacity;
        public float largeBayFertility;
        public int largeBayCapacity;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref smallBayFertility, "smallBayFertility", 1);
            Scribe_Values.Look(ref smallBayCapacity, "smallBayCapacity", 16);
            Scribe_Values.Look(ref mediumBayFertility, "mediumBayFertility", 1);
            Scribe_Values.Look(ref mediumBayCapacity, "mediumBayCapacity", 64);
            Scribe_Values.Look(ref largeBayFertility, "largeBayFertility", 1);
            Scribe_Values.Look(ref largeBayCapacity, "largeBayCapacity", 256);
            base.ExposeData();
        }

        public void ApplySettings()
        {
            var smallHydroBay = DefDatabase<ThingDef>.GetNamed("HDH_Hydroponics_Small");
            var hydroStats = smallHydroBay.GetModExtension<HydroStatsExtension>();
            if (hydroStats != null)
            {
                hydroStats.fertility = smallBayFertility;
                hydroStats.capacity = smallBayCapacity;
            }

            var mediumHydroBay = DefDatabase<ThingDef>.GetNamed("HDH_Hydroponics_Medium");
            hydroStats = mediumHydroBay.GetModExtension<HydroStatsExtension>();
            if (hydroStats != null)
            {
                hydroStats.fertility = mediumBayFertility;
                hydroStats.capacity = mediumBayCapacity;
            }

            var largeHydroBay = DefDatabase<ThingDef>.GetNamed("HDH_Hydroponics_Large");
            hydroStats = largeHydroBay.GetModExtension<HydroStatsExtension>();
            if (hydroStats != null)
            {
                hydroStats.fertility = largeBayFertility;
                hydroStats.capacity = largeBayCapacity;
            }

            var maps = Find.Maps;
            foreach (var map in maps)
            {
                var buildings = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
                foreach (var building in buildings)
                {
                    if (building.def.thingClass == typeof(Building_HighDensityHydro))
                    {
                        var hydroBay = building as Building_HighDensityHydro;
                        if (hydroBay != null)
                        {
                            hydroBay.loadConfig();
                        }
                    }
                }
            }
        }
    }
}
