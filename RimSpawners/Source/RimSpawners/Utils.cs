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
            Pawn hostilePawn = null;
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

            if (hostilePawns.Count > 0)
            {
                hostilePawn = hostilePawns[Rand.Range(0, hostilePawns.Count)];
            }

            return hostilePawn;
        }

        public static TargetInfo FindDropCenter(bool dropNearEnemy)
        {
            var dropCenter = IntVec3.Invalid;
            var targetCell = IntVec3.Invalid;
            Map targetMap = null;

            // prioritize maps with hostiles over preferred spot
            var maps = Find.Maps;
            foreach (var map in maps)
            {
                var target = FindRandomActiveHostile(map);
                if (target != null)
                {
                    targetCell = GetTargetCellFromHostile(target, dropNearEnemy);
                    targetMap = map;
                    break;
                }
            }

            if (targetCell != IntVec3.Invalid)
            {
                DropCellFinder.TryFindDropSpotNear(targetCell, targetMap, out dropCenter, true, false, false);
            }

            if (targetMap == null)
            {
                targetMap = Find.AnyPlayerHomeMap;
            }

            if (dropCenter == IntVec3.Invalid && targetMap != null)
            {
                dropCenter = DropCellFinder.FindRaidDropCenterDistant(targetMap);
            }

            return new TargetInfo(dropCenter, targetMap);
        }

        public static IntVec3 GetTargetCellFromHostile(Pawn target, bool dropNearEnemy)
        {
            if (dropNearEnemy)
            {
                return target.Position;
            }
            return DropCellFinder.FindRaidDropCenterDistant(target.Map);
        }
    }
}
