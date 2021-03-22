using System;
using Verse;

namespace RimSpawners
{
    class RimSpawnersPawnComp : ThingComp
    {
        static readonly RimSpawnersSettings Settings = LoadedModManager.GetMod<RimSpawners>().GetSettings<RimSpawnersSettings>();

        public CompProperties_RimSpawnersPawn Props => (CompProperties_RimSpawnersPawn)props;

        public override void Initialize(CompProperties initialProps)
        {
            base.Initialize(initialProps);

            AddCustomHediffs();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            AddCustomHediffs();
        }

        private void AddCustomHediffs()
        {
            if (parent is Pawn parentPawn)
            {
                // add hediff to remove pawn needs
                HediffDef rimSpawnersPawnHediffDef = DefDatabase<HediffDef>.GetNamed("RimSpawnersPawnHediff");
                if (!parentPawn.health.hediffSet.HasHediff(rimSpawnersPawnHediffDef))
                {
                    Hediff rimSpawnersPawnHediff = HediffMaker.MakeHediff(rimSpawnersPawnHediffDef, parentPawn);
                    parentPawn.health.AddHediff(rimSpawnersPawnHediff);
                }

                if (Settings.disableNeeds)
                {
                    HediffDef disableNeedHediffDef = DefDatabase<HediffDef>.GetNamed("RimSpawnersNoNeedsHediff");
                    if (!parentPawn.health.hediffSet.HasHediff(disableNeedHediffDef))
                    {
                        Hediff disableNeedHediff = HediffMaker.MakeHediff(disableNeedHediffDef, parentPawn);
                        parentPawn.health.AddHediff(disableNeedHediff);
                    }
                }
            }
        }
    }

    class CompProperties_RimSpawnersPawn : CompProperties
    {
        private readonly CompUniversalSpawnerPawn spawnerComp;

        public CompUniversalSpawnerPawn SpawnerComp { get => spawnerComp; }

        public CompProperties_RimSpawnersPawn(CompUniversalSpawnerPawn cusp)
        {
            compClass = typeof(RimSpawnersPawnComp);
            spawnerComp = cusp;
        }

        public CompProperties_RimSpawnersPawn(Type compClass) : base(compClass)
        {
            this.compClass = compClass;
        }
    }
}
