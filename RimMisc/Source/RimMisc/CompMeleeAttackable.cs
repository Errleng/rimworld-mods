using System.Collections.Generic;
using Verse;

namespace RimMisc
{
    internal class CompMeleeAttackable : ThingComp
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            var meleeAttackDesignator = new Designator_MeleeAttack();
            if (meleeAttackDesignator.CanDesignateThing(parent).Accepted)
            {
                yield return new Command_Action
                {
                    defaultLabel = "RimMisc_DesignatorMeleeAttack".Translate(),
                    defaultDesc = "RimMisc_DesignatorMeleeAttackDesc".Translate(),
                    icon = meleeAttackDesignator.icon,
                    action = () => meleeAttackDesignator.DesignateThing(parent)
                };
            }
        }
    }
}