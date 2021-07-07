using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace HighDensityHydroCustom
{
    class HighDensityHydroCustom : Mod
    {
        private static readonly float RECT_WIDTH_MARGIN = 10f;
        private static readonly float RECT_HEIGHT_MARGIN = 10f;
        private static readonly float TEXT_HEIGHT = 30f;
        private static readonly float LABEL_WIDTH = 200f;
        private static readonly float TEXT_FIELD_WIDTH = 60f;

        public readonly HighDensityHydroSettings Settings;

        public HighDensityHydroCustom(ModContentPack content) : base(content)
        {
            Settings = GetSettings<HighDensityHydroSettings>();
        }

        public override string SettingsCategory()
        {
            return "High Density Hydroponics Custom";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect settingsRect = inRect.TopHalf();
            GUI.BeginGroup(settingsRect);

            Rect labelRect = new Rect(0, 0, LABEL_WIDTH, TEXT_HEIGHT);
            Rect fertilityFieldRect = new Rect(LABEL_WIDTH + RECT_WIDTH_MARGIN, 0, TEXT_FIELD_WIDTH, TEXT_HEIGHT);
            Rect capacityFieldRect = new Rect(fertilityFieldRect.x + fertilityFieldRect.width + RECT_WIDTH_MARGIN, 0, TEXT_FIELD_WIDTH, TEXT_HEIGHT);
            Rect powerFieldRect = new Rect(capacityFieldRect.x + capacityFieldRect.width + RECT_WIDTH_MARGIN, 0, TEXT_FIELD_WIDTH, TEXT_HEIGHT);
            Rect fertilityColumnRect = new Rect(LABEL_WIDTH + RECT_WIDTH_MARGIN, 0, TEXT_FIELD_WIDTH, TEXT_HEIGHT);
            Rect capacityColumnRect = new Rect(fertilityColumnRect.x + fertilityColumnRect.width + RECT_WIDTH_MARGIN, 0, TEXT_FIELD_WIDTH, TEXT_HEIGHT);
            Rect powerColumnRect = new Rect(capacityColumnRect.x + capacityColumnRect.width + RECT_WIDTH_MARGIN, 0, TEXT_FIELD_WIDTH, TEXT_HEIGHT);

            Widgets.Label(fertilityColumnRect, "HDHSettings_FertilityColumn".Translate());
            Widgets.Label(capacityColumnRect, "HDHSettings_CapacityColumn".Translate());
            Widgets.Label(powerColumnRect, "HDHSettings_PowerColumn".Translate());

            labelRect.y += TEXT_HEIGHT + RECT_HEIGHT_MARGIN;
            fertilityFieldRect.y += TEXT_HEIGHT + RECT_HEIGHT_MARGIN;
            capacityFieldRect.y += TEXT_HEIGHT + RECT_HEIGHT_MARGIN;
            powerFieldRect.y += TEXT_HEIGHT + RECT_HEIGHT_MARGIN;
            string smallBayFertilityString = null;
            string smallBayCapacityString = null;
            string smallBayPowerString = null;
            Widgets.Label(labelRect, "HDHSettings_SmallBay".Translate(Settings.smallBayFertility));
            Widgets.TextFieldNumeric(fertilityFieldRect, ref Settings.smallBayFertility, ref smallBayFertilityString, HighDensityHydroSettings.MIN_FERTILITY, HighDensityHydroSettings.MAX_FERTILITY);
            Widgets.TextFieldNumeric(capacityFieldRect, ref Settings.smallBayCapacity, ref smallBayCapacityString, HighDensityHydroSettings.MIN_CAPACITY, HighDensityHydroSettings.MAX_CAPACITY);
            Widgets.TextFieldNumeric(powerFieldRect, ref Settings.smallBayPower, ref smallBayPowerString, HighDensityHydroSettings.MIN_POWER, HighDensityHydroSettings.MAX_POWER);

            labelRect.y += TEXT_HEIGHT + RECT_HEIGHT_MARGIN;
            fertilityFieldRect.y += TEXT_HEIGHT + RECT_HEIGHT_MARGIN;
            capacityFieldRect.y += TEXT_HEIGHT + RECT_HEIGHT_MARGIN;
            powerFieldRect.y += TEXT_HEIGHT + RECT_HEIGHT_MARGIN;
            string mediumBayFertilityString = null;
            string mediumBayCapacityString = null;
            string mediumBayPowerString = null;
            Widgets.Label(labelRect, "HDHSettings_MediumBay".Translate(Settings.mediumBayFertility));
            Widgets.TextFieldNumeric(fertilityFieldRect, ref Settings.mediumBayFertility, ref mediumBayFertilityString, HighDensityHydroSettings.MIN_FERTILITY, HighDensityHydroSettings.MAX_FERTILITY);
            Widgets.TextFieldNumeric(capacityFieldRect, ref Settings.mediumBayCapacity, ref mediumBayCapacityString, HighDensityHydroSettings.MIN_CAPACITY, HighDensityHydroSettings.MAX_CAPACITY);
            Widgets.TextFieldNumeric(powerFieldRect, ref Settings.mediumBayPower, ref mediumBayPowerString, HighDensityHydroSettings.MIN_POWER, HighDensityHydroSettings.MAX_POWER);

            labelRect.y += TEXT_HEIGHT + RECT_HEIGHT_MARGIN;
            fertilityFieldRect.y += TEXT_HEIGHT + RECT_HEIGHT_MARGIN;
            capacityFieldRect.y += TEXT_HEIGHT + RECT_HEIGHT_MARGIN;
            powerFieldRect.y += TEXT_HEIGHT + RECT_HEIGHT_MARGIN;
            string largeBayFertilityString = null;
            string largeBayCapacityString = null;
            string largeBayPowerString = null;
            Widgets.Label(labelRect, "HDHSettings_LargeBay".Translate(Settings.largeBayFertility));
            Widgets.TextFieldNumeric(fertilityFieldRect, ref Settings.largeBayFertility, ref largeBayFertilityString, HighDensityHydroSettings.MIN_FERTILITY, HighDensityHydroSettings.MAX_FERTILITY);
            Widgets.TextFieldNumeric(capacityFieldRect, ref Settings.largeBayCapacity, ref largeBayCapacityString, HighDensityHydroSettings.MIN_CAPACITY, HighDensityHydroSettings.MAX_CAPACITY);
            Widgets.TextFieldNumeric(powerFieldRect, ref Settings.largeBayPower, ref largeBayPowerString, HighDensityHydroSettings.MIN_POWER, HighDensityHydroSettings.MAX_POWER);

            GUI.EndGroup();
            Settings.ApplySettings();
            base.DoSettingsWindowContents(inRect);
        }
    }
}
