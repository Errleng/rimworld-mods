using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimSpawners
{
    internal class Building_CoreFabricator : Building
    {
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (Spawned)
            {
                Skyfaller.DrawDropSpotShadow(drawLoc, Rot4.North, ShadowMaterial, def.size.ToVector2(), 65);
                float height = 0.55f + 0.5f * (1f + Mathf.Sin((float)(2 * Math.PI * GenTicks.TicksGame / 600f))) * 0.35f;
                drawLoc.z += height;
                Graphic.Draw(drawLoc, flip ? Rotation.Opposite : Rotation, this, 0);
                SilhouetteUtility.DrawGraphicSilhouette(this, drawLoc);
                return;
            }
            base.DrawAt(drawLoc, flip);
        }

        private Material ShadowMaterial
        {
            get
            {
                if (cachedShadowMaterial == null)
                {
                    cachedShadowMaterial = MaterialPool.MatFrom("Things/Skyfaller/SkyfallerShadowDropPod", ShaderDatabase.Transparent);
                }
                return cachedShadowMaterial;
            }
        }

        private Material cachedShadowMaterial;
    }
}