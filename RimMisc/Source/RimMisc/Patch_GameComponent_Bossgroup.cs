using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimMisc
{
    internal class Patch_GameComponent_Bossgroup
    {
        [HarmonyPatch(typeof(GameComponent_Bossgroup), "Notify_BossgroupCalled")]
        public static class Patch_Notify_BossgroupCalled
        {
            static void Postfix(GameComponent_Bossgroup __instance)
            {
                if (RimMisc.Settings.myMiscStuff)
                {
                    __instance.lastBossgroupCalled = 0;
                }
            }
        }
    }
}
