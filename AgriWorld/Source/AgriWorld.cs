using HarmonyLib;
using Verse;

namespace AgriWorld
{
    class AgriWorld : Mod
    {
        public AgriWorld(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("com.agriworld.rimworld.mod");
            harmony.PatchAll();
        }

        //[HarmonyPatch(typeof(PlantUtility), "CanSowOnGrower")]
        //public class Patch_Sow
        //{
        //    static void Postfix(ThingDef plantDef, object obj, ref bool __result)
        //    {
        //        __result = true;
        //    }
        //}
    }
}
