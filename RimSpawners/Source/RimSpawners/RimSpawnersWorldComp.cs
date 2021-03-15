using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimSpawners
{
    class RimSpawnersWorldComp : WorldComponent
    {
        static readonly RimSpawnersSettings settings = LoadedModManager.GetMod<RimSpawners>().GetSettings<RimSpawnersSettings>();
        static readonly int UPDATE_ALLY_FACTION_TICKS = GenTicks.SecondsToTicks(30);

        public RimSpawnersWorldComp(World world) : base(world)
        {
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            if (settings.useAllyFaction)
            {
                if (Find.TickManager.TicksGame % UPDATE_ALLY_FACTION_TICKS == 0)
                {
                    // update ally faction relations to owner faction relations
                    Faction allyFaction = Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamed("RimSpawnersFriendlyFaction", true));
                    foreach (Faction otherFaction in Find.FactionManager.AllFactions)
                    {
                        if (!otherFaction.IsPlayer && !otherFaction.Equals(allyFaction))
                        {
                            FactionRelation otherFactionRelation = otherFaction.RelationWith(allyFaction);
                            otherFactionRelation.goodwill = otherFaction.PlayerGoodwill;
                            otherFactionRelation.kind = otherFaction.PlayerRelationKind;

                            FactionRelation allyFactionRelation = allyFaction.RelationWith(otherFaction);
                            allyFactionRelation.goodwill = otherFactionRelation.goodwill;
                            allyFactionRelation.kind = otherFactionRelation.kind;
                        }
                    }
                }
            }
        }
    }
}
