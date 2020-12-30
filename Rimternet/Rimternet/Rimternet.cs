using System;
using System.Reflection;
using RimWorld;
using Verse;
using HarmonyLib;

namespace Rimternet
{
    public class Rimternet : Mod
    {
        private RimternetSettings settings;
        public Rimternet(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<RimternetSettings>();
        }
        public override string SettingsCategory()
        {
            return "Rimternet".Translate();
        }
    }

    public class RimternetSettings : ModSettings
    {
    }

    [StaticConstructorOnStartup]
    public class Patcher
    {
        static Patcher()
        {
            Harmony harmony = new Harmony("com.rimternet.rimworld.mod");
            Assembly assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
            Log.Message("Rimternet loaded");
        }
    }
}
