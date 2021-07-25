using Verse;

namespace RimMisc
{
    internal class UnfinishedCondenserThing : UnfinishedThing
    {
        public override string LabelNoCount => "RimMisc_UnfinishedCondenserThingLabel".Translate(Recipe.products[0].thingDef.label);

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            // account for dev mode spawning
            if (Recipe == null)
            {
                Log.Warning("Destroying UnfinishedCondenserThing with no recipe");
                Destroy();
            }
        }
    }
}