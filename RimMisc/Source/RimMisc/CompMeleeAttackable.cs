using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimMisc
{
    class CompMeleeAttackable : ThingComp
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
