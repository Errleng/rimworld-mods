using CombatExtended;
using HarmonyLib;
using RimWorld;
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

        public static void Apply(Harmony harmony)
        {
            if (ModsConfig.IsActive("Haplo.Miscellaneous.TurretBaseAndObjects"))
            {
                Log.Message("RimCheats compatibility patches for Misc Turretbases");
                foreach (var type in typeof(MiscTurretBase_Patches).GetNestedTypes(AccessTools.all))
                {
                    new PatchClassProcessor(harmony, type).Patch();
                }
                new PatchClassProcessor(harmony, typeof(MiscTurretBase_Patches)).Patch();
            }
            if (ModsConfig.IsActive("CETeam.CombatExtended"))
            {
                Log.Message("RimCheats compatibility patches for Combat Extended");
                foreach (var type in typeof(CombatExtended_Patches).GetNestedTypes(AccessTools.all))
                {
                    new PatchClassProcessor(harmony, type).Patch();
                }
            }
        }

        class MiscTurretBase_Patches
        {
            // Prevent guns from being destroyed when building is destroyed
            [HarmonyPatch(typeof(Building), "Destroy")]
            class ReversePatch_Building_Destroy
            {
                [HarmonyReversePatch]
                [MethodImpl(MethodImplOptions.NoInlining)]
                public static void Destroy(object instance, DestroyMode mode)
                {
                }
            }

            [HarmonyPatch(typeof(Building), "DeSpawn")]
            class ReversePatch_Building_DeSpawn
            {
                [HarmonyReversePatch]
                [MethodImpl(MethodImplOptions.NoInlining)]
                public static void DeSpawn(object instance, DestroyMode mode)
                {
                }
            }

            [HarmonyPatch]
            class Patch_Destroy
            {
                // Avoid errors from unloaded types
                static MethodBase TargetMethod()
                {
                    return AccessTools.Method("Building_TurretWeaponBase:Destroy");
                }

                static bool Prefix(object __instance, DestroyMode mode)
                {
                    Log.Message($"Disable destroy for turret base: {Settings.autoRepair}");
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
                static MethodBase TargetMethod()
                {
                    return AccessTools.Method("Building_TurretWeaponBase:DeSpawn");
                }

                static bool Prefix(object __instance, DestroyMode mode)
                {
                    Log.Message($"Disable despawn for turret base: {Settings.autoRepair}");
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
            [HarmonyPatch]
            class Patch_GetShootingAccuracy
            {
                static MethodBase TargetMethod()
                {
                    return AccessTools.PropertyGetter("Verb_LaunchProjectileCE:ShootingAccuracy");
                }

                static void Postfix(Verb __instance, ref float __result)
                {
                    if (!Settings.perfectAccuracy || !__instance.Caster.Faction.IsPlayer)
                    {
                        return;
                    }
                    __result = 4.5f;
                }
            }

            [HarmonyPatch]
            class Patch_ShiftTarget
            {
                static MethodBase TargetMethod()
                {
                    return AccessTools.Method("Verb_LaunchProjectileCE:ShiftTarget");
                }

                static void Prefix(Verb __instance, ref object report)
                {
                    if (!Settings.perfectAccuracy || !__instance.Caster.Faction.IsPlayer)
                    {
                        return;
                    }
                    var shiftVecReport = (ShiftVecReport)report;
                    shiftVecReport.swayDegrees = 0;
                    shiftVecReport.spreadDegrees = 0;
                    shiftVecReport.weatherShift = 0;
                    shiftVecReport.lightingShift = 0;
                    shiftVecReport.sightsEfficiency = 100;
                    shiftVecReport.aimingAccuracy = 1.5f;
                    shiftVecReport.circularMissRadius = 0;
                    shiftVecReport.maxRange = 100000;
                    shiftVecReport.smokeDensity = 0;
                    shiftVecReport.blindFiring = false;
                }
            }

            [HarmonyPatch]
            class Patch_TryReduceAmmoCount
            {
                static MethodBase TargetMethod()
                {
                    return AccessTools.Method("CompAmmoUser:DoOutOfAmmoAction");
                }

                static bool Prefix(CompAmmoUser __instance)
                {
                    if (!Settings.infiniteTurretAmmo)
                    {
                        return true;
                    }

                    if (__instance.IsEquippedGun)
                    {
                        if (__instance.Wielder.Faction.HostileTo(Faction.OfPlayer))
                        {
                            return true;
                        }
                        __instance.ResetAmmoCount(__instance.SelectedAmmo);
                        __instance.CurMagCount = __instance.MagSize * 10;
                        return false;
                    }

                    if (__instance.turret != null)
                    {
                        if (__instance.turret.Faction.HostileTo(Faction.OfPlayer))
                        {
                            return true;
                        }
                        __instance.ResetAmmoCount(__instance.SelectedAmmo);
                        __instance.CurMagCount = __instance.MagSize * 10;
                        return false;
                    }

                    return true;
                }
            }
        }
    }
}
