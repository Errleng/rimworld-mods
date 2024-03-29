﻿using Verse;

namespace HighDensityHydroCustom
{
    internal class HydroStatsExtension : DefModExtension
    {
        public static readonly HydroStatsExtension defaultValues = new HydroStatsExtension();

        public float fertility = 2.8f;
        public int capacity = 52;
        public int power = 280;
    }
}
