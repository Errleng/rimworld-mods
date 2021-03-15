using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Verse;
using RimWorld;
using Verse.AI.Group;

namespace RimSpawners
{
    [StaticConstructorOnStartup]
    public class Patcher
    {
        static RimSpawnersSettings settings;

        static Patcher()
        {
            settings = LoadedModManager.GetMod<RimSpawners>().GetSettings<RimSpawnersSettings>();
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
                if ((___pawn.Faction != null) && ___pawn.Faction.IsPlayer)
                {
                    RimSpawnersPawnComp customThingComp = ___pawn.GetComp<RimSpawnersPawnComp>();
                    if (customThingComp != null)
                    {
                        ___pawn.Kill(dinfo, null);
                        return false;
                    }
                }

                return true;
            }
        }


        [HarmonyPatch(typeof(Pawn), "Kill")]
        class Pawn_Kill_Patch
        {
            public static bool Prefix(Pawn __instance)
            {
                if ((__instance.Faction != null) && __instance.Faction.IsPlayer)
                {
                    RimSpawnersPawnComp customThingComp = __instance.GetComp<RimSpawnersPawnComp>();
                    if ((customThingComp != null) && settings.disableCorpses)
                    {
                        __instance.SetFaction(null, null);

                        __instance.inventory?.DestroyAll();
                        __instance.apparel?.DestroyAll();
                        __instance.equipment?.DestroyAllEquipment();
                    }
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
    }
}
