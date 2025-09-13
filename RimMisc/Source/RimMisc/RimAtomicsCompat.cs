using Rimatomics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimMisc
{
    internal class RimAtomicsCompat
    {
        public void FinishRimatomicsResearch()
        {
            if (ModsConfig.IsActive("Dubwise.Rimatomics"))
            {
                try
                {
                    foreach (var research in DubUtils.GetResearch().AllProjects)
                    {
                        Building_RimatomicsResearchBench.Purchase(research);
                        Building_RimatomicsResearchBench.DebugFinish(research);
                    }
                }
                catch (NullReferenceException)
                {
                    Log.Warning("DubUtils.GetResearch() is null, skipping finishing research");
                }
            }
        }
    }
}
