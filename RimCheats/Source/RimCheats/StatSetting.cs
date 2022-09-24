using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimCheats
{
    public class StatSetting : IExposable
    {
        public string statName;
        public float multiplier;
        public bool enabled;
        public StatSetting() { }

        public StatSetting(string statName)
        {
            this.statName = statName;
            multiplier = 100f;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref statName, "statName");
            Scribe_Values.Look(ref multiplier, "multiplier");
            Scribe_Values.Look(ref enabled, "enabled");
        }
    }
}
