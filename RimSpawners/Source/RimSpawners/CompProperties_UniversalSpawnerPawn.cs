using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI.Group;

namespace RimSpawners
{
    class CompProperties_UniversalSpawnerPawn : CompProperties
    {
        public CompProperties_UniversalSpawnerPawn()
        {
            compClass = typeof(CompUniversalSpawnerPawn);
        }

        public List<PawnKindDef> spawnablePawnKinds;

        public SoundDef spawnSound;

        public string spawnMessageKey;

        public string noPawnsLeftToSpawnKey;

        public string pawnsLeftToSpawnKey;

        public bool showNextSpawnInInspect;

        public bool shouldJoinParentLord;

        public Type lordJob;

        public float defendRadius = 21f;

        public int initialPawnsCount;

        public float initialPawnsPoints;

        public float maxSpawnedPawnsPoints = -1f;

        public FloatRange pawnSpawnIntervalDays = new FloatRange(0.85f, 1.15f);

        public int pawnSpawnRadius = 2;

        public IntRange maxPawnsToSpawn = IntRange.zero;

        public bool chooseSingleTypeToSpawn;

        public string nextSpawnInspectStringKey;

        public string nextSpawnInspectStringKeyDormant;
    }
}
