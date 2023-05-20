using RimWorld;
using System.Collections.Generic;
using Verse;

namespace RimSpawners
{
    internal class Utils
    {
        public static bool OtherMapsHostile(List<Map> nonThreatMaps, Faction faction)
        {
            // all given maps must have no threats
            // at least one of the non-given maps must have threats
            var maps = Find.Maps;
            foreach (var map in maps)
            {
                if (nonThreatMaps.Contains(map))
                {
                    if (GenHostility.AnyHostileActiveThreatTo(map, faction))
                    {
                        return false;
                    }
                    continue;
                }

                if (GenHostility.AnyHostileActiveThreatTo(map, faction))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
