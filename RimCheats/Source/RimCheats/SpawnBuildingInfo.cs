using RimWorld;
using Verse;

namespace RimCheats
{
    public class SpawnBuildingInfo : IExposable
    {
        public ThingDef def;
        public ThingDef stuff;
        public Map map;
        public IntVec3 position;
        public Rot4 rotation;
        public ThingStyleDef styleDef;
        public Precept_ThingStyle styleSourcePrecept;

        public SpawnBuildingInfo()
        {

        }

        public SpawnBuildingInfo(ThingDef def, ThingDef stuff, Map map, IntVec3 position, Rot4 rotation, ThingStyleDef styleDef, Precept_ThingStyle styleSourcePrecept)
        {
            this.def = def;
            this.stuff = stuff;
            this.map = map;
            this.position = position;
            this.rotation = rotation;
            this.styleDef = styleDef;
            this.styleSourcePrecept = styleSourcePrecept;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Defs.Look(ref stuff, "stuff");
            Scribe_References.Look(ref map, "map");
            Scribe_Values.Look(ref position, "position");
            Scribe_Values.Look(ref rotation, "rotation");
            Scribe_Defs.Look(ref styleDef, "styleDef");
            Scribe_References.Look(ref styleSourcePrecept, "styleSourcePrecept");
        }
    }
}
