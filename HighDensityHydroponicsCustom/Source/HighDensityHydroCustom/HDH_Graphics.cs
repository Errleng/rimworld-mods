using UnityEngine;
using Verse;

namespace HighDensityHydroCustom
{
    [StaticConstructorOnStartup]
    public static class HDH_Graphics
    {
        // Token: 0x0400000C RID: 12
        public static readonly Material HDHBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.2f, 0.85f, 0.2f), false);

        // Token: 0x0400000D RID: 13
        public static readonly Material HDHBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f), false);
    }
}