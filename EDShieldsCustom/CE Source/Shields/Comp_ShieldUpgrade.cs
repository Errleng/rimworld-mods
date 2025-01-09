using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace Jaxxa.EnhancedDevelopment.Shields.Shields
{
    class Comp_ShieldUpgrade : ThingComp
    {

        public CompProperties_ShieldUpgrade Properties;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            Properties = ((CompProperties_ShieldUpgrade)props);
            
            parent.Map.GetComponent<ShieldManagerMapComp>().RecalaculateAll();

        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);

            map.GetComponent<ShieldManagerMapComp>().RecalaculateAll();

        }
        
    }
}
