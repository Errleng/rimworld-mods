using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimCheats
{
    public class RimCheats : Mod
    {
        private readonly RimCheatsSettings settings;
        private Vector2 scrollPos = new Vector2(0, 0);

        public RimCheats(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<RimCheatsSettings>();
        }

        private void TextFieldNumericLabeled(Listing_Standard listingStandard, string label, ref float value, float min = RimCheatsSettings.MIN_VALUE, float max = RimCheatsSettings.MAX_VALUE)
        {
            string buffer = null;
            listingStandard.TextFieldNumericLabeled(label, ref value, ref buffer, min, max);
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            var listHeight = settings.statDefMults.Count * 46;
            Rect listingRect = new Rect(inRect.x, inRect.y, inRect.width - 40, inRect.height + listHeight);
            listingStandard.Begin(listingRect);

            var outRect = new Rect(0, 0, inRect.width, inRect.height - 20);
            var viewRect = new Rect(0, 0, inRect.width, listingRect.height);
            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);

            listingStandard.CheckboxLabeled("PathingToggleLabel".Translate(), ref settings.enablePathing);
            listingStandard.CheckboxLabeled("PathingNonHumanToggleLabel".Translate(), ref settings.enablePathingNonHuman);
            listingStandard.CheckboxLabeled("PathingAllyToggleLabel".Translate(), ref settings.enablePathingAlly);
            listingStandard.CheckboxLabeled("IgnoreTerrainCostToggleLabel".Translate(), ref settings.disableTerrainCost);
            listingStandard.CheckboxLabeled("IgnoreTerrainCostNonHumanToggleLabel".Translate(), ref settings.disableTerrainCostNonHuman);
            listingStandard.CheckboxLabeled("ToilSpeedToggleLabel".Translate(), ref settings.enableToilSpeed);
            listingStandard.CheckboxLabeled("AutoCleanToggleLabel".Translate(), ref settings.autoClean);
            listingStandard.CheckboxLabeled("AutoRepairToggleLabel".Translate((int)Math.Round(RimCheatsSettings.REPAIR_PERCENT * 100)), ref settings.autoRepair);
            listingStandard.CheckboxLabeled("MaxSkillsToggleLabel".Translate(), ref settings.maxSkills);
            listingStandard.CheckboxLabeled("CarryingCapacityMassToggleLabel".Translate(), ref settings.enableCarryingCapacityMass);
            listingStandard.CheckboxLabeled("PerfectAccuracyToggleLabel".Translate(), ref settings.perfectAccuracy);
            listingStandard.CheckboxLabeled("InfiniteTurretAmmoToggleLabel".Translate(), ref settings.infiniteTurretAmmo);

            listingStandard.GapLine();
            foreach (var key in settings.statDefMults.Keys.OrderBy(x => x))
            {
                var stat = settings.statDefMults[key];
                float multiplier = stat.multiplier;
                bool enabled = stat.enabled;
                listingStandard.CheckboxLabeled("StatMultiplierToggleLabel".Translate(key), ref enabled);
                TextFieldNumericLabeled(listingStandard, "", ref multiplier, 0, 100000000);
                settings.statDefMults[key].enabled = enabled;
                settings.statDefMults[key].multiplier = multiplier;
            }

            Widgets.EndScrollView();
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "RimCheats";
        }
    }

    public class RimCheatsSettings : ModSettings
    {
        public const float MIN_VALUE = 0;
        public const float MAX_VALUE = 1000;
        public const float REPAIR_PERCENT = 0.1f;

        public bool enablePathing;
        public bool enablePathingNonHuman;
        public bool enablePathingAlly;
        public bool disableTerrainCost;
        public bool disableTerrainCostNonHuman;
        public bool enableCarryingCapacityMass;
        public bool enableToilSpeed;
        public bool autoClean;
        public bool autoRepair;
        public bool maxSkills;
        public bool cheapRecipes;
        public bool perfectAccuracy;
        public bool infiniteTurretAmmo;
        public float toilSpeedMultiplier;
        public Dictionary<string, StatSetting> statDefMults = new Dictionary<string, StatSetting>();

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enablePathing, "enablePathing");
            Scribe_Values.Look(ref enablePathingNonHuman, "enablePathingNonHuman");
            Scribe_Values.Look(ref enablePathingAlly, "enablePathingAlly");
            Scribe_Values.Look(ref enableToilSpeed, "enableToilSpeed");
            Scribe_Values.Look(ref disableTerrainCost, "disableTerrainCost");
            Scribe_Values.Look(ref disableTerrainCostNonHuman, "disableTerrainCostNonHuman");
            Scribe_Values.Look(ref enableCarryingCapacityMass, "enableCarryingCapacityMass");
            Scribe_Values.Look(ref autoClean, "autoClean");
            Scribe_Values.Look(ref autoRepair, "autoRepair");
            Scribe_Values.Look(ref maxSkills, "maxSkills");
            Scribe_Values.Look(ref cheapRecipes, "cheapRecipes");
            Scribe_Values.Look(ref perfectAccuracy, "accurateTurrets");
            Scribe_Values.Look(ref infiniteTurretAmmo, "infiniteTurretAmmo");
            Scribe_Values.Look(ref toilSpeedMultiplier, "toilSpeedMultiplier", 1f);
            Scribe_Collections.Look(ref statDefMults, "statMultipliers", LookMode.Value, LookMode.Deep);


            foreach (var def in DefDatabase<StatDef>.AllDefs)
            {
                var name = def.defName;
                if (!statDefMults.ContainsKey(name))
                {
                    statDefMults.Add(name, new StatSetting(name));
                }
            }

            base.ExposeData();
        }
    }


}