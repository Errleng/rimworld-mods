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
        public const float MAX_VALUE = 10000;
        private const string V = ", ";
        public HediffDef spawnedPawnHediff;

        public bool cachePawns;
        public bool disableCorpses;
        public bool disableNeeds;
        public bool doNotAttackFleeing;
        public bool maxSkills;
        public float maxSpawnerPoints;

        public bool spawnOnlyOnThreat;
        public bool crossMap;
        public float spawnOnThreatSpeedMultiplier;

        public SpawnTimeSetting spawnTime;
        public float spawnTimePointsPerSecond;
        public float spawnTimeSecondsPerSpawn;
        public bool useAllyFaction;
        public Dictionary<string, StatOffset> hediffStatOffsets = new Dictionary<string, StatOffset>();
        public Dictionary<string, CapMod> hediffCapMods = new Dictionary<string, CapMod>();

        public override void ExposeData()
        {
            Scribe_Values.Look(ref maxSpawnerPoints, "maxSpawnerPoints", 500f);
            Scribe_Values.Look(ref spawnTime, "spawnTime");
            Scribe_Values.Look(ref spawnTimePointsPerSecond, "spawnTimePointsPerSecond", 1f);
            Scribe_Values.Look(ref spawnTimeSecondsPerSpawn, "spawnTimeSecondsPerSpawn", 1f);
            Scribe_Values.Look(ref cachePawns, "cachePawns");
            Scribe_Values.Look(ref useAllyFaction, "useAllyFaction", true);
            Scribe_Values.Look(ref maxSkills, "maxSkills");
            Scribe_Values.Look(ref disableCorpses, "disableCorpses", true);
            Scribe_Values.Look(ref disableNeeds, "disableNeeds", true);
            Scribe_Values.Look(ref doNotAttackFleeing, "doNotAttackFleeing");
            Scribe_Values.Look(ref spawnOnlyOnThreat, "spawnOnlyOnThreat");
            Scribe_Values.Look(ref crossMap, "crossMap");
            Scribe_Values.Look(ref spawnOnThreatSpeedMultiplier, "spawnOnThreatSpeedMultiplier", 2f);
            Scribe_Collections.Look(ref hediffStatOffsets, "hediffStatOffsets", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref hediffCapMods, "hediffCapMods", LookMode.Value, LookMode.Deep);

            var traverse = new Traverse(typeof(StatDefOf));
            foreach (var field in traverse.Fields())
            {
                if (!hediffStatOffsets.ContainsKey(field))
                {
                    hediffStatOffsets.Add(field, new StatOffset(field));
                }
            }

            traverse = new Traverse(typeof(PawnCapacityDefOf));
            foreach (var field in traverse.Fields())
            {
                if (!hediffCapMods.ContainsKey(field))
                {
                    hediffCapMods.Add(field, new CapMod(field));
                }
            }
            Log.Message($"hediffStatOffsets: {string.Join(", ", hediffStatOffsets.Keys.ToList())}");
            Log.Message($"hediffCapMods: {string.Join(", ", hediffCapMods.Keys.ToList())}");

            base.ExposeData();
        }

        public void ApplySettings()
        {
            if (spawnedPawnHediff == null)
            {
                spawnedPawnHediff = DefDatabase<HediffDef>.GetNamed("RimSpawners_VanometricPawnHediff");
            }

            string[] spawnerNames =
            {
                "VanometricFabricator"
            };

            foreach (var spawnerName in spawnerNames)
            {
                var spawner = DefDatabase<ThingDef>.GetNamed(spawnerName);
                var comp = spawner.GetCompProperties<CompProperties_VanometricFabricatorPawn>();
                comp.maxSpawnedPawnsPoints = maxSpawnerPoints;
                comp.pawnSpawnIntervalSeconds = spawnTimeSecondsPerSpawn;
                comp.chooseSingleTypeToSpawn = true;
            }

            foreach (var stage in spawnedPawnHediff.stages)
            {
                foreach (var offset in hediffStatOffsets)
                {
                    try
                    {
                        var statOffsetIndex = stage.statOffsets.FindIndex(x => x.stat?.defName.Equals(offset.Key) ?? false);
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
                            stage.statOffsets.Add(statMod);
                            //Log.Message($"Added stat offset to hediff: {offset.Key} = {offset.Value.offset}");
                        }
                        else
                        {
                            var statMod = stage.statOffsets[statOffsetIndex];
                            //Log.Message($"Changed stat offset from: {statMod} to {offset.Value.offset}");
                            if (offset.Value.enabled)
                            {
                                statMod.value = offset.Value.offset / 100;
                            }
                            else
                            {
                                statMod.value = 0;
                            }
                        }
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
                                capMod.offset = 0;
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