using Verse;

namespace RimCheats
{
    public class SpawnBuildingInfo : IExposable
    {
        public Thing thing;
        public Map map;

        public SpawnBuildingInfo()
        {

        }

        public SpawnBuildingInfo(Thing thing, Map map)
        {
            this.thing = thing;
            this.map = map;
        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref thing, true, "thing");
            Scribe_References.Look(ref map, "map");
        }
    }
}
