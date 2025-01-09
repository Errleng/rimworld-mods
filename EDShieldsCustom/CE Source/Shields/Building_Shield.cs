using CombatExtended;
using RimWorld;
using Verse;

//using EnhancedDevelopment.Shields.ShieldUtils;

namespace Jaxxa.EnhancedDevelopment.Shields.Shields
{
    [StaticConstructorOnStartup]
    public class Building_Shield : Building
    {

        #region Methods

        public override string GetInspectString()
        {
            return GetComp<Comp_ShieldGenerator>().CompInspectStringExtra();
        }

        public bool WillInterceptDropPod(DropPodIncoming dropPodToCheck)
        {
            return GetComp<Comp_ShieldGenerator>().WillInterceptDropPod(dropPodToCheck);
        }

        public bool WillProjectileBeBlocked(Projectile projectileToCheck)
        {
            return GetComp<Comp_ShieldGenerator>().WillProjectileBeBlocked(projectileToCheck);
        }

        public bool WillProjectileBeBlocked(ProjectileCE projectileToCheck)
        {
            return GetComp<Comp_ShieldGenerator>().WillProjectileBeBlocked(projectileToCheck);
        }

        public bool WillProjectileBeReflected()
        {
            return GetComp<Comp_ShieldGenerator>().reflectProjectiles;
        }

        public void TakeDamageFromProjectile(Projectile projectile)
        {
            GetComp<Comp_ShieldGenerator>().FieldIntegrity_Current -= projectile.DamageAmount;
        }
        public void TakeDamageFromProjectile(ProjectileCE projectile)
        {
            GetComp<Comp_ShieldGenerator>().FieldIntegrity_Current -= (int)projectile.DamageAmount;
        }


        public void RecalculateStatistics()
        {
            //Log.Message("Calculate");
            GetComp<Comp_ShieldGenerator>().RecalculateStatistics();
        }

        #endregion //Methods

    }
}