using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimSpawners
{
    internal class LordJob_SearchAndDestroy : LordJob
    {
        private static SpawnerManager spawnerManager;

        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();
            List<LordToil> list = new List<LordToil>();
            LordToil lordToil = new LordToil_SearchAndDestroy();
            stateGraph.AddToil(lordToil);
            LordToil_ExitMap lordToil_ExitMap = new LordToil_ExitMap(LocomotionUrgency.Jog, false, true);
            lordToil_ExitMap.useAvoidGrid = true;
            stateGraph.AddToil(lordToil_ExitMap);
            return stateGraph;
        }

        public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
        {
            base.Notify_PawnLost(p, condition);
            if (condition == PawnLostCondition.ExitedMap)
            {
                var spawnerManager = Find.World.GetComponent<SpawnerManager>();
                spawnerManager.RemoveSpawnedPawns(new HashSet<string> { p.ThingID });
            }
        }
    }
}
