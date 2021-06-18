using System;
using System.Collections.Generic;
using Verse;

namespace RimSpawners
{
    class CompProperties_VanometricFabricatorPawn : CompProperties
    {
        public CompProperties_VanometricFabricatorPawn()
        {
            compClass = typeof(CompVanometricFabricatorPawn);
        }

        public List<PawnKindDef> spawnablePawnKinds;

        public SoundDef spawnSound;

        public string spawnMessageKey;

        //public string noPawnsLeftToSpawnKey;

        //public string pawnsLeftToSpawnKey;

        //public bool showNextSpawnInInspect;

        public bool shouldJoinParentLord;

        public Type lordJob;

        public float defendRadius = 21f;

        public int initialPawnsCount;

        public float initialPawnsPoints;

        public float maxSpawnedPawnsPoints = -1f;

        public float pawnSpawnIntervalSeconds;

        public int pawnSpawnRadius = 2;

        public IntRange maxPawnsToSpawn = IntRange.zero;

        public bool chooseSingleTypeToSpawn;
    }
}
