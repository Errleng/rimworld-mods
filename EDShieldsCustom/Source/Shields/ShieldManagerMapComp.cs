using RimWorld;
using System.Collections.Generic;
using System.Linq;
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

        public void RecalaculateAll()
        {
            _ShieldBuildings.ForEach(x => x.RecalculateStatistics());
        }
    }
}
