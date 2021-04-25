using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimMisc
{
    class Building_TempChanger : Building_TempControl
    {
        public override void TickRare()
        {
            if (compPowerTrader.PowerOn)
            {
                float energyLimit = compTempControl.Props.energyPerSecond;
                float tempChange = GenTemperature.ControlTemperatureTempChange(Position, Map, energyLimit, compTempControl.targetTemperature);
                bool isChangingTemp = !Mathf.Approximately(tempChange, 0f);
                CompProperties_Power props = compPowerTrader.Props;
                if (isChangingTemp)
                {
                    this.GetRoomGroup().Temperature += tempChange;
                    compPowerTrader.PowerOutput = -props.basePowerConsumption;
                }
                else
                {
                    compPowerTrader.PowerOutput = -props.basePowerConsumption * compTempControl.Props.lowPowerConsumptionFactor;
                }
                compTempControl.operatingAtHighPower = isChangingTemp;
            }
        }
    }
}
