using Verse;

namespace RimSpawners
{
    internal class CompPointStorage : ThingComp
    {
        public CompProperties_PointStorage Props
        {
            get
            {
                return (CompProperties_PointStorage)props;
            }
        }

        public int PointsStored => Props.pointsStored;
    }
}
