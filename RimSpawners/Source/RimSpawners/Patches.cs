using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Verse;

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


    public class CompProperties_DeathOnDownedChance : CompProperties
    {
        public float deathChance; // value between 0 and 1 inclusive

        public CompProperties_DeathOnDownedChance()
        {
            compClass = typeof(DeathOnDownedChance);
        }
    }


    public class DeathOnDownedChance : ThingComp
    {
        public CompProperties_DeathOnDownedChance Props
        {
            get
            {
                return Props;
            }
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
                }
                ___pawn.Kill(dinfo, null);
                return false;
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
}
