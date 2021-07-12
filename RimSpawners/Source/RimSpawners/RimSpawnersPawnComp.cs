using System;
using Verse;

namespace RimSpawners
{
    internal class RimSpawnersPawnComp : ThingComp
    {
        private static readonly RimSpawnersSettings Settings = LoadedModManager.GetMod<RimSpawners>().GetSettings<RimSpawnersSettings>();

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
                var rimSpawnersPawnHediffDef = DefDatabase<HediffDef>.GetNamed("RimSpawners_VanometricPawnHediff");
                if (!parentPawn.health.hediffSet.HasHediff(rimSpawnersPawnHediffDef))
                {
                    var rimSpawnersPawnHediff = HediffMaker.MakeHediff(rimSpawnersPawnHediffDef, parentPawn);
                    parentPawn.health.AddHediff(rimSpawnersPawnHediff);
                }

                if (Settings.disableNeeds)
                {
                    var disableNeedHediffDef = DefDatabase<HediffDef>.GetNamed("RimSpawners_NoNeedsHediff");
                    if (!parentPawn.health.hediffSet.HasHediff(disableNeedHediffDef))
                    {
                        var disableNeedHediff = HediffMaker.MakeHediff(disableNeedHediffDef, parentPawn);
                        parentPawn.health.AddHediff(disableNeedHediff);
                    }
                }
            }
        }
    }

    internal class CompProperties_RimSpawnersPawn : CompProperties
    {
        public CompProperties_RimSpawnersPawn(CompVanometricFabricatorPawn cusp)
        {
            compClass = typeof(RimSpawnersPawnComp);
            SpawnerComp = cusp;
        }

        public CompProperties_RimSpawnersPawn(Type compClass) : base(compClass)
        {
            this.compClass = compClass;
        }

        public CompVanometricFabricatorPawn SpawnerComp { get; }
    }
}