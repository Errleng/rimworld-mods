using RimWorld;
using UnityEngine;
using Verse;

namespace RimMisc
{
    internal class Building_TempChanger : Building_TempControl
    {
        public override void TickRare()
        {
            if (compPowerTrader.PowerOn)
            {
                var energyLimit = compTempControl.Props.energyPerSecond;
                var tempChange = GenTemperature.ControlTemperatureTempChange(Position,
                    Map,
                    energyLimit,
                    compTempControl.targetTemperature);
                var isChangingTemp = !Mathf.Approximately(tempChange, 0f);
                var props = compPowerTrader.Props;
                if (isChangingTemp)
                {
                    this.GetRoom().Temperature += tempChange;
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