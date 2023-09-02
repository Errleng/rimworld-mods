using RimWorld;
using UnityEngine;
using Verse;

namespace Jaxxa.EnhancedDevelopment.Shields.Shields
{
    class ITab_ShieldGenerator : ITab
    {

        //private Comp_ShieldGenerator _CachedComp;

        //public ITab_ShieldGenerator() : base()
        //{
        //    _CachedComp = 

        //}

        private static readonly Vector2 WinSize = new Vector2(500f, 400f);

        private Comp_ShieldGenerator SelectedCompShieldGenerator
        {
            get
            {
                Thing thing = Find.Selector.SingleSelectedThing;
                MinifiedThing minifiedThing = thing as MinifiedThing;
                if (minifiedThing != null)
                {
                    thing = minifiedThing.InnerThing;
                }
                if (thing == null)
                {
                    return null;
                }
                return thing.TryGetComp<Comp_ShieldGenerator>();
            }
        }

        public override bool IsVisible
        {
            get
            {
                return SelectedCompShieldGenerator != null;
            }
        }

        public ITab_ShieldGenerator()
        {
            size = WinSize;
            labelKey = "TabShield";
        }

        protected override void FillTab()
        {

            Vector2 winSize = WinSize;
            float x = winSize.x;
            Vector2 winSize2 = WinSize;
            Rect rect = new Rect(0f, 0f, x, winSize2.y).ContractedBy(10f);
            //Rect rect2 = rect;
            //Text.Font = GameFont.Medium;
            //Widgets.Label(rect2, "Shield Generator Label Rec2");
            //if (ITab_Art.cachedImageSource != this.SelectedCompArt || ITab_Art.cachedTaleRef != this.SelectedCompArt.TaleRef)
            //{
            //    ITab_Art.cachedImageDescription = this.SelectedCompArt.GenerateImageDescription();
            //    ITab_Art.cachedImageSource = this.SelectedCompArt;
            //    ITab_Art.cachedTaleRef = this.SelectedCompArt.TaleRef;
            //}
            //Rect rect3 = rect;
            //rect3.yMin += 35f;
            //Text.Font = GameFont.Small;
            //Widgets.Label(rect3, "ShieldGenerator Rec3");

            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.ColumnWidth = 250f;
            listing_Standard.Begin(rect);


            listing_Standard.GapLine(12f);
            listing_Standard.Label("Charge: " + SelectedCompShieldGenerator.FieldIntegrity_Current + " / " + SelectedCompShieldGenerator.m_FieldIntegrity_Max);
            listing_Standard.Label("Charge Rate: " + SelectedCompShieldGenerator.m_FieldRegenRate + " per second");

            listing_Standard.Gap(12f);

            listing_Standard.Label("Radius: " + SelectedCompShieldGenerator.m_FieldRadius_Requested + " / " + SelectedCompShieldGenerator.m_FieldRadius_Avalable);
            listing_Standard.IntAdjuster(ref SelectedCompShieldGenerator.m_FieldRadius_Requested, 1, 1);
            if (SelectedCompShieldGenerator.m_FieldRadius_Requested > SelectedCompShieldGenerator.m_FieldRadius_Avalable)
            {
                SelectedCompShieldGenerator.m_FieldRadius_Requested = SelectedCompShieldGenerator.m_FieldRadius_Avalable;
            }

            //Direct
            if (SelectedCompShieldGenerator.BlockDirect_Active())
            {
                if (listing_Standard.ButtonText("Toggle Direct: Active"))
                {
                    SelectedCompShieldGenerator.SwitchDirect();
                }
            }
            else
            {
                if (listing_Standard.ButtonText("Toggle Direct: Inactive"))
                {
                    SelectedCompShieldGenerator.SwitchDirect();
                }

            }

            //Indirect
            if (SelectedCompShieldGenerator.BlockIndirect_Active())
            {
                if (listing_Standard.ButtonText("Toggle Indirect: Active"))
                {
                    SelectedCompShieldGenerator.SwitchIndirect();
                }
            }
            else
            {
                if (listing_Standard.ButtonText("Toggle Indirect: Inactive"))
                {
                    SelectedCompShieldGenerator.SwitchIndirect();
                }

            }

            if (SelectedCompShieldGenerator.IsInterceptDropPod_Avalable())
            {
                if (SelectedCompShieldGenerator.IntercepDropPod_Active())
                {
                    if (listing_Standard.ButtonText("Toggle DropPod Intercept: Active"))
                    {
                        SelectedCompShieldGenerator.SwitchInterceptDropPod();
                    }
                }
                else
                {
                    if (listing_Standard.ButtonText("Toggle DropPod Intercept: Inactive"))
                    {
                        SelectedCompShieldGenerator.SwitchInterceptDropPod();
                    }

                }

            }
            else
            {
                listing_Standard.Label("DropPod Intercept Unavalable");
            }

            if (SelectedCompShieldGenerator.IdentifyFriendFoe_Active())
            {
                listing_Standard.Label("IFF Active");

            }
            else
            {
                listing_Standard.Label("IFF Inactive");
            }

            listing_Standard.End();
        }

    } //Class

} //NameSpace

