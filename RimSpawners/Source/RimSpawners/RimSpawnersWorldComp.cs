using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimSpawners
{
    internal class RimSpawnersWorldComp : WorldComponent
    {
        private static readonly RimSpawnersSettings Settings = LoadedModManager.GetMod<RimSpawners>().GetSettings<RimSpawnersSettings>();
        private static readonly int UPDATE_ALLY_FACTION_TICKS = GenTicks.SecondsToTicks(30);

        public RimSpawnersWorldComp(World world) : base(world)
        {
            RimSpawners.spawnedPawnFactionDef = DefDatabase<FactionDef>.GetNamed("RimSpawnersFriendlyFaction", false);
            RimSpawners.spawnedPawnFaction = Find.FactionManager.FirstFactionOfDef(RimSpawners.spawnedPawnFactionDef);
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            if (Settings.useAllyFaction)
            {
                if (Find.TickManager.TicksGame % UPDATE_ALLY_FACTION_TICKS == 0)
                {
                    // update ally faction relations to owner faction relations
                    var allyFaction = RimSpawners.spawnedPawnFaction;
                    allyFaction.hostileFromMemberCapture = false;

                    var playerFactionRelation = allyFaction.RelationWith(Faction.OfPlayer);
                    if (!playerFactionRelation.kind.Equals(FactionRelationKind.Ally))
                    {
                        playerFactionRelation.goodwill = 100;
                        playerFactionRelation.kind = FactionRelationKind.Ally;
                    }

                    foreach (var otherFaction in Find.FactionManager.AllFactions)
                    {
                        if (!otherFaction.IsPlayer && !otherFaction.Equals(allyFaction))
                        {
                            var otherFactionRelation = otherFaction.RelationWith(allyFaction);
                            otherFactionRelation.goodwill = otherFaction.PlayerGoodwill;
                            otherFactionRelation.kind = otherFaction.PlayerRelationKind;

                            var allyFactionRelation = allyFaction.RelationWith(otherFaction);
                            allyFactionRelation.goodwill = otherFactionRelation.goodwill;
                            allyFactionRelation.kind = otherFactionRelation.kind;
                        }
                    }
                }
            }
        }
    }
}