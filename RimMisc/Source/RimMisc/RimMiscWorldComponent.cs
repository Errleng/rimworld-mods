﻿using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimMisc
{
    internal class RimMiscWorldComponent : WorldComponent
    {
        public static readonly int AUTO_CLOSE_LETTERS_CHECK_TICKS = GenTicks.SecondsToTicks(10);
        public static readonly int CHECK_THREAT_TICKS = GenTicks.SecondsToTicks(5);

        private static readonly Dictionary<Letter, int> letterStartTimes = new Dictionary<Letter, int>();

        public RimMiscWorldComponent(World world) : base(world)
        {
            RimMisc.Settings.ApplySettings();
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            var currentTicks = Find.TickManager.TicksGame;
            if (RimMisc.Settings.autoCloseLetters)
            {
                if (currentTicks % AUTO_CLOSE_LETTERS_CHECK_TICKS == 0)
                {
                    var letters = Find.LetterStack.LettersListForReading;

                    for (var i = letters.Count - 1; i >= 0; i--)
                    {
                        var letter = letters[i];
                        if (!letterStartTimes.ContainsKey(letter))
                        {
                            letterStartTimes.Add(letter, currentTicks);
                        }
                        else
                        {
                            if (letterStartTimes[letter] + RimMisc.Settings.autoCloseLettersSeconds.SecondsToTicks() < currentTicks)
                            {
                                Find.LetterStack.RemoveLetter(letter);
                                letterStartTimes.Remove(letter);
                            }
                        }
                    }
                }
            }

            if (currentTicks % CHECK_THREAT_TICKS == 0)
            {
                foreach (var map in Find.Maps)
                {
                    var hasThreat = GenHostility.AnyHostileActiveThreatToPlayer(map);
                    foreach (var building in map.listerBuildings.allBuildingsColonist)
                    {
                        var compThreatToggle = building.GetComp<CompThreatToggle>();
                        if (compThreatToggle == null || !compThreatToggle.enableOnlyOnThreat)
                        {
                            continue;
                        }
                        var compFlickable = building.GetComp<CompFlickable>();
                        if (compFlickable == null)
                        {
                            Log.Error($"Found CompThreatToggle but not CompFlickable for {building}");
                            continue;
                        }
                        Traverse.Create(compFlickable).Field("wantSwitchOn").SetValue(hasThreat);
                        compFlickable.SwitchIsOn = hasThreat;
                    }
                }
            }

            if (RimMisc.Settings.myMiscStuff)
            {
                // daily
                if (currentTicks % GenDate.TicksPerDay == 0)
                {
                    foreach (var map in Find.Maps)
                    {
                        // delete bad quality items
                        foreach (var thing in map.listerThings.AllThings.ToList())
                        {
                            // do not delete installed buildings
                            // do not delete things that do not belong to the player
                            if ((thing.def.category == ThingCategory.Building && !(thing is MinifiedThing))
                                || (thing.Faction != null && thing.Faction.IsPlayer))
                            {
                                continue;
                            }
                            QualityCategory quality;
                            if (thing.TryGetQuality(out quality) && quality < QualityCategory.Excellent)
                            {
                                thing.Destroy();
                            }
                        }

                        // repair equipment
                        foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
                        {
                            foreach (var apparel in pawn.apparel.WornApparel)
                            {
                                apparel.HitPoints = apparel.MaxHitPoints;
                            }
                            foreach (var equipment in pawn.equipment.AllEquipmentListForReading)
                            {
                                equipment.HitPoints = equipment.MaxHitPoints;
                            }
                        }
                    }
                }
            }
        }
    }
}