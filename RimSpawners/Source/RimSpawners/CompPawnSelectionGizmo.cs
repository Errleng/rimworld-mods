using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            yield return new Command_Action()
            {
                defaultLabel = "RimSpawners_KillSwitch".Translate(),
                defaultDesc = "RimSpawners_KillSwitchDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Detonate"),
                action = () =>
                {
                    UniversalSpawner us = parent as UniversalSpawner;
                    if (us != null)
                    {
                        us.RemoveAllSpawnedPawns();
                    }
                }
            };
            yield return new Command_Action()
            {
                defaultLabel = "RimSpawners_Reset".Translate(),
                defaultDesc = "RimSpawners_ResetDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TryReconnect"),
                action = () =>
                {
                    UniversalSpawner us = parent as UniversalSpawner;
                    if (us != null)
                    {
                        us.SetChosenKind(null);
                    }
                }
            };
        }
    }
}
