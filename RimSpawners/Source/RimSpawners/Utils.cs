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

        public static Pawn FindRandomActiveHostile(Map map)
        {
            var hostilePawns = GetActiveHostilesOnMap(map);
            if (hostilePawns.Count > 0)
            {
                return hostilePawns[Rand.Range(0, hostilePawns.Count)];
            }
            return null;
        }

        public static List<Pawn> GetActiveHostilesOnMap(Map map)
        {
            var hostilePawns = new List<Pawn>();
            var pawnsOnMap = map.mapPawns.AllPawnsSpawned;
            foreach (var pawn in pawnsOnMap)
            {
                if (pawn.HostileTo(Faction.OfPlayer) && !pawn.Downed)
                {
                    var dormantComp = pawn.GetComp<CompCanBeDormant>();
                    if (dormantComp == null || dormantComp.Awake)
                    {
                        hostilePawns.Add(pawn);
                    }
                }
            }
            return hostilePawns;
        }
    }
}
