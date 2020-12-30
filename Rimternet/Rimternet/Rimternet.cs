using System;
using System.Reflection;
using RimWorld;
using Verse;

namespace Rimternet
{
    public class Rimternet : Mod
    {
        public Rimternet(ModContentPack content) : base(content)
        {
            LongEventHandler.QueueLongEvent(HelpBuilder.ResolveImpliedDefs, "BuildingHelpDatabase", false, null);
        }
    }

    public static class ObjectExtension
    {
        public static string ToStringNullable(this object value)
        {
            return (value ?? "Null").ToString();
        }
    }
}
