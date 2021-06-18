using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace RimMisc
{
    public class RimMisc : Mod
    {
        public static RimMiscSettings Settings;
        public RimMisc(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RimMiscSettings>();
            Harmony harmony = new Harmony("com.rimmisc.rimworld.mod");
            harmony.PatchAll();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.CheckboxLabeled("DefaultDoUntil".Translate(), ref Settings.defaultDoUntil);
            listingStandard.CheckboxLabeled("AutoCloseLetters".Translate(), ref Settings.autoCloseLetters);

            listingStandard.Label("AutoCloseLettersSeconds".Translate(Settings.autoCloseLettersSeconds));
            Settings.autoCloseLettersSeconds = listingStandard.Slider(Settings.autoCloseLettersSeconds, 1, 600);

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "RimMisc".Translate();
        }
    }
}
