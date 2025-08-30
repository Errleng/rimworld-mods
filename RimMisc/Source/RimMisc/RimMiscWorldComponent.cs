using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Verse;
using Verse.AI;

namespace RimMisc
{
    internal class RimMiscWorldComponent : WorldComponent
    {
        public static readonly int AUTO_CLOSE_LETTERS_CHECK_TICKS = GenTicks.SecondsToTicks(10);
        public static readonly int CHECK_THREAT_TICKS = GenTicks.SecondsToTicks(5);
        public static readonly int KILL_DOWNED_TICKS = GenTicks.SecondsToTicks(30);
        public static readonly string SAFE_AREA_SUFFIX = "#safe";

        private static readonly Dictionary<Letter, int> letterStartTimes = new Dictionary<Letter, int>();
        private static readonly Dictionary<Map, bool> mapPrevHasThreat = new Dictionary<Map, bool>();
        // If the pawn is in the cache, then kill them. Else add them to the cache
        private static readonly HashSet<Pawn> downedPawnCache = new HashSet<Pawn>();

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

                    if (RimMisc.Settings.changeAreaOnThreat && (!mapPrevHasThreat.ContainsKey(map) || hasThreat != mapPrevHasThreat[map]))
                    {
                        // Does not work with Better Pawn Control
                        var pawns = map.mapPawns.FreeColonists.Concat(map.mapPawns.SpawnedColonyMechs).Concat(map.mapPawns.SpawnedColonyAnimals).ToList();
                        if (hasThreat)
                        {
                            foreach (Pawn pawn in pawns)
                            {
                                pawn.mindState.priorityWork.ClearPrioritizedWorkAndJobQueue();
                                if (pawn.Spawned && !pawn.Downed && !pawn.InMentalState && !pawn.Drafted)
                                {
                                    pawn.Map.pawnDestinationReservationManager.ReleaseAllClaimedBy(pawn);
                                }
                                pawn.jobs.ClearQueuedJobs(true);
                                if (pawn.jobs.curJob != null && pawn.jobs.IsCurrentJobPlayerInterruptible() && !pawn.Downed && !pawn.InMentalState && !pawn.Drafted)
                                {
                                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true, true);
                                }
                                var curArea = pawn.playerSettings.AreaRestrictionInPawnCurrentMap;
                                if (curArea == null)
                                {
                                    continue;
                                }
                                var safeAreaName = curArea.Label + SAFE_AREA_SUFFIX;
                                var safeAreas = map.areaManager.AllAreas.Where((Area x) => x is Area_Allowed && Regex.IsMatch(x.Label, $"^{safeAreaName}$", RegexOptions.IgnoreCase | RegexOptions.ECMAScript)).Cast<Area_Allowed>();
                                if (safeAreas.Count() > 1)
                                {
                                    Log.Error($"Found multiple safe areas matching {safeAreaName} on map {map.Parent.LabelCap}: {string.Join(", ", safeAreas.Select(x => x.Label))})");
                                }
                                var safeArea = safeAreas.FirstOrDefault();
                                if (safeArea == null)
                                {
                                    continue;
                                }
                                pawn.playerSettings.AreaRestrictionInPawnCurrentMap = safeArea;
                                //Messages.Message("ToggleSafeArea".Translate(pawn.LabelCap, safeArea.Label, map.Parent.LabelCap), MessageTypeDefOf.SilentInput, false);
                            }
                        }
                        else
                        {
                            foreach (Pawn pawn in pawns)
                            {
                                var curArea = pawn.playerSettings.AreaRestrictionInPawnCurrentMap;
                                if (curArea == null)
                                {
                                    continue;
                                }
                                var unsafeAreaName = Regex.Replace(curArea.Label, $@"{SAFE_AREA_SUFFIX}$", string.Empty);
                                var unsafeAreas = map.areaManager.AllAreas.Where((Area x) => x is Area_Allowed && Regex.IsMatch(x.Label, $"^{unsafeAreaName}$", RegexOptions.IgnoreCase | RegexOptions.ECMAScript)).Cast<Area_Allowed>();
                                if (unsafeAreas.Count() > 1)
                                {
                                    Log.Error($"Found multiple unsafe areas matching {unsafeAreaName} on map {map.Parent.LabelCap}: {string.Join(", ", unsafeAreas.Select(x => x.Label))})");
                                }
                                var unsafeArea = unsafeAreas.FirstOrDefault();
                                if (unsafeArea == null)
                                {
                                    continue;
                                }
                                pawn.playerSettings.AreaRestrictionInPawnCurrentMap = unsafeArea;
                                //Messages.Message("ToggleUnsafeArea".Translate(pawn.LabelCap, unsafeArea.Label, map.Parent.LabelCap), MessageTypeDefOf.SilentInput, false);
                            }
                        }
                    }
                    mapPrevHasThreat[map] = hasThreat;

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

            if (RimMisc.Settings.killDownedPawns && currentTicks % KILL_DOWNED_TICKS == 0)
            {
                foreach (var pawn in downedPawnCache)
                {
                    if (isPawnValidToKill(pawn))
                    {
                        try
                        {
                            pawn.Kill(null);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"RimMisc failed to kill downed pawn: {pawn.LabelCap}. {ex.Message}");
                        }
                    }
                }
                downedPawnCache.Clear();
                foreach (var map in Find.Maps)
                {
                    foreach (var pawn in map.mapPawns.AllPawnsSpawned)
                    {
                        if (isPawnValidToKill(pawn))
                        {
                            downedPawnCache.Add(pawn);
                        }
                    }
                }
            }

            if (RimMisc.Settings.myMiscStuff)
            {
                // daily
                if (currentTicks % GenDate.TicksPerDay == 0)
                {
                    var fleckTypeCounts = new Dictionary<string, int>();

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

                        // Check flecks
                        foreach (var fleckSystem in map.flecks.Systems)
                        {
                            foreach (var fleck in fleckSystem.EnumerateFlecks())
                            {
                                var key = "none";
                                if (fleck is FleckSplash fleckSplash)
                                {
                                    key = fleckSplash.def.defName;
                                } else if (fleck is FleckStatic fleckStatic)
                                {
                                    key = fleckStatic.def.defName;
                                }
                                else if (fleck is FleckThrown fleckThrown)
                                {
                                    key = fleckThrown.baseData.def.defName;
                                }

                                if (!fleckTypeCounts.ContainsKey(key))
                                {
                                    fleckTypeCounts[key] = 0;
                                }
                                fleckTypeCounts[key] += 1;
                            }
                        }
                    }

                    Log.Message($"Fleck counts: {string.Join(",", fleckTypeCounts.Select(x => $"({x.Key } : {x.Value})"))}");
                }
            }
        }

        private bool isPawnValidToKill(Pawn pawn)
        {
            return pawn.Downed &&
                pawn.Faction != null &&
                !pawn.Faction.IsPlayer &&
                !pawn.IsPrisonerOfColony &&
                pawn.Faction.HostileTo(Faction.OfPlayer) &&
                !pawn.IsOnHoldingPlatform;
        }
    }
}