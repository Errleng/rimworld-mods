using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimMisc
{
    internal class Designator_MeleeAttack : Designator
    {
        public Designator_MeleeAttack()
        {
            defaultLabel = "RimMisc_DesignatorMeleeAttack".Translate();
            defaultDesc = "RimMisc_DesignatorMeleeAttackDesc".Translate();
            icon = ContentFinder<Texture2D>.Get("Designations/AttackMelee");
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Designate_Deconstruct;
        }

        public override int DraggableDimensions => 2;

        protected override DesignationDef Designation => RimMiscDefOf.MeleeAttackDesignation;

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!c.InBounds(Map))
            {
                return false;
            }

            if (!DebugSettings.godMode && c.Fogged(Map))
            {
                return false;
            }

            AcceptanceReport result;
            if (TopDeconstructibleInCell(c, out result) == null)
            {
                return result;
            }

            return true;
        }

        public override void DesignateSingleCell(IntVec3 loc)
        {
            AcceptanceReport acceptanceReport;
            DesignateThing(TopDeconstructibleInCell(loc, out acceptanceReport));
        }

        private Thing TopDeconstructibleInCell(IntVec3 loc, out AcceptanceReport reportToDisplay)
        {
            reportToDisplay = AcceptanceReport.WasRejected;
            foreach (var thing in from t in Map.thingGrid.ThingsAt(loc)
                orderby t.def.altitudeLayer descending
                select t)
            {
                var acceptanceReport = CanDesignateThing(thing);
                if (CanDesignateThing(thing).Accepted)
                {
                    reportToDisplay = AcceptanceReport.WasAccepted;
                    return thing;
                }

                if (!acceptanceReport.Reason.NullOrEmpty())
                {
                    reportToDisplay = acceptanceReport;
                }
            }

            return null;
        }

        public override void DesignateThing(Thing t)
        {
            if (Prefs.DevMode)
            {
                t.Destroy();
            }
            else
            {
                Map.designationManager.AddDesignation(new Designation(t, Designation));
            }
        }

        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            if (t.HitPoints <= 0)
            {
                return false;
            }

            if (Map.designationManager.DesignationOn(t, Designation) != null)
            {
                return false;
            }

            if (Map.designationManager.DesignationOn(t, DesignationDefOf.Uninstall) != null)
            {
                return false;
            }

            return true;
        }

        public override void SelectedUpdate()
        {
            GenUI.RenderMouseoverBracket();
        }
    }
}