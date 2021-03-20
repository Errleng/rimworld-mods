﻿using HarmonyLib;
using System.Reflection;
using RimWorld;
using Verse;

namespace RimSpawners
{
    [StaticConstructorOnStartup]
    public class Patcher
    {
        static readonly RimSpawnersSettings settings = LoadedModManager.GetMod<RimSpawners>().GetSettings<RimSpawnersSettings>();

        static Patcher()
        {
            Harmony harmony = new Harmony("com.rimspawners.rimworld.mod");
            Assembly assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
            Log.Message("RimSpawners loaded");
        }

        [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
        class Pawn_HealthTracker_MakeDowned_Patch
        {
            public static bool Prefix(Pawn_HealthTracker __instance, DamageInfo? dinfo, Hediff hediff, Pawn ___pawn)
            {
                // the spawner Lord has LordJob.RemoveDownedPawns = true
                //   cannot loop over spawnedPawns later to kill downed, must kill before MakeDowned runs
                RimSpawnersPawnComp customThingComp = ___pawn.GetComp<RimSpawnersPawnComp>();
                if (customThingComp != null)
                {
                    ___pawn.Kill(dinfo);
                    return false;
                }

                return true;
            }
        }


        [HarmonyPatch(typeof(Pawn), "Kill")]
        class Pawn_Kill_Patch
        {
            public static bool Prefix(Pawn __instance)
            {
                RimSpawnersPawnComp customThingComp = __instance.GetComp<RimSpawnersPawnComp>();
                if ((customThingComp != null) && settings.disableCorpses)
                {
                    // make it like the pawn never existed
                    __instance.SetFaction(null);
                    __instance.relations?.ClearAllRelations();

                    // destroy everything they owned
                    __instance.inventory?.DestroyAll();
                    __instance.apparel?.DestroyAll();
                    __instance.equipment?.DestroyAllEquipment();
                }
                return true;
            }

            public static void Postfix(Pawn __instance)
            {
                RimSpawnersPawnComp customThingComp = __instance.GetComp<RimSpawnersPawnComp>();
                if ((customThingComp != null) && settings.disableCorpses)
                {
                    __instance.Corpse?.Destroy();
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed")]
        class Pawn_NeedsTracker_ShouldHaveNeed_Patch
        {
            public static bool Prefix(ref bool __result, Pawn ___pawn)
            {
                RimSpawnersPawnComp customThingComp = ___pawn.GetComp<RimSpawnersPawnComp>();
                if (customThingComp != null && settings.disableNeeds)
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }
    }
}
