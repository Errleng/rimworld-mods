using CombatExtended;
using CombatExtended.Compatibility;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimCheats
{
    internal class RimCheatsWorldComp : WorldComponent
    {
        private static readonly RimCheatsSettings settings = LoadedModManager.GetMod<RimCheats>().GetSettings<RimCheatsSettings>();
        private static readonly int SHORT_UPDATE_TICKS = GenDate.TicksPerHour;
        private static readonly int LONG_UPDATE_TICKS = GenDate.TicksPerDay;

        public List<SpawnBuildingInfo> buildingsToRestore = new List<SpawnBuildingInfo>();

        public RimCheatsWorldComp(World world) : base(world)
        {
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref buildingsToRestore, "buildingsToRestore", LookMode.Deep);
            if (buildingsToRestore == null)
            {
                buildingsToRestore = new List<SpawnBuildingInfo>();
            }
            var numNullBuildings = buildingsToRestore.Where(x => x == null || x.thing == null).Count();
            if (numNullBuildings > 0)
            {
                Log.Error($"Found {numNullBuildings} nulls in list of buildings to restore. Removing.");
                buildingsToRestore = buildingsToRestore.Where(x => x != null && x.thing != null).ToList();
            }
            base.ExposeData();
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            var ticks = Find.TickManager.TicksGame;

            if (settings.autoRepair && ticks % SHORT_UPDATE_TICKS == 0)
            {
                var map = Find.CurrentMap;
                var buildings = map.listerBuildings.allBuildingsColonist;
                foreach (var building in buildings)
                {
                    if (building.IsBrokenDown())
                    {
                        building.GetComp<CompBreakdownable>().Notify_Repaired();
                    }
                    building.HitPoints += (int)Math.Ceiling(building.MaxHitPoints * RimCheatsSettings.REPAIR_PERCENT);
                    building.HitPoints = Math.Min(building.HitPoints, building.MaxHitPoints);
                    map.listerBuildingsRepairable.Notify_BuildingRepaired(building);
                }
            }

            if (settings.infiniteTurretAmmo && ticks % SHORT_UPDATE_TICKS == 0)
            {
                if (ModsConfig.IsActive("CETeam.CombatExtended"))
                {
                    foreach (var map in Find.Maps)
                    {
                        // reload friendlies with Combat Extended ammo
                        ReloadCombatExtendedAmmo(map);
                    }
                }
            }

            if (ticks % LONG_UPDATE_TICKS == 0)
            {
                // repair all equipment
                foreach (var colonist in PawnsFinder.AllMaps_FreeColonists)
                {
                    foreach (var thing in colonist.equipment.AllEquipmentListForReading)
                    {
                        thing.HitPoints = thing.MaxHitPoints;
                    }

                    foreach (var thing in colonist.apparel.WornApparel)
                    {
                        thing.HitPoints = thing.MaxHitPoints;
                    }
                }

                foreach (var info in buildingsToRestore)
                {
                    var thing = info.thing;
                    thing.stackCount = 1;
                    thing.HitPoints = thing.MaxHitPoints;
                    Traverse.Create(thing).Field("mapIndexOrState").SetValue((sbyte)-1); // avoids error message in SpawnSetup
                    try
                    {
                        GenSpawn.Spawn(thing, thing.Position, info.map, thing.Rotation, WipeMode.Vanish, false);
                    }
                    catch (Exception ex)
                    {
                        var message = $"Failed to restore building {thing} on map {info.map}: {ex.Message}";
                        Log.Error(message);
                        Messages.Message(message, MessageTypeDefOf.NegativeEvent);
                    }
                }
                buildingsToRestore.Clear();

                if (settings.maxSkills)
                {
                    foreach (var colonist in PawnsFinder.AllMaps_FreeColonists)
                    {
                        foreach (var skill in colonist.skills.skills)
                        {
                            skill.Level += 20;
                        }
                    }
                }

                if (settings.autoClean)
                {
                    var map = Find.CurrentMap;
                    var filths = map.listerThings.ThingsInGroup(ThingRequestGroup.Filth);
                    int cleaned = 0;
                    for (int i = filths.Count - 1; i >= 0; --i)
                    {
                        var filth = filths[i] as Filth;
                        if (filth == null)
                        {
                            Log.Error($"Thing {filths[i]} is not filth!");
                        }
                        else
                        {
                            filth.DeSpawn();
                            if (!filth.Destroyed)
                            {
                                filth.Destroy(DestroyMode.Vanish);
                            }
                            if (!filth.Discarded)
                            {
                                filth.Discard();
                            }
                            ++cleaned;
                        }
                    }
                }
            }
        }

        void ReloadCombatExtendedAmmo(Map map)
        {
            foreach (var pawn in map.mapPawns.AllPawnsSpawned.ToList())
            {
                if (pawn.Faction.HostileTo(Faction.OfPlayer))
                {
                    continue;
                }
                var inventory = pawn.TryGetComp<CompInventory>();
                var loadout = pawn.GetLoadout();
                if (inventory == null || loadout == null)
                {
                    continue;
                }

                var weapons = inventory.rangedWeaponList.ToList();
                if (pawn.equipment?.Primary != null)
                {
                    weapons.Add(pawn.equipment.Primary);
                }

                foreach (var weapon in weapons)
                {
                    var compAmmoUser = weapon.TryGetComp<CompAmmoUser>();
                    if (compAmmoUser == null)
                    {
                        continue;
                    }
                    compAmmoUser.ResetAmmoCount(compAmmoUser.SelectedAmmo);
                    compAmmoUser.CurMagCount = compAmmoUser.MagSize * 10;
                }
            }

            foreach (var turret in map.listerBuildings.AllBuildingsColonistOfClass<Building_Turret>().ToList())
            {
                var compAmmoUser = turret.GetAmmo();
                if (compAmmoUser == null)
                {
                    continue;
                }
                compAmmoUser.ResetAmmoCount(compAmmoUser.SelectedAmmo);
                compAmmoUser.CurMagCount = compAmmoUser.MagSize * 4;
            }
        }
    }
}
