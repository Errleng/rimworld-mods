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

        static UniversalSpawner GetUniversalSpawner(CompSpawnerPawn cps)
        {
            if (cps.parent.Faction.IsPlayer)
            {
                UniversalSpawner us = cps.parent as UniversalSpawner;
                return us;
            }
            return null;
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
                        __instance.SetFaction(null, null);

                        __instance.inventory.DestroyAll();
                        __instance.apparel.DestroyAll();
                        __instance.equipment.DestroyAllEquipment();
                    }
                }
                return true;
            }

            public static void Postfix(Pawn __instance)
            {
                RimSpawnersPawnComp customThingComp = __instance.GetComp<RimSpawnersPawnComp>();
                if ((customThingComp != null) && settings.disableCorpses)
                {
                    if (__instance.Corpse != null)
                    {
                        __instance.Corpse.Destroy();
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PawnDiedOrDownedThoughtsUtility), "GetThoughts")]
        class PawnDiedOrDownedThoughtsUtility_GetThoughts
        {
            public static bool Prefix(Pawn victim)
            {
                // prevent spawned humanlike pawns from causing mood debuffs (e.g. "Colonist died")
                RimSpawnersPawnComp customThingComp = victim.GetComp<RimSpawnersPawnComp>();
                if (customThingComp != null)
                {
                    return false;
                }
                return true;
            }
        }

        class CompSpawnerPawn_Patches
        {

            [HarmonyPatch(typeof(CompSpawnerPawn), "TrySpawnPawn")]
            class CompSpawnerPawn_TrySpawnPawn_Patch
            {
                public static void Prefix(CompSpawnerPawn __instance, PawnKindDef ___chosenKind)
                {
                    // before spawn hooks and logic
                    if (__instance.parent.Faction.IsPlayer)
                    {
                        UniversalSpawner us = __instance.parent as UniversalSpawner;
                        if ((us != null) && (___chosenKind != null))
                        {
                            // handle humanlikes, which have no lifeStages (causes errors)
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
                }

                public static void Postfix(CompSpawnerPawn __instance, PawnKindDef ___chosenKind, ref Pawn pawn)
                {
                    // on spawn hooks and logic
                    if (GetUniversalSpawner(__instance) != null)
                    {
                        // pawn spawned notification
                        Messages.Message($"{___chosenKind.label} assembly complete".Translate(), __instance.parent, MessageTypeDefOf.PositiveEvent, true);

                        if (___chosenKind.race.race.Humanlike)
                        {
                            // fix humanlike ai
                            pawn.playerSettings.hostilityResponse = HostilityResponseMode.Attack;

                            if (ModLister.RoyaltyInstalled)
                            {
                                // disable royalty titles because
                                //   the pawn may use permits or psychic powers (too powerful)
                                //   on death, the pawn's title may be inherited by a colonist
                                List<RoyalTitle> titles = pawn.royalty.AllTitlesForReading;
                                List<Faction> titleFactions = titles.Select(title => title.faction).Distinct().ToList();
                                foreach (Faction faction in titleFactions)
                                {
                                    pawn.royalty.SetTitle(faction, null, false, false, false);
                                }
                            }
                        }


                        // add custom ThingComp to spawned pawn
                        RimSpawnersPawnComp customThingComp = new RimSpawnersPawnComp();
                        RimSpawnersPawnCompProperties customThingCompProps = new RimSpawnersPawnCompProperties();
                        customThingComp.parent = pawn;
                        pawn.AllComps.Add(customThingComp);
                        customThingComp.Initialize(customThingCompProps);
                    }
                }
            }

            [HarmonyPatch(typeof(CompSpawnerPawn), "CompTick")]
            class CompSpawnerPawn_CompTick_Patch
            {
                public static bool Prefix(CompSpawnerPawn __instance)
                {
                    UniversalSpawner us = GetUniversalSpawner(__instance);
                    if (us != null)
                    {
                        // continue spawning pawns while under threat
                        if (settings.spawnOnlyOnThreat && !us.ThreatActive)
                        {
                            // CompTick only deals with interval spawns
                            // Disable CompTick and call TrySpawnPawn using SpawnPawnsUntilPoints
                            return false;
                        }
                    }

                    return true;
                }
            }

            [HarmonyPatch(typeof(CompSpawnerPawn), "CalculateNextPawnSpawnTick", new Type[] { typeof(float) })]
            class CompSpawnerPawn_CalculateNextPawnSpawnTick_Patch
            {
                public static void Prefix(CompSpawnerPawn __instance, ref float delayTicks, PawnKindDef ___chosenKind)
                {
                    // custom calculation for nextSpawnTick
                    if (GetUniversalSpawner(__instance) != null)
                    {
                        if (settings.scaleSpawnIntervals)
                        {
                            float secondsToNextSpawn = ___chosenKind.combatPower / settings.pointsPerSecond;
                            float ticksToNextSpawn = GenTicks.SecondsToTicks(secondsToNextSpawn);
                            delayTicks = ticksToNextSpawn;
                        }
                    }
                }
            }
        }
    }
}
