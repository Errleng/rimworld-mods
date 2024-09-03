using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimSpawners
{
    internal class LordToil_SearchAndDestroy : LordToil
    {
        public override void UpdateAllDuties()
        {
            for (int i = 0; i < lord.ownedPawns.Count; i++)
            {
                if (lord.ownedPawns[i].mindState != null)
                {
                    lord.ownedPawns[i].mindState.duty = new PawnDuty(RimSpawnersDefOf.SearchAndDestroy);
                    lord.ownedPawns[i].mindState.duty.pickupOpportunisticWeapon = false;
                    CompCanBeDormant compCanBeDormant = lord.ownedPawns[i].TryGetComp<CompCanBeDormant>();
                    if (compCanBeDormant != null)
                    {
                        compCanBeDormant.WakeUp();
                    }
                }
            }
        }
    }
}
