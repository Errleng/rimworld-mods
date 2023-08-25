using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;
using Verse.AI;

namespace RimCheats
{
    [StaticConstructorOnStartup]
    internal class HarmonyPatches
    {
        static readonly RimCheatsSettings settings;

        static HarmonyPatches()
        {
            settings = LoadedModManager.GetMod<RimCheats>().GetSettings<RimCheatsSettings>();
            var harmony = new Harmony("com.rimcheats.rimworld.mod");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
            foreach (var method in harmony.GetPatchedMethods())
            {
                Log.Message($"RimCheats patched {method.DeclaringType.FullName}.{method.Name}");
            }
            Log.Message("RimCheats loaded");
        }

        [HarmonyPatch(typeof(Pawn_PathFollower), "TrySetNewPath")]
        class Patch_Pawn_PathFollower_TrySetNewPath
        {
            static void Postfix(Pawn_PathFollower __instance, bool __result, Pawn ___pawn)
            {
                if (!__result)
                {
                    return;
                }
                bool appliesToPawn = false;
                if (settings.enablePathing)
                {
                    appliesToPawn = ___pawn.IsColonistPlayerControlled;
                }
                if (!appliesToPawn && settings.enablePathingNonHuman && !___pawn.IsColonistPlayerControlled)
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

            static void Postfix(Pawn_PathFollower __instance, Pawn pawn, IntVec3 c, ref int __result)
            {
                bool appliesToPawn = false;
                if (settings.disableTerrainCost)
                {
                    appliesToPawn = pawn.IsColonistPlayerControlled;
                }
                if (settings.disableTerrainCostNonHuman && !pawn.IsColonistPlayerControlled)
                {
                    appliesToPawn = pawn.Faction != null && pawn.Faction.IsPlayer;
                }
                if (appliesToPawn)
                {
                    // based off floating pawn code from Alpha Animals
                    int cost = __result;
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
                if ((pawn != null) && (pawn.IsColonistPlayerControlled))
                {
                    string key = stat.defName;
                    if (!settings.statDefMults.ContainsKey(key))
                    {
                        Log.Warning($"No stat setting found for stat {stat.defName} in {String.Join(", ", settings.statDefMults.Keys.ToArray())}");
                        settings.statDefMults.Add(key, new StatSetting(key));
                        return;
                    }
                    var statSetting = settings.statDefMults[key];
                    if (statSetting.enabled)
                    {
                        __result *= (statSetting.multiplier / 100);
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

        [HarmonyPatch(typeof(ThingUtility), "CheckAutoRebuildOnDestroyed")]
        class Patch_CheckAutoRebuildOnDestroyed
        {
            static bool Prefix(Thing thing, DestroyMode mode, Map map, BuildableDef buildingDef)
            {
                bool shouldRebuild = Find.PlaySettings.autoRebuild && mode == DestroyMode.KillFinalize && thing.Faction == Faction.OfPlayer && buildingDef.blueprintDef != null && buildingDef.IsResearchFinished && map.areaManager.Home[thing.Position] && GenConstruct.CanPlaceBlueprintAt(buildingDef, thing.Position, thing.Rotation, map, false, null, null, thing.Stuff).Accepted;
                if (settings.autoRepair && shouldRebuild)
                {
                    var worldComp = Find.World.GetComponent<RimCheatsWorldComp>();
                    worldComp.buildingsToRestore.Add(new SpawnBuildingInfo(thing, map));
                    return false;
                }
                return true;
            }
        }
    }
}
