using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

using EquipmentState_;

using FrameOfSystem3.Recipe;
using FrameOfSystem3.Component;
using FrameOfSystem3.Functional;
using FrameOfSystem3.SECSGEM;
using FrameOfSystem3.SECSGEM.DefineSecsGem;
using Define.DefineEnumProject;
using FrameOfSystem3.Views.Functional;
using FrameOfSystem3.SECSGEM.Scenario;

using EFEM.Modules;
using EFEM.MaterialTracking;
using EFEM.CustomizedByProcessType.PWA500BIN;

namespace EFEM.CustomizedByProcessType.UserInterface.OperationSecsGem.PWA500BIN
{
    public partial class SubViewOperationSecsGemCustomized : UserControlForMainView.CustomView
    {
        #region <Constructors>
        public SubViewOperationSecsGemCustomized()
        {
            InitializeComponent();

            _selectionList = Form_SelectionList.GetInstance();
            _messageBox = Form_MessageBox.GetInstance();
            _postOffice = PostOffice.GetInstance();
            _keyboard = Form_Keyboard.GetInstance();
            _recipe = FrameOfSystem3.Recipe.Recipe.GetInstance();
            _equipmentState = EquipmentState.GetInstance();
            _scenarioOperator = ScenarioOperator.Instance;
            _loadPortManager = LoadPortManager.Instance;
            _substrateManager = SubstrateManager.Instance;
            _carrierServer = CarrierManagementServer.Instance;
            _scenarioManager = ScenarioManagerForPWA500BIN_TP.Instance;

            DataToSend = new Dictionary<string, string>();
            CarrierScenario = new Dictionary<string, ScenarioListTypes>();
            SubstrateScenario = new Dictionary<string, ScenarioListTypes>();
            ETCScenario = new Dictionary<string, ScenarioListTypes>();
            RecipeHandlingScenario = new Dictionary<string, ScenarioListTypes>();

            CoreCarriers = new Dictionary<int, string>();
            BinCarriers = new Dictionary<int, string>();
            EmptyCarriers = new Dictionary<int, string>();

            CoreSubstrates = new Dictionary<int, Substrate>();
            BinSubstrates = new Dictionary<int, Substrate>();

            _substrates = new List<Substrate>();

            var types = Enum.GetValues(typeof(ScenarioListTypes));
            foreach (var item in types)
            {
                ClassifyScenarioTypesByEnum((ScenarioListTypes)item);
            }

            _selectedScenario = ScenarioListTypes.SCENARIO_REQ_LOT_INFO_CORE_1.ToString();
            _selectedPortId = 1;
        }
        #endregion </Constructors>

        #region <Fields>
        private static Form_MessageBox _messageBox = null;
        private static Form_SelectionList _selectionList = null;
        private static Form_Keyboard _keyboard = null;
        private static PostOffice _postOffice = null;
        private static FrameOfSystem3.Recipe.Recipe _recipe = null;
        private static ScenarioOperator _scenarioOperator = null;
        private static LoadPortManager _loadPortManager = null;
        private static EquipmentState _equipmentState = null;
        private static SubstrateManager _substrateManager = null;
        private static CarrierManagementServer _carrierServer = null;
        private static ScenarioManagerForPWA500BIN_TP _scenarioManager = null;
        private string _selectedScenario;
        private int _selectedPortId;
        private readonly Dictionary<string, string> DataToSend = null;
        private readonly Dictionary<string, ScenarioListTypes> CarrierScenario = null;
        private readonly Dictionary<string, ScenarioListTypes> SubstrateScenario = null;
        private readonly Dictionary<string, ScenarioListTypes> ETCScenario = null;
        private readonly Dictionary<string, ScenarioListTypes> RecipeHandlingScenario = null;

        private readonly Dictionary<int, Substrate> CoreSubstrates = null;
        private readonly Dictionary<int, Substrate> BinSubstrates = null;
        private readonly Dictionary<int, string> CoreCarriers = null;
        private readonly Dictionary<int, string> BinCarriers = null;
        private readonly Dictionary<int, string> EmptyCarriers = null;
        private List<Substrate> _substrates = null;

        private Substrate _selectedCoreSubstrate = null;
        private Substrate _selectedBinOrEmptySubstrate = null;
        #endregion </Fields>

        #region <Properties>
        private bool EnableUIControl
        {
            get
            {
                if (false == _equipmentState.GetState().Equals(EQUIPMENT_STATE.IDLE) &&
                    false == _equipmentState.GetState().Equals(EQUIPMENT_STATE.PAUSE))
                    return false;

                if (false == _scenarioOperator.UseScenario)
                    return false;

                return true;
            }
        }
        #endregion </Properties>

        #region <Methods>

        #region <Overrides>
        protected override void ProcessWhenActivation()
        {
            UpdateCarrierInfo();
            UpdateSubstrateInfo();
            UpdateDataToSend();
        }
        protected override void ProcessWhenDeactivation()
        {
        }
        public override void CallFunctionByTimer()
        {
            Enabled = EnableUIControl;
        }
        #endregion </Overrides>

        #region <UI Events>
        private async void BtnExecuteScenarioClicked(object sender, EventArgs e)
        {
            if (false == EnableUIControl)
                return;

            //if (!(sender is Component.CustomActionButton button))
            //    return;

            // DataToSend.Clear();
            //for (int row = 0; row < gvMessageToSend.Rows.Count; ++row)
            //{
            //    string key = gvMessageToSend[0, row].Value.ToString();
            //    string value = string.Empty;
            //    if (gvMessageToSend[1, row].Value != null)
            //        value = gvMessageToSend[1, row].Value.ToString();
            //    DataToSend[key] = value;
            //}
            bool includeUpdateParam = true;
            ScenarioListTypes scenarioTypeToExecute;

            if (sender.Equals(btnDownloadRecipe) || sender.Equals(btnUploadRecipe))
            {
                includeUpdateParam = false;
                if (sender.Equals(btnDownloadRecipe))
                {
                    scenarioTypeToExecute = ScenarioListTypes.SCENARIO_REQ_RECIPE_DOWNLOAD;
                }
                else
                {
                    scenarioTypeToExecute = ScenarioListTypes.SCENARIO_REQ_RECIPE_UPLOAD;
                }
                var paramList = _scenarioOperator.GetScenarioParameterList(scenarioTypeToExecute);
                if (paramList == null)
                    return;

                Dictionary<string, string> paramsToUpdate = new Dictionary<string, string>();
                for (int i = 0; i < paramList.Count; ++i)
                {
                    string paramName = paramList[i];
                    string paramValue = string.Empty;
                    if (paramName.Equals(RecipeHandlingKeys.KeyParamRecipeId))
                    {
                        paramValue = lblSelectedRecipeName.Text;
                    }
                    else if (paramName.Equals(RecipeHandlingKeys.KeyUseCommunicationToPM))
                    {
                        paramValue = bool.TrueString;
                    }

                    paramsToUpdate[paramName] = paramValue;
                }
                _scenarioOperator.UpdateScenarioParam(scenarioTypeToExecute, paramsToUpdate);
            }
            else
            {
                if (false == Enum.TryParse(_selectedScenario, out scenarioTypeToExecute))
                    return;
            }

            Enabled = false;
            var waitResponse = System.Threading.Tasks.Task.Run(() => ExecuteScenarioAsync(scenarioTypeToExecute, includeUpdateParam));
            var result = await waitResponse;
            Enabled = true;

            string message = string.Format("Scenario : {0}\r\nResult : {1}", scenarioTypeToExecute, result.ToString());
            _messageBox.ShowMessage(message);
        }


        //private void BtnExecuteScenarioClicked(object sender, EventArgs e)
        //{
        //    if (false == _messageBox.ShowMessage(string.Format("{0} scenario run?", _selectedScenario)))
        //        return;

        //    DataToSend.Clear();
        //    for (int row = 0; row < gvMessageToSend.Rows.Count; ++row)
        //    {
        //        string key = gvMessageToSend[0, row].Value.ToString();
        //        string value = string.Empty;
        //        if (gvMessageToSend[1, row].Value != null)
        //            value = gvMessageToSend[1, row].Value.ToString();
        //        DataToSend[key] = value;
        //    }

        //    if (false == Enum.TryParse(_selectedScenario, out ScenarioListTypes convertedScenarioName))
        //        return;

        //    _postOffice.SendMail(Define.DefineEnumProject.Mail.EN_SUBSCRIBER.ScenarioCirculator
        //        , Define.DefineEnumProject.Mail.EN_MAIL.SendScenario
        //        , convertedScenarioName
        //        , DataToSend);
        //}
        private void BtnScenarioSelectionClicked(object sender, EventArgs e)
        {
            if (sender.Equals(btnScenarioSelectionForCarrier))
            {
                if (false == _selectionList.CreateForm("CARRIER SCENARIO", CarrierScenario.Keys.ToArray(), _selectedScenario))
                    return;
            }
            else if (sender.Equals(btnScenarioSelectionForSubstrate))
            {
                if (false == _selectionList.CreateForm("SUBSTRATE SCENARIO", SubstrateScenario.Keys.ToArray(), _selectedScenario))
                    return;
            }
            else
            {
                if (false == _selectionList.CreateForm("ETC SCENARIO", ETCScenario.Keys.ToArray(), _selectedScenario))
                    return;
            }

            _selectionList.GetResult(ref _selectedScenario);

            lblSelectedScenarioName.Text = _selectedScenario.ToString();

            UpdateDataToSend();
        }
        private void LblSubstrateInfoClicked(object sender, EventArgs e)
        {
            if (!(sender is Sys3Controls.Sys3Label label))
                return;

            List<string> substrateNames = new List<string>();
            if (label.Equals(lblCoreSubstrateInfo))
            {
                foreach (var item in CoreSubstrates)
                {
                    substrateNames.Add(item.Value.GetName());
                }
            }
            else if (label.Equals(lblBinOrEmptySubstrateInfo))
            {
                foreach (var item in BinSubstrates)
                {
                    substrateNames.Add(item.Value.GetName());
                }
            }

            if (false == _selectionList.CreateForm("Select Substrate", substrateNames.ToArray(), string.Empty))
                return;

            string substrateName = string.Empty;
            _selectionList.GetResult(ref substrateName);

            Substrate substrate = new Substrate("");
            if (false == _substrateManager.GetSubstrateByName(substrateName, ref substrate))
                return;

            if (label.Equals(lblCoreSubstrateInfo))
            {
                _selectedCoreSubstrate = substrate;
            }
            else if (label.Equals(lblBinOrEmptySubstrateInfo))
            {
                _selectedBinOrEmptySubstrate = substrate;
            }

            label.Text = substrateName;
        }
        private void LblCarrierInfoClicked(object sender, EventArgs e)
        {
            if (sender.Equals(lblCarrierInfo))
            {
                Dictionary<int, string> carriers = new Dictionary<int, string>();
                for (int i = 0; i < _loadPortManager.Count; ++i)
                {
                    if (false == _loadPortManager.IsLoadPortEnabled(i))
                        continue;

                    int portId = _loadPortManager.GetLoadPortPortId(i);
                    if (false == _carrierServer.HasCarrier(portId))
                        continue;

                    carriers[portId] = _carrierServer.GetCarrierId(portId);
                }

                if (false == _selectionList.CreateForm("Select Carrier", carriers.Values.ToArray(), carriers.Keys.ToArray(), _selectedPortId))
                    return;

                _selectionList.GetResult(ref _selectedPortId);

                DisplaySelectedCarrierInfo();
            }
        }
        private void BtnApplySubstrateInfoToScenarioClicked(object sender, EventArgs e)
        {
            if (false == Enum.TryParse(_selectedScenario, out ScenarioListTypes scenario))
                return;

            if (false == SubstrateScenario.ContainsKey(_selectedScenario))
                return;
            
            if (sender.Equals(btnApplyBinOrEmptySubstrateInfoToScenario))
            {
                UpdateSubstrateScenarioData(scenario, _selectedBinOrEmptySubstrate, null);
            }
            else if (sender.Equals(btnApplyCoreSubstrateInfoToScenario))
            {
                UpdateSubstrateScenarioData(scenario, _selectedCoreSubstrate, _selectedBinOrEmptySubstrate);
            }
        }

        private void BtnEditSubstrateInfoClicked(object sender, EventArgs e)
        {
            if (sender.Equals(btnEditCoreSubstrateInfo))
            {
                if (_selectedCoreSubstrate == null)
                    return;

                Dictionary<string, string> targetAttributes = _selectedCoreSubstrate.GetAttributesAll();
                FormMaterialEdit materialEdit = new FormMaterialEdit();
                if (materialEdit.CreateEditForm(targetAttributes))
                {
                    if (false == _messageBox.ShowMessage("정말로 자재정보를 변경할까요?"))
                        return;

                    Dictionary<string, string> attributeResults = new Dictionary<string, string>();
                    materialEdit.GetResult(ref attributeResults);

                    _selectedCoreSubstrate.SetAttributesAll(attributeResults);
                    if (false == _selectedCoreSubstrate.GetName().Equals(lblCoreSubstrateInfo.Text))
                    {
                        lblCoreSubstrateInfo.Text = _selectedCoreSubstrate.GetName();
                    }
                }
            }
            else if (sender.Equals(btnEditBinOrEmptySubstrateInfo))
            {
                if (_selectedBinOrEmptySubstrate == null)
                    return;

                Dictionary<string, string> targetAttributes = _selectedBinOrEmptySubstrate.GetAttributesAll();
                FormMaterialEdit materialEdit = new FormMaterialEdit();
                if (materialEdit.CreateEditForm(targetAttributes))
                {
                    if (false == _messageBox.ShowMessage("정말로 자재정보를 변경할까요?"))
                        return;

                    Dictionary<string, string> attributeResults = new Dictionary<string, string>();
                    materialEdit.GetResult(ref attributeResults);

                    _selectedBinOrEmptySubstrate.SetAttributesAll(attributeResults);
                    if (false == _selectedBinOrEmptySubstrate.GetName().Equals(lblBinOrEmptySubstrateInfo.Text))
                    {
                        lblBinOrEmptySubstrateInfo.Text = _selectedBinOrEmptySubstrate.GetName();
                    }
                }
            }
        }
        private void BtnApplyCarrierInfoClicked(object sender, EventArgs e)
        {
            if (sender.Equals(btnApplyCarrierInfo))
            {
                if (false == Enum.TryParse(_selectedScenario, out ScenarioListTypes scenario))
                    return;

                if (false == CarrierScenario.ContainsKey(_selectedScenario))
                    return;

                if (false == _carrierServer.HasCarrier(_selectedPortId))
                    return;

                UpdateCarrierDataToSend(scenario, _selectedPortId);
            }
        }

        private void LblSelectedRecipeNameClicked(object sender, EventArgs e)
        {
            if (false == EnableUIControl)
                return;

            if (_keyboard.CreateForm(lblSelectedRecipeName.Text, 200, false, "Recipe name to handling"))
            {
                string result = string.Empty;
                _keyboard.GetResult(ref result);                
                lblSelectedRecipeName.Text = result;
            }
        }
        #endregion </UI Events>

        #region <Internal>
        private EN_SCENARIO_RESULT ExecuteScenarioAsync(ScenarioListTypes scenario, bool includeUpdateScenario)
        {
            EN_SCENARIO_RESULT result;
            TickCounter_.TickCounter tick = new TickCounter_.TickCounter();

            _scenarioOperator.InitScenarioAll();
            if (includeUpdateScenario)
            {
                DataToSend.Clear();
                for (int row = 0; row < gvMessageToSend.Rows.Count; ++row)
                {
                    string key = gvMessageToSend[0, row].Value.ToString();
                    string value = string.Empty;
                    if (gvMessageToSend[1, row].Value != null)
                        value = gvMessageToSend[1, row].Value.ToString();
                    DataToSend[key] = value;
                }

                _scenarioOperator.UpdateScenarioParam(scenario, DataToSend);
            }
            tick.SetTickCount(30000);
            while (true)
            {
                System.Threading.Thread.Sleep(1);

                if (tick.IsTickOver(true))
                {
                    return EN_SCENARIO_RESULT.TIMEOUT_ERROR;                    
                }

                result = _scenarioOperator.ExecuteScenario(scenario);
                switch (result)
                {
                    case EN_SCENARIO_RESULT.COMPLETED:
                        {
                            var scenarioResult = _scenarioOperator.GetScenarioResultData(scenario);
                            _scenarioManager.ExecuteAfterScenarioCompletion(scenario,
                                DataToSend, 
                                scenarioResult,
                                null,
                                EN_MESSAGE_RESULT.OK,
                                true);
                        }
                        break;
                    default:
                        break;
                }

                if (result.Equals(EN_SCENARIO_RESULT.PROCEED))
                    continue;

                return result;
            }
        }

        private void ClassifyScenarioTypesByEnum(ScenarioListTypes scenario)
        {
            switch (scenario)
            {
                case ScenarioListTypes.SCENARIO_REQ_LOT_INFO_CORE_1:
                case ScenarioListTypes.SCENARIO_REQ_LOT_INFO_CORE_2:
                case ScenarioListTypes.SCENARIO_REQ_LOT_INFO_EMPTY_TAPE:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_1:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_2:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_3:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_4:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_5:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_6:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_1:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_2:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_3:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_4:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_5:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_6:
                case ScenarioListTypes.SCENARIO_CARRIER_LOAD:
                case ScenarioListTypes.SCENARIO_CARRIER_UNLOAD:
                case ScenarioListTypes.SCENARIO_RFID_READ_CORE_1:
                case ScenarioListTypes.SCENARIO_RFID_READ_CORE_2:
                case ScenarioListTypes.SCENARIO_RFID_READ_EMPTY_TAPE:
                case ScenarioListTypes.SCENARIO_RFID_READ_BIN_1:
                case ScenarioListTypes.SCENARIO_RFID_READ_BIN_2:
                case ScenarioListTypes.SCENARIO_RFID_READ_BIN_3:
                case ScenarioListTypes.SCENARIO_REQ_SLOT_INFO_CORE_1:
                case ScenarioListTypes.SCENARIO_REQ_SLOT_INFO_CORE_2:
                case ScenarioListTypes.SCENARIO_REQ_SLOT_INFO_EMPTY_TAPE:
                case ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_CORE_1:
                case ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_CORE_2:
                case ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_EMPTY_TAPE:
                case ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_BIN_1:
                case ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_BIN_2:
                case ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_BIN_3:
                case ScenarioListTypes.SCENARIO_REQ_LOT_MERGE_CORE_1:
                case ScenarioListTypes.SCENARIO_REQ_LOT_MERGE_CORE_2:
                case ScenarioListTypes.SCENARIO_REQ_LOT_ID_MERGE_AND_CHANGE_BIN_1:
                case ScenarioListTypes.SCENARIO_REQ_LOT_ID_MERGE_AND_CHANGE_BIN_2:
                case ScenarioListTypes.SCENARIO_REQ_LOT_ID_MERGE_AND_CHANGE_BIN_3:
                    CarrierScenario[scenario.ToString()] = scenario;
                    break;
                case ScenarioListTypes.SCENARIO_REQ_RECIPE_DOWNLOAD:
                case ScenarioListTypes.SCENARIO_REQ_RECIPE_UPLOAD:
                    ETCScenario[scenario.ToString()] = scenario;
                    //RecipeHandlingScenario[scenario.ToString()] = scenario;
                    break;

                case ScenarioListTypes.SCENARIO_PROCESS_START:
                case ScenarioListTypes.SCENARIO_PROCESS_END:
                case ScenarioListTypes.SCENARIO_REQ_TRACK_IN:
                case ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_TRACK_OUT:
                case ScenarioListTypes.SCENARIO_REQ_LOT_MATCH:
                case ScenarioListTypes.SCENARIO_REQ_BIN_WAFER_TRACK_OUT:
                case ScenarioListTypes.SCENARIO_WORK_START:
                case ScenarioListTypes.SCENARIO_WORK_END:
                case ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_SPLIT:
                case ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_SPLIT_LAST:
                case ScenarioListTypes.SCENARIO_CORE_WAFER_DETACH_START:
                case ScenarioListTypes.SCENARIO_CORE_WAFER_DETACH_END:
                case ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_SPLIT_FIRST:
                case ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_SPLIT:
                case ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_FULL_SPLIT_FIRST:
                case ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_FULL_SPLIT:
                case ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_MERGE:
                case ScenarioListTypes.SCENARIO_BIN_WAFER_ID_READ:
                case ScenarioListTypes.SCENARIO_BIN_WORK_END:
                case ScenarioListTypes.SCENARIO_REQ_BIN_WAFER_ID_ASSIGN:
                case ScenarioListTypes.SCENARIO_BIN_SORTING_START_1:
                case ScenarioListTypes.SCENARIO_BIN_SORTING_END_1:
                case ScenarioListTypes.SCENARIO_BIN_SORTING_START_2:
                case ScenarioListTypes.SCENARIO_BIN_SORTING_END_2:
                case ScenarioListTypes.SCENARIO_BIN_SORTING_START_3:
                case ScenarioListTypes.SCENARIO_BIN_SORTING_END_3:
                case ScenarioListTypes.SCENARIO_REQ_UPLOAD_BINFILE:
                case ScenarioListTypes.SCENARIO_ASSIGN_SUBSTRATE_ID:
                case ScenarioListTypes.SCENARIO_BIN_DATA_UPLOAD:
                    SubstrateScenario[scenario.ToString()] = scenario;
                    break;

                default:
                    ETCScenario[scenario.ToString()] = scenario;
                    break;
            }
        }
        private void UpdateGridViewByAppliedData()
        {
            for (int i = 0; i < gvMessageToSend.Rows.Count; ++i)
            {
                gvMessageToSend[1, i].Value = string.Empty;

                string nameOfKey = gvMessageToSend[0, i].Value.ToString();
                if (false == DataToSend.TryGetValue(nameOfKey, out string value))
                    continue;

                gvMessageToSend[1, i].Value = value;
            }
        }
        private void UpdateCarrierInfo()
        {
            for (int i = 0; i < _loadPortManager.Count; ++i)
            {
                if (false == _loadPortManager.IsLoadPortEnabled(i))
                    continue;

                var substrateType = _scenarioManager.GetSubstrateTypeByLoadPortIndex(i);
                string lpName = _scenarioManager.GetSubstrateTypeForUILoadPortIndex(i);
                
                switch (substrateType)
                {
                    case SubstrateType.Core:
                        CoreCarriers[i] = lpName;
                        break;
                    case SubstrateType.Empty:
                        EmptyCarriers[i] = lpName;
                        break;
                    case SubstrateType.Bin1:
                    case SubstrateType.Bin2:
                    case SubstrateType.Bin3:
                        BinCarriers[i] = lpName;
                        break;
                    default:
                        break;
                }
            }            
        }
        private void UpdateSubstrateInfo()
        {
            BinSubstrates.Clear();
            CoreSubstrates.Clear();

            _substrates.Clear();
            if (false == _substrateManager.GetSubstratesAll(ref _substrates))
                return;

            for(int i = 0; i < _substrates.Count; ++i)
            {
                string substrateTypeString = _substrates[i].GetAttribute(PWA500BINSubstrateAttributes.SubstrateType);
                if (false == Enum.TryParse(substrateTypeString, out SubstrateType substrateType))
                    continue;

                switch (substrateType)
                {
                    case SubstrateType.Core:
                        CoreSubstrates[CoreSubstrates.Count] = _substrates[i];
                        break;
                    case SubstrateType.Empty:
                    case SubstrateType.Bin1:
                    case SubstrateType.Bin2:
                    case SubstrateType.Bin3:
                        BinSubstrates[BinSubstrates.Count] = _substrates[i];
                        break;
                    default:
                        break;
                }
            }

            DisplaySelectedCarrierInfo();
        }
        private void DisplaySelectedCarrierInfo()
        {
            if (_selectedPortId <= 0 || false == _carrierServer.HasCarrier(_selectedPortId))
            {
                lblCarrierInfo.Text = string.Empty;
            }
            else
            {
                lblCarrierInfo.Text = _carrierServer.GetCarrierId(_selectedPortId);
            }
        }
        private void UpdateDataToSend()
        {
            lblSelectedScenarioName.Text = _selectedScenario;

            if (false == Enum.TryParse(_selectedScenario, out ScenarioListTypes convertedScenarioName))
                return;

            var dataToSend = _scenarioOperator.GetScenarioParameterList(convertedScenarioName);
            DataToSend.Clear();
            gvMessageToSend.Rows.Clear();

            if (dataToSend == null)
                return;

            for (int i = 0; i < dataToSend.Count; ++i)
            {
                string dataKey = dataToSend[i];
                DataToSend[dataKey] = string.Empty;

                gvMessageToSend.Rows.Add();
                gvMessageToSend[0, i].Value = dataKey;
                gvMessageToSend[1, i].Value = string.Empty;
            }
        }
        private void UpdateCarrierDataToSend(ScenarioListTypes scenario, int portId)
        {
            string lotId = _carrierServer.GetCarrierLotId(_selectedPortId);
            string carrierId = _carrierServer.GetCarrierId(_selectedPortId);
            string portName = _scenarioManager.GetPortName(_selectedPortId);
            string operatorId = "AUTO";

            switch (scenario)
            {
                case ScenarioListTypes.SCENARIO_REQ_LOT_INFO_CORE_1:
                case ScenarioListTypes.SCENARIO_REQ_LOT_INFO_CORE_2:
                case ScenarioListTypes.SCENARIO_REQ_LOT_INFO_EMPTY_TAPE:
                case ScenarioListTypes.SCENARIO_REQ_SLOT_INFO_CORE_1:
                case ScenarioListTypes.SCENARIO_REQ_SLOT_INFO_CORE_2:
                case ScenarioListTypes.SCENARIO_REQ_SLOT_INFO_EMPTY_TAPE:
                    {
                        DataToSend[LotInfoKeys.KeyParamLotId] = lotId;
                        DataToSend[LotInfoKeys.KeyParamCarrierId] = carrierId;
                    }
                    break;

                case ScenarioListTypes.SCENARIO_RFID_READ_CORE_1:
                case ScenarioListTypes.SCENARIO_RFID_READ_CORE_2:
                case ScenarioListTypes.SCENARIO_RFID_READ_EMPTY_TAPE:
                case ScenarioListTypes.SCENARIO_RFID_READ_BIN_1:
                case ScenarioListTypes.SCENARIO_RFID_READ_BIN_2:
                case ScenarioListTypes.SCENARIO_RFID_READ_BIN_3:
                    {
                        DataToSend[RFIDReadKeys.KeyParamLotId] = lotId;
                        DataToSend[RFIDReadKeys.KeyParamCarrierId] = carrierId;
                        DataToSend[RFIDReadKeys.KeyParamPortId] = portName;
                        DataToSend[RFIDReadKeys.KeyParamOperatorId] = operatorId;
                    }
                    break;                

                case ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_CORE_1:
                case ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_CORE_2:
                case ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_EMPTY_TAPE:
                case ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_BIN_1:
                case ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_BIN_2:
                case ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_BIN_3:
                case ScenarioListTypes.SCENARIO_REQ_LOT_ID_MERGE_AND_CHANGE_BIN_1:
                case ScenarioListTypes.SCENARIO_REQ_LOT_ID_MERGE_AND_CHANGE_BIN_2:
                case ScenarioListTypes.SCENARIO_REQ_LOT_ID_MERGE_AND_CHANGE_BIN_3:
                    {
                        DataToSend[SlotMappingKeys.KeyParamLotId] = lotId;
                        DataToSend[SlotMappingKeys.KeyParamCarrierId] = carrierId;
                        var substrates = _substrateManager.GetSubstratesAtLoadPort(_selectedPortId);
                        for (int i = 0; i < 25; ++i)
                        {
                            string keyId = string.Format("{0}{1}_{2}", SlotMappingKeys.KeyParamSlotNamePre, i + 1, SlotMappingKeys.KeyParamSlotNamePost);
                            string keyQty = string.Format("{0}{1}_{2}", SlotMappingKeys.KeyParamSlotQtyPre, i + 1, SlotMappingKeys.KeyParamSlotQtyPost);
                            string valueId = string.Empty;
                            string valueQty = "";
                            if (substrates.TryGetValue(i, out Substrate substrate))
                            {
                                valueId = substrate.GetName();
                                valueQty = substrate.GetAttribute(PWA500BINSubstrateAttributes.ChipQty);
                                if (string.IsNullOrEmpty(valueQty) || valueQty.Equals("0"))
                                    valueQty = "";
                            }
                            
                            DataToSend[keyId] = valueId;
                            DataToSend[keyQty] = valueQty;
                        }
                    }
                    break;
                case ScenarioListTypes.SCENARIO_REQ_LOT_MERGE_CORE_1:
                case ScenarioListTypes.SCENARIO_REQ_LOT_MERGE_CORE_2:
                    {
                        bool isCoreScenario = false;
                        if (scenario.Equals(ScenarioListTypes.SCENARIO_REQ_LOT_MERGE_CORE_1) ||
                            scenario.Equals(ScenarioListTypes.SCENARIO_REQ_LOT_MERGE_CORE_2))
                        {
                            isCoreScenario = true;
                        }

                        string partId = string.Empty;
                        string recipeId = _scenarioManager.GetRecipeId();
                        var substrates = _substrateManager.GetSubstratesAtLoadPort(_selectedPortId);

                        bool isFisrtSubstrate = false;
                        for (int i = 0; i < 25; ++i)
                        {
                            string keyId = string.Format("{0}{1}_{2}", LotMergeKeys.KeyParamSlotLotIdPre, i + 1, LotMergeKeys.KeyParamSlotLotIdPost);
                            string valueId = string.Empty;
                            if (substrates.TryGetValue(i, out Substrate substrate))
                            {
                                valueId = substrate.GetLotId();
                                partId = substrate.GetAttribute(PWA500BINSubstrateAttributes.PartId);
                                //recipeId = substrate.GetRecipeId();

                                if (false == isCoreScenario)
                                {
                                    if (false == isFisrtSubstrate)
                                    {
                                        isFisrtSubstrate = true;
                                        lotId = valueId;
                                    }
                                }
                            }

                            DataToSend[keyId] = valueId;
                        }

                        DataToSend[LotMergeKeys.KeyParamLotId] = lotId;
                        DataToSend[LotMergeKeys.KeyParamCarrierId] = carrierId;
                        DataToSend[LotMergeKeys.KeyParamPartId] = partId;
                        DataToSend[LotMergeKeys.KeyParamRecipeId] = recipeId;
                        DataToSend[LotMergeKeys.KeyOperatorId] = operatorId;
                    }
                    break;

                case ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_1:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_2:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_3:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_4:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_5:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_6:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_1:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_2:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_3:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_4:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_5:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_6:
                case ScenarioListTypes.SCENARIO_CARRIER_LOAD:
                case ScenarioListTypes.SCENARIO_CARRIER_UNLOAD:
                case ScenarioListTypes.SCENARIO_REQ_RECIPE_DOWNLOAD:
                    break;
            }

            UpdateGridViewByAppliedData();
        }
        private void UpdateSubstrateScenarioData(ScenarioListTypes scenario, Substrate substrate1, Substrate substrate2)
        {
            if (substrate1 == null)
                return;

            int portId = substrate1.GetSourcePortId();
            int slotId = substrate1.GetSourceSlot() + 1;
            
            string substrateName = substrate1.GetName();
            string ringId = substrate1.GetAttribute(PWA500BINSubstrateAttributes.RingId);
            string carrierId = _carrierServer.GetCarrierId(portId);
            string portName = _scenarioManager.GetPortName(portId);
            string lotId = substrate1.GetLotId();
            string partId = substrate1.GetAttribute(PWA500BINSubstrateAttributes.PartId);
            string stepSeq = substrate1.GetAttribute(PWA500BINSubstrateAttributes.StepSeq);
            string recipeId = _scenarioManager.GetRecipeId();
            string operatorId = "AUTO";
            string chipQty = substrate1.GetAttribute(PWA500BINSubstrateAttributes.ChipQty);
            
            switch (scenario)
            {
                #region <Core Only>
                case ScenarioListTypes.SCENARIO_PROCESS_START:
                case ScenarioListTypes.SCENARIO_PROCESS_END:
                    {                        
                        DataToSend[EESKeys.KeyCarrierId] = carrierId;
                        DataToSend[EESKeys.KeyPortId] = portName;
                        DataToSend[EESKeys.KeyLotId] = lotId;
                        DataToSend[EESKeys.KeyPartId] = partId;
                        DataToSend[EESKeys.KeyParamRecipeId] = recipeId;
                        DataToSend[EESKeys.KeyOperatorId] = operatorId;
                    }
                    break;
                case ScenarioListTypes.SCENARIO_REQ_TRACK_IN:
                case ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_TRACK_OUT:
                    {                        
                        DataToSend[TrackInOrOut.KeyParamCarrierId] = carrierId;
                        DataToSend[TrackInOrOut.KeyParamPortId] = portName;
                        DataToSend[TrackInOrOut.KeyParamLotId] = lotId;
                        DataToSend[TrackInOrOut.KeyParamPartId] = partId;
                        DataToSend[TrackInOrOut.KeyParamStepSeq] = stepSeq;
                        DataToSend[TrackInOrOut.KeyParamRecipeId] = recipeId;
                        DataToSend[TrackInOrOut.KeyParamChipQty] = chipQty;
                        DataToSend[TrackInOrOut.KeyParamOperatorId] = operatorId;
                    }
                    break;
                case ScenarioListTypes.SCENARIO_WORK_START:
                    {
                        DataToSend[RequestDownloadMapFileKeys.KeyParamCarrierId] = carrierId;
                        DataToSend[RequestDownloadMapFileKeys.KeyParamPortId] = portName;
                        DataToSend[RequestDownloadMapFileKeys.KeyParamLotId] = lotId;
                        DataToSend[RequestDownloadMapFileKeys.KeyParamPartId] = partId;                        
                        DataToSend[RequestDownloadMapFileKeys.KeyParamRecipeId] = recipeId;
                        DataToSend[RequestDownloadMapFileKeys.KeyParamOperatorId] = operatorId;
                        DataToSend[RequestDownloadMapFileKeys.KeyParamWaferId] = substrateName;
                        DataToSend[RequestDownloadMapFileKeys.KeyParamAngle] = "0";
                        DataToSend[RequestDownloadMapFileKeys.KeyNullBinCode] = " ";
                        DataToSend[RequestDownloadMapFileKeys.KeyUseEventHandling] = "True";
                    }
                    break;

                case ScenarioListTypes.SCENARIO_WORK_END:
                    {
                        DataToSend[UploadCoreOrBinFileKeys.KeyParamCarrierId] = carrierId;
                        DataToSend[UploadCoreOrBinFileKeys.KeyParamPortId] = portName;
                        DataToSend[UploadCoreOrBinFileKeys.KeyParamLotId] = lotId;
                        DataToSend[UploadCoreOrBinFileKeys.KeyParamPartId] = partId;
                        DataToSend[UploadCoreOrBinFileKeys.KeyParamRecipeId] = recipeId;
                        DataToSend[UploadCoreOrBinFileKeys.KeyParamChipQty] = chipQty;
                        DataToSend[UploadCoreOrBinFileKeys.KeyParamOperatorId] = operatorId;
                        DataToSend[UploadCoreOrBinFileKeys.KeyPMSFileName] = string.Empty;
                        DataToSend[UploadCoreOrBinFileKeys.KeyPMSFileBody] = string.Empty;
                        DataToSend[UploadCoreOrBinFileKeys.KeySubstrateName] = substrateName;
                        DataToSend[UploadCoreOrBinFileKeys.KeyWaferAngle] = "0";
                        DataToSend[UploadCoreOrBinFileKeys.KeyCountRow] = "0";
                        DataToSend[UploadCoreOrBinFileKeys.KeyCountCol] = "0";
                        DataToSend[UploadCoreOrBinFileKeys.KeyReferenceX] = "0";
                        DataToSend[UploadCoreOrBinFileKeys.KeyReferenceY] = "0";
                        DataToSend[UploadCoreOrBinFileKeys.KeyStartingPosX] = "0";
                        DataToSend[UploadCoreOrBinFileKeys.KeyStartingPosY] = "0";
                        DataToSend[UploadCoreOrBinFileKeys.KeyNullBinCode] = " ";
                        DataToSend[UploadCoreOrBinFileKeys.KeyUseEventHandling] = "True";
                    }
                    break;

                case ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_SPLIT:
                case ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_SPLIT_LAST:
                    {
                        DataToSend[AssignSubstrateLotIdKeys.KeyParamLotId] = lotId;
                        DataToSend[AssignSubstrateLotIdKeys.KeyParamWaferId] = substrateName;                        
                        DataToSend[AssignSubstrateLotIdKeys.KeyParamPartId] = partId;                        
                        DataToSend[AssignSubstrateLotIdKeys.KeyParamRecipeId] = recipeId;
                        // TODO : 슬롯을 1부터 매기도록 바꿔야하나? -> 여기는 위에서 더해서 내려온다.
                        DataToSend[AssignSubstrateLotIdKeys.KeyParamSlotId] = slotId.ToString();
                        DataToSend[AssignSubstrateLotIdKeys.KeyParamOperatorId] = operatorId;
                    }
                    break;
                
                case ScenarioListTypes.SCENARIO_CORE_WAFER_DETACH_START:
                case ScenarioListTypes.SCENARIO_CORE_WAFER_DETACH_END:
                    {                        
                        DataToSend[DetachingKeys.KeyParamCarrierId] = carrierId;
                        DataToSend[DetachingKeys.KeyParamPortId] = portName;
                        DataToSend[DetachingKeys.KeyParamLotId] = lotId;
                        DataToSend[DetachingKeys.KeyParamPartId] = partId;
                        DataToSend[DetachingKeys.KeyParamRecipeId] = recipeId;
                        DataToSend[DetachingKeys.KeyParamWaferId] = substrateName;
                        // TODO : 슬롯을 1부터 매기도록 바꿔야하나? -> 여기는 위에서 더해서 내려온다.
                        DataToSend[DetachingKeys.KeyParamSlotId] = slotId.ToString();
                        DataToSend[DetachingKeys.KeyParamOperatorId] = operatorId;
                        
                        // FDC는 매뉴얼 입력이니 냅두자
                        if (scenario.Equals(ScenarioListTypes.SCENARIO_CORE_WAFER_DETACH_START))
                        {

                        }
                    }
                    break;
                #endregion </Core Only>

                #region <Core and Bin or Empty Only>
                case ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_SPLIT_FIRST:
                case ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_SPLIT:
                case ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_FULL_SPLIT_FIRST:
                case ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_FULL_SPLIT:
                    {
                        if (substrate2 == null)
                            return;

                        string binCode = substrate2.GetAttribute(PWA500BINSubstrateAttributes.BinCode);
                        string ringIdToSort = substrate2.GetAttribute(PWA500BINSubstrateAttributes.RingId);

                        DataToSend[SplitCoreChipKeys.KeyParamLotId] = lotId;
                        DataToSend[SplitCoreChipKeys.KeyParamSplitWaferId] = substrateName;
                        DataToSend[SplitCoreChipKeys.KeyParamRingFrameId] = ringIdToSort;
                        DataToSend[SplitCoreChipKeys.KeyParamBinType] = binCode;
                        DataToSend[SplitCoreChipKeys.KeyParamSplitChipQty] = chipQty;
                    }
                    break;

                case ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_MERGE:
                    {
                        if (substrate2 == null)
                            return;

                        string binCode = substrate2.GetAttribute(PWA500BINSubstrateAttributes.BinCode);
                        string ringIdToSort = substrate2.GetAttribute(PWA500BINSubstrateAttributes.RingId);
                        string lotIdToSort = substrate2.GetLotId();
                        string splittedLotId = substrate2.GetAttribute(PWA500BINSubstrateAttributes.SplittedLotId);
                        string splittedChipQty = substrate2.GetAttribute(PWA500BINSubstrateAttributes.ChipQty);

                        DataToSend[SplitCoreChipKeys.KeyParamLotId] = lotIdToSort;
                        DataToSend[SplitCoreChipKeys.KeyParamSplitLotId] = splittedLotId;
                        DataToSend[SplitCoreChipKeys.KeyParamSplitWaferId] = substrateName;
                        DataToSend[SplitCoreChipKeys.KeyParamRingFrameId] = ringIdToSort;
                        DataToSend[SplitCoreChipKeys.KeyParamBinType] = binCode;
                        DataToSend[SplitCoreChipKeys.KeyParamSplitChipQty] = splittedChipQty;
                    }
                    break;
                #endregion </Core and Bin or Empty Only>

                #region <Bin or Empty Only>
                case ScenarioListTypes.SCENARIO_REQ_LOT_MATCH:
                    {
                        DataToSend[DetachingKeys.KeyParamLotId] = lotId;
                        DataToSend[DetachingKeys.KeyParamCarrierId] = carrierId;
                        DataToSend[TrackInOrOut.KeyParamChangeReason] = Constants.EmptyWaferChangeReason;
                        DataToSend[TrackInOrOut.KeyParamMaterialType] = Constants.EmptyWaferMaterialType;
                    }
                    break;

                case ScenarioListTypes.SCENARIO_REQ_BIN_WAFER_TRACK_OUT:
                    {
                        int destPortId = substrate1.GetDestinationPortId();
                        string binCode = substrate1.GetAttribute(PWA500BINSubstrateAttributes.BinCode);

                        carrierId = _carrierServer.GetCarrierId(destPortId);
                        portName = _scenarioManager.GetPortName(destPortId);                        

                        DataToSend[TrackInOrOut.KeyParamCarrierId] = carrierId;
                        DataToSend[TrackInOrOut.KeyParamPortId] = portName;
                        DataToSend[TrackInOrOut.KeyParamLotId] = lotId;
                        DataToSend[TrackInOrOut.KeyParamPartId] = partId;
                        DataToSend[TrackInOrOut.KeyParamStepSeq] = stepSeq;
                        DataToSend[TrackInOrOut.KeyParamRecipeId] = recipeId;
                        DataToSend[TrackInOrOut.KeyParamChipQty] = chipQty.ToString();
                        DataToSend[TrackInOrOut.KeyParamBinType] = binCode;
                        DataToSend[TrackInOrOut.KeyParamOperatorId] = operatorId;
                    }
                    break;
                
                case ScenarioListTypes.SCENARIO_BIN_WAFER_ID_READ:
                    {                        
                        DataToSend[AssignRingIdKeys.KeyParamLotId] = lotId;
                        DataToSend[AssignRingIdKeys.KeyParamWaferId] = ringId;
                    }
                    break;

                case ScenarioListTypes.SCENARIO_BIN_DATA_UPLOAD:
                    {
                        portId = substrate1.GetDestinationPortId();
                        carrierId = _carrierServer.GetCarrierId(portId);
                        portName = _scenarioManager.GetPortName(portId);

                        DataToSend[UploadCoreOrBinFileKeys.KeyParamCarrierId] = carrierId;
                        DataToSend[UploadCoreOrBinFileKeys.KeyParamPortId] = portName;
                        DataToSend[UploadCoreOrBinFileKeys.KeyParamLotId] = lotId;
                        DataToSend[UploadCoreOrBinFileKeys.KeyParamRecipeId] = recipeId;
                        DataToSend[UploadCoreOrBinFileKeys.KeyParamPartId] = partId;
                        DataToSend[UploadCoreOrBinFileKeys.KeyParamOperatorId] = operatorId;
                        DataToSend[UploadCoreOrBinFileKeys.KeyParamChipQty] = chipQty;
                        DataToSend[UploadCoreOrBinFileKeys.KeyMapData] = substrate1.GetAttribute(PWA500BINSubstrateAttributes.MapData);
                        DataToSend[UploadCoreOrBinFileKeys.KeyPMSFileName] = string.Empty;
                        DataToSend[UploadCoreOrBinFileKeys.KeyPMSFileBody] = string.Empty;
                        DataToSend[UploadCoreOrBinFileKeys.KeySubstrateName] = substrateName;
                        DataToSend[UploadCoreOrBinFileKeys.KeyWaferAngle] = "0";
                        DataToSend[UploadCoreOrBinFileKeys.KeyCountRow] = "0";
                        DataToSend[UploadCoreOrBinFileKeys.KeyCountCol] = "0";
                        DataToSend[UploadCoreOrBinFileKeys.KeyReferenceX] = "0";
                        DataToSend[UploadCoreOrBinFileKeys.KeyReferenceY] = "0";
                        DataToSend[UploadCoreOrBinFileKeys.KeyStartingPosX] = "0";
                        DataToSend[UploadCoreOrBinFileKeys.KeyStartingPosY] = "0";
                        DataToSend[UploadCoreOrBinFileKeys.KeyNullBinCode] = " ";
                        DataToSend[UploadCoreOrBinFileKeys.KeyUseEventHandling] = "True";
                    }
                    break;

                case ScenarioListTypes.SCENARIO_BIN_WORK_END:
                    {
                        portId = substrate1.GetDestinationPortId();
                        carrierId = _carrierServer.GetCarrierId(portId);
                        portName = _scenarioManager.GetPortName(portId);

                        DataToSend[UploadCoreOrBinFileKeys.KeyParamCarrierId] = carrierId;
                        DataToSend[UploadCoreOrBinFileKeys.KeyParamPortId] = portName;
                        DataToSend[UploadCoreOrBinFileKeys.KeyParamLotId] = lotId;
                        DataToSend[UploadCoreOrBinFileKeys.KeyParamPartId] = partId;
                        DataToSend[UploadCoreOrBinFileKeys.KeyParamRecipeId] = recipeId;
                        DataToSend[UploadCoreOrBinFileKeys.KeyParamChipQty] = chipQty;
                        DataToSend[UploadCoreOrBinFileKeys.KeyParamOperatorId] = operatorId;
                        DataToSend[UploadCoreOrBinFileKeys.KeyPMSFileName] = string.Empty;
                        DataToSend[UploadCoreOrBinFileKeys.KeyPMSFileBody] = string.Empty;
                        DataToSend[UploadCoreOrBinFileKeys.KeySubstrateName] = substrateName;
                        DataToSend[UploadCoreOrBinFileKeys.KeyWaferAngle] = "0";
                        DataToSend[UploadCoreOrBinFileKeys.KeyCountRow] = "0";
                        DataToSend[UploadCoreOrBinFileKeys.KeyCountCol] = "0";
                        DataToSend[UploadCoreOrBinFileKeys.KeyReferenceX] = "0";
                        DataToSend[UploadCoreOrBinFileKeys.KeyReferenceY] = "0";
                        DataToSend[UploadCoreOrBinFileKeys.KeyStartingPosX] = "0";
                        DataToSend[UploadCoreOrBinFileKeys.KeyStartingPosY] = "0";
                        DataToSend[UploadCoreOrBinFileKeys.KeyNullBinCode] = " ";
                        DataToSend[UploadCoreOrBinFileKeys.KeyUseEventHandling] = "True";
                    }
                    break;

                // LOTID
                // BIN_TYPE
                // RINGFRAME_ID
                // SLOTID
                // CHIP_QTY
                case ScenarioListTypes.SCENARIO_REQ_BIN_WAFER_ID_ASSIGN:
                    {
                        slotId = substrate1.GetDestinationSlot() + 1;
                        string binCode = substrate1.GetAttribute(PWA500BINSubstrateAttributes.BinCode);

                        DataToSend[AssignSubstrateIdKeys.KeyParamLotId] = lotId;
                        DataToSend[AssignSubstrateIdKeys.KeyParamBinType] = binCode;
                        DataToSend[AssignSubstrateIdKeys.KeyParamRingFrameId] = ringId;
                        // TODO : 슬롯을 1부터 매기도록 바꿔야하나? -> 여기는 위에서 더해서 내려온다.
                        DataToSend[AssignSubstrateIdKeys.KeyParamSlotId] = slotId.ToString();
                        DataToSend[AssignSubstrateIdKeys.KeyParamChipQty] = chipQty;
                    }
                    break;

                // LOTID
                // BIN_TYPE
                // RING_FRAME_ID
                case ScenarioListTypes.SCENARIO_BIN_SORTING_START_1:
                case ScenarioListTypes.SCENARIO_BIN_SORTING_START_2:
                case ScenarioListTypes.SCENARIO_BIN_SORTING_START_3:
                    {
                        string binCode = substrate1.GetAttribute(PWA500BINSubstrateAttributes.BinCode);

                        DataToSend[SortingKeys.KeyParamLotId] = lotId;
                        DataToSend[SortingKeys.KeyParamBinType] = binCode;
                        DataToSend[SortingKeys.KeyParamRingFrameId] = ringId;
                    }
                    break;

                // LOTID
                // BIN_TYPE
                // RINGFRAME_ID
                // CHIP_QTY
                case ScenarioListTypes.SCENARIO_BIN_SORTING_END_1:
                case ScenarioListTypes.SCENARIO_BIN_SORTING_END_2:
                case ScenarioListTypes.SCENARIO_BIN_SORTING_END_3:
                    //case ScenarioListTypes.SCENARIO_ASSIGN_SUBSTRATE_ID:
                    {
                        string binCode = substrate1.GetAttribute(PWA500BINSubstrateAttributes.BinCode);

                        DataToSend[SortingKeys.KeyParamLotId] = lotId;
                        DataToSend[SortingKeys.KeyParamBinType] = binCode;
                        DataToSend[SortingKeys.KeyParamRingFrameId] = ringId;
                        DataToSend[SortingKeys.KeyParamChipQty] = chipQty;
                    }
                    break;

                case ScenarioListTypes.SCENARIO_REQ_UPLOAD_BINFILE:
                    {
                        string stepId = substrate1.GetAttribute(PWA500BINSubstrateAttributes.StepSeq);

                        // 2024.10.29. jhlim [MOD] StepSeq가 설정값과 다르면 값을 셋한다.
                        string stepSeqFromParam = _scenarioManager.GetStepIdForBinWafer();
                        if (stepId.Equals(stepSeqFromParam))
                        {
                            substrate1.SetAttribute(PWA500BINSubstrateAttributes.StepSeq, stepSeqFromParam);
                        }

                        stepId = stepSeqFromParam;
                        // 2024.10.29. jhlim [END]

                        DataToSend[UploadCoreOrBinFileKeys.KeySubstrateName] = substrateName;
                        DataToSend[UploadCoreOrBinFileKeys.KeyRingId] = ringId;
                        DataToSend[UploadCoreOrBinFileKeys.KeyRecipeId] = recipeId;
                        DataToSend[UploadCoreOrBinFileKeys.KeySubstrateType] = substrate1.GetAttribute(PWA500BINSubstrateAttributes.BinCode);
                        DataToSend[UploadCoreOrBinFileKeys.KeyStepId] = stepId;
                        DataToSend[UploadCoreOrBinFileKeys.KeyEquipId] = FrameOfSystem3.Work.AppConfigManager.Instance.MachineName;
                        DataToSend[UploadCoreOrBinFileKeys.KeyPartId] = partId;
                        DataToSend[UploadCoreOrBinFileKeys.KeySlot] = slotId.ToString();
                        DataToSend[UploadCoreOrBinFileKeys.KeyLotId] = lotId;
                    }
                    break;

                #endregion </Bin or Empty Only>

                default:
                    break;
            }

            UpdateGridViewByAppliedData();
        }
        #endregion </Internal>

        #endregion </Methods>
    }
}