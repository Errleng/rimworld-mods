using CombatExtended;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Jaxxa.EnhancedDevelopment.Shields.Shields
{
    [StaticConstructorOnStartup]
    class ShieldManagerMapComp : MapComponent
    {
        private static readonly int UPDATE_TICKS = GenDate.TicksPerHour;
        public static readonly SoundDef HitSoundDef = SoundDef.Named("Shields_HitShield");
        private List<Building_Shield> _ShieldBuildings = new List<Building_Shield>();

        public ShieldManagerMapComp(Map map) : base(map)
        {
            this.map = map;
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            _ShieldBuildings = map.listerBuildings.AllBuildingsColonistOfClass<Building_Shield>().ToList();
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            var ticks = Find.TickManager.TicksGame;
            if (ticks % UPDATE_TICKS == 0)
            {
                _ShieldBuildings = map.listerBuildings.AllBuildingsColonistOfClass<Building_Shield>().ToList();
            }
        }

        private void ReflectProjectile(Building_Shield shield, Projectile projectile)
        {
            var launcher = projectile.Launcher;
            if (launcher == null || launcher.HostileTo(Faction.OfPlayer) || !shield.WillProjectileBeReflected())
            {
                return;
            }
            //Spawn and launch a projectile
            Projectile reflectedProj = (Projectile)GenSpawn.Spawn(projectile.def, projectile.Position, projectile.Map);
            reflectedProj.Launch(
                shield,
                projectile.ExactPosition,
                new LocalTargetInfo(launcher.Position),
                launcher,
                ProjectileHitFlags.IntendedTarget,
                false,
                launcher
            );
        }

        private void ReflectProjectile(Building_Shield shield, ProjectileCE projectile)
        {
            var launcher = projectile.launcher;
            if (launcher == null || !launcher.HostileTo(Faction.OfPlayer) || !shield.WillProjectileBeReflected())
            {
                return;
            }

            // Calculations
            var projProps = projectile.def.projectile as ProjectilePropertiesCE;
            if (projProps.flyOverhead)
            {
                return;
            }

            CollisionVertical collisionVertical = new CollisionVertical(launcher);
            float shotAngle = ProjectileCE.GetShotAngle(projProps.speed, (launcher.Position - projectile.Position).LengthHorizontal, collisionVertical.HeightRange.Average - 1f, projProps.flyOverhead, projProps.Gravity);

            Vector2 originVec = new Vector2(projectile.TrueCenter().x, projectile.TrueCenter().z);
            Vector2 targetVec = new Vector2(launcher.TrueCenter().x, launcher.TrueCenter().z);
            Vector2 toTargetVec = targetVec - originVec;
            float shotRotation = (-90f + 57.29578f * Mathf.Atan2(toTargetVec.y, toTargetVec.x)) % 360f;

            //Spawn and launch a projectile
            ProjectileCE projectileCE = (ProjectileCE)ThingMaker.MakeThing(projectile.def, null);
            GenSpawn.Spawn(projectileCE, projectile.Position, projectile.Map, WipeMode.Vanish);
            projectileCE.canTargetSelf = false;
            projectileCE.minCollisionDistance = projectile.minCollisionDistance;
            projectileCE.intendedTarget = launcher;
            projectileCE.mount = null;
            projectileCE.AccuracyFactor = 1f;

            //Log.Message($"Original projectile: {projectile}, {projectile.shotAngle}, {projectile.shotRotation}, {projectile.shotHeight}, {projectile.shotSpeed}");
            //Log.Message($"Reflected projectile: {projectileCE}, {shield}, {originVec}, {shotAngle}, {shotRotation}, {projectile.shotHeight}, {projectile.shotSpeed}, {projectile.minCollisionDistance}");
            //Log.Message($"Origin {projectile.ExactPosition}, {originVec}, target {launcher.Position}, {targetVec}, {toTargetVec}");
            projectileCE.Launch(shield, originVec, projectile.shotAngle, shotRotation, projectile.shotHeight);
        }

        public bool WillDropPodBeIntercepted(DropPodIncoming dropPodToTest)
        {
            if (_ShieldBuildings.Any(x => x.WillInterceptDropPod(dropPodToTest)))
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public bool WillProjectileBeBlocked(Verse.Projectile projectile)
        {
            Building_Shield _BlockingShield = _ShieldBuildings.FirstOrFallback(x => x.WillProjectileBeBlocked(projectile));

            if (_BlockingShield != null)
            {
                _BlockingShield.TakeDamageFromProjectile(projectile);

                //On hit effects
                FleckMaker.ThrowLightningGlow(projectile.ExactPosition, map, 0.5f);

                //On hit sound
                HitSoundDef.PlayOneShot((SoundInfo)new TargetInfo(projectile.Position, projectile.Map, false));

                ReflectProjectile(_BlockingShield, projectile);

                projectile.Destroy();
                return true;
            }

            return false;
        }

        public bool WillProjectileBeBlocked(ProjectileCE projectile)
        {
            Building_Shield _BlockingShield = _ShieldBuildings.FirstOrFallback(x => x.WillProjectileBeBlocked(projectile));

            if (_BlockingShield != null)
            {
                _BlockingShield.TakeDamageFromProjectile(projectile);

                //On hit effects
                FleckMaker.ThrowLightningGlow(projectile.ExactPosition, map, 0.5f);

                //On hit sound
                HitSoundDef.PlayOneShot((SoundInfo)new TargetInfo(projectile.Position, projectile.Map, false));

                ReflectProjectile(_BlockingShield, projectile);

                Log.Message($"Destroying projectile: {projectile}");
                projectile.Destroy();
                return true;
            }

            return false;
        }

        public void RecalaculateAll()
        {
            _ShieldBuildings.ForEach(x => x.RecalculateStatistics());
        }
    }
}
