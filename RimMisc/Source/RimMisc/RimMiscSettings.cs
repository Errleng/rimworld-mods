using System;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace RimMisc
{
    public class RimMiscSettings : ModSettings
    {
        public bool defaultDoUntil;
        public bool autoCloseLetters;
        public float autoCloseLettersSeconds;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref defaultDoUntil, "defaultDoUntil");
            Scribe_Values.Look(ref autoCloseLetters, "autoCloseLetters");
            Scribe_Values.Look(ref autoCloseLettersSeconds, "autoCloseLettersSeconds");
            base.ExposeData();
        }
    }
}
