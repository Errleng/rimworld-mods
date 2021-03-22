using HarmonyLib;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace RimSpawners
{
    [StaticConstructorOnStartup]
    public class Patcher
    {
        static readonly RimSpawnersSettings Settings = LoadedModManager.GetMod<RimSpawners>().GetSettings<RimSpawnersSettings>();

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

                if ((customThingComp != null))
                {
                    if (Settings.cachePawns && __instance.RaceProps.Humanlike)
                    {
                        // recycle pawn into spawner
                        CompUniversalSpawnerPawn cups = customThingComp.Props.SpawnerComp;
                        cups.RecyclePawn(__instance);
                    }

                    // make it like the pawn never existed
                    __instance.SetFaction(null);
                    __instance.relations?.ClearAllRelations();

                    if (Settings.disableCorpses)
                    {
                        // destroy everything they owned
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
                if ((customThingComp != null) && Settings.disableCorpses)
                {
                    __instance.Corpse?.Destroy();
                }
            }
        }

        [HarmonyPatch(typeof(PawnUtility), "IsInteractionBlocked")]
        class PawnUtility_IsInteractionBlocked_Patch
        {
            public static bool Prefix(ref bool __result, Pawn pawn)
            {
                // disable social interactions for spawned pawns
                RimSpawnersPawnComp customThingComp = pawn.GetComp<RimSpawnersPawnComp>();
                if (customThingComp != null)
                {
                    __result = true;
                    return false;
                }

                return true;
            }
        }

        //[HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed")]
        //class Pawn_NeedsTracker_ShouldHaveNeed_Patch
        //{
        //    public static bool Prefix(ref bool __result, Pawn ___pawn)
        //    {
        //        // disabling needs with ShouldHaveNeed can cause issues
        //        //   e.g. no food need causes null reference exception when pawn tries to take combat drugs
        //        RimSpawnersPawnComp customThingComp = ___pawn.GetComp<RimSpawnersPawnComp>();
        //        if (customThingComp != null && Settings.disableNeeds)
        //        {
        //            __result = false;
        //            return false;
        //        }
        //        return true;
        //    }
        //}
    }
}
