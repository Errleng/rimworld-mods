using System;
using Verse;

namespace RimMisc
{
    public class RimMisc : Mod
    {
        private RimMiscSettings settings;
        public RimMisc(ModContentPack content) : base(content)
        {
            settings = GetSettings<RimMiscSettings>();
        }
    }
}
