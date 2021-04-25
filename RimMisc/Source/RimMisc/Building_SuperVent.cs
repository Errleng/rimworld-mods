using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace RimMisc
{
    class Building_SuperVent : Building_TempControl
    {
        private static readonly float ORIGINAL_VENT_RATE = 14f;
        private static readonly float VENT_RATE_MULTIPLIER = 100f;

        private CompFlickable flickableComp;

        public override Graphic Graphic => flickableComp.CurrentGraphic;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            flickableComp = GetComp<CompFlickable>();
        }

        public override void TickRare()
        {
            if (FlickUtility.WantsToBeOn(this))
            {
                GenTemperature.EqualizeTemperaturesThroughBuilding(this, ORIGINAL_VENT_RATE * VENT_RATE_MULTIPLIER, true);
            }
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());
            if (!FlickUtility.WantsToBeOn(this))
            {
                if (stringBuilder.Length > 0)
                {
                    stringBuilder.AppendLine();
                }
                stringBuilder.Append("VentClosed".Translate());
            }
            return stringBuilder.ToString();
        }
    }
}
