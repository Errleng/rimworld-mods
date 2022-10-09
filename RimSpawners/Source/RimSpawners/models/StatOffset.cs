using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimSpawners
{
    public class StatOffset : IExposable
    {
        public string statName;
        public float offset;
        public bool enabled;
        public StatOffset() { }

        public StatOffset(string statName)
        {
            this.statName = statName;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref statName, "statName");
            Scribe_Values.Look(ref offset, "offset");
            Scribe_Values.Look(ref enabled, "enabled");
        }
    }
}
