using System;
using Verse;

namespace RimSpawners
{
    class RimSpawnersPawnComp : ThingComp
    {
        static readonly RimSpawnersSettings Settings = LoadedModManager.GetMod<RimSpawners>().GetSettings<RimSpawnersSettings>();

        public override void Initialize(CompProperties initialProps)
        {
            base.Initialize(initialProps);

            if (Settings.disableNeeds)
            {
                RemovePawnNeeds();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            if (Settings.disableNeeds)
            {
                RemovePawnNeeds();
            }
        }

        private void RemovePawnNeeds()
        {
            if (parent is Pawn parentPawn)
            {
                //parentPawn.needs.AllNeeds.Clear();

                // add hediff to remove pawn needs
                HediffDef rimSpawnersPawnHediffDef = DefDatabase<HediffDef>.GetNamed("RimSpawnersPawnHediff");
                if (!parentPawn.health.hediffSet.HasHediff(rimSpawnersPawnHediffDef))
                {
                    Hediff rimSpawnersPawnHediff = HediffMaker.MakeHediff(rimSpawnersPawnHediffDef, parentPawn);
                    parentPawn.health.AddHediff(rimSpawnersPawnHediff);
                }
            }
        }
    }

    class RimSpawnersPawnCompProperties : CompProperties
    {
        public RimSpawnersPawnCompProperties()
        {
            compClass = typeof(RimSpawnersPawnComp);
        }

        public RimSpawnersPawnCompProperties(Type compClass) : base(compClass)
        {
            this.compClass = compClass;
        }
    }
}
