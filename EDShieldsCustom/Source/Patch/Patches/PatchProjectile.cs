using HarmonyLib;
using Jaxxa.EnhancedDevelopment.Shields.Shields;
using System;
using System.Reflection;
using Verse;

namespace Jaxxa.EnhancedDevelopment.Shields.Patch.Patches
{
    class PatchProjectile : Patch
    {

        protected override void ApplyPatch(Harmony harmony = null)
        {
            this.ApplyTickPatch(harmony);
        }

        protected override string PatchDescription()
        {
            return "PatchProjectile";
        }

        protected override bool ShouldPatchApply()
        {
            return true;
        }

        #region "Tick Patch"

        private void ApplyTickPatch(Harmony harmony)
        {

            //Get the Launch Method
            MethodInfo _ProjectileTick = typeof(Verse.Projectile).GetMethod("Tick");
            Patcher.LogNULL(_ProjectileTick, "_ProjectileTick");

            //Get the Launch Prefix Patch
            MethodInfo _ProjectileTickPrefix = typeof(PatchProjectile).GetMethod("ProjectileTickPrefix", BindingFlags.Public | BindingFlags.Static);
            Patcher.LogNULL(_ProjectileTickPrefix, "_ProjectileTickPrefix");

            //Apply the Prefix Patches
            harmony.Patch(_ProjectileTick, new HarmonyMethod(_ProjectileTickPrefix), null);
        }


        // prefix
        // - wants instance, result and count
        // - wants to change count
        // - returns a boolean that controls if original is executed (true) or not (false)
        public static Boolean ProjectileTickPrefix(ref Projectile __instance)
        {
            if (__instance.Map.GetComponent<ShieldManagerMapComp>().WillProjectileBeBlocked(__instance))
            {
                return false;
            }
            return true;
        }

        #endregion
    }
}