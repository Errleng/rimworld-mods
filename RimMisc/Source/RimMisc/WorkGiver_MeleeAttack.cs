using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace RimMisc
{
    internal class WorkGiver_MeleeAttack : WorkGiver_Scanner
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return from designation in pawn.Map.designationManager.SpawnedDesignationsOfDef(RimMiscDefOf.Designation_MeleeAttack)
                   select designation.target.Thing;
        }

        // Token: 0x0600001F RID: 31 RVA: 0x000026EC File Offset: 0x000008EC
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            DesignationManager designationManager = t.Map.designationManager;
            return pawn.CanReserve(t, 1, -1, null, forced) && designationManager.DesignationOn(t, RimMiscDefOf.Designation_MeleeAttack) != null;
        }

        // Token: 0x06000020 RID: 32 RVA: 0x00002708 File Offset: 0x00000908
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(RimMiscDefOf.Job_MeleeAttack, t);
        }
    }
}
