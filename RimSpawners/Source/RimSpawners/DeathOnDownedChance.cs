using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimSpawners
{
    class DeathOnDownedChance : ThingComp
    {
        public CompProperties_DeathOnDownedChance Props
        {
            get
            {
                return (CompProperties_DeathOnDownedChance)props;
            }
        }
    }

    class CompProperties_DeathOnDownedChance : CompProperties
    {
        public float deathChance; // value between 0 and 1 inclusive

        public CompProperties_DeathOnDownedChance()
        {
            compClass = typeof(DeathOnDownedChance);
        }
    }
}
