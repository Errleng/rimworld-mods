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

        public static TargetInfo FindSpawnCenter(bool useDropPod, bool spawnNearEnemy)
        {
            var spawnCenter = IntVec3.Invalid;
            var targetCell = IntVec3.Invalid;
            Map targetMap = null;

            var maps = Find.Maps;
            foreach (var map in maps)
            {
                var target = FindRandomActiveHostile(map);
                if (target != null)
                {
                    targetCell = target.Position;
                    targetMap = map;
                    break;
                }
            }

            if (targetMap == null)
            {
                // no hostiles on any map found, so spawn randomly on a player map
                targetMap = Find.AnyPlayerHomeMap;
            }

            if (useDropPod)
            {
                if (spawnNearEnemy && targetCell != IntVec3.Invalid)
                {
                    // found a hostile to spawn near
                    DropCellFinder.TryFindDropSpotNear(targetCell, targetMap, out spawnCenter, true, false, false);
                }

                if (spawnCenter == IntVec3.Invalid)
                {
                    // otherwise spawn far away
                    spawnCenter = DropCellFinder.FindRaidDropCenterDistant(targetMap);
                }
            }
            else
            {
                if (spawnNearEnemy && targetCell != IntVec3.Invalid)
                {
                    // found a hostile to spawn near
                    spawnCenter = CellFinder.RandomClosewalkCellNear(targetCell, targetMap, 20);
                }

                if (spawnCenter == IntVec3.Invalid)
                {
                    // otherwise spawn far away
                    if (!RCellFinder.TryFindRandomPawnEntryCell(out spawnCenter, targetMap, 0f, false, null))
                    {
                        spawnCenter = CellFinder.RandomCell(targetMap);
                    }
                }
            }

            return new TargetInfo(spawnCenter, targetMap);
        }
    }
}
