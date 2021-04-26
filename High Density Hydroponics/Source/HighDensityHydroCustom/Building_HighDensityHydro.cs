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

        protected ThingOwner innerContainer;

        protected int capacity = 52;

        protected float fertility = 2.8f;

        private BayStage bayStage = BayStage.Sowing;

        private float highestGrowth = 0f;

        private Vector2 barsize;

        private float margin;

        private int updateInterval = SLOW_UPDATE_INTERVAL;

        protected enum BayStage
        {
            Sowing,
            Growing,
            Harvest
        }

        IEnumerable<IntVec3> IPlantToGrowSettable.Cells => this.OccupiedRect().Cells;

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

            int numSurfacePlants = 0;
            foreach (Plant plant in PlantsOnMe)
            {
                if (plant.LifeStage == PlantLifeStage.Growing)
                {
                    ++numSurfacePlants;
                    if (innerContainer.Count >= capacity)
                    {
                        plant.Destroy();
                    }
                }
            }

            if (numSurfacePlants >= SURFACE_PLANTS_THRESHOLD)
            {
                foreach (Plant plant in PlantsOnMe)
                {
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
                }
            }

            if (innerContainer.Count == 0)
            {
                bayStage = BayStage.Sowing;
                highestGrowth = 0f;
            }

            if (base.CanAcceptSowNow())
            {
                float temperature = Position.GetTemperature(Map);
                bool canGrow = bayStage == BayStage.Growing &&
                               temperature > Plant.MinOptimalGrowthTemperature &&
                               temperature < Plant.MaxOptimalGrowthTemperature;

                if (canGrow)
                {
                    Plant firstPlant = innerContainer[0] as Plant;
                    float fertilitySensitivity = firstPlant.def.plant.fertilitySensitivity;
                    float fertilityGrowthRateFactor = (fertility * fertilitySensitivity) + (1 - fertilitySensitivity);
                    float growthPerDay = 1f / (60000f * firstPlant.def.plant.growDays);
                    float growthAmount = fertilityGrowthRateFactor * growthPerDay * updateInterval;
                    firstPlant.Growth += growthAmount;
                    highestGrowth = firstPlant.Growth;
                    if (firstPlant.LifeStage == PlantLifeStage.Mature)
                    {
                        bayStage = BayStage.Harvest;
                    }
                }

                if (bayStage == BayStage.Harvest)
                {
                    int numCells = this.OccupiedRect().Width * this.OccupiedRect().Height;
                    foreach (Thing thing in ((IEnumerable<Thing>)innerContainer))
                    {
                        Plant plant = thing as Plant;
                        int numCellsLooked = 0;
                        foreach (IntVec3 cell in this.OccupiedRect())
                        {
                            List<Thing> thingsAtCell = Map.thingGrid.ThingsListAt(cell);
                            bool cellIsFree = true;
                            foreach (Thing thingAtCell in thingsAtCell)
                            {
                                if (thingAtCell is Plant)
                                {
                                    cellIsFree = false;
                                    break;
                                }
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

                        if (numCellsLooked >= numCells)
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                foreach (Thing thing in innerContainer)
                {
                    Plant plant = thing as Plant;
                    DamageInfo dinfo = new DamageInfo(DamageDefOf.Rotting, 1f);
                    plant.TakeDamage(dinfo);
                }
            }
        }

        public override void Draw()
        {
            base.Draw();
            bool flag = innerContainer.Count >= capacity && base.CanAcceptSowNow();
            if (flag)
            {
                GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
                r.center = DrawPos + Vector3.up * 0.1f;
                r.size = barsize;
                r.fillPercent = highestGrowth;
                r.filledMat = HDH_Graphics.HDHBarFilledMat;
                r.unfilledMat = HDH_Graphics.HDHBarUnfilledMat;
                r.margin = margin;
                Rot4 rotation = Rotation;
                rotation.Rotate(RotationDirection.Clockwise);
                r.rotation = rotation;
                GenDraw.DrawFillableBar(r);
            }
        }

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            text += $"\n{"HDHPlantCount".Translate()}: {innerContainer.Count}";
            if (innerContainer.Count > 0)
            {
                text += $"\n{"HDHHighestGrowth".Translate()}: {highestGrowth * 100}%";
            }
            return text;
        }

        public virtual bool TryAcceptThing(Thing thing, bool allowSpecialEffects = true)
        {
            bool flag = !Accepts(thing);
            bool result;
            if (flag)
            {
                result = false;
            }
            else
            {
                bool flag2 = thing.holdingOwner != null;
                if (flag2)
                {
                    thing.holdingOwner.TryTransferToContainer(thing, innerContainer, thing.stackCount);
                    result = true;
                }
                else
                {
                    result = innerContainer.TryAdd(thing);
                }
            }
            return result;
        }

        public new bool CanAcceptSowNow()
        {
            return base.CanAcceptSowNow() && bayStage == BayStage.Sowing;
        }

        public Building_HighDensityHydro()
        {
            innerContainer = new ThingOwner<Thing>(this, false);
        }

        public override void PostMake()
        {
            base.PostMake();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            loadConfig();
            int x = def.size.x;
            int z = def.size.z;
            Log.Message("x: " + x.ToString() + " y: " + z.ToString());
            bool flag = x == 1;
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
            float x2 = (float)z - 0.4f;
            barsize = new Vector2(x2, y);
        }

        private void loadConfig()
        {
            HydroStatsExtension modExtension = def.GetModExtension<HydroStatsExtension>();
            bool flag = modExtension != null;
            if (flag)
            {
                capacity = modExtension.capacity;
                fertility = modExtension.fertility;
            }
        }

        public virtual void EjectContents()
        {
            innerContainer.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return null;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look<ThingOwner>(ref innerContainer, "innerContainer", new object[]
            {
                this
            });
            Scribe_Values.Look<BayStage>(ref bayStage, "growingStage", BayStage.Growing);
            Scribe_Values.Look<float>(ref highestGrowth, "highestGrowth");
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            innerContainer.ClearAndDestroyContents();
            base.Destroy(mode);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            innerContainer.ClearAndDestroyContents();
            foreach (Plant plant in PlantsOnMe)
            {
                plant.Destroy();
            }
            base.DeSpawn(mode);
        }

        public virtual bool Accepts(Thing thing)
        {
            return innerContainer.CanAcceptAnyOf(thing);
        }

        public new void SetPlantDefToGrow(ThingDef plantDef)
        {
            base.SetPlantDefToGrow(plantDef);
            bool flag = bayStage == BayStage.Sowing;
            if (flag)
            {
                innerContainer.ClearAndDestroyContents();
                foreach (Plant plant in PlantsOnMe)
                {
                    plant.Destroy();
                }
            }
        }
    }
}
