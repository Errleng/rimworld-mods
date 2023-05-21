using Verse;

namespace RimSpawners
{
    internal class CompProperties_PointStorage : CompProperties
    {
        public int pointsStored;

        public CompProperties_PointStorage()
        {
            compClass = typeof(CompPointStorage);
        }
    }
}
