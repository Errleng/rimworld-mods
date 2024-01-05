using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Jaxxa.EnhancedDevelopment.Shields.Shields
{

    [StaticConstructorOnStartup]
    public class Comp_ShieldGenerator : ThingComp
    {
        private static readonly Material ForceFieldMat = MaterialPool.MatFrom("Other/ForceField", ShaderDatabase.MoteGlow);
        private static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();

        public CompProperties_ShieldGenerator Properties;

        #region Variables

        //UI elements - Unsaved
        private static Texture2D UI_DIRECT_ON;
        private static Texture2D UI_DIRECT_OFF;

        private static Texture2D UI_INDIRECT_ON;
        private static Texture2D UI_INDIRECT_OFF;

        private static Texture2D UI_INTERCEPT_DROPPOD_ON;
        private static Texture2D UI_INTERCEPT_DROPPOD_OFF;

        private static Texture2D UI_SHOW_ON;
        private static Texture2D UI_SHOW_OFF;

        private static Texture2D UI_LAUNCH_REPORT;

        //Visual Settings
        private bool m_ShowVisually_Active = true;
        private float m_ColourRed;
        private float m_ColourGreen;
        private float m_ColourBlue;

        //Field Settings
        public int m_FieldIntegrity_Max;
        private int m_FieldIntegrity_Initial;
        public int m_FieldRegenRate;

        //Recovery Settings
        private int m_RechargeTickDelayInterval;
        private int m_RecoverWarmupDelayTicks;
        private int m_WarmupTicksRemaining;

        private List<Building> m_AppliedUpgrades = new List<Building>();

        #endregion Variables

        #region Settings

        // Power Usage --------------------------------------------------------------

        //Comp, found each time.
        CompPowerTrader m_Power;

        private int m_PowerRequired;


        // Range --------------------------------------------------------------------

        public int m_FieldRadius_Avalable;
        public int m_FieldRadius_Requested = 999;

        public int FieldRadius_Active()
        {
            return Math.Min(m_FieldRadius_Requested, m_FieldRadius_Avalable);
        }

        // Block Direct -------------------------------------------------------------


        private bool m_BlockDirect_Avalable;

        private bool m_BlockDirect_Requested = true;

        public bool BlockDirect_Active()
        {
            return m_BlockDirect_Avalable && m_BlockDirect_Requested;
        }

        // Block Indirect -----------------------------------------------------------


        private bool m_BlockIndirect_Avalable;

        private bool m_BlockIndirect_Requested = true;

        public bool BlockIndirect_Active()
        {
            return m_BlockIndirect_Avalable && m_BlockIndirect_Requested;
        }

        //Block Droppods ------------------------------------------------------------

        private bool m_InterceptDropPod_Avalable;

        private bool m_InterceptDropPod_Requested = true;

        public bool IntercepDropPod_Active()
        {
            return m_InterceptDropPod_Avalable && m_InterceptDropPod_Requested;
        }

        public bool IsInterceptDropPod_Avalable()
        {
            return m_InterceptDropPod_Avalable;
        }

        // Identify Friend Foe ------------------------------------------------------

        private bool m_IdentifyFriendFoe_Avalable = false;

        private bool m_IdentifyFriendFoe_Requested = true;

        public bool IdentifyFriendFoe_Active()
        {
            return m_IdentifyFriendFoe_Avalable && m_IdentifyFriendFoe_Requested;
        }

        // Slow Discharge -----------------------------------------------------------

        public bool SlowDischarge_Active;

        // Reflect

        public bool reflectProjectiles = false;

        #endregion

        #region Initilisation

        //Static Construtor
        static Comp_ShieldGenerator()
        {
            //Setup UI
            UI_DIRECT_OFF = ContentFinder<Texture2D>.Get("UI/DirectOff", true);
            UI_DIRECT_ON = ContentFinder<Texture2D>.Get("UI/DirectOn", true);
            UI_INDIRECT_OFF = ContentFinder<Texture2D>.Get("UI/IndirectOff", true);
            UI_INDIRECT_ON = ContentFinder<Texture2D>.Get("UI/IndirectOn", true);
            UI_INTERCEPT_DROPPOD_OFF = ContentFinder<Texture2D>.Get("UI/FireOff", true);
            UI_INTERCEPT_DROPPOD_ON = ContentFinder<Texture2D>.Get("UI/FireOn", true);

            UI_SHOW_ON = ContentFinder<Texture2D>.Get("UI/ShieldShowOn", true);
            UI_SHOW_OFF = ContentFinder<Texture2D>.Get("UI/ShieldShowOff", true);
            UI_LAUNCH_REPORT = ContentFinder<Texture2D>.Get("UI/Commands/LaunchReport");
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            Properties = ((CompProperties_ShieldGenerator)props);
            m_Power = parent.GetComp<CompPowerTrader>();

            RecalculateStatistics();
        }

        public void RecalculateStatistics()
        {
            //Log.Message("RecalculateStatistics");

            //Visual Settings
            m_ColourRed = 0.5f;
            m_ColourGreen = 0.0f;
            m_ColourBlue = 0.5f;

            //Field Settings
            m_FieldIntegrity_Max = Properties.m_FieldIntegrity_Max_Base;
            m_FieldIntegrity_Initial = Properties.m_FieldIntegrity_Initial;
            m_FieldRegenRate = Properties.m_FieldRegenRate_Initial;
            m_FieldRadius_Avalable = Properties.m_Field_Radius_Base;

            //Mode Settings - Avalable
            m_BlockIndirect_Avalable = Properties.m_BlockIndirect_Avalable;
            m_BlockDirect_Avalable = Properties.m_BlockDirect_Avalable;
            m_InterceptDropPod_Avalable = Properties.m_InterceptDropPod_Avalable;

            //Power Settings
            m_PowerRequired = Properties.m_PowerRequired_Charging;

            //Recovery Settings
            m_RechargeTickDelayInterval = Properties.m_RechargeTickDelayInterval_Base;
            m_RecoverWarmupDelayTicks = Properties.m_RecoverWarmupDelayTicks_Base;

            //Power converter
            SlowDischarge_Active = false;

            //IFF
            m_IdentifyFriendFoe_Avalable = false;

            //Store the List of Building in initilisation????

            m_AppliedUpgrades.ForEach(b =>
            {
                Building _Building = b as Building;
                Comp_ShieldUpgrade _Comp = _Building.GetComp<Comp_ShieldUpgrade>();

                Patch.Patcher.LogNULL(_Building, "_Building");
                Patch.Patcher.LogNULL(_Comp, "_Comp");

                AddStatsFromUpgrade(_Comp);

            });

            m_Power.powerOutputInt = -m_PowerRequired;

        }

        private void AddStatsFromUpgrade(Comp_ShieldUpgrade comp)
        {

            CompProperties_ShieldUpgrade _Properties = ((CompProperties_ShieldUpgrade)comp.props);
            Patch.Patcher.LogNULL(_Properties, "_Properties");

            m_FieldIntegrity_Max += _Properties.FieldIntegrity_Increase;
            m_FieldRegenRate += _Properties.FieldRegenRate_Increase;
            m_FieldRadius_Avalable += _Properties.Range_Increase;

            //Power
            m_PowerRequired += _Properties.PowerUsage_Increase;

            if (_Properties.DropPodIntercept)
            {
                m_InterceptDropPod_Avalable = true;
            }

            if (_Properties.IdentifyFriendFoe)
            {
                //Log.Message("Setting IFF");
                m_IdentifyFriendFoe_Avalable = true;
            }

            if (_Properties.SlowDischarge)
            {
                SlowDischarge_Active = true;
            }
        }

        #endregion Initilisation

        #region Methods

        public override void CompTick()
        {
            base.CompTick();

            //this.RecalculateStatistics();

            UpdateShieldStatus();

            TickRecharge();

        }

        public void UpdateShieldStatus()
        {
            Boolean _PowerAvalable = CheckPowerOn();

            switch (CurrentStatus)
            {

                case (EnumShieldStatus.Offline):

                    //If it is offline bit has Power start initialising
                    if (_PowerAvalable)
                    {
                        CurrentStatus = EnumShieldStatus.Initilising;
                        m_WarmupTicksRemaining = m_RecoverWarmupDelayTicks;
                    }
                    break;

                case (EnumShieldStatus.Initilising):
                    if (_PowerAvalable)
                    {
                        if (m_WarmupTicksRemaining > 0)
                        {
                            m_WarmupTicksRemaining--;
                        }
                        else
                        {
                            CurrentStatus = EnumShieldStatus.ActiveCharging;
                            FieldIntegrity_Current = m_FieldIntegrity_Max;
                        }
                    }
                    else
                    {
                        CurrentStatus = EnumShieldStatus.Offline;
                    }
                    break;

                case (EnumShieldStatus.ActiveDischarging):
                    if (_PowerAvalable)
                    {
                        CurrentStatus = EnumShieldStatus.ActiveCharging;
                    }
                    else
                    {
                        if (!SlowDischarge_Active)
                        {
                            m_FieldIntegrity_Current = 0;
                        }

                        if (FieldIntegrity_Current <= 0)
                        {
                            CurrentStatus = EnumShieldStatus.Offline;

                        }
                    }
                    break;

                case (EnumShieldStatus.ActiveCharging):
                    if (FieldIntegrity_Current < 0)
                    {
                        CurrentStatus = EnumShieldStatus.Offline;
                    }
                    else
                    {
                        if (!_PowerAvalable)
                        {
                            CurrentStatus = EnumShieldStatus.ActiveDischarging;
                        }
                        else if (FieldIntegrity_Current >= m_FieldIntegrity_Max)
                        {
                            CurrentStatus = EnumShieldStatus.ActiveSustaining;
                        }
                    }
                    break;

                case (EnumShieldStatus.ActiveSustaining):
                    if (!_PowerAvalable)
                    {
                        CurrentStatus = EnumShieldStatus.ActiveDischarging;
                    }
                    else
                    {
                        if (FieldIntegrity_Current < m_FieldIntegrity_Max)
                        {
                            CurrentStatus = EnumShieldStatus.ActiveCharging;
                        }
                    }
                    break;
            }
        }

        public bool IsActive()
        {
            //return true;
            return (CurrentStatus == EnumShieldStatus.ActiveCharging ||
                 CurrentStatus == EnumShieldStatus.ActiveDischarging ||
                 CurrentStatus == EnumShieldStatus.ActiveSustaining);
        }

        public bool CheckPowerOn()
        {
            if (m_Power != null)
            {
                if (m_Power.PowerOn)
                {
                    return true;
                }
            }
            return false;
        }

        public void TickRecharge()
        {
            if (Find.TickManager.TicksGame % m_RechargeTickDelayInterval == 0)
            {
                if (CurrentStatus == EnumShieldStatus.ActiveCharging)
                {
                    FieldIntegrity_Current += m_FieldRegenRate;
                }
                else if (CurrentStatus == EnumShieldStatus.ActiveDischarging)
                {
                    FieldIntegrity_Current--;
                }
            }
        }

        public bool WillInterceptDropPod(DropPodIncoming dropPodToCheck)
        {
            //Check if can and wants to intercept
            if (!IntercepDropPod_Active())
            {
                return false;
            }

            //Check if online
            if (CurrentStatus == EnumShieldStatus.Offline || CurrentStatus == EnumShieldStatus.Initilising)
            {
                return false;
            }


            //Check IFF
            if (IdentifyFriendFoe_Active())
            {
                bool _Hostile = dropPodToCheck.Contents.innerContainer.Any(x => x.Faction.HostileTo(Faction.OfPlayer));

                if (!_Hostile)
                {
                    return false;
                }
            }

            //Check Distance
            float _Distance = Vector3.Distance(dropPodToCheck.Position.ToVector3(), parent.Position.ToVector3());
            float _Radius = FieldRadius_Active();
            if (_Distance > _Radius)
            {
                return false;
            }

            //All Tests passed so intercept the pod
            return true;

        }

        public bool WillProjectileBeBlocked(Verse.Projectile projectile)
        {

            //Check if online
            if (CurrentStatus == EnumShieldStatus.Offline || CurrentStatus == EnumShieldStatus.Initilising)
            {
                return false;
            }

            //Check if can and wants to intercept
            if (projectile.def.projectile.flyOverhead)
            {
                if (!BlockIndirect_Active()) { return false; }
            }
            else
            {
                if (!BlockDirect_Active()) { return false; }
            }

            //Check Distance
            float _Distance = Vector3.Distance(projectile.Position.ToVector3(), parent.Position.ToVector3());
            if (_Distance > FieldRadius_Active())
            {
                return false;
            }

            //Check Angle
            if (!CorrectAngleToIntercept(projectile, parent))
            {
                return false;
            }

            //Check IFF
            if (IdentifyFriendFoe_Active())
            {
                var launcher = projectile.Launcher;
                if (launcher != null && launcher.Faction.HostileTo(Faction.OfPlayer))
                {
                    return false;
                }
            }

            return true;

        }

        public static Boolean CorrectAngleToIntercept(Projectile pr, Thing shieldBuilding)
        {
            //Detect proper collision using angles
            Quaternion targetAngle = pr.ExactRotation;

            Vector3 projectilePosition2D = pr.ExactPosition;
            projectilePosition2D.y = 0;

            Vector3 shieldPosition2D = shieldBuilding.Position.ToVector3();
            shieldPosition2D.y = 0;

            Quaternion shieldProjAng = Quaternion.LookRotation(projectilePosition2D - shieldPosition2D);

            if ((Quaternion.Angle(targetAngle, shieldProjAng) > 90))
            {
                return true;
            }

            return false;
        }

        #endregion Methods

        #region Properties

        public EnumShieldStatus CurrentStatus
        {
            get
            {
                return m_CurrentStatus;
            }
            set
            {
                m_CurrentStatus = value;

                //if (this.m_CurrentStatus == EnumShieldStatus.ActiveSustaining)
                //{
                //    this.m_Power.powerOutputInt = -this.m_PowerRequired_Standby;
                //}
                //else
                //{
                //    this.m_Power.powerOutputInt = -this.m_PowerRequired_Charging;
                //}
            }
        }
        private EnumShieldStatus m_CurrentStatus = EnumShieldStatus.Offline;

        public int FieldIntegrity_Current
        {
            get
            {
                return m_FieldIntegrity_Current;
            }
            set
            {
                if (value < 0)
                {
                    CurrentStatus = EnumShieldStatus.Offline;
                    m_FieldIntegrity_Current = 0;
                }
                else if (value > m_FieldIntegrity_Max)
                {
                    m_FieldIntegrity_Current = m_FieldIntegrity_Max;
                }
                else
                {
                    m_FieldIntegrity_Current = value;
                }
            }
        }
        private int m_FieldIntegrity_Current;

        #endregion Properties

        #region Drawing

        public override void PostDraw()
        {
            //Log.Message("DrawComp");
            base.PostDraw();

            DrawShields();


        }

        /// <summary>
        /// Draw the shield Field
        /// </summary>
        public void DrawShields()
        {
            if (!IsActive() || !m_ShowVisually_Active)
            {
                return;
            }

            //Draw field
            DrawField(Utilities.VectorsUtils.IntVecToVec(parent.Position));
        }

        //public override void DrawExtraSelectionOverlays()
        //{
        //    //    GenDraw.DrawRadiusRing(base.Position, shieldField.shieldShieldRadius);
        //}

        public void DrawSubField(IntVec3 center, float radius)
        {
            DrawSubField(Utilities.VectorsUtils.IntVecToVec(center), radius);
        }

        //Draw the field on map
        public void DrawField(Vector3 center)
        {
            DrawSubField(center, FieldRadius_Active());
        }

        public void DrawSubField(Vector3 position, float shieldRadius)
        {
            position = position + (new Vector3(0.5f, 0f, 0.5f));
            var color = new Color(0.4f, 0.4f, 0.4f, 0.5f);
            MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, color);
            Matrix4x4 matrix = default;
            var scalingRadius = shieldRadius * 2f * 1.1601562f;
            Vector3 scaling = new Vector3(scalingRadius, 1f, scalingRadius);
            matrix.SetTRS(position, Quaternion.identity, scaling);
            Graphics.DrawMesh(MeshPool.plane10, matrix, ForceFieldMat, 0, null, 0, MatPropertyBlock);
        }

        #endregion Drawing

        #region UI

        public override string CompInspectStringExtra()
        {
            StringBuilder _StringBuilder = new StringBuilder();
            //return base.CompInspectStringExtra();
            _StringBuilder.Append(base.CompInspectStringExtra());

            if (IsActive())
            {
                _StringBuilder.AppendLine("Shield: " + FieldIntegrity_Current + "/" + m_FieldIntegrity_Max);
            }
            else if (CurrentStatus == EnumShieldStatus.Initilising)
            {
                //stringBuilder.AppendLine("Initiating shield: " + ((warmupTicks * 100) / recoverWarmup) + "%");
                _StringBuilder.AppendLine("Ready in " + Math.Round(GenTicks.TicksToSeconds(m_WarmupTicksRemaining)) + " seconds.");
                //stringBuilder.AppendLine("Ready in " + m_warmupTicksCurrent + " seconds.");
            }
            else
            {
                _StringBuilder.AppendLine("Shield disabled!");
            }

            if (m_Power != null)
            {
                string text = m_Power.CompInspectStringExtra();
                if (!text.NullOrEmpty())
                {
                    _StringBuilder.Append(text);
                }
                else
                {
                    _StringBuilder.Append("Error, No Power Comp Text.");
                }
            }
            else
            {
                _StringBuilder.Append("Error, No Power Comp.");
            }

            return _StringBuilder.ToString();

        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            //return base.CompGetGizmosExtra();

            //Add the stock Gizmoes
            foreach (var g in base.CompGetGizmosExtra())
            {
                yield return g;
            }

            if (m_BlockDirect_Avalable)
            {
                if (BlockDirect_Active())
                {

                    Command_Action act = new Command_Action();
                    //act.action = () => Designator_Deconstruct.DesignateDeconstruct(this);
                    act.action = () => SwitchDirect();
                    act.icon = UI_DIRECT_ON;
                    act.defaultLabel = "Block Direct";
                    act.defaultDesc = "On";
                    act.activateSound = SoundDef.Named("Click");
                    //act.hotKey = KeyBindingDefOf.DesignatorDeconstruct;
                    //act.groupKey = 689736;
                    yield return act;
                }
                else
                {

                    Command_Action act = new Command_Action();
                    //act.action = () => Designator_Deconstruct.DesignateDeconstruct(this);
                    act.action = () => SwitchDirect();
                    act.icon = UI_DIRECT_OFF;
                    act.defaultLabel = "Block Direct";
                    act.defaultDesc = "Off";
                    act.activateSound = SoundDef.Named("Click");
                    //act.hotKey = KeyBindingDefOf.DesignatorDeconstruct;
                    //act.groupKey = 689736;
                    yield return act;
                }
            }

            if (m_BlockIndirect_Avalable)
            {
                if (BlockIndirect_Active())
                {

                    Command_Action act = new Command_Action();
                    //act.action = () => Designator_Deconstruct.DesignateDeconstruct(this);
                    act.action = () => SwitchIndirect();
                    act.icon = UI_INDIRECT_ON;
                    act.defaultLabel = "Block Indirect";
                    act.defaultDesc = "On";
                    act.activateSound = SoundDef.Named("Click");
                    //act.hotKey = KeyBindingDefOf.DesignatorDeconstruct;
                    //act.groupKey = 689736;
                    yield return act;
                }
                else
                {

                    Command_Action act = new Command_Action();
                    //act.action = () => Designator_Deconstruct.DesignateDeconstruct(this);
                    act.action = () => SwitchIndirect();
                    act.icon = UI_INDIRECT_OFF;
                    act.defaultLabel = "Block Indirect";
                    act.defaultDesc = "Off";
                    act.activateSound = SoundDef.Named("Click");
                    //act.hotKey = KeyBindingDefOf.DesignatorDeconstruct;
                    //act.groupKey = 689736;
                    yield return act;
                }
            }

            if (m_InterceptDropPod_Avalable)
            {
                if (IntercepDropPod_Active())
                {

                    Command_Action act = new Command_Action();
                    //act.action = () => Designator_Deconstruct.DesignateDeconstruct(this);
                    act.action = () => SwitchInterceptDropPod();
                    act.icon = UI_INTERCEPT_DROPPOD_ON;
                    act.defaultLabel = "Intercept DropPod";
                    act.defaultDesc = "On";
                    act.activateSound = SoundDef.Named("Click");
                    //act.hotKey = KeyBindingDefOf.DesignatorDeconstruct;
                    //act.groupKey = 689736;
                    yield return act;
                }
                else
                {

                    Command_Action act = new Command_Action();
                    //act.action = () => Designator_Deconstruct.DesignateDeconstruct(this);
                    act.action = () => SwitchInterceptDropPod();
                    act.icon = UI_INTERCEPT_DROPPOD_OFF;
                    act.defaultLabel = "Intercept DropPod";
                    act.defaultDesc = "Off";
                    act.activateSound = SoundDef.Named("Click");
                    //act.hotKey = KeyBindingDefOf.DesignatorDeconstruct;
                    //act.groupKey = 689736;
                    yield return act;
                }
            }


            if (true)
            {
                if (m_ShowVisually_Active)
                {

                    Command_Action act = new Command_Action();
                    //act.action = () => Designator_Deconstruct.DesignateDeconstruct(this);
                    act.action = () => SwitchVisual();
                    act.icon = UI_SHOW_ON;
                    act.defaultLabel = "Show Visually";
                    act.defaultDesc = "Show";
                    act.activateSound = SoundDef.Named("Click");
                    //act.hotKey = KeyBindingDefOf.DesignatorDeconstruct;
                    //act.groupKey = 689736;
                    yield return act;
                }
                else
                {

                    Command_Action act = new Command_Action();
                    //act.action = () => Designator_Deconstruct.DesignateDeconstruct(this);
                    act.action = () => SwitchVisual();
                    act.icon = UI_SHOW_OFF;
                    act.defaultLabel = "Show Visually";
                    act.defaultDesc = "Hide";
                    act.activateSound = SoundDef.Named("Click");
                    //act.hotKey = KeyBindingDefOf.DesignatorDeconstruct;
                    //act.groupKey = 689736;
                    yield return act;
                }
            }

            if (true)
            {
                Command_Action act = new Command_Action();
                //act.action = () => Designator_Deconstruct.DesignateDeconstruct(this);
                act.action = () => ApplyUpgrades();
                act.icon = UI_LAUNCH_REPORT;
                act.defaultLabel = "Apply Upgrades";
                act.defaultDesc = "Apply Upgrades";
                act.activateSound = SoundDef.Named("Click");
                //act.hotKey = KeyBindingDefOf.DesignatorDeconstruct;
                //act.groupKey = 689736;
                yield return act;
            }

            yield return new Command_Toggle()
            {
                defaultLabel = "Reflect Projectiles",
                defaultDesc = "Reflect blocked projectiles back at shooter",
                icon = ContentFinder<Texture2D>.Get("UI/Reflect"),
                isActive = () => reflectProjectiles,
                toggleAction = delegate
                {
                    reflectProjectiles = !reflectProjectiles;
                }
            };

        } //CompGetGizmosExtra()

        public void ApplyUpgrades()
        {
            var _PotentialUpgradeBuildings = parent
                                                .Map
                                                .listerBuildings
                                                .allBuildingsColonist
                                                //Add adjacent including diagonally.
                                                .Where(x => x.Position.InHorDistOf(parent.Position, 1.6f))
                                                .Where(x => x.TryGetComp<Comp_ShieldUpgrade>() != null);



            var _BuildingToAdd = _PotentialUpgradeBuildings.FirstOrDefault(x => IsAvalableUpgrade(x));
            if (_BuildingToAdd != null)
            {
                m_AppliedUpgrades.Add(_BuildingToAdd);
                _BuildingToAdd.DeSpawn();
                Messages.Message("Applying Shield Upgrade: " + _BuildingToAdd.def.label, parent, MessageTypeDefOf.PositiveEvent);
            }
            else
            {

                var _InvalidBuildings = _PotentialUpgradeBuildings.Where(x => !IsAvalableUpgrade(x, true));
                if (_InvalidBuildings.Any())
                {
                    Messages.Message("No Valid Shield Upgrades Found.", parent, MessageTypeDefOf.RejectInput);
                }
                else
                {
                    Messages.Message("No Shield Upgrades Found.", parent, MessageTypeDefOf.RejectInput);
                }
            }
        }

        private bool IsAvalableUpgrade(Building buildingToCheck, bool ResultMessages = false)
        {
            Comp_ShieldUpgrade _Comp = buildingToCheck.TryGetComp<Comp_ShieldUpgrade>();

            if (_Comp == null)
            {
                if (ResultMessages)
                {
                    Messages.Message("Upgrade Comp Not Found, How did you even get here?.",
                        buildingToCheck,
                        MessageTypeDefOf.RejectInput);
                }
                return false;
            }

            if (m_IdentifyFriendFoe_Avalable && _Comp.Properties.IdentifyFriendFoe)
            {
                if (ResultMessages)
                {
                    Messages.Message("Upgrade Contains IFF while shield already has it.",
                        buildingToCheck,
                        MessageTypeDefOf.RejectInput);
                }
                return false;
            }

            if (SlowDischarge_Active && _Comp.Properties.SlowDischarge)
            {

                if (ResultMessages)
                {
                    Messages.Message("Upgrade for slow discharge while shield already has it.",
                        buildingToCheck,
                        MessageTypeDefOf.RejectInput);
                }
                return false;
            }

            if (m_InterceptDropPod_Avalable && _Comp.Properties.DropPodIntercept)
            {

                if (ResultMessages)
                {
                    Messages.Message("Upgrade for drop pod intercept while shield already has it.",
                        buildingToCheck,
                        MessageTypeDefOf.RejectInput);
                }
                return false;
            }

            return true;
        }

        public void SwitchDirect()
        {
            m_BlockDirect_Requested = !m_BlockDirect_Requested;
        }

        public void SwitchIndirect()
        {
            m_BlockIndirect_Requested = !m_BlockIndirect_Requested;
        }

        public void SwitchInterceptDropPod()
        {
            m_InterceptDropPod_Requested = !m_InterceptDropPod_Requested;
        }

        private void SwitchVisual()
        {
            m_ShowVisually_Active = !m_ShowVisually_Active;
        }

        #endregion UI

        #region DataAcess

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref m_FieldRadius_Requested, "m_FieldRadius_Requested");
            Scribe_Values.Look(ref m_BlockDirect_Requested, "m_BlockDirect_Requested");
            Scribe_Values.Look(ref m_BlockIndirect_Requested, "m_BlockIndirect_Requested");
            Scribe_Values.Look(ref m_InterceptDropPod_Requested, "m_InterceptDropPod_Requested");
            Scribe_Values.Look(ref m_IdentifyFriendFoe_Requested, "m_IdentifyFriendFoe_Requested");

            Scribe_Values.Look(ref m_RechargeTickDelayInterval, "m_shieldRechargeTickDelay");
            Scribe_Values.Look(ref m_RecoverWarmupDelayTicks, "m_shieldRecoverWarmup");

            Scribe_Values.Look(ref m_ShowVisually_Active, "m_ShowVisually_Active", true);
            Scribe_Values.Look(ref m_ColourRed, "m_colourRed");
            Scribe_Values.Look(ref m_ColourGreen, "m_colourGreen");
            Scribe_Values.Look(ref m_ColourBlue, "m_colourBlue");

            Scribe_Values.Look(ref m_WarmupTicksRemaining, "m_WarmupTicksRemaining");

            Scribe_Values.Look(ref m_CurrentStatus, "m_CurrentStatus");
            Scribe_Values.Look(ref m_FieldIntegrity_Current, "m_FieldIntegrity_Current");
            Scribe_Values.Look(ref reflectProjectiles, "reflectProjectiles");

            Scribe_Collections.Look<Building>(ref m_AppliedUpgrades, "m_AppliedUpgrades", LookMode.Deep);

        }

        #endregion DataAcess

    }
}
