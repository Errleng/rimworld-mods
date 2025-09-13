using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace VanoTech
{
    [StaticConstructorOnStartup]
    public static class VanoTech
    {
        static VanoTech()
        {
            Log.Message("VanoTech loaded");
        }
    }
}
