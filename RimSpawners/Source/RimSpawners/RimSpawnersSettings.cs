﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimSpawners
{
    class RimSpawnersSettings : ModSettings
    {
        public float maxSpawnerPoints;
        public float daysToSpawn;
        public bool disableCorpses;
        public bool disableNeeds;
        public bool spawnOnlyOnThreat;

        public bool scaleSpawnIntervals;
        public float pointsPerSecond;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref maxSpawnerPoints, "maxSpawnerPoints", 500f);
            Scribe_Values.Look(ref daysToSpawn, "daysToSpawn", 1f);
            Scribe_Values.Look(ref disableCorpses, "disableCorpses", false);
            Scribe_Values.Look(ref disableNeeds, "disableNeeds", false);
            Scribe_Values.Look(ref spawnOnlyOnThreat, "spawnOnlyOnThreat", false);
            Scribe_Values.Look(ref scaleSpawnIntervals, "scaleSpawnIntervals", false);
            Scribe_Values.Look(ref pointsPerSecond, "pointsPerSecond", 1f);
            base.ExposeData();
        }

        public void ApplySettings()
        {
            string[] spawnerNames = new string[]
            {
                "UniversalSpawner"
            };

            foreach (string spawnerName in spawnerNames)
            {
                ThingDef spawner = DefDatabase<ThingDef>.GetNamed(spawnerName, true);
                CompProperties_SpawnerPawn comp = spawner.GetCompProperties<CompProperties_SpawnerPawn>();
                comp.maxSpawnedPawnsPoints = maxSpawnerPoints;
                comp.pawnSpawnIntervalDays.min = daysToSpawn;
                comp.pawnSpawnIntervalDays.max = daysToSpawn;
                comp.chooseSingleTypeToSpawn = true;
            }
        }
    }
}
