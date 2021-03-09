using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimSpawners
{
    class RimSpawnersPawnComp : ThingComp
    {
        static RimSpawnersSettings settings;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);

            Log.Message($"RimSpawners ThingComp Initialize");
            settings = LoadedModManager.GetMod<RimSpawners>().GetSettings<RimSpawnersSettings>();
            if (settings.disableNeeds)
            {
                RemovePawnNeeds();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Log.Message($"RimSpawners ThingComp PostExposeData");
            if (settings.disableNeeds)
            {
                RemovePawnNeeds();
            }
        }

        private void RemovePawnNeeds()
        {
            Pawn parentPawn = parent as Pawn;
            if (parentPawn != null)
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
