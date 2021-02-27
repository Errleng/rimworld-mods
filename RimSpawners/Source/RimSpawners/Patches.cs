using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Verse;
using RimWorld;

namespace RimSpawners
{
    [StaticConstructorOnStartup]
    public class Patcher
    {
        static Patcher()
        {
            Harmony harmony = new Harmony("com.rimspawners.rimworld.mod");
            Assembly assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
            Log.Message("RimSpawners loaded");
        }
    }


    // if a pawn is downed and has the DeathOnDownedChance comp, kill it according to DeathChance
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
            return true;
        }
    }


    // handles death message of pawns
    [HarmonyPatch(typeof(Pawn_HealthTracker), "NotifyPlayerOfKilled")]
    class Pawn_HealthTracker_NotifyPlayerOfKilled_Patch
    {
        public static bool Prefix(Pawn ___pawn)
        {
            DeathOnDownedChance comp = ___pawn.GetComp<DeathOnDownedChance>();
            if (comp != null)
            {
                // need relations to be non-null for SetFaction to notify
                if (___pawn.relations == null)
                {
                    ___pawn.relations = new RimWorld.Pawn_RelationsTracker(___pawn);
                }
                ___pawn.SetFaction(null, null);
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CompSpawnerPawn), "TrySpawnPawn")]
    class CompSpawnerPawn_TrySpawnPawn_Patch
    {
        public static void Postfix(CompSpawnerPawn __instance)
        {
            Log.Message($"Spawner {__instance.parent.Label} of faction {__instance.parent.Faction.Name}, which is player: {__instance.parent.Faction.IsPlayer}");
            if (__instance.parent.Faction.IsPlayer)
            {
                UniversalSpawner us = __instance.parent as UniversalSpawner;
                if (us != null)
                {
                    us.KillDownedPawns();
                }
            }
        }
    }
}
