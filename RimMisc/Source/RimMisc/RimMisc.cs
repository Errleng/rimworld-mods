using HarmonyLib;
using RimWorld;
using RocketMan;
using Soyuz;
using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimMisc
{
    [StaticConstructorOnStartup]
    public class Loader
    {
        static Loader()
        {
            RimMisc.Settings.ApplySettings();
            AddComps();
            if (RimMisc.Settings.patchBuildingHp)
            {
                PatchBuildingHP();
            }
        }

        public static void AddComps()
        {
            var things = DefDatabase<ThingDef>.AllDefs;
            foreach (var thingDef in things)
            {
                if (typeof(ThingWithComps).IsAssignableFrom(thingDef.thingClass) && thingDef.destroyable)
                {
                    thingDef.comps.Add(new CompProperties(typeof(CompMeleeAttackable)));
                }
                if (thingDef.HasComp(typeof(CompFlickable)))
                {
                    thingDef.comps.Add(new CompProperties(typeof(CompThreatToggle)));
                }
            }
        }

        public static void PatchBuildingHP()
        {
            Predicate<ThingDef> isValidBuilding = delegate (ThingDef def)
            {
                return def.IsBuildingArtificial &&
                (def.building.buildingTags.Contains("Production") || def.IsWorkTable);
            };

            foreach (var def in DefDatabase<ThingDef>.AllDefs)
            {
                if (isValidBuilding(def))
                {
                    def.SetStatBaseValue(StatDefOf.MaxHitPoints, 100000);
                }
            }

            if (Find.CurrentMap != null)
            {
                foreach (var building in Find.CurrentMap.listerBuildings.allBuildingsColonist)
                {
                    if (isValidBuilding(building.def))
                    {
                        building.HitPoints = building.MaxHitPoints;
                    }
                }
            }
        }
    }

    public class RimMisc : Mod
    {
        private static readonly float SEARCH_RESULT_ROW_HEIGHT = 30f;
        private static readonly float ITEM_PADDING = 10f;
        private static readonly float BUTTON_WIDTH = 60f;
        private static readonly float SCROLLBAR_WIDTH = 20;
        private static readonly float MIN_AUTOCLOSE_SECONDS = RimMiscWorldComponent.AUTO_CLOSE_LETTERS_CHECK_TICKS.TicksToSeconds();
        private static readonly float MAX_AUTOCLOSE_SECONDS = 600;

        public static RimMiscSettings Settings;
        private Vector2 settingsScrollPos;

        public RimMisc(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RimMiscSettings>();
            var harmony = new Harmony("com.rimmisc.rimworld.mod");
            harmony.PatchAll();
            foreach (var method in harmony.GetPatchedMethods())
            {
                Log.Message($"RimMisc patched {method.Name}");
            }
            Log.Message("RimMisc loaded");
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var settingsRect = inRect.TopPart(0.5f).Rounded();
            var listingStandard = new Listing_Standard();
            var listHeight = 400;
            Rect listingRect = new Rect(inRect.x, inRect.y, inRect.width - 40, inRect.height + 20 + listHeight);
            listingStandard.Begin(listingRect);

            var outRect = new Rect(0, 0, settingsRect.width - SCROLLBAR_WIDTH, settingsRect.height - 20);
            var viewRect = new Rect(0, 0, listingRect.width, listHeight);
            Widgets.BeginScrollView(outRect, ref settingsScrollPos, viewRect);

            listingStandard.CheckboxLabeled("RimMisc_DefaultDoUntil".Translate(), ref Settings.defaultDoUntil);
            listingStandard.CheckboxLabeled("RimMisc_AutoCloseLetters".Translate(), ref Settings.autoCloseLetters);
            listingStandard.CheckboxLabeled("RimMisc_DisableEnemyUninstall".Translate(), ref Settings.disableEnemyUninstall);
            listingStandard.CheckboxLabeled("RimMisc_KillDownedPawns".Translate(), ref Settings.killDownedPawns);
            listingStandard.CheckboxLabeled("RimMisc_PatchBuildingHp".Translate(), ref Settings.patchBuildingHp);
            listingStandard.CheckboxLabeled("RimMisc_PreventSkyfallDestruction".Translate(), ref Settings.preventSkyfallDestruction);
            listingStandard.CheckboxLabeled("RimMisc_ConstructEvenIfNotEnough".Translate(), ref Settings.constructEvenIfNotEnough);
            listingStandard.CheckboxLabeled("RimMisc_ChangeAreaOnThreat".Translate(), ref Settings.changeAreaOnThreat);
            listingStandard.CheckboxLabeled("RimMisc_PreventRoofCollapse".Translate(), ref Settings.preventRoofCollapse);
            listingStandard.CheckboxLabeled("RimMisc_MyMiscStuff".Translate(), ref Settings.myMiscStuff);

            if (listingStandard.ButtonText("RimMisc_EnableRocketmanTimeDilation".Translate()))
            {
                new RocketManCompat().EnableRocketmanRaces();
            }

            listingStandard.Label("RimMisc_AutoCloseLettersSeconds".Translate(Settings.autoCloseLettersSeconds));
            Settings.autoCloseLettersSeconds = listingStandard.Slider(Settings.autoCloseLettersSeconds, MIN_AUTOCLOSE_SECONDS, MAX_AUTOCLOSE_SECONDS);
            listingStandard.Label("RimMisc_DefaultIngredientRadius".Translate(Settings.defaultIngredientRadius));
            Settings.defaultIngredientRadius = listingStandard.Slider(Settings.defaultIngredientRadius, 0, Bill.MaxIngredientSearchRadius);

            Widgets.EndScrollView();
            listingStandard.End();
            
            base.DoSettingsWindowContents(inRect);
        }

        public override void WriteSettings()
        {
            Settings.ApplySettings();
            base.WriteSettings();
            // Causes errors if Rimatomics is not loaded. Fix later
            try
            {
                new RimAtomicsCompat().FinishRimatomicsResearch();
            }
            catch (TypeLoadException)
            {
                Log.Warning("Rimatomics not found, skipping finishing research");
            }
        }

        public override string SettingsCategory()
        {
            return "RimMisc";
        }


    }
}