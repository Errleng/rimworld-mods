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
                icon = ContentFinder<Texture2D>.Get("UI/SpawnerManagerWindow"),
                action = () => { Find.WindowStack.Add(new SpawnerManagerWindow()); }
            };

            yield return new Command_Toggle
            {
                defaultLabel = "RimSpawners_Pause".Translate(),
                defaultDesc = "RimSpawners_PauseDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Pause"),
                isActive = () => !spawnerManager.active,
                toggleAction = () => { spawnerManager.active = !spawnerManager.active; }
            };

            yield return new Command_Toggle
            {
                defaultLabel = "RimSpawners_DropPodToggle".Translate(),
                defaultDesc = "RimSpawners_DropPodToggleDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/DropPod"),
                isActive = () => spawnerManager.useDropPod,
                toggleAction = () => { spawnerManager.useDropPod = !spawnerManager.useDropPod; }
            };

            yield return new Command_Toggle
            {
                defaultLabel = "RimSpawners_SpawnNearEnemyToggle".Translate(),
                defaultDesc = "RimSpawners_SpawnNearEnemyToggleDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/SpawnNearEnemy"),
                isActive = () => spawnerManager.spawnNearEnemy,
                toggleAction = () =>
                {
                    spawnerManager.spawnNearEnemy = !spawnerManager.spawnNearEnemy;
                }
            };

            yield return new Command_Action
            {
                defaultLabel = "RimSpawners_KillSwitch".Translate(),
                defaultDesc = "RimSpawners_KillSwitchDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/KillSwitch"),
                action = spawnerManager.RemoveAllSpawnedPawns
            };

            yield return new Command_Action
            {
                defaultLabel = "RimSpawners_SpawnerManagerResetQueue".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/ResetQueue"),
                action = spawnerManager.GenerateQueue
            };

            yield return new Command_Action
            {
                defaultLabel = "RimSpawners_Reset".Translate(),
                defaultDesc = "RimSpawners_ResetDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Reset"),
                action = spawnerManager.Reset
            };

            yield return new Command_Action
            {
                defaultLabel = "RimSpawners_SpawnerManagerUpdate".Translate(),
                defaultDesc = "RimSpawners_SpawnerManagerUpdateDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Calculate"),
                action = spawnerManager.CalculateCache
            };
        }

        public override string CompInspectStringExtra()
        {
            return spawnerManager.GetInspectString();
        }
    }
}
