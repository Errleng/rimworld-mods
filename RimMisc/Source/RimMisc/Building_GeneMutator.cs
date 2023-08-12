using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimMisc
{
    internal class Building_GeneMutator : Building
    {
        public static readonly int TICKS_BETWEEN_SPAWNS = GenDate.TicksPerQuadrum;
        int lastSpawnTick = -1;
        CompPowerTrader compPowerTrader;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (lastSpawnTick < 0)
            {
                lastSpawnTick = Find.TickManager.TicksGame;
            }
            compPowerTrader = GetComp<CompPowerTrader>();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref lastSpawnTick, "lastSpawnTick");
            base.ExposeData();
        }

        public override void TickRare()
        {
            if (!compPowerTrader.PowerOn)
            {
                return;
            }
            int curTick = Find.TickManager.TicksGame;
            if (curTick - lastSpawnTick >= TICKS_BETWEEN_SPAWNS)
            {
                lastSpawnTick = curTick;
                var gene = GetUnownedGene();
                if (gene != null)
                {
                    GenPlace.TryPlaceThing(gene, Position, Map, ThingPlaceMode.Near);
                    Messages.Message("RimMisc_GeneMutationComplete".Translate(gene.LabelNoCount), new LookTargets(new TargetInfo[] { gene }), MessageTypeDefOf.PositiveEvent, true);
                }
            }
        }

        public override string GetInspectString()
        {
            string str = base.GetInspectString();
            float daysUntilSpawn = Math.Max(0, (TICKS_BETWEEN_SPAWNS + lastSpawnTick - Find.TickManager.TicksGame) / GenDate.TicksPerDay);
            str += "\n" + "RimMisc_GeneMutationTime".Translate(Math.Round(daysUntilSpawn, 2));
            return str;
        }

        private Genepack GetUnownedGene()
        {
            var ownedGenes = new HashSet<string>();
            foreach (var map in Find.Maps)
            {
                foreach (Building building in map.listerBuildings.allBuildingsColonist)
                {
                    CompGenepackContainer compGenepackContainer = building.TryGetComp<CompGenepackContainer>();
                    if (compGenepackContainer != null)
                    {
                        foreach (var genepack in compGenepackContainer.ContainedGenepacks)
                        {
                            foreach (var gene in genepack.GeneSet.GenesListForReading)
                            {
                                ownedGenes.Add(gene.defName);
                            }
                        }
                    }
                }

                foreach (var thing in map.listerThings.AllThings)
                {
                    if (thing is GeneSetHolderBase genepack)
                    {
                        foreach (var gene in genepack.GeneSet.GenesListForReading)
                        {
                            ownedGenes.Add(gene.defName);
                        }
                    }
                }
            }

            var unownedGenes = DefDatabase<GeneDef>.AllDefs.Where(x => !ownedGenes.Contains(x.defName)).ToList();
            if (unownedGenes.Count == 0)
            {
                return null;
            }
            GeneDef unownedGene = unownedGenes.RandomElement();
            Genepack generatedGenepack = (Genepack)ThingMaker.MakeThing(ThingDefOf.Genepack, null);
            generatedGenepack.Initialize(new List<GeneDef> { unownedGene });
            Log.Message($"Unowned gene: {unownedGene.LabelCap}, {ownedGenes.Count} owned genes: {string.Join(", ", ownedGenes)}");
            return generatedGenepack;
        }
    }
}
