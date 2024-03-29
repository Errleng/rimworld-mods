﻿using CombatExtended;
using HarmonyLib;
using System.Reflection;
using System.Runtime.CompilerServices;
using Verse;

namespace RimCheats
{
    internal class ModCompatibility
    {
        static RimCheatsSettings Settings
        {
            get => LoadedModManager.GetMod<RimCheats>().GetSettings<RimCheatsSettings>();
        }

        class MiscTurretBase_Patches
        {
            // Prevent guns from being destroyed when building is destroyed

            static bool IsModActive()
            {
                return ModsConfig.IsActive("Haplo.Miscellaneous.TurretBaseAndObjects");
            }

            [HarmonyPatch(typeof(Building), "Destroy")]
            class ReversePatch_Building_Destroy
            {
                static bool Prepare()
                {
                    return IsModActive();
                }

                [HarmonyReversePatch]
                [MethodImpl(MethodImplOptions.NoInlining)]
                public static void Destroy(object instance, DestroyMode mode)
                {
                }
            }

            [HarmonyPatch(typeof(Building), "DeSpawn")]
            class ReversePatch_Building_DeSpawn
            {
                static bool Prepare()
                {
                    return IsModActive();
                }

                [HarmonyReversePatch]
                [MethodImpl(MethodImplOptions.NoInlining)]
                public static void DeSpawn(object instance, DestroyMode mode)
                {
                }
            }

            [HarmonyPatch]
            class Patch_Destroy
            {
                static bool Prepare()
                {
                    return IsModActive();
                }

                static MethodBase TargetMethod()
                {
                    return AccessTools.Method("Building_TurretWeaponBase:Destroy");
                }

                static bool Prefix(object __instance, DestroyMode mode)
                {
                    if (Settings.autoRepair)
                    {
                        ReversePatch_Building_Destroy.Destroy(__instance, mode);
                        return false;
                    }
                    return true;
                }
            }

            [HarmonyPatch]
            class Patch_DeSpawn
            {
                static bool Prepare()
                {
                    return IsModActive();
                }

                static MethodBase TargetMethod()
                {
                    return AccessTools.Method("Building_TurretWeaponBase:DeSpawn");
                }

                static bool Prefix(object __instance, DestroyMode mode)
                {
                    if (Settings.autoRepair)
                    {
                        ReversePatch_Building_DeSpawn.DeSpawn(__instance, mode);
                        return false;
                    }
                    return true;
                }
            }
        }

        class CombatExtended_Patches
        {
            static bool IsModActive()
            {
                return ModsConfig.IsActive("CETeam.CombatExtended");
            }

            [HarmonyPatch(typeof(Verb_LaunchProjectileCE), "ShootingAccuracy", MethodType.Getter)]
            class Patch_GetShootingAccuracy
            {
                static bool Prepare()
                {
                    return IsModActive();
                }

                static void Postfix(Verb_LaunchProjectileCE __instance, ref float __result)
                {
                    if (!Settings.perfectAccuracy || !__instance.Caster.Faction.IsPlayer)
                    {
                        return;
                    }
                    __result = 4.5f;
                }
            }

            [HarmonyPatch(typeof(Verb_LaunchProjectileCE), "ShiftTarget")]
            class Patch_ShiftTarget
            {
                static bool Prepare()
                {
                    return IsModActive();
                }

                static void Prefix(Verb_LaunchProjectileCE __instance, ref ShiftVecReport report)
                {
                    if (!Settings.perfectAccuracy || !__instance.Caster.Faction.IsPlayer)
                    {
                        return;
                    }
                    report.swayDegrees = 0;
                    report.spreadDegrees = 0;
                    report.weatherShift = 0;
                    report.lightingShift = 0;
                    report.sightsEfficiency = 100;
                    report.aimingAccuracy = 1.5f;
                    report.circularMissRadius = 0;
                    report.maxRange = 100000;
                    report.smokeDensity = 0;
                    report.blindFiring = false;
                }
            }
        }
    }
}
