using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace RimCheats
{
    public class RimCheats : Mod
    {
        private RimCheatsSettings settings;
        public RimCheats(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<RimCheatsSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("Enable pathing", ref settings.enablePathing, "Paths and moves in one tick");
            listingStandard.CheckboxLabeled("Enable ignore terrain cost", ref settings.disableTerrainCost, "Colonists ignore terrain movement penalties");
            listingStandard.CheckboxLabeled("Enable working", ref settings.enableWorking, "Global work speed multiplied by amount");
            listingStandard.CheckboxLabeled("Enable learning", ref settings.enableLearning, "Global learning speed multiplied by amount");
            listingStandard.CheckboxLabeled("Enable carrying capacity", ref settings.enableCarryingCapacity, "Carrying capacity multiplied by amount");
            listingStandard.CheckboxLabeled("Enable faster progress bar toils", ref settings.enableFasterProgressBars, "Multiply the speed that toils with progress bars are completed");
            listingStandard.Label($"Work multplier: {settings.workMultiplier}");
            settings.workMultiplier = listingStandard.Slider(settings.workMultiplier, 0f, 100f);
            listingStandard.Label($"Learning multplier: {settings.learnMultiplier}");
            settings.learnMultiplier = listingStandard.Slider(settings.learnMultiplier, 0f, 10000f);
            listingStandard.Label($"Carrying capacity multplier: {settings.carryingCapacityMultiplier}");
            settings.carryingCapacityMultiplier = listingStandard.Slider(settings.carryingCapacityMultiplier, 0f, 100f);
            listingStandard.Label($"Progress bar speed multplier: {settings.progressBarSpeedMultiplier}");
            settings.progressBarSpeedMultiplier = listingStandard.Slider(settings.progressBarSpeedMultiplier, 0f, 100f);
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "RimCheats".Translate();
        }
    }

    public class RimCheatsSettings : ModSettings
    {
        public bool enablePathing;
        public bool enableWorking;
        public bool enableLearning;
        public bool disableTerrainCost;
        public bool enableCarryingCapacity;
        public bool enableFasterProgressBars;
        public float workMultiplier;
        public float learnMultiplier;
        public float carryingCapacityMultiplier;
        public float progressBarSpeedMultiplier;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enablePathing, "enablePathing");
            Scribe_Values.Look(ref enableWorking, "enableWorking");
            Scribe_Values.Look(ref enableLearning, "enableLearning");
            Scribe_Values.Look(ref disableTerrainCost, "disableTerrainCost");
            Scribe_Values.Look(ref enableCarryingCapacity, "enableCarryingCapacity");
            Scribe_Values.Look(ref enableFasterProgressBars, "enableFasterProgressBars");
            Scribe_Values.Look(ref workMultiplier, "workMultiplier", 1f);
            Scribe_Values.Look(ref learnMultiplier, "learnMultiplier", 1f);
            Scribe_Values.Look(ref carryingCapacityMultiplier, "carryingCapacityMultiplier", 1f);
            Scribe_Values.Look(ref progressBarSpeedMultiplier, "progressBarSpeedMultiplier", 1f);
            base.ExposeData();
        }
    }

    [StaticConstructorOnStartup]
    public class Patcher
    {
        static Patcher()
        {
            var harmony = new Harmony("com.pathing.rimworld.mod");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
            Log.Message("Pathing mod loaded");
        }
    }

    [HarmonyPatch(typeof(Pawn_PathFollower), "PatherTick")]
    class PatchPawn_PathFollowerPatherTick
    {
        private static IntVec3 lastPos;

        static void Postfix(Pawn_PathFollower __instance, Pawn ___pawn)
        {
            bool enablePathing = LoadedModManager.GetMod<RimCheats>().GetSettings<RimCheatsSettings>().enablePathing;
            if (___pawn.IsColonistPlayerControlled && __instance.Moving && enablePathing)
            {
                if (___pawn.CurJob != null && (___pawn.CurJob.def == JobDefOf.GotoWander || ___pawn.CurJob.def == JobDefOf.Wait_Wander))
                {
                    return;
                }

                if (__instance.nextCellCostLeft > 0f)
                {
                    __instance.nextCellCostLeft = 0;
                }

                if (!___pawn.Position.Equals(lastPos))
                {
                    lastPos = ___pawn.Position;
                    __instance.PatherTick();
                }
                else
                {
                    lastPos = ___pawn.Position;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_PathFollower), "CostToMoveIntoCell", new Type[] { typeof(Pawn), typeof(IntVec3) })]
    class PatchPawn_CostToMoveIntoCell
    {
        private static IntVec3 lastPos;

        static void Postfix(Pawn_PathFollower __instance, Pawn pawn, IntVec3 c, ref int __result)
        {
            bool disableTerrainCost = LoadedModManager.GetMod<RimCheats>().GetSettings<RimCheatsSettings>().disableTerrainCost;
            if (pawn.IsColonistPlayerControlled && disableTerrainCost)
            {
                // based off floating pawn code from Alpha Animals
                int cost = __result;
                if (cost < 10000)
                {
                    if (c.x == pawn.Position.x || c.z == pawn.Position.z)
                    {
                        cost = pawn.TicksPerMoveCardinal;
                    }
                    else
                    {
                        cost = pawn.TicksPerMoveDiagonal;
                    }
                    TerrainDef terrainDef = pawn.Map.terrainGrid.TerrainAt(c);
                    if (terrainDef == null)
                    {
                        cost = 10000;
                    }
                    else if (terrainDef.passability == Traversability.Impassable && !terrainDef.IsWater)
                    {
                        cost = 10000;
                    }
                    //else if (terrainDef.IsWater)
                    //{
                    //    cost = 10000;
                    //}
                    List<Thing> list = pawn.Map.thingGrid.ThingsListAt(c);
                    for (int i = 0; i < list.Count; i++)
                    {
                        Thing thing = list[i];
                        if (thing.def.passability == Traversability.Impassable)
                        {
                            cost = 10000;
                        }
                        //if (thing is Building_Door)
                        //{
                        //    cost += 45;
                        //}
                    }
                    __result = cost;
                }
            }
        }
    }

    [HarmonyPatch(typeof(StatExtension), "GetStatValue")]
    class PatchStatExtensionGetStatValue
    {
        static void Postfix(Thing thing, StatDef stat, bool applyPostProcess, ref float __result)
        {
            Pawn pawn = thing as Pawn;
            if ((pawn != null) && (pawn.IsColonistPlayerControlled))
            {
                bool enableWorking = LoadedModManager.GetMod<RimCheats>().GetSettings<RimCheatsSettings>().enableWorking;
                bool enableLearning = LoadedModManager.GetMod<RimCheats>().GetSettings<RimCheatsSettings>().enableLearning;
                bool enableCarryingCapacity = LoadedModManager.GetMod<RimCheats>().GetSettings<RimCheatsSettings>().enableCarryingCapacity;

                if (enableWorking && stat.Equals(StatDefOf.WorkSpeedGlobal))
                {
                    float workMultiplier = LoadedModManager.GetMod<RimCheats>().GetSettings<RimCheatsSettings>().workMultiplier;
                    __result *= workMultiplier;
                }
                else if (enableLearning && stat.Equals(StatDefOf.GlobalLearningFactor))
                {
                    float learnMultiplier = LoadedModManager.GetMod<RimCheats>().GetSettings<RimCheatsSettings>().learnMultiplier;
                    __result *= learnMultiplier;
                }
                else if (enableCarryingCapacity && stat.Equals(StatDefOf.CarryingCapacity))
                {
                    float carryingCapacityMultiplier = LoadedModManager.GetMod<RimCheats>().GetSettings<RimCheatsSettings>().carryingCapacityMultiplier;
                    __result *= carryingCapacityMultiplier;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Toils_General), "Wait")]
    class Patch_Toils_General_Wait
    {
        static void Postfix(ref Toil __result)
        {
            bool enableFasterProgressBars = LoadedModManager.GetMod<RimCheats>().GetSettings<RimCheatsSettings>().enableFasterProgressBars;
            if (enableFasterProgressBars)
            {
                Toil toil = __result;
                __result.initAction = delegate ()
                {
                    if (toil.actor.IsColonistPlayerControlled)
                    {
                        float progressBarSpeedMultiplier = LoadedModManager.GetMod<RimCheats>().GetSettings<RimCheatsSettings>().progressBarSpeedMultiplier;

                        float oldDefaultDuration = toil.defaultDuration;
                        float oldTicksLeftThisToil = toil.actor.jobs.curDriver.ticksLeftThisToil;
                        toil.defaultDuration = (int)(toil.defaultDuration / progressBarSpeedMultiplier);
                        //Log.Message($"Toil defaultDuration: {oldDefaultDuration} -> {toil.defaultDuration}");
                        toil.actor.jobs.curDriver.ticksLeftThisToil = (int)(toil.actor.jobs.curDriver.ticksLeftThisToil / progressBarSpeedMultiplier);
                        //Log.Message($"CurDriver ticksLeftThisToil: {oldTicksLeftThisToil} -> {toil.actor.jobs.curDriver.ticksLeftThisToil}");
                    }
                    toil.actor.pather.StopDead();
                };
            }
        }
    }

    // carrying capacity is different from "mass capacity"/"inventory space"
    //[HarmonyPatch(typeof(MassUtility), "Capacity")]
    //public static class MassUtility_Capacity_Patch
    //{
    //    [HarmonyPrefix]
    //    public static bool Capacity(ref float __result, Pawn p, StringBuilder explanation)
    //    {
    //        if (!MassUtility.CanEverCarryAnything(p))
    //        {
    //            __result = 0f;
    //            return false;
    //        }

    //        __result = p.BodySize * p.GetStatValue(StatDefOf.CarryingCapacity);
    //        if (explanation != null)
    //        {
    //            if (explanation.Length > 0)
    //            {
    //                explanation.AppendLine();
    //            }
    //            explanation.Append("  - " + p.LabelShortCap + ": " + __result.ToStringMassOffset());
    //        }
    //        return false;
    //    }
    //}

    // faster job speed
    //[HarmonyPatch(typeof(JobDriver), "DriverTick")]
    //class PatchJobDriverDriverTick
    //{
    //    static void Postfix(JobDriver __instance, bool ___wantBeginNextToil, ToilCompleteMode ___curToilCompleteMode, int ___curToilIndex, List<Toil> ___toils)
    //    {
    //        if ((__instance.pawn != null) && __instance.pawn.IsColonistPlayerControlled)
    //        {
    //            if (___curToilIndex < 0 || __instance.job == null || __instance.pawn.CurJob != __instance.job)
    //            {
    //                return;
    //            }
    //            if (___curToilIndex >= ___toils.Count)
    //            {
    //                return;
    //            }
    //            Toil curToil = ___toils[___curToilIndex];
    //            if (curToil != null)
    //            {
    //                Job job = __instance.job;
    //                JobDef jobDef = job.def;
    //                if (jobDef.joyDuration == 0 || ((jobDef.joyDuration == 4000) && (jobDef.joyGainRate == 1f) && (jobDef.joyMaxParticipants == 1)))
    //                {
    //                    __instance.ticksLeftThisToil = 0;
    //                }
    //            }
    //        }
    //    }
    //}

    //[HarmonyPatch(typeof(JobDriver_CleanFilth), "Filth", MethodType.Getter)]
    //class PatchJobDriver_CleanFilthFilth
    //{
    //    static void Postfix(ref Filth __result)
    //    {
    //        if (!__result.Destroyed)
    //        {
    //            __result.Destroy(DestroyMode.Vanish);
    //        }
    //    }
    //}
}