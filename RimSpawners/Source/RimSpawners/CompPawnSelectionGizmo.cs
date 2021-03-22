using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimSpawners
{
    class CompPawnSelectionGizmo : ThingComp
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo baseGizmo in base.CompGetGizmosExtra())
            {
                yield return baseGizmo;
            }
            yield return new Command_Action()
            {
                defaultLabel = "RimSpawners_PawnSelection".Translate(),
                defaultDesc = "RimSpawners_PawnSelectionDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Draft"),
                action = () =>
                {
                    Find.WindowStack.Add(new PawnSelectionWindow());
                }
            };
        }
    }
}
