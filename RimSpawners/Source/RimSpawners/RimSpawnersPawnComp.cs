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
                if (!parentPawn.health.hediffSet.HasHediff(Settings.spawnedPawnHediff))
                {
                    var spawnedPawnHediff = HediffMaker.MakeHediff(Settings.spawnedPawnHediff, parentPawn);
                    parentPawn.health.AddHediff(spawnedPawnHediff);
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
        public Action<Pawn> Recycle { get; }

        public CompProperties_RimSpawnersPawn(Action<Pawn> RecycleFunc)
        {
            compClass = typeof(RimSpawnersPawnComp);
            Recycle = RecycleFunc;
        }

        public CompProperties_RimSpawnersPawn(Type compClass) : base(compClass)
        {
            this.compClass = compClass;
        }
    }
}