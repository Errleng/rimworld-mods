using System;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace RimMisc
{
    class RimMiscSettings : ModSettings
    {
        public bool defaultDoUntil;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref defaultDoUntil, "defaultDoUntil");
            base.ExposeData();
        }
    }
}
