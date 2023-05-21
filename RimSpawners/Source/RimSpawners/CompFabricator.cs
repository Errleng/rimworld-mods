using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimSpawners
{
    internal class CompFabricator : ThingComp
    {
        private static SpawnerManager spawnerManager;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            spawnerManager = Find.World.GetComponent<SpawnerManager>();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Action
            {
                defaultLabel = "RimSpawners_OpenSpawnerManager".Translate(),
                defaultDesc = "RimSpawners_OpenSpawnerManagerDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Draft"),
                action = () => { Find.WindowStack.Add(new SpawnerManagerWindow()); }
            };

            yield return new Command_Toggle
            {
                defaultLabel = "RimSpawners_Pause".Translate(),
                defaultDesc = "RimSpawners_PauseDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt"),
                isActive = () => !spawnerManager.active,
                toggleAction = () => { spawnerManager.active = !spawnerManager.active; }
            };

            yield return new Command_Toggle
            {
                defaultLabel = "RimSpawners_DropPodNearEnemyToggle".Translate(),
                defaultDesc = "RimSpawners_DropPodNearEnemyToggleDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/DropCarriedPawn"),
                isActive = () => spawnerManager.dropNearEnemy,
                toggleAction = () =>
                {
                    spawnerManager.dropNearEnemy = !spawnerManager.dropNearEnemy;
                }
            };

            yield return new Command_Action
            {
                defaultLabel = "RimSpawners_KillSwitch".Translate(),
                defaultDesc = "RimSpawners_KillSwitchDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Detonate"),
                action = spawnerManager.RemoveAllSpawnedPawns
            };

            yield return new Command_Action
            {
                defaultLabel = "RimSpawners_SpawnerManagerResetQueue".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TryReconnect"),
                action = spawnerManager.GenerateQueue
            };

            yield return new Command_Action
            {
                defaultLabel = "RimSpawners_Reset".Translate(),
                defaultDesc = "RimSpawners_ResetDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("Things/Special/Fire/FireA"),
                action = spawnerManager.Reset
            };
        }

        public override string CompInspectStringExtra()
        {
            return spawnerManager.GetInspectString();
        }
    }
}
