﻿using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HighDensityHydroCustom
{
    public class Building_HighDensityHydro : Building_PlantGrower, IThingHolder, IPlantToGrowSettable
    {
        private static readonly int FAST_UPDATE_INTERVAL = GenTicks.SecondsToTicks(1);
        private static readonly int SLOW_UPDATE_INTERVAL = GenTicks.SecondsToTicks(10);
        private static readonly int SURFACE_PLANTS_THRESHOLD = 2;

        private Vector2 barsize;

        private BayStage bayStage = BayStage.Sowing;

        protected int capacity = 52;

        protected float fertility = 2.8f;

        private float highestGrowth;

        protected ThingOwner innerContainer;

        private float margin;

        private int updateInterval = SLOW_UPDATE_INTERVAL;

        public Building_HighDensityHydro()
        {
            innerContainer = new ThingOwner<Thing>(this, false);
        }

        IEnumerable<IntVec3> IPlantToGrowSettable.Cells => this.OccupiedRect().Cells;

        public new bool CanAcceptSowNow()
        {
            return base.CanAcceptSowNow() && bayStage == BayStage.Sowing;
        }

        public new void SetPlantDefToGrow(ThingDef plantDef)
        {
            base.SetPlantDefToGrow(plantDef);
            var flag = bayStage == BayStage.Sowing;
            if (flag)
            {
                innerContainer.ClearAndDestroyContents();
                foreach (var plant in PlantsOnMe) plant.Destroy();
            }
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return null;
        }

        public override void Tick()
        {
            base.Tick();

            if (!this.IsHashIntervalTick(updateInterval)) return;

            if (bayStage == BayStage.Sowing || bayStage == BayStage.Harvest)
                updateInterval = FAST_UPDATE_INTERVAL;
            else if (bayStage == BayStage.Growing) updateInterval = SLOW_UPDATE_INTERVAL;

            var numSurfacePlants = 0;
            foreach (var plant in PlantsOnMe)
                if (plant.LifeStage == PlantLifeStage.Growing)
                {
                    ++numSurfacePlants;
                    if (innerContainer.Count >= capacity) plant.Destroy();
                }

            if (numSurfacePlants >= SURFACE_PLANTS_THRESHOLD)
                foreach (var plant in PlantsOnMe)
                    if (plant.LifeStage == PlantLifeStage.Growing && !plant.Blighted)
                    {
                        plant.DeSpawn();
                        TryAcceptThing(plant);
                        if (innerContainer.Count >= capacity)
                        {
                            bayStage = BayStage.Growing;
                            SoundDefOf.CryptosleepCasket_Accept.PlayOneShot(new TargetInfo(Position, Map));
                            break;
                        }
                    }

            if (innerContainer.Count == 0)
            {
                bayStage = BayStage.Sowing;
                highestGrowth = 0f;
            }

            if (base.CanAcceptSowNow())
            {
                var temperature = Position.GetTemperature(Map);
                var canGrow = bayStage == BayStage.Growing &&
                              temperature > Plant.MinOptimalGrowthTemperature &&
                              temperature < Plant.MaxOptimalGrowthTemperature;

                if (canGrow)
                {
                    var firstPlant = innerContainer[0] as Plant;
                    var fertilitySensitivity = firstPlant.def.plant.fertilitySensitivity;
                    var fertilityGrowthRateFactor = fertility * fertilitySensitivity + (1 - fertilitySensitivity);
                    var growthPerDay = 1f / (GenDate.TicksPerDay * firstPlant.def.plant.growDays);
                    var growthAmount = fertilityGrowthRateFactor * growthPerDay * updateInterval;
                    firstPlant.Growth += growthAmount;
                    highestGrowth = firstPlant.Growth;
                    if (firstPlant.LifeStage == PlantLifeStage.Mature) bayStage = BayStage.Harvest;
                }

                if (bayStage == BayStage.Harvest)
                {
                    highestGrowth = 1;
                    var numCells = this.OccupiedRect().Width * this.OccupiedRect().Height;
                    foreach (var thing in innerContainer)
                    {
                        var plant = thing as Plant;
                        var numCellsLooked = 0;
                        foreach (var cell in this.OccupiedRect())
                        {
                            var thingsAtCell = Map.thingGrid.ThingsListAt(cell);
                            var cellIsFree = true;
                            foreach (var thingAtCell in thingsAtCell)
                                if (thingAtCell is Plant)
                                {
                                    cellIsFree = false;
                                    break;
                                }

                            if (cellIsFree)
                            {
                                plant.Growth = 1f;
                                Thing resultingThing;
                                innerContainer.TryDrop_NewTmp(plant, cell, Map, ThingPlaceMode.Direct, out resultingThing);
                                break;
                            }

                            numCellsLooked++;
                        }

                        if (numCellsLooked >= numCells) break;
                    }
                }
            }
            else
            {
                foreach (var thing in innerContainer)
                {
                    var plant = thing as Plant;
                    var dinfo = new DamageInfo(DamageDefOf.Rotting, 1f);
                    plant.TakeDamage(dinfo);
                }
            }
        }

        public override void Draw()
        {
            base.Draw();
            var flag = innerContainer.Count >= capacity && base.CanAcceptSowNow();
            if (flag)
            {
                var r = default(GenDraw.FillableBarRequest);
                r.center = DrawPos + Vector3.up * 0.1f;
                r.size = barsize;
                r.fillPercent = highestGrowth;
                r.filledMat = HDH_Graphics.HDHBarFilledMat;
                r.unfilledMat = HDH_Graphics.HDHBarUnfilledMat;
                r.margin = margin;
                var rotation = Rotation;
                rotation.Rotate(RotationDirection.Clockwise);
                r.rotation = rotation;
                GenDraw.DrawFillableBar(r);
            }
        }

        public override string GetInspectString()
        {
            var text = base.GetInspectString();
            text += $"\n{"HDHFertility".Translate(Math.Round(fertility * 100), 2)}";
            if (innerContainer.Count > 0)
            {
                var firstPlant = innerContainer[0] as Plant;
                var fertilitySensitivity = firstPlant.def.plant.fertilitySensitivity;
                var fertilityGrowthRateFactor = fertility * fertilitySensitivity + (1 - fertilitySensitivity);
                var growthPerDay = 1f / (GenDate.TicksPerDay * firstPlant.def.plant.growDays);
                var growthPerDayAdjusted = fertilityGrowthRateFactor * growthPerDay * GenDate.TicksPerDay;
                var daysToMature = (1 - highestGrowth) / growthPerDayAdjusted;
                text += $"\n{"HDHPlantCount".Translate(innerContainer.Count, capacity)}";
                text += $"\n{"HDHHighestGrowth".Translate(Math.Round(highestGrowth * 100, 2), Math.Round(daysToMature, 2))}";
            }

            return text;
        }

        public virtual bool TryAcceptThing(Thing thing, bool allowSpecialEffects = true)
        {
            if (!Accepts(thing))
            {
                return false;
            }
            else
            {
                if (thing.holdingOwner != null)
                {
                    thing.holdingOwner.TryTransferToContainer(thing, innerContainer, thing.stackCount);
                    return true;
                }
                else
                {
                    return innerContainer.TryAdd(thing);
                }
            }
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
        }

        public void loadConfig()
        {
            var modExtension = def.GetModExtension<HydroStatsExtension>();
            if (modExtension != null)
            {
                capacity = modExtension.capacity;
                fertility = modExtension.fertility;
                PowerComp.Props.basePowerConsumption = modExtension.power;
                PowerComp.SetUpPowerVars();
            }
        }

        public virtual void EjectContents()
        {
            innerContainer.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_Values.Look(ref bayStage, "growingStage", BayStage.Growing);
            Scribe_Values.Look(ref highestGrowth, "highestGrowth");
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            innerContainer.ClearAndDestroyContents();
            base.Destroy(mode);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            innerContainer.ClearAndDestroyContents();
            foreach (var plant in PlantsOnMe) plant.Destroy();
            base.DeSpawn(mode);
        }

        public virtual bool Accepts(Thing thing)
        {
            return innerContainer.CanAcceptAnyOf(thing);
        }

        protected enum BayStage
        {
            Sowing,
            Growing,
            Harvest
        }
    }
}