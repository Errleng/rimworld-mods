using Verse;

namespace RimSpawners
{
    internal class CompProperties_PointGenerator : CompProperties
    {
        public int pointsPerSecond;

        public CompProperties_PointGenerator()
        {
            compClass = typeof(CompPointGenerator);
        }
    }
}
