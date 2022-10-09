using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimSpawners
{
    public class CapMod : IExposable
    {
        public string capacityName;
        public float offset;
        public bool enabled;
        public CapMod() { }

        public CapMod(string name)
        {
            capacityName = name;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref capacityName, "capacityName");
            Scribe_Values.Look(ref offset, "offset");
            Scribe_Values.Look(ref enabled, "enabled");
        }
    }
}
