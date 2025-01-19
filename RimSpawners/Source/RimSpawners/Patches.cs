using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace RimSpawners
{
    public class Patcher
    {
        private static readonly RimSpawnersSettings Settings = RimSpawners.settings;

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
                        customThingComp.Props.Recycle(__instance);
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

        [HarmonyPatch(typeof(JobGiver_AITrashColonyClose), "TryGiveJob")]
        private class JobGiver_AITrashColonyClose_Patch
        {
            public static bool Prefix(JobGiver_AITrashColonyClose __instance, Pawn pawn, ref Job __result)
            {
                if (!pawn.HasComp<RimSpawnersPawnComp>() || pawn.HostileTo(Faction.OfPlayer))
                {
                    return true;
                }
                CellRect cellRect = CellRect.CenteredOn(pawn.Position, 5);
                for (int i = 0; i < 35; i++)
                {
                    IntVec3 randomCell = cellRect.RandomCell;
                    if (randomCell.InBounds(pawn.Map))
                    {
                        Building edifice = randomCell.GetEdifice(pawn.Map);
                        if (edifice != null && edifice.HostileTo(Faction.OfPlayer) && TrashUtility.ShouldTrashBuilding(pawn, edifice, true) && GenSight.LineOfSight(pawn.Position, randomCell, pawn.Map))
                        {
                            if (DebugViewSettings.drawDestSearch && Find.CurrentMap == pawn.Map)
                            {
                                Find.CurrentMap.debugDrawer.FlashCell(randomCell, 1f, "trash bld", 50);
                            }
                            Job job = TrashUtility.TrashJob(pawn, edifice, true, false);
                            if (job != null)
                            {
                                __result = job;
                                return false;
                            }
                        }
                        if (DebugViewSettings.drawDestSearch && Find.CurrentMap == pawn.Map)
                        {
                            Find.CurrentMap.debugDrawer.FlashCell(randomCell, 0f, "trash no", 50);
                        }
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(JobGiver_AITrashBuildingsDistant), "TryGiveJob")]
        private class JobGiver_AITrashBuildingsDistant_Patch
        {
            public static bool Prefix(JobGiver_AITrashBuildingsDistant __instance, Pawn pawn, ref Job __result)
            {
                if (!pawn.HasComp<RimSpawnersPawnComp>() || pawn.HostileTo(Faction.OfPlayer))
                {
                    return true;
                }
                List<Building> possibleTargets = pawn.Map.listerBuildings.allBuildingsNonColonist.Where(x => x.HostileTo(Faction.OfPlayer)).ToList();
                if (possibleTargets.Count == 0)
                {
                    return true;
                }
                for (int i = 0; i < 75; i++)
                {
                    Building target = possibleTargets.RandomElement();
                    if (TrashUtility.ShouldTrashBuilding(pawn, target, true))
                    {
                        Job job = TrashUtility.TrashJob(pawn, target, true, false);
                        if (job != null)
                        {
                            __result = job;
                        }
                    }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(Thing), "TakeDamage")]
        private class Thing_TakeDamage_Patch
        {
            static void Prefix(ref DamageInfo dinfo, Thing __instance)
            {
                if (dinfo.Instigator == null)
                {
                    return;
                }
                Pawn pawn = dinfo.Instigator as Pawn;
                if (pawn == null)
                {
                    return;
                }
                if (!pawn.HasComp<RimSpawnersPawnComp>() || pawn.HostileTo(Faction.OfPlayer))
                {
                    return;
                }

                Thing victim = __instance;
                if (Settings.doNotDamagePlayerBuildings && victim.def.category == ThingCategory.Building && victim.Faction == Faction.OfPlayer)
                {
                    dinfo.SetAmount(0);
                    return;
                }
                if (Settings.doNotDamageFriendlies && !victim.HostileTo(dinfo.Instigator))
                {
                    dinfo.SetAmount(0);
                    return;
                }
                if (Settings.massivelyDamageEnemyBuildings && victim.Faction.HostileTo(Faction.OfPlayer))
                {
                    dinfo.SetAmount(dinfo.Amount * 10);
                    return;
                }
            }
        }

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