using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimMisc
{
    internal class CompThreatToggle : ThingComp
    {
        public bool enableOnlyOnThreat;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref enableOnlyOnThreat, "enableOnlyOnThreat", false);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            if (parent.Faction == Faction.OfPlayer)
            {
                yield return new Command_Toggle
                {
                    hotKey = KeyBindingDefOf.Command_TogglePower,
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/DesirePower"),
                    defaultLabel = "RimMisc_ThreatToggleLabel".Translate(),
                    defaultDesc = "RimMisc_ThreatToggleDesc".Translate(),
                    isActive = () => enableOnlyOnThreat,
                    toggleAction = delegate
                    {
                        enableOnlyOnThreat = !enableOnlyOnThreat;
                    }
                };
            }
        }
    }
}
