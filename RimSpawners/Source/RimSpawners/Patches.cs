using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimSpawners
{
    [StaticConstructorOnStartup]
    public class Patcher
    {
        private static readonly RimSpawnersSettings Settings = LoadedModManager.GetMod<RimSpawners>().GetSettings<RimSpawnersSettings>();

        static Patcher()
        {
            var harmony = new Harmony("com.rimspawners.rimworld.mod");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
            Settings.ApplySettings();
            Log.Message("RimSpawners loaded");
        }

        [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
        private class Pawn_HealthTracker_MakeDowned_Patch
        {
            public static bool Prefix(Pawn_HealthTracker __instance, DamageInfo? dinfo, Hediff hediff, Pawn ___pawn)
            {
                // the spawner Lord has LordJob.RemoveDownedPawns = true
                //   cannot loop over spawnedPawns later to kill downed, must kill before MakeDowned runs
                var customThingComp = ___pawn.GetComp<RimSpawnersPawnComp>();
                if (customThingComp != null)
                {
                    ___pawn.Kill(dinfo);
                    return false;
                }

                return true;
            }
        }


        [HarmonyPatch(typeof(Pawn), "Kill")]
        private class Pawn_Kill_Patch
        {
            public static bool Prefix(Pawn __instance)
            {
                var customThingComp = __instance.GetComp<RimSpawnersPawnComp>();

                if (customThingComp != null)
                {
                    if (Settings.cachePawns && __instance.RaceProps.Humanlike)
                    {
                        // recycle pawn into spawner
                        var cusp = customThingComp.Props.SpawnerComp;
                        cusp.RecyclePawn(__instance);
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
                var customThingComp = __instance.GetComp<RimSpawnersPawnComp>();
                if (customThingComp != null && Settings.disableCorpses)
                {
                    __instance.Corpse?.Destroy();
                }
            }
        }

        // disable social interactions for spawned pawns
        // this isn't really necessary, just extra details
        [HarmonyPatch(typeof(PawnUtility), "IsInteractionBlocked")]
        private class PawnUtility_IsInteractionBlocked_Patch
        {
            public static bool Prefix(ref bool __result, Pawn pawn)
            {
                var customThingComp = pawn.GetComp<RimSpawnersPawnComp>();
                if (customThingComp != null)
                {
                    __result = true;
                    return false;
                }

                return true;
            }
        }

        // stop spawned pawns from committing a war crime
        [HarmonyPatch(typeof(Pawn), "ThreatDisabled")]
        private class Pawn_ThreatDisabled_Patch
        {
            public static void Postfix(Pawn __instance, ref bool __result, IAttackTargetSearcher disabledFor)
            {
                var isThreat = !__result;
                if (isThreat && Settings.doNotAttackFleeing)
                {
                    if (disabledFor != null && disabledFor.Thing is Pawn attacker)
                    {
                        var customThingComp = attacker.GetComp<RimSpawnersPawnComp>();
                        if (customThingComp != null)
                        {
                            __result = !GenHostility.IsActiveThreatTo(__instance, Faction.OfPlayer);
                        }
                    }
                }
            }
        }

        // tested on predators
        [HarmonyPatch(typeof(GenHostility), "HostileTo", typeof(Thing), typeof(Thing))]
        [HarmonyPriority(Priority.Last)]
        private class GenHostility_HostileTo1_Patch
        {
            public static void Postfix(ref bool __result, Thing a, Thing b)
            {
                if (Settings.useAllyFaction && !__result && a.Faction != b.Faction)
                {
                    if (RimSpawners.spawnedPawnFaction != null)
                    {
                        if (a.Faction == RimSpawners.spawnedPawnFaction && b.Faction != Faction.OfPlayer)
                        {
                            __result = b.HostileTo(Faction.OfPlayer);
                        }
                        else if (b.Faction == RimSpawners.spawnedPawnFaction && a.Faction != Faction.OfPlayer)
                        {
                            __result = a.HostileTo(Faction.OfPlayer);
                        }

                        //Log.Message($"HostileTo1 patch {a.LabelCap} of faction {a.Faction} vs. {b.LabelCap} of faction {b.Faction}: {__result}");
                    }
                }
            }
        }

        // tested on predators
        [HarmonyPatch(typeof(GenHostility), "HostileTo", typeof(Thing), typeof(Faction))]
        [HarmonyPriority(Priority.Last)]
        private class GenHostility_HostileTo2_Patch
        {
            public static void Postfix(ref bool __result, Thing t, Faction fac)
            {
                if (Settings.useAllyFaction && !__result && t.Faction != fac)
                {
                    if (RimSpawners.spawnedPawnFaction != null)
                    {
                        if (t.Faction != Faction.OfPlayer && fac == RimSpawners.spawnedPawnFaction)
                        {
                            __result = t.HostileTo(Faction.OfPlayer);
                        }

                        //Log.Message($"HostileTo2 patch {t.LabelCap} of faction {t.Faction} vs. faction {fac}: {__result}");
                    }
                }
            }
        }

        //[HarmonyPatch(typeof(JobGiver_AIFightEnemy), "FindAttackTarget")]
        //private class JobGiver_AIFightEnemy_FindAttackTarget_Patch
        //{
        //    public static void Postfix(ref Thing __result, Pawn pawn)
        //    {
        //        if (Settings.useAllyFaction)
        //        {
        //            if (RimSpawners.spawnedPawnFaction != null)
        //            {
        //                if (pawn.Faction == RimSpawners.spawnedPawnFaction)
        //                {
        //                    Log.Message($"{pawn.Name} FindAttackTarget {__result.ToStringNullable()}");
        //                }
        //            }
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(AttackTargetsCache), "GetPotentialTargetsFor")]
        //private class AttackTargetsCache_GetPotentialTargetsFor_Patch
        //{
        //    public static void Postfix(ref List<IAttackTarget> __result, AttackTargetsCache __instance, IAttackTargetSearcher th)
        //    {
        //        if (Settings.useAllyFaction && th is Pawn pawn)
        //        {
        //            if (RimSpawners.spawnedPawnFaction != null)
        //            {
        //                if (pawn.Faction == RimSpawners.spawnedPawnFaction)
        //                {
        //                    var targets = string.Join(",", __result.Select(x => x.Thing.LabelCap).ToList());
        //                    var hostileToFaction = string.Join(",", __instance.TargetsHostileToFaction(pawn.Faction).Select(x => x.Thing.LabelCap));
        //                    var hostileToColony = string.Join(",", __instance.TargetsHostileToColony.Select(x => x.Thing.LabelCap));
        //                    Log.Message($"{pawn.Name} (targeting {pawn.mindState.enemyTarget?.LabelCap}) GetPotentialTargetsFor ({__result.Count}) {targets}, targets hostile to faction: {hostileToFaction}, targets hostile to colony: {hostileToColony}");
        //                    foreach (var hostile in __instance.TargetsHostileToFaction(pawn.Faction))
        //                    {
        //                        Log.Message($"Hostile: {hostile.Thing.LabelCap}, threat disabled: {hostile.ThreatDisabled(pawn)}, auto targetable: {AttackTargetFinder.IsAutoTargetable(hostile)}");
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

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