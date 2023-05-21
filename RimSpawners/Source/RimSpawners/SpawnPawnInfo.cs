using Verse;

namespace RimSpawners
{
    internal class SpawnPawnInfo : IExposable
    {
        public int count;
        public string pawnKindDefName;
        public string pawnKindLabel;

        public SpawnPawnInfo()
        {
            count = 0;
        }

        public SpawnPawnInfo(string kindDefName, string kindLabel)
        {
            count = 0;
            pawnKindDefName = kindDefName;
            pawnKindLabel = kindLabel;
        }

        public SpawnPawnInfo(SpawnPawnInfo other)
        {
            count = other.count;
            pawnKindDefName = other.pawnKindDefName;
            pawnKindLabel = other.pawnKindLabel;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref count, "count");
            Scribe_Values.Look(ref pawnKindDefName, "pawnKindDefName");
            Scribe_Values.Look(ref pawnKindLabel, "pawnKindLabel");
        }

        public string GetKindLabel()
        {
            return pawnKindLabel ?? pawnKindDefName;
        }
    }
}
