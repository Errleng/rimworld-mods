using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimCheats
{
    internal class RimCheatsWorldComp : WorldComponent
    {
        private static readonly RimCheatsSettings settings = LoadedModManager.GetMod<RimCheats>().GetSettings<RimCheatsSettings>();
        private static readonly int CLEAN_FILTH_TICKS = GenDate.TicksPerDay; // 1 day
        private static readonly int UPDATE_PAWNS_TICK = GenDate.TicksPerDay; // 1 day

        public RimCheatsWorldComp(World world) : base(world)
        {
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            var ticks = Find.TickManager.TicksGame;

            if (settings.autoClean && ticks % CLEAN_FILTH_TICKS == 0)
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

            if (ticks % UPDATE_PAWNS_TICK == 0)
            {
                if (settings.maxSkills)
                {
                    foreach (var colonist in PawnsFinder.AllMaps_FreeColonists)
                    {
                        foreach (var skill in colonist.skills.skills)
                        {
                            skill.Level += 20;
                        }
                    }
                }
            }
        }
    }
}
