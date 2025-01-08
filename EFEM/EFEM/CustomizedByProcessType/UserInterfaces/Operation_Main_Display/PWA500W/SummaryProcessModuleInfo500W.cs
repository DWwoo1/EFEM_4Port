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

using EFEM.Modules;
using EFEM.Defines.AtmRobot;
using EFEM.MaterialTracking;
using EFEM.Defines.ProcessModule;
using EFEM.CustomizedByProcessType.PWA500W;

using FrameOfSystem3.Views;

namespace EFEM.CustomizedByProcessType.UserInterface.OperationMainSummary.PWA500W
{
    public partial class SummaryProcessModuleInfo500W : UserControl
    {
        #region <Constructors>
        public SummaryProcessModuleInfo500W(int pmIndex)
        {
            InitializeComponent();

            ProcessModuleIndex = pmIndex;
            _processGroup = ProcessModuleGroup.Instance;
            _substrateManager = SubstrateManager.Instance;

            _communicationInfo = new NetworkInformation();

            _temporaryList = new List<Substrate>();
            _temporaryCoreList = new List<Substrate>();
            _temporarySortList = new List<Substrate>();

            CoreSubstrates = new List<Substrate>();
            SortSubstrates = new List<Substrate>();

            MappedServiceIndex = new Dictionary<int, int>();
            MappedClientIndex = new Dictionary<int, int>();

            //_temporaryLocations = new List<string>();
            _temporaryLoadingLocations = new List<string>();
            _temporaryUnloadingLocations = new List<string>();
            RequestedLocation = new Dictionary<string, int>();

            InitGridControl();
            InitConnectionStatusGridViews();

            ProcessModuleName = _processGroup.GetProcessModuleName(ProcessModuleIndex);

            this.Dock = DockStyle.Fill;
        }

        #endregion </Constructors>

        #region <Fields>
        private readonly int ProcessModuleIndex;
        private static ProcessModuleGroup _processGroup = null;
        private string _temporaryEquipmentState;
        private string _temporaryRecipeId;
        private static SubstrateManager _substrateManager = null;

        private readonly List<Substrate> CoreSubstrates = null;
        private readonly List<Substrate> SortSubstrates = null;
        private List<Substrate> _temporaryList = null;
        private List<Substrate> _temporaryCoreList = null;
        private List<Substrate> _temporarySortList = null;

        private const int ColumnSubstrateIndex = 0;
        private const int ColumnSubstrateName = 1;

        private const int ColumnRequestEnabled = 0;
        private const int ColumnRequestLocation = 1;

        private const int ColumnConnectionStatus = 0;
        private const int ColumnConnectionName = 1;

        private List<string> _temporaryLoadingLocations = null;
        private List<string> _temporaryUnloadingLocations = null;
        private readonly Dictionary<string, int> RequestedLocation = null;

        private readonly Color EnabledColor = Color.LimeGreen;
        private readonly Color DisabledColor = Color.White;

        private NetworkInformation _communicationInfo;

        private readonly Dictionary<int, int> MappedServiceIndex = null;
        private readonly Dictionary<int, int> MappedClientIndex = null;
        private readonly string ProcessModuleName = string.Empty;
        #endregion </Fields>

        #region <Properties>
        #endregion </Properties>

        #region <Methods>
        private void InitConnectionStatusGridViews()
        {
            if (_processGroup.GetCommunicationInfo(ProcessModuleIndex, ref _communicationInfo))
            {
                // Service
                gvServiceStatus.Rows.Clear();
                int index = 0;
                foreach (var item in _communicationInfo.ServiceInfo)
                {
                    gvServiceStatus.Rows.Add();
                    gvServiceStatus[ColumnConnectionName, index].Value = item.Value.Name;
                    MappedServiceIndex[item.Key] = index;
                    ++index;
                }

                gvClientStatus.Rows.Clear();
                index = 0;
                foreach (var item in _communicationInfo.ClientInfo)
                {
                    gvClientStatus.Rows.Add();
                    gvClientStatus[ColumnConnectionName, index].Value = item.Value.Name;
                    MappedClientIndex[item.Key] = index;
                    ++index;
                }
            }

        }
        private void InitGridControl()
        {
            string[] locations = _processGroup.GetProcessModuleLocations(ProcessModuleIndex);

            gvReceivedRequest.Rows.Clear();
            for (int i = 0; i < locations.Length; ++i)
            {
                string[] splitted = locations[i].Split('.');
                if (splitted.Length < 2)
                    continue;

                string loc = splitted[splitted.Length - 1];
                gvReceivedRequest.Rows.Add();
                gvReceivedRequest[ColumnRequestLocation, i].Value = loc;

                RequestedLocation[locations[i]] = i;               
            }
        }
        private void UpdateSortGridView()
        {
            bool updateAll = false;
            if (SortSubstrates.Count != _temporarySortList.Count)
            {
                SortSubstrates.Clear();
                gvSortSubstrateList.Rows.Clear();
                updateAll = true;
            }

            for (int i = 0; i < _temporarySortList.Count; ++i)
            {
                if (updateAll || SortSubstrates[i].GetName() != _temporarySortList[i].GetName())
                {
                    if (updateAll)
                    {
                        SortSubstrates.Add(_temporarySortList[i]);
                        gvSortSubstrateList.Rows.Add();
                        gvSortSubstrateList[ColumnSubstrateIndex, i].Value = (i + 1).ToString();
                    }
                    else
                    {
                        SortSubstrates[i] = _temporarySortList[i];
                    }

                    gvSortSubstrateList[ColumnSubstrateName, i].Value = _temporarySortList[i].GetName();
                }
            }
        }
        private void UpdateCoreGridView()
        {
            bool updateAll = false;
            if (CoreSubstrates.Count != _temporaryCoreList.Count)
            {
                CoreSubstrates.Clear();
                gvCoreSubstrateList.Rows.Clear();
                updateAll = true;
            }

            for (int i = 0; i < _temporaryCoreList.Count; ++i)
            {
                if (updateAll || CoreSubstrates[i].GetName() != _temporaryCoreList[i].GetName())
                {
                    if (updateAll)
                    {
                        CoreSubstrates.Add(_temporaryCoreList[i]);
                        gvCoreSubstrateList.Rows.Add();
                        gvCoreSubstrateList[ColumnSubstrateIndex, i].Value = (i + 1).ToString();
                    }
                    else
                    {
                        CoreSubstrates[i] = _temporaryCoreList[i];
                    }

                    gvCoreSubstrateList[ColumnSubstrateName, i].Value = _temporaryCoreList[i].GetName();
                }
            }
        }
        private void UpdateRequestedCellColor(int cellIndex, bool enabled)
        {
            if (gvReceivedRequest.Rows.Count <= cellIndex)
                return;
            if(enabled)
            {
                gvReceivedRequest.Rows[cellIndex].Cells[ColumnRequestEnabled].Style.BackColor = EnabledColor;
                gvReceivedRequest.Rows[cellIndex].Cells[ColumnRequestEnabled].Style.SelectionBackColor = EnabledColor;
            }
            else
            {
                gvReceivedRequest.Rows[cellIndex].Cells[ColumnRequestEnabled].Style.BackColor = DisabledColor;
                gvReceivedRequest.Rows[cellIndex].Cells[ColumnRequestEnabled].Style.SelectionBackColor = DisabledColor;
            }
        }
        private void UpdateServiceStatus()
        {
            if (_processGroup.GetCommunicationInfo(ProcessModuleIndex, ref _communicationInfo))
            {
                foreach (var item in _communicationInfo.ServiceInfo)
                {
                    int row = MappedServiceIndex[item.Key];
                    if (row < gvClientStatus.Rows.Count)
                    {
                        if (item.Value.ConnectionStatus)
                        {
                            gvServiceStatus.Rows[row].Cells[ColumnConnectionStatus].Style.BackColor = EnabledColor;
                            gvServiceStatus.Rows[row].Cells[ColumnConnectionStatus].Style.SelectionBackColor = EnabledColor;
                        }
                        else
                        {
                            gvServiceStatus.Rows[row].Cells[ColumnConnectionStatus].Style.BackColor = DisabledColor;
                            gvServiceStatus.Rows[row].Cells[ColumnConnectionStatus].Style.SelectionBackColor = DisabledColor;
                        }
                    }
                }

                foreach (var item in _communicationInfo.ClientInfo)
                {
                    int row = MappedClientIndex[item.Key];
                    if (row < gvClientStatus.Rows.Count)
                    {
                        // gvServiceStatus[ColumnStatus, item.Key];

                        if (item.Value.ConnectionStatus)
                        {
                            gvClientStatus.Rows[row].Cells[ColumnConnectionStatus].Style.BackColor = EnabledColor;
                            gvClientStatus.Rows[row].Cells[ColumnConnectionStatus].Style.SelectionBackColor = EnabledColor;
                        }
                        else
                        {
                            gvClientStatus.Rows[row].Cells[ColumnConnectionStatus].Style.BackColor = DisabledColor;
                            gvClientStatus.Rows[row].Cells[ColumnConnectionStatus].Style.SelectionBackColor = DisabledColor;
                        }
                    }
                }
            }
        }
        public void RefreshSubstrateList()
        {
            if (_substrateManager.GetSubstratesAtProcessModule(ProcessModuleName, ref _temporaryList))
            {
                _temporaryCoreList.Clear();
                _temporarySortList.Clear();
                for (int i = 0; i < _temporaryList.Count; ++i)
                {
                    string subType = _temporaryList[i].GetAttribute(PWA500WSubstrateAttributes.SubstrateType);
                    if (false == Enum.TryParse(subType, out SubstrateType convertedSubType))
                        continue;

                    switch (convertedSubType)
                    {
                        case SubstrateType.Core_8:
                        case SubstrateType.Core_12:
                            _temporaryCoreList.Add(_temporaryList[i]);
                            break;
                        case SubstrateType.Bin:
                            _temporarySortList.Add(_temporaryList[i]);
                            break;
                        default:
                            break;
                    }
                }

                UpdateCoreGridView();
                UpdateSortGridView();
                UpdateServiceStatus();
                //for (int i = 0; i < _temporaryList.Count; ++i)
                //{
                //    if (needRedraw || Substrates[i].Name != _temporaryList[i].Name)
                //    {
                //        if (needRedraw)
                //        {
                //            Substrates.Add(_temporaryList[i]);
                //            gvSortSubstrateList.Rows.Add();
                //            gvSortSubstrateList[ColumnSubstrateIndex, i].Value = (i + 1).ToString();
                //        }
                //        else
                //        {
                //            Substrates[i] = _temporaryList[i];
                //        }

                //        gvSortSubstrateList[ColumnSubstrateName, i].Value = _temporaryList[i].Name;
                //    }
                //}
            }
        }
        public void RefreshModuleInfo()
        {
            #region <Status>
            _temporaryEquipmentState = _processGroup.GetEquipmentState(ProcessModuleIndex).ToString();
            if (false == lblEquipmentState.Text.Equals(_temporaryEquipmentState))
            {
                lblEquipmentState.Text = _temporaryEquipmentState;
            }

            _temporaryRecipeId = _processGroup.GetRecipeId(ProcessModuleIndex);
            if (false == lblRecipeId.Text.Equals(_temporaryRecipeId))
            {
                lblRecipeId.Text = _temporaryRecipeId;
            }
            #endregion </Status>
            
            #region <Requests>
            //_temporaryLocations.Clear();
            _temporaryLoadingLocations.Clear();
            _temporaryUnloadingLocations.Clear();
            _processGroup.IsLoadingRequested(ProcessModuleIndex, ref _temporaryLoadingLocations);
            _processGroup.IsUnloadingRequested(ProcessModuleIndex, ref _temporaryUnloadingLocations);

            //_temporaryLocations.AddRange(_temporaryLoadingLocations);
            //_temporaryLocations.AddRange(_temporaryUnloadingLocations);


            foreach (var item in RequestedLocation)
            {
                bool hasRequest = (_temporaryLoadingLocations.Contains(item.Key)
                    || _temporaryUnloadingLocations.Contains(item.Key));

                UpdateRequestedCellColor(item.Value, hasRequest);
            }




            //for (int i = 0; i < _temporaryLocations.Count; ++i)
            //{
            //    _temporaryLocation = _temporaryLocations[i];
            //    if (RequestedLocation.ContainsKey(_temporaryLocation))
            //    {
            //        _temporaryIndex = RequestedLocation[_temporaryLocation];
            //        gvReceivedRequest.Rows[_temporaryIndex].Cells[ColumnRequestEnabled].Style.BackColor = EnabledColor;
            //        gvReceivedRequest.Rows[_temporaryIndex].Cells[ColumnRequestEnabled].Style.SelectionBackColor = EnabledColor;
            //    }
            //    else
            //    {
            //        _temporaryIndex = RequestedLocation[_temporaryLocation];
            //        gvReceivedRequest.Rows[_temporaryIndex].Cells[ColumnRequestEnabled].Style.BackColor = DisabledColor;
            //        gvReceivedRequest.Rows[_temporaryIndex].Cells[ColumnRequestEnabled].Style.SelectionBackColor = DisabledColor;
            //    }
            //}

            #endregion </Requests>
        }
        #endregion </Methods>
    }
}
