using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimCheats
{
    internal class RimCheatsWorldComp : WorldComponent
    {
        private static readonly RimCheatsSettings settings = LoadedModManager.GetMod<RimCheats>().GetSettings<RimCheatsSettings>();
        private static readonly int CLEAN_FILTH_TICKS = 60000; // 1 day

        public RimCheatsWorldComp(World world) : base(world)
        {
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            if (settings.autoClean && Find.TickManager.TicksGame % CLEAN_FILTH_TICKS == 0)
            {
                var map = Find.CurrentMap;
                var filths = map.listerThings.ThingsInGroup(ThingRequestGroup.Filth);
                int cleaned = 0;
                for (int i = filths.Count - 1; i >= 0; --i)
                {
                    var filth = filths[i] as Filth;
                    if (filth == null)
                    {
                        Log.Error($"Thing {filths[i]} is not filth!");
                    }
                    else
                    {
                        filth.DeSpawn();
                        if (!filth.Destroyed)
                        {
                            filth.Destroy(DestroyMode.Vanish);
                        }
                        if (!filth.Discarded)
                        {
                            filth.Discard();
                        }
                        ++cleaned;
                    }
                }
                Log.Message($"Cleaned {cleaned} filth");
            }
        }
    }
}
