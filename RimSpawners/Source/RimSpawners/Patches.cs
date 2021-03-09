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
                DeathOnDownedChance comp = ___pawn.GetComp<DeathOnDownedChance>();
                if (comp != null)
                {
                    // roll the dice for the pawn dying
                    if (Rand.Chance(comp.Props.deathChance))
                    {
                        ___pawn.Kill(dinfo, null);
                        return false;
                    }
                }

                // the spawner Lord has LordJob.RemoveDownedPawns = true
                //   cannot loop over spawnedPawns later to kill downed, must kill before MakeDowned runs
                if ((___pawn.Faction != null) && ___pawn.Faction.IsPlayer)
                {
                    RimSpawnersPawnComp customThingComp = ___pawn.GetComp<RimSpawnersPawnComp>();
                    if (customThingComp != null)
                    {
                        ___pawn.Kill(dinfo, null);
                    }

                    // old method without ThingComp
                    //if (___pawn.Map.IsPlayerHome)
                    //{
                    //    Log.Message($"Checking to see if {___pawn.Label} is from a spawner");
                    //    IEnumerable<UniversalSpawner> spawners = ___pawn.Map.listerBuildings.AllBuildingsColonistOfClass<UniversalSpawner>();
                    //    foreach (UniversalSpawner spawner in spawners)
                    //    {
                    //        CompSpawnerPawn cps = spawner.GetComp<CompSpawnerPawn>();
                    //        if (cps.spawnedPawns.Contains(___pawn))
                    //        {
                    //            Log.Message($"{___pawn.Label} is from a spawner and is being killed on downed");
                    //            ___pawn.Kill(dinfo, null);
                    //            return false;
                    //        }
                    //    }
                    //}
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
                        // maybe call pawn DeathActionWorker here (e.g. boomalopes explode on death)

                        __instance.Destroy();
                        return false;
                    }
                }
                return true;
            }
        }


        // handles death message of pawns
        //[HarmonyPatch(typeof(Pawn_HealthTracker), "NotifyPlayerOfKilled")]
        //class Pawn_HealthTracker_NotifyPlayerOfKilled_Patch
        //{
        //    public static bool Prefix(Pawn ___pawn)
        //    {
        //        DeathOnDownedChance comp = ___pawn.GetComp<DeathOnDownedChance>();
        //        if (comp != null)
        //        {
        //            // need relations to be non-null for SetFaction to notify
        //            if (___pawn.relations == null)
        //            {
        //                ___pawn.relations = new RimWorld.Pawn_RelationsTracker(___pawn);
        //            }
        //            ___pawn.SetFaction(null, null);
        //        }
        //        return true;
        //    }
        //}

        [HarmonyPatch(typeof(CompSpawnerPawn), "TrySpawnPawn")]
        class CompSpawnerPawn_TrySpawnPawn_Patch
        {
            public static bool Prefix(CompSpawnerPawn __instance, PawnKindDef ___chosenKind)
            {
                if (__instance.parent.Faction.IsPlayer)
                {
                    UniversalSpawner us = __instance.parent as UniversalSpawner;
                    if ((us != null) && (___chosenKind != null))
                    {
                        // handle special pawns
                        if ((___chosenKind.lifeStages.Count == 0) && (___chosenKind.RaceProps.Humanlike))
                        {

                            ___chosenKind.lifeStages = new List<PawnKindLifeStage>();

                            // TrySpawnPawn picks the age of the pawn at lifeStageAges[(lifeStages.Count - 1)]
                            int numLifeStages = ___chosenKind.race.race.lifeStageAges.Count;
                            PawnKindLifeStage placeholderLifeStage = new PawnKindLifeStage();
                            for (int i = 0; i < numLifeStages; i++)
                            {
                                ___chosenKind.lifeStages.Add(placeholderLifeStage);
                            }
                        }
                    }
                }
                return true;
            }

            public static void Postfix(CompSpawnerPawn __instance, PawnKindDef ___chosenKind, ref Pawn pawn)
            {
                if (__instance.parent.Faction.IsPlayer)
                {
                    UniversalSpawner us = __instance.parent as UniversalSpawner;
                    if ((us != null) && (us.GetChosenKind() != null))
                    {
                        // pawn spawned notification
                        Messages.Message($"{___chosenKind.label} assembly complete".Translate(), __instance.parent, MessageTypeDefOf.PositiveEvent, true);

                        RimSpawnersPawnComp customThingComp = new RimSpawnersPawnComp();
                        RimSpawnersPawnCompProperties customThingCompProps = new RimSpawnersPawnCompProperties();
                        customThingComp.parent = pawn;
                        pawn.AllComps.Add(customThingComp);
                        customThingComp.Initialize(customThingCompProps);

                        Lord currentLord = pawn.GetLord();
                    }
                }
            }
        }


        [HarmonyPatch(typeof(PawnDiedOrDownedThoughtsUtility), "GetThoughts")]
        class PawnDiedOrDownedThoughtsUtility_GetThoughts
        {
            public static bool Prefix(Pawn victim)
            {
                RimSpawnersPawnComp customThingComp = victim.GetComp<RimSpawnersPawnComp>();
                if (customThingComp != null)
                {
                    return false;
                }
                return true;
            }
        }
    }
}
