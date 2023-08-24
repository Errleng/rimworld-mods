using HarmonyLib;
using System;
using System.Collections.Generic;
using Verse;

namespace RimMisc
{
    [HarmonyPatch(typeof(RoofCollapserImmediate))]
    internal class Patch_RoofCollapse
    {
        [HarmonyPatch("DropRoofInCells", new Type[] { typeof(IntVec3), typeof(Map), typeof(List<Thing>) })]
        [HarmonyPrefix]
        static bool Prefix1()
        {
            return !RimMisc.Settings.preventRoofCollapse;
        }

        [HarmonyPatch("DropRoofInCells", new Type[] { typeof(IEnumerable<IntVec3>), typeof(Map), typeof(List<Thing>) })]
        [HarmonyPrefix]
        static bool Prefix2()
        {
            return !RimMisc.Settings.preventRoofCollapse;
        }

        [HarmonyPatch("DropRoofInCells", new Type[] { typeof(List<IntVec3>), typeof(Map), typeof(List<Thing>) })]
        [HarmonyPrefix]
        static bool Prefix3()
        {
            return !RimMisc.Settings.preventRoofCollapse;
        }
    }
}
