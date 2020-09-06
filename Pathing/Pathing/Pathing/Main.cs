using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
            listingStandard.CheckboxLabeled("Enable working", ref settings.enableWorking, "Global work speed multiplied by amount");
            listingStandard.CheckboxLabeled("Enable learning", ref settings.enableLearning, "Global learning speed multiplied by amount");
            listingStandard.CheckboxLabeled("Enable carrying capacity", ref settings.enableCarryingCapacity, "Carrying capacity multiplied by amount");
            listingStandard.Label($"Work multplier: {settings.workMultiplier}");
            settings.workMultiplier = listingStandard.Slider(settings.workMultiplier, 0f, 100f);
            listingStandard.Label($"Learning multplier: {settings.learnMultiplier}");
            settings.learnMultiplier = listingStandard.Slider(settings.learnMultiplier, 0f, 100f);
            listingStandard.Label($"Carrying capacity multplier: {settings.carryingCapacityMultiplier}");
            settings.carryingCapacityMultiplier = listingStandard.Slider(settings.carryingCapacityMultiplier, 0f, 100f);
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
        public bool enableCarryingCapacity;
        public float workMultiplier;
        public float learnMultiplier;
        public float carryingCapacityMultiplier;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enablePathing, "enablePathing");
            Scribe_Values.Look(ref enableWorking, "enableWorking");
            Scribe_Values.Look(ref enableLearning, "enableLearning");
            Scribe_Values.Look(ref enableCarryingCapacity, "enableCarryingCapacity");
            Scribe_Values.Look(ref workMultiplier, "workMultiplier", 1f);
            Scribe_Values.Look(ref learnMultiplier, "learnMultiplier", 1f);
            Scribe_Values.Look(ref carryingCapacityMultiplier, "carryingCapacityMultiplier", 1f);
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
            if (enablePathing && ___pawn.IsColonistPlayerControlled && __instance.Moving)
            {
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