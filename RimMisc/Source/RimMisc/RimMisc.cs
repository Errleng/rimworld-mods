using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace RimMisc
{
    public class RimMisc : Mod
    {
        private RimMiscSettings settings;
        public RimMisc(ModContentPack content) : base(content)
        {
            settings = GetSettings<RimMiscSettings>();
            Harmony harmony = new Harmony("com.rimmisc.rimworld.mod");
            harmony.PatchAll();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.CheckboxLabeled("DefaultDoUntil".Translate(), ref settings.defaultDoUntil);

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "RimMisc".Translate();
        }
    }
}
