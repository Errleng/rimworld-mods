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
        public List<CondenserItem> condenserItems;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref defaultDoUntil, "defaultDoUntil");
            Scribe_Values.Look(ref autoCloseLetters, "autoCloseLetters");
            Scribe_Values.Look(ref autoCloseLettersSeconds, "autoCloseLettersSeconds", 10f);
            Scribe_Collections.Look(ref condenserItems, "condenserItems", LookMode.Deep);
            base.ExposeData();
        }
    }
}
