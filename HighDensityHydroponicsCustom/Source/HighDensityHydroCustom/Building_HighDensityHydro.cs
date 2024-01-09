using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HighDensityHydroCustom
{
    public class Building_HighDensityHydro : Building_PlantGrower, IPlantToGrowSettable
    {
        private static readonly int FAST_UPDATE_INTERVAL = GenTicks.SecondsToTicks(1);
        private static readonly int SLOW_UPDATE_INTERVAL = GenTicks.SecondsToTicks(10);

        private Vector2 barsize;
        private BayStage bayStage = BayStage.Sowing;
        private int numPlants;
        private Plant simPlant;

        protected int capacity = 52;

        protected float fertility = 2.8f;

        private float margin;

        private int updateInterval = SLOW_UPDATE_INTERVAL;
        private int growUntil = 1000000;

        private bool autoFarm;
        private readonly ResearchProjectDef autoFarmResearch;

        public Building_HighDensityHydro()
        {
            autoFarmResearch = DefDatabase<ResearchProjectDef>.GetNamed("HDH_Autofarm");
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var baseGizmo in base.GetGizmos())
            {
                yield return baseGizmo;
            }
            yield return new Command_SetValue
            {
                defaultLabel = "HDHGizmoGrowLimit".Translate(),
                defaultDesc = "HDHGizmoGrowLimitDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Designators/Harvest", true),
                initialVal = growUntil,
                maxVal = 1000000,
                minVal = 0,
                onValueChange = (int value) =>
                {
                    growUntil = value;
                }
            };
            if (autoFarmResearch.IsFinished)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "HDHGizmoAutofarmToggle".Translate(),
                    defaultDesc = "HDHGizmoAutofarmToggleDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Designators/Harvest", true),
                    isActive = () => autoFarm,
                    toggleAction = () =>
                    {
                        autoFarm = !autoFarm;
                    }
                };
            }
        }

        public override void Draw()
        {
            base.Draw();

            if (bayStage == BayStage.Growing)
            {
                var r = default(GenDraw.FillableBarRequest);
                r.center = DrawPos + Vector3.up * 0.1f;
                r.size = barsize;
                r.fillPercent = simPlant.Growth;
                r.filledMat = HDH_Graphics.HDHBarFilledMat;
                r.unfilledMat = HDH_Graphics.HDHBarUnfilledMat;
                r.margin = margin;
                var rotation = Rotation;
                rotation.Rotate(RotationDirection.Clockwise);
                r.rotation = rotation;
                GenDraw.DrawFillableBar(r);
            }
        }

        public int GetStockpiledProducts()
        {
            if (simPlant.def?.plant?.harvestedThingDef == null)
            {
                return 0;
            }
            var things = Map.listerThings.ThingsOfDef(simPlant.def.plant.harvestedThingDef);
            int count = 0;
            foreach (var thing in things)
            {
                count += thing.stackCount;
            }
            return count;
        }

        IEnumerable<IntVec3> IPlantToGrowSettable.Cells => this.OccupiedRect().Cells;

        public new bool CanAcceptSowNow()
        {
            return base.CanAcceptSowNow() && bayStage == BayStage.Sowing && !autoFarm;
        }

        public new void SetPlantDefToGrow(ThingDef plantDef)
        {
            base.SetPlantDefToGrow(plantDef);
            simPlant = (Plant)ThingMaker.MakeThing(GetPlantDefToGrow(), null);
            simPlant.Growth = 0;
            if (bayStage == BayStage.Sowing)
            {
                numPlants = 0;
                foreach (var plant in PlantsOnMe)
                {
                    plant.Destroy();
                }
            }
            else if (bayStage == BayStage.Growing && !autoFarm)
            {
                bayStage = BayStage.Sowing;
                numPlants = 0;
            }
        }

        private void AcceptPlants()
        {
            foreach (var plant in PlantsOnMe)
            {
                if (plant.LifeStage == PlantLifeStage.Growing)
                {
                    plant.Destroy();
                    if (numPlants >= capacity)
                    {
                        bayStage = BayStage.Growing;
                        SoundDefOf.CryptosleepCasket_Accept.PlayOneShot(new TargetInfo(Position, Map));
                        break;
                    }
                    ++numPlants;
                }
            }
        }

        public override void Tick()
        {
            base.Tick();

            if (!this.IsHashIntervalTick(updateInterval))
            {
                return;
            }

            if (bayStage == BayStage.Sowing || bayStage == BayStage.Harvest)
            {
                updateInterval = FAST_UPDATE_INTERVAL;
            }
            else if (bayStage == BayStage.Growing)
            {
                updateInterval = SLOW_UPDATE_INTERVAL;
            }

            AcceptPlants();

            if (base.CanAcceptSowNow())
            {
                var temperature = Position.GetTemperature(Map);
                var canGrow = bayStage == BayStage.Growing &&
                              temperature > Plant.MinOptimalGrowthTemperature &&
                              temperature < Plant.MaxOptimalGrowthTemperature;
                var plantProps = simPlant.def.plant;
                if (canGrow)
                {
                    if (simPlant.LifeStage == PlantLifeStage.Mature && GetStockpiledProducts() < growUntil)
                    {
                        bayStage = BayStage.Harvest;
                    }
                    var fertilitySensitivity = 1;
                    var fertilityGrowthRateFactor = fertility * fertilitySensitivity + (1 - fertilitySensitivity);
                    var growthPerDay = 1f / (GenDate.TicksPerDay * plantProps.growDays);
                    var growthAmount = fertilityGrowthRateFactor * growthPerDay * updateInterval;
                    simPlant.Growth += growthAmount;
                }

                if (bayStage == BayStage.Harvest)
                {
                    var numCells = this.OccupiedRect().Width * this.OccupiedRect().Height;
                    var expectedYield = simPlant.YieldNow();

                    for (int i = 0; i < numPlants; i++)
                    {
                        if (autoFarm)
                        {
                            if (plantProps.harvestedThingDef != null)
                            {
                                var harvestResult = ThingMaker.MakeThing(plantProps.harvestedThingDef);
                                harvestResult.stackCount = expectedYield;
                                harvestResult.SetForbidden(false, true);
                                if (harvestResult.stackCount > 0)
                                {
                                    GenPlace.TryPlaceThing(harvestResult, Position, Map, ThingPlaceMode.Near);
                                    plantProps.soundHarvestFinish.PlayOneShot(new TargetInfo(Position, Map));
                                }
                                else
                                {
                                    Log.Warning($"HDH {simPlant.def.LabelCap} harvest yield is 0! Num plants: {numPlants}, growth: {simPlant.Growth}, lifestage: {simPlant.LifeStage}, yield: {expectedYield}");
                                }
                            }
                            continue;
                        }

                        var numCellsLooked = 0;
                        foreach (var cell in this.OccupiedRect())
                        {
                            var thingsAtCell = Map.thingGrid.ThingsListAt(cell);
                            var cellIsFree = true;
                            foreach (var thingAtCell in thingsAtCell)
                            {
                                if (thingAtCell is Plant)
                                {
                                    cellIsFree = false;
                                    break;
                                }
                            }

                            if (cellIsFree)
                            {
                                var plantClone = (Plant)ThingMaker.MakeThing(GetPlantDefToGrow(), null);
                                plantClone.Growth = 1;
                                Thing resultingThing;
                                GenDrop.TryDropSpawn(plantClone, cell, Map, ThingPlaceMode.Direct, out resultingThing, null, null, true);
                                --numPlants;
                                break;
                            }

                            ++numCellsLooked;
                        }

                        if (numCellsLooked >= numCells)
                        {
                            break;
                        }
                    }

                    if (autoFarm)
                    {
                        numPlants = 0;
                    }
                    if (numPlants <= 0)
                    {
                        bayStage = BayStage.Sowing;
                        simPlant.Growth = 0;
                    }
                }

                if (bayStage == BayStage.Sowing && autoFarm)
                {
                    numPlants = capacity;
                    bayStage = BayStage.Growing;
                    SoundDefOf.CryptosleepCasket_Accept.PlayOneShot(new TargetInfo(Position, Map));
                }
            }
            else
            {
                var dinfo = new DamageInfo(DamageDefOf.Rotting, 1f);
                simPlant.TakeDamage(dinfo);
            }
        }

        public override string GetInspectString()
        {
            var text = base.GetInspectString();

            var temperature = Position.GetTemperature(Map);
            if (temperature <= Plant.MinOptimalGrowthTemperature || temperature >= Plant.MaxOptimalGrowthTemperature)
            {
                text += $"\n{"HDHBadTemperature".Translate(temperature, Plant.MinOptimalGrowthTemperature.ToStringTemperature(), Plant.MaxOptimalGrowthTemperature.ToStringTemperature())}";
            }

            var fertilitySensitivity = 1;
            var fertilityGrowthRateFactor = fertility * fertilitySensitivity + (1 - fertilitySensitivity);
            var growthPerDay = 1f / (GenDate.TicksPerDay * simPlant.def.plant.growDays);
            var growthPerDayAdjusted = fertilityGrowthRateFactor * growthPerDay * GenDate.TicksPerDay;
            var daysToMature = (1 - simPlant.Growth) / growthPerDayAdjusted;
            text += $"\n{"HDHFertility".Translate(Math.Round(fertility * 100), 2)}, {"HDHCapacity".Translate(numPlants, capacity)}";
            text += $"\n{"HDHGrowth".Translate(simPlant.def.LabelCap, Math.Round(simPlant.Growth * 100, 2), Math.Round(daysToMature, 2))}";
            text += $"\n{"HDHGrowUntil".Translate(GetStockpiledProducts(), growUntil)}";

            return text;
        }

        public override void PostMake()
        {
            base.PostMake();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            loadConfig();
            var x = def.size.x;
            var z = def.size.z;
            Log.Message("x: " + x + " y: " + z);
            var flag = x == 1;
            float y;
            if (flag)
            {
                y = 0.6f;
                margin = 0.15f;
            }
            else
            {
                y = 0.1f;
                margin = 0.05f;
            }

            var x2 = z - 0.4f;
            barsize = new Vector2(x2, y);

            if (simPlant == null)
            {
                simPlant = (Plant)ThingMaker.MakeThing(GetPlantDefToGrow(), null);
                simPlant.Growth = 0;
            }
        }

        public void loadConfig()
        {
            var modExtension = def.GetModExtension<HydroStatsExtension>();
            if (modExtension != null)
            {
                capacity = modExtension.capacity;
                fertility = modExtension.fertility;
                Traverse.Create(PowerComp.Props).Field("basePowerConsumption").SetValue(modExtension.power);
                PowerComp.SetUpPowerVars();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref simPlant, "plant");
            Scribe_Values.Look(ref numPlants, "numPlants", 0);
            Scribe_Values.Look(ref bayStage, "growingStage", BayStage.Growing);
            Scribe_Values.Look(ref autoFarm, "autoFarm");
            Scribe_Values.Look(ref growUntil, "growUntil");
        }

        protected enum BayStage
        {
            Sowing,
            Growing,
            Harvest
        }
    }
}