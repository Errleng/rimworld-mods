using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using System.Text;
using RimWorld.QuestGen;

namespace RimCheats
{
    public class RimCheats : Mod
    {
        private RimCheatsSettings settings;
        public RimCheats(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<RimCheatsSettings>();
        }

        private void TextFieldNumericLabeled(Listing_Standard listingStandard, string label, ref float value, float min = RimCheatsSettings.MIN_VALUE, float max = RimCheatsSettings.MAX_VALUE)
        {
            string buffer = null;
            listingStandard.TextFieldNumericLabeled(label, ref value, ref buffer, min, max);
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("PathingToggleLabel".Translate(), ref settings.enablePathing);
            listingStandard.CheckboxLabeled("PathingNonHumanToggleLabel".Translate(), ref settings.enablePathingNonHuman);
            listingStandard.CheckboxLabeled("IgnoreTerrainCostToggleLabel".Translate(), ref settings.disableTerrainCost);
            listingStandard.CheckboxLabeled("IgnoreTerrainCostNonHumanToggleLabel".Translate(), ref settings.disableTerrainCostNonHuman);
            listingStandard.CheckboxLabeled("WorkSpeedToggleLabel".Translate(), ref settings.enableWorking);
            listingStandard.CheckboxLabeled("ToilSpeedToggleLabel".Translate(), ref settings.enableToilSpeed);
            listingStandard.CheckboxLabeled("LearningSpeedToggleLabel".Translate(), ref settings.enableLearning);
            listingStandard.CheckboxLabeled("CarryingCapacityToggleLabel".Translate(), ref settings.enableCarryingCapacity);
            listingStandard.CheckboxLabeled("CarryingCapacityMassToggleLabel".Translate(), ref settings.enableCarryingCapacityMass);
            listingStandard.CheckboxLabeled("CleaningSpeedToggleLabel".Translate(), ref settings.enableInstantCleaning);
            TextFieldNumericLabeled(listingStandard, "WorkSpeedMultiplierLabel".Translate(), ref settings.workMultiplier);
            TextFieldNumericLabeled(listingStandard, "ToilSpeedMultiplierLabel".Translate(), ref settings.toilSpeedMultiplier);
            TextFieldNumericLabeled(listingStandard, "LearningSpeedMultiplierLabel".Translate(), ref settings.learnMultiplier);
            TextFieldNumericLabeled(listingStandard, "CarryingCapacityMultiplierLabel".Translate(), ref settings.carryingCapacityMultiplier);
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "RimCheats";
        }
    }

    public class RimCheatsSettings : ModSettings
    {
        public const float MIN_VALUE = 0;
        public const float MAX_VALUE = 1000;
        public bool enablePathing;
        public bool enablePathingNonHuman;
        public bool enableWorking;
        public bool enableLearning;
        public bool disableTerrainCost;
        public bool disableTerrainCostNonHuman;
        public bool enableCarryingCapacity;
        public bool enableCarryingCapacityMass;
        public bool enableToilSpeed;
        public bool enableInstantCleaning;
        public float workMultiplier;
        public float learnMultiplier;
        public float carryingCapacityMultiplier;
        public float toilSpeedMultiplier;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enablePathing, "enablePathing");
            Scribe_Values.Look(ref enablePathingNonHuman, "enablePathingNonHuman");
            Scribe_Values.Look(ref enableWorking, "enableWorking");
            Scribe_Values.Look(ref enableToilSpeed, "enableToilSpeed");
            Scribe_Values.Look(ref enableLearning, "enableLearning");
            Scribe_Values.Look(ref disableTerrainCost, "disableTerrainCost");
            Scribe_Values.Look(ref disableTerrainCostNonHuman, "disableTerrainCostNonHuman");
            Scribe_Values.Look(ref enableCarryingCapacity, "enableCarryingCapacity");
            Scribe_Values.Look(ref enableCarryingCapacityMass, "enableCarryingCapacityMass");
            Scribe_Values.Look(ref enableInstantCleaning, "enableInstantCleaning");
            Scribe_Values.Look(ref workMultiplier, "workMultiplier", 1f);
            Scribe_Values.Look(ref toilSpeedMultiplier, "toilSpeedMultiplier", 1f);
            Scribe_Values.Look(ref learnMultiplier, "learnMultiplier", 1f);
            Scribe_Values.Look(ref carryingCapacityMultiplier, "carryingCapacityMultiplier", 1f);
            base.ExposeData();
        }
    }

    [StaticConstructorOnStartup]
    public class Patcher
    {
        static RimCheatsSettings settings;

        static Patcher()
        {
            settings = LoadedModManager.GetMod<RimCheats>().GetSettings<RimCheatsSettings>();
            var harmony = new Harmony("com.rimcheats.rimworld.mod");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
            Log.Message("RimCheats loaded");
        }

        [HarmonyPatch(typeof(Pawn_PathFollower), "PatherTick")]
        class PatchPawn_PathFollowerPatherTick
        {
            private static IntVec3 lastPos;

            static void Postfix(Pawn_PathFollower __instance, Pawn ___pawn)
            {
                bool appliesToPawn = false;
                if (settings.enablePathing)
                {
                    appliesToPawn = ___pawn.IsColonistPlayerControlled;
                }
                if (settings.enablePathingNonHuman && !___pawn.IsColonistPlayerControlled)
                {
                    appliesToPawn = ___pawn.Faction != null && ___pawn.Faction.IsPlayer;
                }
                if (appliesToPawn && __instance.Moving)
                {
                    // disable speed on wander or waiting for better idle pawn performance
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
                bool appliesToPawn = false;
                if (settings.disableTerrainCost)
                {
                    appliesToPawn = pawn.IsColonistPlayerControlled;
                }
                if (settings.disableTerrainCostNonHuman && !pawn.IsColonistPlayerControlled)
                {
                    appliesToPawn = pawn.Faction != null && pawn.Faction.IsPlayer;
                }
                if (appliesToPawn)
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
                    bool enableWorking = settings.enableWorking;
                    bool enableLearning = settings.enableLearning;
                    bool enableCarryingCapacity = settings.enableCarryingCapacity;

                    if (enableWorking && stat.Equals(StatDefOf.WorkSpeedGlobal))
                    {
                        float workMultiplier = settings.workMultiplier;
                        __result *= workMultiplier;
                    }
                    else if (enableLearning && stat.Equals(StatDefOf.GlobalLearningFactor))
                    {
                        float learnMultiplier = settings.learnMultiplier;
                        __result *= learnMultiplier;
                    }
                    else if (enableCarryingCapacity && stat.Equals(StatDefOf.CarryingCapacity))
                    {
                        float carryingCapacityMultiplier = settings.carryingCapacityMultiplier;
                        __result *= carryingCapacityMultiplier;
                    }
                }
            }
        }

        //[HarmonyPatch(typeof(Toils_General), "Wait")]
        //class Patch_Toils_General_Wait
        //{
        //    static void Postfix(ref Toil __result)
        //    {
        //        bool enableToilSpeed = settings.enableToilSpeed;
        //        if (enableToilSpeed)
        //        {
        //            Toil toil = __result;
        //            __result.initAction = delegate ()
        //            {
        //                if (toil.actor.IsColonistPlayerControlled)
        //                {
        //                    float toilSpeedMultiplier = settings.toilSpeedMultiplier;

        //                    float oldDefaultDuration = toil.defaultDuration;
        //                    float oldTicksLeftThisToil = toil.actor.jobs.curDriver.ticksLeftThisToil;
        //                    toil.defaultDuration = (int)(toil.defaultDuration / toilSpeedMultiplier);
        //                    //Log.Message($"Toil defaultDuration: {oldDefaultDuration} -> {toil.defaultDuration}");
        //                    toil.actor.jobs.curDriver.ticksLeftThisToil = (int)(toil.actor.jobs.curDriver.ticksLeftThisToil / toilSpeedMultiplier);
        //                    //Log.Message($"CurDriver ticksLeftThisToil: {oldTicksLeftThisToil} -> {toil.actor.jobs.curDriver.ticksLeftThisToil}");
        //                }
        //                toil.actor.pather.StopDead();
        //            };
        //        }
        //    }
        //}

        [HarmonyPatch(typeof(Pawn_JobTracker), "JobTrackerTick")]
        class Patch_Pawn_JobTracker_JobTrackerTick
        {
            static void Postfix(Pawn_JobTracker __instance)
            {
                if (settings.enableToilSpeed)
                {
                    if (__instance.curDriver != null)
                    {
                        int multiplier = Convert.ToInt32(settings.toilSpeedMultiplier);
                        for (int i = 0; i < multiplier; i++)
                        {
                            __instance.curDriver.DriverTick();
                        }
                    }
                }
            }
        }

        // carrying capacity is different from "mass capacity"/"inventory space"
        [HarmonyPatch(typeof(MassUtility), "Capacity")]
        public static class MassUtility_Capacity_Patch
        {
            [HarmonyPrefix]
            public static bool Capacity(ref float __result, Pawn p, StringBuilder explanation = null)
            {
                bool enableCarryingCapacityMass = settings.enableCarryingCapacityMass;
                if (enableCarryingCapacityMass)
                {
                    if (!MassUtility.CanEverCarryAnything(p))
                    {
                        __result = 0f;
                        return false;
                    }

                    float capacity = p.BodySize * 35f * p.GetStatValue(StatDefOf.CarryingCapacity);

                    if (explanation != null)
                    {
                        if (explanation.Length > 0)
                        {
                            explanation.AppendLine();
                        }
                        explanation.Append("  - " + p.LabelShortCap + ": " + capacity.ToStringMassOffset());
                    }
                    __result = capacity;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(JobDriver_CleanFilth), "Filth", MethodType.Getter)]
        class Patch_JobDriver_CleanFilth_Filth
        {
            static void Postfix(ref Filth __result)
            {
                if (settings.enableInstantCleaning)
                {
                    if (__result.thickness < 0)
                    {
                        __result.Destroy();
                    }
                    else
                    {
                        __result.thickness = 0;
                    }
                }
            }
        }

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
    }
}