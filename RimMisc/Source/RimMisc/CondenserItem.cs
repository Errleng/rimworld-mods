using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimMisc
{
    public class CondenserItem : IExposable
    {
        private string thingDefName;
        private int work;
        private int yield;

        public string ThingDefName
        {
            get => thingDefName;
            set => thingDefName = value;
        }

        public int Work
        {
            get => work;
            set => work = value;
        }

        public int Yield
        {
            get => yield;
            set => yield = value;
        }

        public ThingDef ThingDef
        {
            get => DefDatabase<ThingDef>.GetNamed(ThingDefName);
        }

        public CondenserItem(string thingDefName, int work, int @yield)
        {
            this.thingDefName = thingDefName;
            this.work = work;
            this.yield = yield;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref thingDefName, "thingDefName");
            Scribe_Values.Look(ref thingDefName, "work");
            Scribe_Values.Look(ref thingDefName, "yield");
        }
    }
}
