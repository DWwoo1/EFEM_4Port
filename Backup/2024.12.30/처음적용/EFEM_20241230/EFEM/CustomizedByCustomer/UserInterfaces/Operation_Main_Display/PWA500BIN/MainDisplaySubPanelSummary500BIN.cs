﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using EFEM.Modules;
using FrameOfSystem3.Views;
using FrameOfSystem3.Views.Operation.SubPanelSummary.LoadPortSummary;

namespace EFEM.CustomizedByCustomer.UserInterface.OperationMainSummary.PWA500BIN
{
    public partial class MainDisplaySubPanelSummary500BIN : ParameterPanel
    {
        #region <Constructors>
        public MainDisplaySubPanelSummary500BIN(string name)
        {
            InitializeComponent();

            this.Tag = name;

            _loadPortManager = LoadPortManager.Instance;

            LoadPortPanels = new Dictionary<int, Panel>
            {
                { pnLoadPort1.TabIndex, pnLoadPort1 },
                { pnLoadPort2.TabIndex, pnLoadPort2 },
                { pnLoadPort3.TabIndex, pnLoadPort3 },
                { pnLoadPort4.TabIndex, pnLoadPort4 },
                { pnLoadPort5.TabIndex, pnLoadPort5 },
                { pnLoadPort6.TabIndex, pnLoadPort6 }
            };

            LoadPortSlots = new Dictionary<int, SummaryLoadPortState>();
            foreach (var item in LoadPortPanels)
            {
                LoadPortSlots.Add(item.Key, new SummaryLoadPortState(item.Key));
                LoadPortSlots[item.Key].Dock = DockStyle.Fill;

                item.Value.Controls.Add(LoadPortSlots[item.Key]);
            }

            RobotInfo = new SummaryRobotInfo(0);
            pnRobotInfo.Controls.Add(RobotInfo);

            ProcessModuleInfo = new SummaryProcessModuleInfo500BIN(0);
            pnProcessModuleInfo.Controls.Add(ProcessModuleInfo);

            QuickButtons = new SummaryQuickButtons();
            pnSummaryQuickButton.Controls.Add(QuickButtons);

            this.Dock = DockStyle.Fill;
        }
        #endregion </Constructors>

        #region <Fields>
        private static LoadPortManager _loadPortManager = null;

        private readonly SummaryRobotInfo RobotInfo = null;
        private readonly SummaryProcessModuleInfo500BIN ProcessModuleInfo = null;
        private readonly SummaryQuickButtons QuickButtons = null;
        private readonly Dictionary<int, Panel> LoadPortPanels = null;
        private readonly Dictionary<int, SummaryLoadPortState> LoadPortSlots = null;
        #endregion </Fields>

        #region <Properties>
        #endregion </Properties>

        #region <Methods>

        #region <Override Methods>
        protected override void ProcessWhenActivation()
        {
            foreach (var item in LoadPortSlots)
            {
                item.Value.ProcessWhenActivation();
            }

            base.ProcessWhenActivation();
        }
        public override void CallFunctionByTimer()
        {
            foreach (var item in LoadPortSlots)
            {
                item.Value.CallFunctionByTimer();
            }

            RobotInfo.RefreshRobotInfo();
            ProcessModuleInfo.RefreshModuleInfo();
            ProcessModuleInfo.RefreshSubstrateList();
            QuickButtons.CallFunctionByTimer();

            base.CallFunctionByTimer();
        }
        protected override void ProcessWhenDeactivation()
        {
            foreach (var item in LoadPortSlots)
            {
                item.Value.ProcessWhenDeactivation();
            }
        }
        #endregion </Override Methods>

        #endregion </Methods>
    }
}