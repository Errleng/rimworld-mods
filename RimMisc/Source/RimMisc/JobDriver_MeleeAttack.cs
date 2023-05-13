using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimMisc
{
    internal class JobDriver_MeleeAttack : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetThingA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnThingMissingDesignation(TargetIndex.A, RimMiscDefOf.Designation_MeleeAttack);
            yield return Toils_General.Do(delegate
            {
                Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, TargetA);
                Pawn targetPawn = TargetThingA as Pawn;
                if (targetPawn != null)
                {
                    job.killIncappedTarget = targetPawn.Downed;
                }
                Messages.Message("RimMisc_JobMeleeAttackMessage".Translate(pawn, TargetThingA), TargetThingA, MessageTypeDefOf.SilentInput, false);
                pawn.jobs.StartJob(job, JobCondition.Succeeded);
            });
            yield break;
        }
    }
}
