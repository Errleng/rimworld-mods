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
			LongEventHandler.QueueLongEvent (HelpBuilder.ResolveImpliedDefs, "BuildingHelpDatabase", false, null);
        }
    }
}
