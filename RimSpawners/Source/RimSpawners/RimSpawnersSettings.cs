using Verse;

namespace RimSpawners
{
    class RimSpawnersSettings : ModSettings
    {
        public float maxSpawnerPoints;
        public float spawnTimeSecondsPerSpawn;
        public bool cachePawns;
        public bool useAllyFaction;
        public bool maxSkills;
        public bool disableCorpses;
        public bool disableNeeds;

        public bool spawnOnlyOnThreat;
        public float spawnOnThreatSpeedMultiplier;

        public SpawnTimeSetting spawnTime;
        public float spawnTimePointsPerSecond;


        public override void ExposeData()
        {
            Scribe_Values.Look(ref maxSpawnerPoints, "maxSpawnerPoints", 500f);
            Scribe_Values.Look(ref spawnTime, "spawnTime", SpawnTimeSetting.Scaled);
            Scribe_Values.Look(ref spawnTimePointsPerSecond, "spawnTimePointsPerSecond", 1f);
            Scribe_Values.Look(ref spawnTimeSecondsPerSpawn, "spawnTimeSecondsPerSpawn", 1f);
            Scribe_Values.Look(ref cachePawns, "cachePawns", false);
            Scribe_Values.Look(ref useAllyFaction, "useAllyFaction", false);
            Scribe_Values.Look(ref maxSkills, "maxSkills", false);
            Scribe_Values.Look(ref disableCorpses, "disableCorpses", false);
            Scribe_Values.Look(ref disableNeeds, "disableNeeds", false);
            Scribe_Values.Look(ref spawnOnlyOnThreat, "spawnOnlyOnThreat", false);
            Scribe_Values.Look(ref spawnOnThreatSpeedMultiplier, "spawnOnThreatSpeedMultiplier", 2f);
            base.ExposeData();
        }

        public void ApplySettings()
        {
            string[] spawnerNames = new string[]
            {
                "VanometricFabricator"
            };

            foreach (string spawnerName in spawnerNames)
            {
                ThingDef spawner = DefDatabase<ThingDef>.GetNamed(spawnerName);
                CompProperties_VanometricFabricatorPawn comp = spawner.GetCompProperties<CompProperties_VanometricFabricatorPawn>();
                comp.maxSpawnedPawnsPoints = maxSpawnerPoints;
                comp.pawnSpawnIntervalSeconds = spawnTimeSecondsPerSpawn;
                comp.chooseSingleTypeToSpawn = true;
            }
        }
    }
}
