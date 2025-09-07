using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimSpawners
{
    internal class RimSpawnersSettings : ModSettings
    {
        public const float MIN_VALUE = 1;
        public const float MAX_VALUE = 100000;
        public HediffDef spawnedPawnHediff;

        public bool cachePawns;
        public bool disableCorpses;
        public bool disableNeeds;
        public bool doNotAttackFleeing;
        public bool maxSkills;

        public bool spawnOnlyOnThreat;
        public bool crossMap;
        public bool doNotDamagePlayerBuildings;
        public bool doNotDamageFriendlies;
        public bool massivelyDamageEnemyBuildings;
        public bool randomizeLoadouts;

        public float matterSiphonPointsPerSecond;
        public float controlNodePointsStored;
        public bool useAllyFaction;
        public bool groupPawnkinds;

        public HashSet<string> selectedWeapons = new HashSet<string>();
        public HashSet<string> selectedApparel = new HashSet<string>();

        public Dictionary<string, StatOffset> hediffStatOffsets = new Dictionary<string, StatOffset>();
        public Dictionary<string, CapMod> hediffCapMods = new Dictionary<string, CapMod>();

        public override void ExposeData()
        {
            Scribe_Values.Look(ref matterSiphonPointsPerSecond, "matterSiphonPointsPerSecond", 1f);
            Scribe_Values.Look(ref controlNodePointsStored, "controlNodePointsStored", 100f);
            Scribe_Values.Look(ref cachePawns, "cachePawns", false);
            Scribe_Values.Look(ref useAllyFaction, "useAllyFaction", true);
            Scribe_Values.Look(ref maxSkills, "maxSkills", defaultValue: false);
            Scribe_Values.Look(ref disableCorpses, "disableCorpses", true);
            Scribe_Values.Look(ref disableNeeds, "disableNeeds", true);
            Scribe_Values.Look(ref doNotAttackFleeing, "doNotAttackFleeing", false);
            Scribe_Values.Look(ref spawnOnlyOnThreat, "spawnOnlyOnThreat", false);
            Scribe_Values.Look(ref crossMap, "crossMap", false);
            Scribe_Values.Look(ref groupPawnkinds, "groupPawnkinds", true);
            Scribe_Values.Look(ref doNotDamagePlayerBuildings, "doNotDamagePlayerBuildings", false);
            Scribe_Values.Look(ref doNotDamageFriendlies, "doNotDamageFriendlies", false);
            Scribe_Values.Look(ref massivelyDamageEnemyBuildings, "massivelyDamageEnemyBuildings", false);
            Scribe_Values.Look(ref randomizeLoadouts, "randomizeLoadouts", false);
            Scribe_Collections.Look(ref selectedWeapons, "selectedWeapons", LookMode.Deep);
            Scribe_Collections.Look(ref selectedApparel, "selectedApparel", LookMode.Deep);
            Scribe_Collections.Look(ref hediffStatOffsets, "hediffStatOffsets", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref hediffCapMods, "hediffCapMods", LookMode.Value, LookMode.Deep);

            // Initialize collections if needed
            if (selectedWeapons == null)
            {
                selectedWeapons = new HashSet<string>();
            }
            if (selectedApparel == null)
            {
                selectedApparel = new HashSet<string>();
            }
            Log.Message($"Selected weapon pool of size {selectedWeapons.Count}: {string.Join(", ", selectedWeapons)}");
            Log.Message($"Selected apparel pool of size {selectedApparel.Count}: {string.Join(", ", selectedApparel)}");
            selectedWeapons.RemoveWhere(string.IsNullOrWhiteSpace);
            selectedApparel.RemoveWhere(string.IsNullOrWhiteSpace);

            foreach (var def in DefDatabase<StatDef>.AllDefs)
            {
                var name = def.defName;
                if (!hediffStatOffsets.ContainsKey(name))
                {
                    hediffStatOffsets.Add(name, new StatOffset(name));
                }
            }

            foreach (var def in DefDatabase<PawnCapacityDef>.AllDefs)
            {
                var name = def.defName;
                if (!hediffCapMods.ContainsKey(name))
                {
                    hediffCapMods.Add(name, new CapMod(name));
                }
            }

            base.ExposeData();
        }

        public void ApplyStatOffsets(List<StatModifier> statOffsets)
        {
            foreach (var offset in hediffStatOffsets)
            {
                var statOffsetIndex = statOffsets.FindIndex(x => x.stat?.defName.Equals(offset.Key) ?? false);
                if (statOffsetIndex == -1)
                {
                    if (!offset.Value.enabled)
                    {
                        continue;
                    }
                    var statMod = new StatModifier();
                    var traverse = Traverse.Create(typeof(StatDefOf));
                    statMod.stat = (StatDef)traverse.Field(offset.Value.statName).GetValue();
                    statMod.value = offset.Value.offset / 100;
                    statOffsets.Add(statMod);
                    //Log.Message($"Added stat offset to hediff: {offset.Key} = {offset.Value.offset}");
                }
                else
                {
                    var statMod = statOffsets[statOffsetIndex];
                    //Log.Message($"Changed stat offset from: {statMod} to {offset.Value.offset}");
                    if (offset.Value.enabled)
                    {
                        statMod.value = offset.Value.offset / 100;
                    }
                    else
                    {
                        statOffsets.RemoveAt(statOffsetIndex);
                    }
                }
            }
        }

        public void ApplySettings()
        {
            if (spawnedPawnHediff == null)
            {
                spawnedPawnHediff = DefDatabase<HediffDef>.GetNamed("RimSpawners_VanometricPawnHediff");
            }

            foreach (var stage in spawnedPawnHediff.stages)
            {
                foreach (var offset in hediffStatOffsets)
                {
                    try
                    {
                        ApplyStatOffsets(stage.statOffsets);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Exception when trying to add stat offset to hediff: {offset.Key} = {offset.Value.offset}:\n{ex.Message}");
                        throw ex;
                    }
                }


                foreach (var mod in hediffCapMods)
                {
                    try
                    {
                        var capModIndex = stage.capMods.FindIndex(x => x.capacity?.defName.Equals(mod.Key) ?? false);
                        if (capModIndex == -1)
                        {
                            if (!mod.Value.enabled)
                            {
                                continue;
                            }
                            var traverse = Traverse.Create(typeof(PawnCapacityDefOf));
                            var capMod = new PawnCapacityModifier();
                            capMod.capacity = (PawnCapacityDef)traverse.Field(mod.Value.capacityName).GetValue();
                            capMod.offset = mod.Value.offset / 100;
                            stage.capMods.Add(capMod);
                            //Log.Message($"Added capacity mod to hediff: {mod.Key} = {mod.Value}");
                        }
                        else
                        {
                            var capMod = stage.capMods[capModIndex];
                            //Log.Message($"Changed capacity mod from: {capMod.capacity.defName} to {mod.Value.offset}");
                            if (mod.Value.enabled)
                            {
                                capMod.offset = mod.Value.offset / 100;
                            }
                            else
                            {
                                stage.capMods.RemoveAt(capModIndex);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Exception when trying to add capacity mod to hediff: {mod.Key} = {mod.Value}:\n{ex.Message}");
                        throw ex;
                    }
                }
            }
        }
    }
}

//