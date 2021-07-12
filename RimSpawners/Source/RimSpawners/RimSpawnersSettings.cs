using Verse;

namespace RimSpawners
{
    internal class RimSpawnersSettings : ModSettings
    {
        public bool cachePawns;
        public bool disableCorpses;
        public bool disableNeeds;
        public bool doNotAttackFleeing;
        public bool maxSkills;
        public float maxSpawnerPoints;

        public bool spawnOnlyOnThreat;
        public float spawnOnThreatSpeedMultiplier;

        public SpawnTimeSetting spawnTime;
        public float spawnTimePointsPerSecond;
        public float spawnTimeSecondsPerSpawn;
        public bool useAllyFaction;


        public override void ExposeData()
        {
            Scribe_Values.Look(ref maxSpawnerPoints, "maxSpawnerPoints", 500f);
            Scribe_Values.Look(ref spawnTime, "spawnTime");
            Scribe_Values.Look(ref spawnTimePointsPerSecond, "spawnTimePointsPerSecond", 1f);
            Scribe_Values.Look(ref spawnTimeSecondsPerSpawn, "spawnTimeSecondsPerSpawn", 1f);
            Scribe_Values.Look(ref cachePawns, "cachePawns");
            Scribe_Values.Look(ref useAllyFaction, "useAllyFaction", true);
            Scribe_Values.Look(ref maxSkills, "maxSkills");
            Scribe_Values.Look(ref disableCorpses, "disableCorpses", true);
            Scribe_Values.Look(ref disableNeeds, "disableNeeds", true);
            Scribe_Values.Look(ref doNotAttackFleeing, "doNotAttackFleeing");
            Scribe_Values.Look(ref spawnOnlyOnThreat, "spawnOnlyOnThreat");
            Scribe_Values.Look(ref spawnOnThreatSpeedMultiplier, "spawnOnThreatSpeedMultiplier", 2f);
            base.ExposeData();
        }

        public void ApplySettings()
        {
            string[] spawnerNames =
            {
                "VanometricFabricator"
            };

            foreach (var spawnerName in spawnerNames)
            {
                var spawner = DefDatabase<ThingDef>.GetNamed(spawnerName);
                var comp = spawner.GetCompProperties<CompProperties_VanometricFabricatorPawn>();
                comp.maxSpawnedPawnsPoints = maxSpawnerPoints;
                comp.pawnSpawnIntervalSeconds = spawnTimeSecondsPerSpawn;
                comp.chooseSingleTypeToSpawn = true;
            }
        }
    }
}