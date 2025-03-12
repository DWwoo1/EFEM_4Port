using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using EquipmentState_;

using FrameOfSystem3.Task;
using Define.DefineEnumProject.Task;
using FrameOfSystem3.Recipe;
using FrameOfSystem3.SECSGEM.Scenario;
using FrameOfSystem3.Views;
using FrameOfSystem3.Views.Functional;
using FrameOfSystem3.Views.MapManager;
using FrameOfSystem3.Views.Operation.SubPanelSummary.LoadPortSummary;

using EFEM.Modules;
using EFEM.Defines.AtmRobot;
using EFEM.MaterialTracking;
using EFEM.Defines.MaterialTracking;
using EFEM.MaterialTracking.LocationServer;
using EFEM.ActionScheduler;
using EFEM.CustomizedByProcessType.PWA500W;

namespace EFEM.CustomizedByProcessType.UserInterface.OperationMainManual.PWA500W
{
    public partial class MainDisplaySubPanelManualOperationEditor500W : UserControlForMainView.CustomView
    {
        #region <Constructors>
        public MainDisplaySubPanelManualOperationEditor500W()
        {
            InitializeComponent();

            //this.Tag = name;

            _equipmentState = EquipmentState.GetInstance();
            _taskOperator = TaskOperator.GetInstance();
            _loadPortManager = LoadPortManager.Instance;
            _carrierServer = CarrierManagementServer.Instance;
            _processGroup = ProcessModuleGroup.Instance;
            _substrateManager = SubstrateManager.Instance;
            _robotManager = AtmRobotManager.Instance;
            _robotSchedulerManager = RobotActionSchedulerManager.Instance;
            _locationServer = LocationServer.Instance;
            _messageBox = Form_MessageBox.GetInstance();
            _recipe = FrameOfSystem3.Recipe.Recipe.GetInstance();
            _scenarioManager = ScenarioManagerForPWA500W_NRD.Instance;

            LoadPortPanels = new Dictionary<int, Panel>
            {
                { pnLoadPort1.TabIndex, pnLoadPort1 },
                { pnLoadPort2.TabIndex, pnLoadPort2 },
                { pnLoadPort3.TabIndex, pnLoadPort3 },
                { pnLoadPort4.TabIndex, pnLoadPort4 }
            };

            LoadPortSlots = new Dictionary<int, SummaryLoadPortState_Slot>();
            foreach (var item in LoadPortPanels)
            {
                var eventHandler = new DelegateCellClicked(LoadPortMapCellClicked);
                LoadPortSlots.Add(item.Key, new SummaryLoadPortState_Slot(item.Key, eventHandler));
                LoadPortSlots[item.Key].Dock = DockStyle.Fill;

                item.Value.Controls.Add(LoadPortSlots[item.Key]);
            }

            _robotStateInformation = new RobotStateInformation();
            RobotArmControls = new ConcurrentDictionary<RobotArmTypes, Sys3Controls.Sys3Label>();
            RobotArmControls.TryAdd(RobotArmTypes.UpperArm, lblUpperArmSubstrateInfo);
            RobotArmControls.TryAdd(RobotArmTypes.LowerArm, lblLowerArmSubstrateInfo);

            _temporarySubstrate = new Substrate("");
            _substratesInArm = new Dictionary<RobotArmTypes, Substrate>();

            _substratesAtProcessModule = new List<Substrate>();
            _core_8_SubstratesAtProcessModule = new List<Substrate>();
            _core_12_SubstratesAtProcessModule = new List<Substrate>();
            _sortSubstratesAtProcessModule = new List<Substrate>();

            _selectedSubstrate = new Substrate("");
            _selectionList = Form_SelectionList.GetInstance();

            LoadPortNames = new ConcurrentDictionary<int, Sys3Controls.Sys3Label>();
            LoadPortNames[0] = lblLoadPort1;
            LoadPortNames[1] = lblLoadPort2;
            LoadPortNames[2] = lblLoadPort3;
            LoadPortNames[3] = lblLoadPort4;

            InitInfo();

            ProcessModuleName = _processGroup.GetProcessModuleName(ProcessModuleIndex);
        }
        #endregion </Constructors>

        #region <Fields>

        #region <Instances>
        private static TaskOperator _taskOperator = null;
        private static LoadPortManager _loadPortManager = null;
        private static AtmRobotManager _robotManager = null;
        private static CarrierManagementServer _carrierServer = null;
        private static ProcessModuleGroup _processGroup = null;
        private static SubstrateManager _substrateManager = null;
        private static EquipmentState _equipmentState;
        private static RobotActionSchedulerManager _robotSchedulerManager = null;
        private static Form_MessageBox _messageBox = null;
        private static LocationServer _locationServer = null;
        private static Form_SelectionList _selectionList = null;
        private static FrameOfSystem3.Recipe.Recipe _recipe = null;
        private static ScenarioManagerForPWA500W_NRD _scenarioManager = null;
        #endregion </Instances>

        #region <Constants>
        private const int RobotIndex = 0;
        private const int ProcessModuleIndex = 0;

        private const string TitleBinLoadPort = "BIN LOADPORT";
        private const string TitleEmptyLoadPort = "EMPTY LOADPORT";
        #endregion </Constants>

        #region <Substrate from LoadPort>
        private readonly Dictionary<int, Panel> LoadPortPanels = null;
        private readonly Dictionary<int, SummaryLoadPortState_Slot> LoadPortSlots = null;
        #endregion </Substrate from LoadPort>

        #region <Substrates in Process Module>
        private const int ColumnSubstrateName = 0;
        private const int ColumnRequestEnabled = 0;
        private const int ColumnRequestLocation = 1;

        private List<Substrate> _substratesAtProcessModule = null;
        private List<Substrate> _core_8_SubstratesAtProcessModule = null;   // 2025.02.13 dwlim [ADD] 500W에 맞게 수정
        private List<Substrate> _core_12_SubstratesAtProcessModule = null;  // 2025.02.13 dwlim [ADD] 500W에 맞게 수정
        private List<Substrate> _sortSubstratesAtProcessModule = null;
        #endregion </Substrates in Process Module>

        #region <Substrates In Robot>
        private RobotStateInformation _robotStateInformation = null;
        private readonly ConcurrentDictionary<RobotArmTypes, Sys3Controls.Sys3Label> RobotArmControls = null;
        private Substrate _temporarySubstrate = null;
        private Dictionary<RobotArmTypes, Substrate> _substratesInArm = null;
        #endregion </Substrates In Robot>

        #region <ETC>
        private readonly ConcurrentDictionary<int, Sys3Controls.Sys3Label> LoadPortNames = null;
        #endregion </ETC>

        private readonly string ProcessModuleName = string.Empty;
        private string _selectedLocationName = string.Empty;
        private Substrate _selectedSubstrate = null;
        #endregion </Fields>

        #region <Properties>
        #endregion </Properties>

        #region <Methods>

        #region <Override Methods>
        protected override void ProcessWhenActivation()
        {
            foreach (var item in LoadPortSlots)
            {
                item.Value.ActivateView();
            }
            
            gvCoreSubstrateList.ClearSelection();
            gvSortSubstrateList.ClearSelection();
            _selectedLocationName = string.Empty;
            _selectedSubstrate = null;

            UpdateSelectedSubstrateInfo();
            DisplayLoadPortNames();
            base.ProcessWhenActivation();
        }
        public override void CallFunctionByTimer()
        {
            foreach (var item in LoadPortSlots)
            {
                item.Value.CallFunctionByTimer();
            }

            //UpdateSelectedSubstrate();
            //DisplayModuleInfo();
            DisplayRobotInfo();
            RefreshSubstrateList();
            EnableEditButtons();

            base.CallFunctionByTimer();
        }
        protected override void ProcessWhenDeactivation()
        {
            foreach (var item in LoadPortSlots)
            {
                item.Value.DeactivateView();
            }

            base.ProcessWhenDeactivation();
        }
        #endregion </Override Methods>

        #region <UI Events>
        private void LoadPortMapCellClicked(int clickedMapIndex, Queue<int> polints)
        {
            UpdateLoadPortInfo(clickedMapIndex, polints.Last());

            DisableHighlight(clickedMapIndex);
            
            UpdateSelectedSubstrateInfo();
            //UpdateSelectedSubstrate();
        }
        private void BtnSelectArmClicked(object sender, EventArgs e)
        {
            RobotArmTypes arm;
            if (sender.Equals(lblUpperArmSubstrateInfo))
            {
                arm = RobotArmTypes.UpperArm;
            }
            else if (sender.Equals(lblLowerArmSubstrateInfo))
            {
                arm = RobotArmTypes.LowerArm;
            }
            else
                return;

            _robotStateInformation = _robotManager.GetStateInformation(RobotIndex);
            if (_robotStateInformation == null)
                return;

            string robotName = _robotManager.GetRobotName(RobotIndex);
            _robotManager.GetSubstrate(robotName, arm, ref _selectedSubstrate);

            RobotLocation location = new RobotLocation(RobotArmTypes.All, "");
            if (_locationServer.GetRobotLocation(robotName, arm, ref location))
            {
                _selectedLocationName = location.Name;
                UpdateSelectedSubstrateInfo();
            }
        }
        private void GvEditProcessModuleClicked(object sender, EventArgs e)
        {
            if (!(sender is Sys3Controls.Sys3DoubleBufferedDataGridView gv))
                return;

            string[] locations = _processGroup.GetProcessModuleLocations(ProcessModuleIndex);
            
            if (sender.Equals(gvCoreSubstrateList))
            {
                for(int i = 0; i < locations.Length; ++i)
                {
                    if (locations[i].Contains("Core"))
                    {
                        _selectedLocationName = locations[i];
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < locations.Length; ++i)
                {
                    if (locations[i].Contains("Sort"))
                    {
                        _selectedLocationName = locations[i];
                        break;
                    }
                }
            }
            _selectedSubstrate = null;
            UpdateSelectedSubstrateInfo();
        }

        private void GvEditProcessModuleCellClicked(object sender, DataGridViewCellEventArgs e)
        {
            if (!(sender is Sys3Controls.Sys3DoubleBufferedDataGridView gv))
                return;

            if (e.ColumnIndex < 0 || e.RowIndex < 0)
                return;
           
            if (sender.Equals(gvCoreSubstrateList))
            {
                gvSortSubstrateList.ClearSelection();
            }
            else
            {
                gvCoreSubstrateList.ClearSelection();
            }

            string selectedName = gv[ColumnSubstrateName, e.RowIndex].Value.ToString();

            if (false == _substrateManager.GetSubstrateAtProcessModule(ProcessModuleName, selectedName, ref _selectedSubstrate))
                return;
                
            _selectedLocationName = _selectedSubstrate.GetLocation().Name;
            UpdateSelectedSubstrateInfo();
        }

        private void MainControlClicked(object sender, EventArgs e)
        {
            _selectedLocationName = string.Empty;
            _selectedSubstrate = null;

            UpdateSelectedSubstrateInfo();
        }

        private void BtnEditClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedLocationName))
                return;

            if (sender.Equals(btnCreate))
            {
                if (false == _messageBox.ShowMessage("정말로 자재정보를 생성할까요?"))
                    return;

                Location location = new Location(_selectedLocationName);
                if (_locationServer.GetLocationByName(_selectedLocationName, ref location))
                {
                    Substrate temporarySubstrate = new Substrate("");
                    temporarySubstrate.SetLocation(location);
                    
                    if (location is LoadPortLocation)
                    {
                        var loc = location as LoadPortLocation;
                        temporarySubstrate.SetSourcePortId(loc.PortId);
                        temporarySubstrate.SetDestinationPortId(loc.PortId);
                        temporarySubstrate.SetSourceSlot(loc.Slot);
                        temporarySubstrate.SetDestinationSlot(loc.Slot);
                    }

                    FormMaterialEdit materialEdit = new FormMaterialEdit();
                    Dictionary<string, string> targetAttributes = temporarySubstrate.GetAttributesAll();
                    if (materialEdit.CreateEditForm(targetAttributes))
                    {
                        Dictionary<string, string> attributeResults = new Dictionary<string, string>();
                        materialEdit.GetResult(ref attributeResults);

                        bool isInValidNewName = (false == attributeResults.TryGetValue("Name", out string resultName) || false == _substrateManager.IsValidSubstrateName(resultName));
                        if (isInValidNewName)
                        {
                            _messageBox.ShowMessage(string.Format("이름에 사용할 수 없는 문자열이 포함되었습니다. : {0}", resultName));
                        }
                        else
                        {
                            temporarySubstrate.SetAttributesAll(attributeResults);

                            if (location is LoadPortLocation)
                            {
                                var loc = location as LoadPortLocation;
                                _substrateManager.AssignSubstrateInLoadPort(loc.PortId, loc.Slot, temporarySubstrate);
                            }
                            else if (location is ProcessModuleLocation)
                            {
                                var loc = location as ProcessModuleLocation;
                                _substrateManager.AssignSubstrateInProcessModule(loc.ProcessModuleName, temporarySubstrate);
                                _processGroup.AssignSubstrate(loc.ProcessModuleName, temporarySubstrate);
                            }
                            else if (location is RobotLocation)
                            {
                                var loc = location as RobotLocation;
                                _substrateManager.AssignSubstrateInRobot(loc.RobotName, loc.Arm, temporarySubstrate);
                                //_robotManager.AssignSubstrate(RobotIndex, loc.Arm, temporarySubstrate);
                            }

                            _selectedSubstrate = null;
                            _selectedLocationName = string.Empty;
                        }
                    }

                    materialEdit.DisposeControls();
                    materialEdit = null;
                }
            }
            else if (sender.Equals(btnEdit))
            {
                //string currentLocation = _selectedSubstrate.GetLocation();
                Dictionary<string, string> targetAttributes = _selectedSubstrate.GetAttributesAll();
                FormMaterialEdit materialEdit = new FormMaterialEdit();
                if (materialEdit.CreateEditForm(targetAttributes))
                {
                    if (_messageBox.ShowMessage("정말로 자재정보를 변경할까요?"))
                    {
                        Dictionary<string, string> attributeResults = new Dictionary<string, string>();
                        materialEdit.GetResult(ref attributeResults);

                        bool isInValidNewName = (false == attributeResults.TryGetValue("Name", out string resultName) || false == _substrateManager.IsValidSubstrateName(resultName));
                        if (isInValidNewName)
                        {
                            _messageBox.ShowMessage(string.Format("이름에 사용할 수 없는 문자열이 포함되었습니다. : {0}", resultName));

                        }
                        else
                        {
                            _selectedSubstrate.SetAttributesAll(attributeResults);
                        }
                    }
                }

                materialEdit.DisposeControls();
                materialEdit = null;
            }
            else if (sender.Equals(btnDisable))
            {
                Location location = new Location(_selectedLocationName);
                if (_locationServer.GetLocationByName(_selectedLocationName, ref location))
                {
                    //if (!(location is RobotLocation targetLocation))
                    //    return;

                    Substrate substrate = new Substrate("");
                    if (false == _substrateManager.GetSubstrate(location, string.Empty, ref substrate))
                        return;

                    substrate.SetProcessingStatus(ProcessingStates.Skipped);
                }
            }
            else if (sender.Equals(btnChangePortInfo))
            {
                Location location = new Location(_selectedLocationName);
                if (_locationServer.GetLocationByName(_selectedLocationName, ref location))
                {
                    //if (!(location is RobotLocation targetLocation))
                    //    return;

                    // 1. 목표 자재를 가져온다.
                    Substrate substrate = new Substrate("");
                    if (false == _substrateManager.GetSubstrate(location, string.Empty, ref substrate))
                        return;

                    // 2. UI 선택을 위한 로드포트 이름을 가져온다.
                    Dictionary<int, string> loadPortNames = new Dictionary<int, string>();
                    foreach (var item in LoadPortNames)
                    {
                        int portId = _loadPortManager.GetLoadPortPortId(item.Key);
                        if (false == _carrierServer.HasCarrier(portId))
                            continue;

                        loadPortNames[item.Key] = item.Value.Text;
                    }

                    // 3. 로드포트 선택창을 띄운다.
                    if (_selectionList.CreateForm("Select LoadPort", loadPortNames.Values.ToArray(), loadPortNames.Keys.ToArray(), 0))
                    {
                        int selectedLoadPort = 0;
                        _selectionList.GetResult(ref selectedLoadPort);
                        int portId = _loadPortManager.GetLoadPortPortId(selectedLoadPort);
                        if (portId <= 0)
                            return;

                        int capacity = _carrierServer.GetCapacity(portId);
                        Dictionary<int, string> slotNames = new Dictionary<int, string>();

                        // 4. Substrate 목록을 가져와 이름을 갱신한다.
                        var substrates = _substrateManager.GetSubstratesAtLoadPort(portId);
                        for(int i = 0; i < capacity; ++i)
                        {
                            if (substrates.ContainsKey(i))
                                continue;

                            slotNames[i] = string.Format("Slot {0}", i);
                        }

                        // 5. 슬롯 번호를 띄운다.
                        if (_selectionList.CreateForm("Select Slot", slotNames.Values.ToArray(), slotNames.Keys.ToArray(), 0))
                        {
                            int selectedSlot = 0;
                            _selectionList.GetResult(ref selectedSlot);
                            if (selectedSlot < 0)
                                return;

                            if (false == _messageBox.ShowWarningMessage(string.Format("현재 선택된 정보가 맞습니까? [Port : {0}, Slot : {1}]\r\n※주의 : 자재 포트 정보가 변경 됩니다.", loadPortNames[selectedLoadPort], slotNames[selectedSlot])))
                                return;

                            // 6. 선택한 슬롯으로 설정한다.
                            substrate.SetSourcePortId(portId);
                            substrate.SetSourceSlot(selectedSlot);
                            substrate.SetDestinationPortId(portId);
                            substrate.SetDestinationSlot(selectedSlot);
                            LoadPortLocation targetLocation = new LoadPortLocation(portId, selectedSlot, string.Empty);
                            _locationServer.GetLoadPortSlotLocation(portId, selectedSlot, ref targetLocation);
                            _substrateManager.MoveMaterialToModule(targetLocation, substrate);
                        }
                    }
                }
            }
            else if (sender.Equals(btnReplaceFromPM) || sender.Equals(btnReplaceFromLP))
            {
                Location location = new Location(_selectedLocationName);
                if (_locationServer.GetLocationByName(_selectedLocationName, ref location))
                {
                    // 1. 원본 Substrate 백업(제거를 위함)
                    if (!(location is RobotLocation targetLocation))
                        return;

                    Substrate substrateOriginal = new Substrate("");
                    string robotName = _robotManager.GetRobotName(RobotIndex);
                    if (false == _robotManager.GetSubstrate(robotName, targetLocation.Arm, ref substrateOriginal))
                        return;
                    string originalName = substrateOriginal.GetName();

                    // 2. 공정설비로부터 Substrate List를 가져옴
                    Dictionary<int, string> substrateNames = new Dictionary<int, string>();

                    if (sender.Equals(btnReplaceFromPM))
                    {
                        List<Substrate> substrates = new List<Substrate>();
                        if (false == _substrateManager.GetSubstratesAtProcessModule(ProcessModuleName, ref substrates))
                            return;

                        int index = 0;
                        for (int i = 0; i < substrates.Count; ++i)
                        {
                            if (substrates[i].GetName().Equals(originalName))
                                continue;

                            substrateNames.Add(index, substrates[i].GetName());
                            index++;
                        }
                    }
                    else
                    {
                        Dictionary<int, string> loadPortNames = new Dictionary<int, string>();
                        foreach (var item in LoadPortNames)
                        {
                            int portId = _loadPortManager.GetLoadPortPortId(item.Key);
                            if (false == _carrierServer.HasCarrier(portId))
                                continue;

                            loadPortNames[item.Key] = item.Value.Text;
                        }

                        // 2-1. 로드포트 선택
                        if (_selectionList.CreateForm("Select LoadPort", loadPortNames.Values.ToArray(), loadPortNames.Keys.ToArray(), 0))
                        {
                            int selectedLoadPort = 0;
                            _selectionList.GetResult(ref selectedLoadPort);
                            int portId = _loadPortManager.GetLoadPortPortId(selectedLoadPort);
                            if (portId <= 0)
                                return;

                            // 2-2. Substrate 목록을 가져와 이름을 갱신한다.
                            var substrates = _substrateManager.GetSubstratesAtLoadPort(portId);
                            foreach (var item in substrates)
                            {
                                substrateNames[item.Key] = item.Value.GetName();
                            }
                        }
                    }

                    // 3. 자재를 선택(UI)
                    if (_selectionList.CreateForm("Select Substrate", substrateNames.Values.ToArray(), substrateNames.Keys.ToArray(), 0))
                    {
                        string selectedName = string.Empty;
                        _selectionList.GetResult(ref selectedName);

                        if (string.IsNullOrEmpty(selectedName))
                            return;

                        if (false == _messageBox.ShowWarningMessage(string.Format("현재 선택된 정보가 맞습니까? [{0} -> {1}]\r\n※주의 : 자재 정보가 변경됩니다.", originalName, selectedName)))
                            return;
                        
                        // 4. 타겟 자재를 가져옴
                        Substrate selectedSubstrate = new Substrate("");
                        if (false == _substrateManager.GetSubstrateByName(selectedName, ref selectedSubstrate))
                            return;

                        // 5. 원본 Substrate 제거
                        _substrateManager.RemoveSubstrate(substrateOriginal.GetName(), location);

                        // 6. 타겟 자재가 존재하는 위치에서 제거
                        _substrateManager.MoveMaterialToRobot(selectedSubstrate.GetLocation(), robotName, targetLocation.Arm, selectedSubstrate);

                        // 7. 원본 위치에 Set
                        _substrateManager.SaveRecoveryDataAll();
                    }
                    
                }

            }
            else if (sender.Equals(btnRemove))
            {
                if (false == _messageBox.ShowMessage("정말로 자재정보를 제거할까요?"))
                    return;

                string targetSubstrateName = _selectedSubstrate.GetName();
                Location location = _selectedSubstrate.GetLocation();
                if (location is LoadPortLocation)
                {
                    var lpLoc = location as LoadPortLocation;
                    _substrateManager.RemoveSubstrate(targetSubstrateName, lpLoc);
                }
                else if (location is ProcessModuleLocation)
                {
                    _processGroup.RemoveSubstrate(targetSubstrateName);
                }
                else if (location is RobotLocation)
                {
                    var robotLoc = location as RobotLocation;

                    _robotManager.RemoveSubstrate(RobotIndex, robotLoc.Arm);
                    RobotArmControls[robotLoc.Arm].Text = string.Empty;
                }

                if (_selectedLocationName == "ProcessModule")
                {
                    _processGroup.RemoveSubstrate(targetSubstrateName);
                }
                
                // LP에도 남아있는 경우
                var transferStatus = _selectedSubstrate.GetTransferStatus();
                switch (transferStatus)
                {
                    case SubstrateTransferStates.AtSource:
                    case SubstrateTransferStates.AtWork:
                        {
                            int portId = _selectedSubstrate.GetSourcePortId();
                            int slot = _selectedSubstrate.GetSourceSlot();

                            LoadPortLocation lpLocation = new LoadPortLocation(portId, slot, "");
                            if (_locationServer.GetLoadPortSlotLocation(portId, slot, ref lpLocation))
                            {
                                string name = _substrateManager.GetSubstrateNameAtLoadPort(portId, slot);
                                _substrateManager.RemoveSubstrate(name, lpLocation);
                            }
                        }
                        break;
                    case SubstrateTransferStates.AtDestination:
                        {
                            int portId = _selectedSubstrate.GetDestinationPortId();
                            int slot = _selectedSubstrate.GetDestinationSlot();
                            LoadPortLocation lpLocation = new LoadPortLocation(portId, slot, "");
                            if (_locationServer.GetLoadPortSlotLocation(portId, slot, ref lpLocation))
                            {
                                string name = _substrateManager.GetSubstrateNameAtLoadPort(portId, slot);
                                _substrateManager.RemoveSubstrate(name, lpLocation);
                            }
                        }
                        break;
                    default:
                        break;
                }

                _substrateManager.RemoveSubstrate(targetSubstrateName, location);
                _selectedSubstrate = null;
            }

            UpdateSelectedSubstrateInfo();
        }
        #endregion </UI Events>

        #region <Display>
        private void DisplayLoadPortNames()
        {
            foreach (var item in LoadPortNames)
            {
                // 2024.09.03. jhlim [MOD] SubType을 UI에는 Center/Left/Right로 지정되도록 변경
                //var recipe = PARAM_EQUIPMENT.LoadPortType1 + item.Key;
                //string name = _recipe.GetValue(EN_RECIPE_TYPE.EQUIPMENT, recipe.ToString(), SubstrateType.Core.ToString());
                //item.Value.Text = name;
                item.Value.Text = _scenarioManager.GetSubstrateTypeForUILoadPortIndex(item.Key);
                // 2024.09.03. jhlim [END]
            }
        }
        private void DisableHighlight(int index)
        {
            foreach (var item in LoadPortSlots)
            {
                if (index >= 0)
                {
                    if (item.Key.Equals(index))
                        continue;
                }

                item.Value.DisableHighlight();
            }
        }
        private void DisplayRobotInfo()
        {
            _robotStateInformation = _robotManager.GetStateInformation(RobotIndex);
            if (_robotStateInformation == null)
                return;
            
            string robotName = _robotManager.GetRobotName(RobotIndex);
            foreach (var item in RobotArmControls)
            {
                if (_robotManager.GetSubstrate(robotName, item.Key, ref _temporarySubstrate))
                {
                    if (false == RobotArmControls[item.Key].Text.Equals(_temporarySubstrate.GetName()))
                    {
                        RobotArmControls[item.Key].Text = _temporarySubstrate.GetName();
                    }
                }
                else
                {
                    RobotArmControls[item.Key].Text = string.Empty;
                }
            }
        }
        private void UpdateSortGridView()
        {
            bool isCleared = false;
            if (gvSortSubstrateList.Rows.Count != _sortSubstratesAtProcessModule.Count)
            {
                gvSortSubstrateList.Rows.Clear();
                isCleared = true;
            }

            for (int i = 0; i < _sortSubstratesAtProcessModule.Count; ++i)
            {
                if (isCleared || gvSortSubstrateList[ColumnSubstrateName, i].Value.ToString() != _sortSubstratesAtProcessModule[i].GetName())
                {
                    if (isCleared)
                    {
                        gvSortSubstrateList.Rows.Add();
                    }

                    gvSortSubstrateList[ColumnSubstrateName, i].Value = _sortSubstratesAtProcessModule[i].GetName();
                }
            }
        }
        private void UpdateCoreGridView()
        {
            int coreSubstratesAtProcessModuleCount = _core_8_SubstratesAtProcessModule.Count + _core_12_SubstratesAtProcessModule.Count;
            int core_8_IndexCount = _core_8_SubstratesAtProcessModule.Count;
            int gvIndexCount = gvCoreSubstrateList.Rows.Count;
            bool isCleared = false;

            if (gvIndexCount != coreSubstratesAtProcessModuleCount)
            {
                gvCoreSubstrateList.Rows.Clear();
                isCleared = true;
                gvIndexCount = 0;
            }

            for (int i = 0; i < _core_8_SubstratesAtProcessModule.Count; ++i)
            {
                if (isCleared || gvCoreSubstrateList[ColumnSubstrateName, i].Value.ToString() != _core_8_SubstratesAtProcessModule[i].GetName())
                {
                    if (isCleared)
                    {
                        gvCoreSubstrateList.Rows.Add();
                        gvIndexCount++;
                    }

                    gvCoreSubstrateList[ColumnSubstrateName, i].Value = _core_8_SubstratesAtProcessModule[i].GetName();
                }
            }
            for (int i = 0; i < _core_12_SubstratesAtProcessModule.Count; ++i)
            {
                if (isCleared || gvCoreSubstrateList[ColumnSubstrateName, i + core_8_IndexCount].Value.ToString() != _core_12_SubstratesAtProcessModule[i].GetName())
                {
                    if (isCleared)
                    {
                        gvCoreSubstrateList.Rows.Add();
                        gvIndexCount++;
                    }

                    gvCoreSubstrateList[ColumnSubstrateName, i + core_8_IndexCount].Value = _core_12_SubstratesAtProcessModule[i].GetName();
                }
            }
        }
        public void RefreshSubstrateList()
        {
            if (_substrateManager.GetSubstratesAtProcessModule(ProcessModuleName, ref _substratesAtProcessModule))
            //if (_processGroup.GetSubstrates(ProcessModuleIndex, ref _temporaryList))
            {
                _core_8_SubstratesAtProcessModule.Clear();
                _core_12_SubstratesAtProcessModule.Clear();
                _sortSubstratesAtProcessModule.Clear();
                for (int i = 0; i < _substratesAtProcessModule.Count; ++i)
                {
                    string subType = _substratesAtProcessModule[i].GetAttribute(PWA500WSubstrateAttributes.SubstrateType);
                    if (false == Enum.TryParse(subType, out SubstrateType convertedSubType))
                        continue;

                    switch (convertedSubType)
                    {
                        case SubstrateType.Core_8:
                            _core_8_SubstratesAtProcessModule.Add(_substratesAtProcessModule[i]);
                            break;
                        case SubstrateType.Core_12:
                            _core_12_SubstratesAtProcessModule.Add(_substratesAtProcessModule[i]);
                            break;
                        case SubstrateType.Sort_12:
                            _sortSubstratesAtProcessModule.Add(_substratesAtProcessModule[i]);
                            break;

                        default:
                            break;
                    }
                }

                UpdateCoreGridView();
                UpdateSortGridView();
            }
        }
        private void UpdateSelectedSubstrateInfo()
        {
            lblSelectedSubstrate.SubText = _selectedSubstrate == null ? _selectedLocationName : _selectedSubstrate.GetLocation().Name;
            lblSelectedSubstrate.Text = _selectedSubstrate == null ? string.Empty : _selectedSubstrate.GetName();
        }
        private void EnableEditButtons()
        {
            bool enabled = IsEquipmentIdleOrPause();
            if (false == enabled || string.IsNullOrEmpty(_selectedLocationName))
            {
                btnCreate.Enabled = false;
                btnEdit.Enabled = false;
                btnChangePortInfo.Enabled = false;
                btnDisable.Enabled = false;
                btnReplaceFromPM.Enabled = false;
                btnReplaceFromLP.Enabled = false;
                btnRemove.Enabled = false;
            }
            else
            {
                bool noSubstrate = _selectedSubstrate == null;
                btnCreate.Enabled = noSubstrate;
                btnEdit.Enabled = false == noSubstrate;
                btnChangePortInfo.Enabled = false == noSubstrate;
                btnDisable.Enabled = false == noSubstrate;
                btnReplaceFromPM.Enabled = false == noSubstrate;
                btnReplaceFromLP.Enabled = false == noSubstrate;
                btnRemove.Enabled = false == noSubstrate;
            }
        }
        #endregion </Display>

        #region <ETC>
        private void InitInfo()
        {
        }
        private bool IsEquipmentIdleOrPause()
        {
            return _equipmentState.GetState().Equals(EQUIPMENT_STATE.IDLE) || _equipmentState.GetState().Equals(EQUIPMENT_STATE.PAUSE);
        }
        private void UpdateLoadPortInfo(int lpIndex, int slot)
        {
            int portId = _loadPortManager.GetLoadPortPortId(lpIndex);
            if (false == _carrierServer.HasCarrier(portId))
                return;

            LoadPortLocation location = new LoadPortLocation(-1, -1, "");
            if (_locationServer.GetLoadPortSlotLocation(portId, slot, ref location))
            {
                _selectedLocationName = location.Name;
                _substrateManager.GetSubstrate(location, "", ref _selectedSubstrate);
            }           
        }
        #endregion </ETC>

        #endregion </Methods>
    }
}