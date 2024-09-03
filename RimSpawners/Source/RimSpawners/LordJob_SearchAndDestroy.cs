using System.Collections.Generic;
using Verse.AI;
using Verse.AI.Group;

namespace RimSpawners
{
    internal class LordJob_SearchAndDestroy : LordJob
    {
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
    }
}
