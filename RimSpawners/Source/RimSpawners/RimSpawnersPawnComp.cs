﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimSpawners
{
    class RimSpawnersPawnComp : ThingComp
    {
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            Log.Message($"RimSpawners ThingComp Initialize");
            if (LoadedModManager.GetMod<RimSpawners>().GetSettings<RimSpawnersSettings>().disableNeeds)
            {
                RemovePawnNeeds();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Log.Message($"RimSpawners ThingComp PostExposeData");
            if (LoadedModManager.GetMod<RimSpawners>().GetSettings<RimSpawnersSettings>().disableNeeds)
            {
                RemovePawnNeeds();
            }
        }

        private void RemovePawnNeeds()
        {
            Pawn parentPawn = parent as Pawn;
            Log.Message($"Parent: {parentPawn.ToStringNullable()}");
            if (parentPawn != null)
            {
                Log.Message($"RimSpawners ThingComp remove all needs from pawns");
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