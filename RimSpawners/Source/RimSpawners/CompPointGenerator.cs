using Verse;

namespace RimSpawners
{
    internal class CompPointGenerator : ThingComp
    {
        public CompProperties_PointGenerator Props
        {
            get
            {
                return (CompProperties_PointGenerator)props;
            }
        }

        public int PointsPerSecond => Props.pointsPerSecond;
    }
}
