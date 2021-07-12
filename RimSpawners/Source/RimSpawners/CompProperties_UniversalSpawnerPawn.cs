using System;
using System.Collections.Generic;
using Verse;

namespace RimSpawners
{
    internal class CompProperties_VanometricFabricatorPawn : CompProperties
    {
        public bool chooseSingleTypeToSpawn;

        public float defendRadius = 21f;

        public int initialPawnsCount;

        public float initialPawnsPoints;

        public Type lordJob;

        public IntRange maxPawnsToSpawn = IntRange.zero;

        public float maxSpawnedPawnsPoints = -1f;

        public float pawnSpawnIntervalSeconds;

        public int pawnSpawnRadius = 2;

        //public string noPawnsLeftToSpawnKey;

        //public string pawnsLeftToSpawnKey;

        //public bool showNextSpawnInInspect;

        public bool shouldJoinParentLord;

        public List<PawnKindDef> spawnablePawnKinds;

        public string spawnMessageKey;

        public SoundDef spawnSound;

        public CompProperties_VanometricFabricatorPawn()
        {
            compClass = typeof(CompVanometricFabricatorPawn);
        }
    }
}