using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimCheats
{
    [StaticConstructorOnStartup]
    internal class HarmonyPatches
    {
        static readonly RimCheatsSettings settings;

        static readonly HashSet<string> increaseOverTimeStats = new HashSet<string>{
            StatDefOf.WorkSpeedGlobal.defName,
        };

        static HarmonyPatches()
        {
            settings = LoadedModManager.GetMod<RimCheats>().GetSettings<RimCheatsSettings>();
            var harmony = new Harmony("com.rimcheats.rimworld.mod");
            foreach (var type in typeof(HarmonyPatches).GetNestedTypes(AccessTools.all))
            {
                new PatchClassProcessor(harmony, type).Patch();
            }
            ModCompatibility.Apply(harmony);
            foreach (var method in harmony.GetPatchedMethods())
            {
                Log.Message($"RimCheats patched {method.DeclaringType.FullName}.{method.Name}");
            }
            Log.Message("RimCheats loaded");
        }

        [HarmonyPatch(typeof(Pawn_PathFollower), "TrySetNewPath")]
        class Patch_Pawn_PathFollower_TrySetNewPath
        {
            static bool Prefix(Pawn_PathFollower __instance, ref bool __result, Pawn ___pawn)
            {
                bool appliesToPawn = false;
                if (settings.enablePathing)
                {
                    appliesToPawn = ___pawn.IsPlayerControlled;
                }
                if (!appliesToPawn && settings.enablePathingNonHuman && !___pawn.IsPlayerControlled)
                {
                    appliesToPawn = ___pawn.Faction != null && ___pawn.Faction.IsPlayer;
                }
                if (!appliesToPawn && settings.enablePathingAlly)
                {
                    appliesToPawn = ___pawn.Faction != null && ___pawn.Faction.RelationWith(Faction.OfPlayer).kind == FactionRelationKind.Ally;
                }
                if (!appliesToPawn)
                {
                    return true;
                }
                // Disable speed on wander or waiting for better idle pawn performance
                if (___pawn.CurJob != null && (___pawn.CurJob.def == JobDefOf.GotoWander || ___pawn.CurJob.def == JobDefOf.Wait_Wander))
                {
                    return true;
                }


                IntVec3 originalPos = ___pawn.Position;
                var dest = __instance.Destination.Cell;

                // Open door so it can defog the adjacent area
                Building_Door door = dest.GetDoor(___pawn.Map);
                if (door != null)
                {
                    door.StartManualOpenBy(___pawn);
                    ___pawn.Map.fogGrid.FloodUnfogAdjacent(door.Position, false);
                }

                // Extinguish fire or else the pawn will be standing on it
                foreach (var thing in dest.GetThingList(___pawn.Map))
                {
                    var fire = thing as Fire;
                    if (fire != null && fire.parent == null)
                    {
                        fire.Destroy(DestroyMode.Vanish);
                    }
                }

                ___pawn.Position = dest;

                IntVec3 nearDest = CellFinder.StandableCellNear(___pawn.Position, ___pawn.Map, 100, null);
                if (nearDest != null)
                {
                    //Log.Message($"Instantly moving {___pawn.LabelCap} from {originalPos} to {nearDest} near destination {__instance.Destination.Cell}");
                    ___pawn.Position = nearDest;
                }
                else
                {
                    Log.Warning($"Could not find cell near {___pawn.LabelCap}'s destination {__instance.Destination.Cell}");
                }
                return true;
            }

            static void Postfix(Pawn_PathFollower __instance, bool __result, Pawn ___pawn)
            {
                if (!__result)
                {
                    return;
                }
                bool appliesToPawn = false;
                if (settings.enablePathing)
                {
                    appliesToPawn = ___pawn.IsPlayerControlled;
                }
                if (!appliesToPawn && settings.enablePathingNonHuman && !___pawn.IsPlayerControlled)
                {
                    appliesToPawn = ___pawn.Faction != null && ___pawn.Faction.IsPlayer;
                }
                if (!appliesToPawn && settings.enablePathingAlly)
                {
                    appliesToPawn = ___pawn.Faction != null && ___pawn.Faction.RelationWith(Faction.OfPlayer).kind == FactionRelationKind.Ally;
                }
                if (appliesToPawn && __instance.Moving)
                {
                    // disable speed on wander or waiting for better idle pawn performance
                    if (___pawn.CurJob != null && (___pawn.CurJob.def == JobDefOf.GotoWander || ___pawn.CurJob.def == JobDefOf.Wait_Wander))
                    {
                        return;
                    }
                    if (__instance.curPath != null)
                    {
                        int nodesToRemain = 1;
                        Building_Door door = ___pawn.Map.thingGrid.ThingAt<Building_Door>(__instance.Destination.Cell);
                        if (door != null)
                        {
                            nodesToRemain = 2;
                        }
                        while (__instance.curPath.NodesLeftCount > nodesToRemain)
                        {
                            __instance.curPath.ConsumeNextNode();
                        }
                        ___pawn.Position = __instance.curPath.Peek(0);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_PathFollower), "CostToMoveIntoCell", new Type[] { typeof(Pawn), typeof(IntVec3) })]
        class PatchPawn_CostToMoveIntoCell
        {
            private static IntVec3 lastPos;

            static void Postfix(Pawn_PathFollower __instance, Pawn pawn, IntVec3 c, ref float __result)
            {
                bool appliesToPawn = false;
                if (settings.disableTerrainCost)
                {
                    appliesToPawn = pawn.IsPlayerControlled;
                }
                if (settings.disableTerrainCostNonHuman && !pawn.IsPlayerControlled)
                {
                    appliesToPawn = pawn.Faction != null && pawn.Faction.IsPlayer;
                }
                if (appliesToPawn)
                {
                    // based off floating pawn code from Alpha Animals
                    float cost = __result;
                    if (cost < 10000)
                    {
                        if (c.x == pawn.Position.x || c.z == pawn.Position.z)
                        {
                            cost = pawn.TicksPerMoveCardinal;
                        }
                        else
                        {
                            cost = pawn.TicksPerMoveDiagonal;
                        }
                        TerrainDef terrainDef = pawn.Map.terrainGrid.TerrainAt(c);
                        if (terrainDef == null)
                        {
                            cost = 10000;
                        }
                        else if (terrainDef.passability == Traversability.Impassable && !terrainDef.IsWater)
                        {
                            cost = 10000;
                        }
                        //else if (terrainDef.IsWater)
                        //{
                        //    cost = 10000;
                        //}
                        List<Thing> list = pawn.Map.thingGrid.ThingsListAt(c);
                        for (int i = 0; i < list.Count; i++)
                        {
                            Thing thing = list[i];
                            if (thing.def.passability == Traversability.Impassable)
                            {
                                cost = 10000;
                            }
                            //if (thing is Building_Door)
                            //{
                            //    cost += 45;
                            //}
                        }
                        __result = cost;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(StatExtension), "GetStatValue")]
        class PatchStatExtensionGetStatValue
        {
            static void Postfix(Thing thing, StatDef stat, bool applyPostProcess, ref float __result)
            {
                Pawn pawn = thing as Pawn;
                if ((pawn != null) && (pawn.IsPlayerControlled))
                {
                    string key = stat.defName;
                    if (settings.statDefMults.ContainsKey(key))
                    {
                        var statSetting = settings.statDefMults[key];
                        if (statSetting.enabled)
                        {
                            __result *= (statSetting.multiplier / 100);
                        }
                    }
                    else
                    {
                        Log.Warning($"No stat setting found for stat {stat.defName} in {string.Join(", ", settings.statDefMults.Keys.ToArray())}");
                        settings.statDefMults.Add(key, new StatSetting(key));
                    }

                    if (settings.accelerateOverTime && increaseOverTimeStats.Contains(key))
                    {
                        __result *= (1 + RimCheats.GetAccelerateOverTimePercentageIncrease() / 100);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_JobTracker), "JobTrackerTick")]
        class Patch_Pawn_JobTracker_JobTrackerTick
        {
            static void Postfix(Pawn_JobTracker __instance)
            {
                if (settings.enableToilSpeed)
                {
                    if (__instance.curDriver != null)
                    {
                        int multiplier = Convert.ToInt32(settings.toilSpeedMultiplier);
                        for (int i = 0; i < multiplier; i++)
                        {
                            __instance.curDriver.DriverTick();
                        }
                    }
                }
            }
        }

        // carrying capacity is different from "mass capacity"/"inventory space"
        [HarmonyPatch(typeof(MassUtility), "Capacity")]
        public static class MassUtility_Capacity_Patch
        {
            [HarmonyPrefix]
            public static bool Capacity(ref float __result, Pawn p, StringBuilder explanation = null)
            {
                bool enableCarryingCapacityMass = settings.enableCarryingCapacityMass;
                if (enableCarryingCapacityMass)
                {
                    if (!MassUtility.CanEverCarryAnything(p))
                    {
                        __result = 0f;
                        return false;
                    }

                    float capacity = p.BodySize * 35f * p.GetStatValue(StatDefOf.CarryingCapacity);

                    if (explanation != null)
                    {
                        if (explanation.Length > 0)
                        {
                            explanation.AppendLine();
                        }
                        explanation.Append("  - " + p.LabelShortCap + ": " + capacity.ToStringMassOffset());
                    }
                    __result = capacity;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ThingUtility), "CheckAutoRebuildOnDestroyed_NewTemp")]
        class Patch_CheckAutoRebuildOnDestroyed_NewTemp
        {
            static bool Prefix(Thing thing, DestroyMode mode, Map map, BuildableDef buildingDef)
            {
                bool shouldRestore = Find.PlaySettings.autoRebuild && mode == DestroyMode.KillFinalize && thing.Faction == Faction.OfPlayer && map.areaManager.Home[thing.Position];
                if (settings.autoRepair && shouldRestore)
                {
                    var worldComp = Find.World.GetComponent<RimCheatsWorldComp>();
                    worldComp.buildingsToRestore.Add(new SpawnBuildingInfo(thing, map));
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Building_Trap), "CheckAutoRebuild")]
        class Patch_Building_Trap_CheckAutoRebuild
        {
            static bool Prefix(Building_Trap __instance, Map map)
            {
                bool shouldRestore = Find.PlaySettings.autoRebuild && __instance.Faction == Faction.OfPlayer && __instance.def.blueprintDef != null && GenConstruct.CanPlaceBlueprintAt(__instance.def, __instance.Position, __instance.Rotation, map, false, null, null, __instance.Stuff, false, false).Accepted;
                if (settings.autoRepair && shouldRestore)
                {
                    var worldComp = Find.World.GetComponent<RimCheatsWorldComp>();
                    worldComp.buildingsToRestore.Add(new SpawnBuildingInfo(__instance, map));
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Projectile), "Launch", new Type[] { typeof(Thing), typeof(Vector3), typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(ProjectileHitFlags), typeof(bool), typeof(Thing), typeof(ThingDef) })]
        class Patch_Launch
        {
            static void Prefix(Projectile __instance, Thing launcher, Vector3 origin, ref LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ref ProjectileHitFlags hitFlags, ref bool preventFriendlyFire, Thing equipment, ThingDef targetCoverDef)
            {
                if (!settings.perfectAccuracy || launcher?.Faction != Faction.OfPlayer)
                {
                    return;
                }

                usedTarget = intendedTarget;
                hitFlags = ProjectileHitFlags.IntendedTarget;
                preventFriendlyFire = true;
            }
        }

        [HarmonyPatch(typeof(CompRefuelable), "ConsumeFuel")]
        class Patch_ConsumeFuel
        {
            static bool Prefix(CompRefuelable __instance, float amount)
            {
                if (settings.infiniteTurretAmmo &&
                    (__instance.parent.def.building != null && __instance.parent.def.building.IsTurret))
                {
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Thing), "TakeDamage")]
        private class Thing_TakeDamage_Patch
        {
            static void Prefix(ref DamageInfo dinfo, Thing __instance)
            {
                // Disable damage from allies to player
                if (!settings.disableFriendlyFire)
                {
                    return;
                }
                if (dinfo.Instigator == null)
                {
                    return;
                }

                Thing victim = __instance;
                if (victim.Faction == Faction.OfPlayer && !victim.Faction.HostileTo(dinfo.Instigator.Faction))
                {
                    if (dinfo.Def == DamageDefOf.Flame)
                    {
                        Log.Message($"Fire started by {dinfo.Instigator} from faction {dinfo.Instigator.Faction} is not considered hostile to {victim.Faction}");
                    }
                    // If the instigator and victim factions are not hostile, then do no damage
                    dinfo.SetAmount(0);
                    return;
                }
            }
        }
    }
}
