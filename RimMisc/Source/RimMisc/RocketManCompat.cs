using RocketMan;
using Soyuz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimMisc
{
    internal class RocketManCompat
    {
        public void EnableRocketmanRaces()
        {
            if (ModsConfig.IsActive("Krkr.RocketMan"))
            {
                foreach (var raceSettings in Context.Settings.AllRaceSettings)
                {
                    var hasCustomThingClass = IgnoreMeDatabase.ShouldIgnore(raceSettings.def);
                    if (raceSettings.enabled || raceSettings.isFastMoving || hasCustomThingClass)
                    {
                        continue;
                    }
                    Context.DilationEnabled[(int)raceSettings.def.index] = true;
                    raceSettings.enabled = true;
                    raceSettings.Prepare(true);
                    Log.Message($"Enable time dilation for {(string)raceSettings.def.LabelCap ?? raceSettings.def.defName}");
                }
            }
        }
    }
}
