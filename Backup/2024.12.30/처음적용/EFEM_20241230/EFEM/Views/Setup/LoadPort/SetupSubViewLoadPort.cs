using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;

using Sys3Controls;

using EFEM.Modules;
using EFEM.Defines.Common;
using EFEM.Defines.LoadPort;
using EFEM.MaterialTracking;
using Define.DefineEnumProject.DigitalIO;

namespace FrameOfSystem3.Views.Setup.LoadPort
{
    public partial class SetupSubViewLoadPort : ParameterPanel
    {
        #region <Constructors>
        public SetupSubViewLoadPort(int lpIndex)
        {
            InitializeComponent();

            MyIndex = lpIndex;
            PortId = MyIndex + 1;
            this.Tag = string.Format("LoadPort {0}", MyIndex + 1);

            m_tabOHT.ButtonClicked = true;
            m_tabLPM.ButtonClicked = false;

            m_InstanceOfDigital = FrameOfSystem3.Config.ConfigDigitalIO.GetInstance();

            //InitializeLoadPortMap();

            _lLed.Add(led_Present);
            _lLed.Add(led_Place);
            _lLed.Add(led_Clamped);
            _lLed.Add(led_Docked);
            _lLed.Add(led_Open);
            _lLed.Add(led_Foup);
            _lLed.Add(led_Cassette);

            foreach (var item in _lLed)
            {
                item.ColorActive = cActivated;
                item.ColorNormal = cNormal;
                item.Active = false;
            }

            //_backupSlotMap = new CarrierSlotMapStates[1];
            MappingDigitalData();

            _carrierMapManager.AssignMapControls(MyIndex, ref map_LoadPortMap);

            this.Dock = DockStyle.Fill;
        }
        #region <Types>
        enum EN_GRIDVIEW_LIST
        {
            OHT = 0,
            LPM,
        }
        #endregion </Types>

        #endregion </Constructors>

        #region <Const>
        //GridView 칼럼 인덱스 번호
        private const int COLUMN_INDEX_OF_INDEX = 0;
        private const int COLUMN_INDEX_OF_NAME = 1;
        private const int COLUMN_INDEX_OF_PULSE = 2;
        #endregion</Const>

        #region <Enums>
        private enum SelectedGridView
        {
            Parallel = 0,
            Digital
        }
        #endregion </Enums>

        #region <Fields>
        private static readonly FrameOfSystem3.Views.MapManager.CarrierMapManager _carrierMapManager = MapManager.CarrierMapManager.Instance;
        private static readonly FrameOfSystem3.Recipe.Recipe _recipe = FrameOfSystem3.Recipe.Recipe.GetInstance();
        #region Color
        //private readonly Color _temporaryColor;
        //private readonly Color cUndefined = Color.WhiteSmoke;
        //private readonly Color cEmpty = Color.DarkGray;
        //private readonly Color cNotempty = Color.Blue;
        //private readonly Color cOccupied = Color.LimeGreen;
        //private readonly Color cDoubleSlot = Color.DarkViolet;
        //private readonly Color cCrossSlot = Color.Brown;
        //private readonly Color cFailed = Color.Red;

        private readonly Color cActivated = Color.LimeGreen;
        private readonly Color cNormal = Color.DimGray;


        //private readonly Color cClrTrue = Color.DodgerBlue;
        private readonly Color cClrFalse = Color.White;

        private readonly Color cClrInputOn = Color.LimeGreen;
        //private readonly Color cClrOutputOn = Color.Red;
        #endregion

        //private Size sizePort = new Size(108, 18);
        //private Point pointPort = new Point(14, 23);

        private readonly List<Sys3LedLabelWithText> _lLed = new List<Sys3LedLabelWithText>();
        //private readonly PanelInterface _panelInstance = new PanelInterface();

        private readonly ReaderWriterLockSlim SlimLock = new ReaderWriterLockSlim();
        private readonly int MyIndex;
        readonly FrameOfSystem3.Config.ConfigDigitalIO m_InstanceOfDigital = null;
        private readonly LoadPortManager _loadPortManager = LoadPortManager.Instance;
        private readonly Functional.Form_MessageBox _messageBox = Functional.Form_MessageBox.GetInstance();
        private bool LedPresentStatus = false;
        private bool LedPlaceStatus = false;
        private bool LedClampStatus = false;
        private bool LedDockStatus = false;
        private bool LedDoorStatus = false;
        private bool LedFoupStatus = false;
        private bool LedCassetteStatus = false;
        private bool LedAutoStatus = false;             // 2024.11.13. by dwlim [ADD] ACCESS MODE 저장
        private bool LedManualStatus = false;           // 2024.11.13. by dwlim [ADD] ACCESS MODE 저장
        //private CarrierSlotMapStates[] _backupSlotMap = null;

        private int m_nSelectedTabIndex = (int)SelectedGridView.Parallel;
        private readonly int PortId;

        private ConcurrentDictionary<int, bool> _readingParallelInputs = new ConcurrentDictionary<int, bool>();
        private ConcurrentDictionary<int, bool> _readingParallelOutputs = new ConcurrentDictionary<int, bool>();
        private ConcurrentDictionary<int, bool> _readingDigitalInputs = new ConcurrentDictionary<int, bool>();
        private ConcurrentDictionary<int, bool> _readingDigitalOutputs = new ConcurrentDictionary<int, bool>();
        private const int CountParallelIO = 8;
        private const int CountDigitalInput = 8;
        private const int CountDigitalOutput = 2;

        //private Carrier _carrier = null;
        #endregion </Fields>

        #region <Properties>
        #endregion </Properties>

        #region <Methods>
        private void MappingDigitalData()
        {
            int offset = MyIndex * 8;
            int lpmOutputOffset = MyIndex * 2;
            int parallelInputIndex = (int)EN_DIGITAL_IN.LP1_PIO_VALID + offset;
            int digitalInputIndex = (int)EN_DIGITAL_IN.LP1_RUN + offset;
            int parallelOutputIndex = (int)EN_DIGITAL_OUT.LP1_PIO_L_REQ + offset;
            int digitalOutputIndex = (int)EN_DIGITAL_OUT.LP1_MANUAL_CASSETTE + lpmOutputOffset;

            {
                var temporaryDictionary = new Dictionary<int, bool>();
                for (int i = parallelInputIndex; i < parallelInputIndex + CountParallelIO; ++i)
                {
                    temporaryDictionary.Add(i, false);
                }
                _readingParallelInputs = new ConcurrentDictionary<int, bool>(temporaryDictionary.OrderBy(item => item.Key).ToDictionary(x => x.Key, x => x.Value));
            }

            {
                var temporaryDictionary = new Dictionary<int, bool>();
                for (int i = parallelOutputIndex; i < parallelOutputIndex + CountParallelIO; ++i)
                {
                    temporaryDictionary.Add(i, false);
                }
                _readingParallelOutputs = new ConcurrentDictionary<int, bool>(temporaryDictionary.OrderBy(item => item.Key).ToDictionary(x => x.Key, x => x.Value));
            }

            {
                var temporaryDictionary = new Dictionary<int, bool>();
                for (int i = digitalInputIndex; i < digitalInputIndex + CountDigitalInput; ++i)
                {
                    temporaryDictionary.Add(i, false);
                }
                _readingDigitalInputs = new ConcurrentDictionary<int, bool>(temporaryDictionary.OrderBy(item => item.Key).ToDictionary(x => x.Key, x => x.Value));
            }

            {
                var temporaryDictionary = new Dictionary<int, bool>();
                for (int i = digitalOutputIndex; i < digitalOutputIndex + CountDigitalOutput; ++i)
                {
                    temporaryDictionary.Add(i, false);
                }
                _readingDigitalOutputs = new ConcurrentDictionary<int, bool>(temporaryDictionary.OrderBy(item => item.Key).ToDictionary(x => x.Key, x => x.Value));
            }
        }
        
        public override void CallFunctionByTimer()
        {
            UpdateTotal();
            UpdateGridView();
            //UpdateOutputPulse();
        }
        //private void InitializeLoadPortMap()
        //{
        //    Sys3Map map_LoadPortMap = new Sys3Map
        //    {
        //        Size = sizePort,
        //        Location = pointPort,
        //        MapSize = default,
        //        BackColor = default
        //    };
        //}
        protected override void ProcessWhenActivation()
        {
            InitList();
            //InitializeList(true);
            //InitializeList(false);
        }
        private void InitList()
        {
            SlimLock.EnterWriteLock();

            toggleUseLoadPort.Active = _recipe.GetValue(FrameOfSystem3.Recipe.EN_RECIPE_TYPE.COMMON, "UseLoadPort_6", MyIndex, FrameOfSystem3.Recipe.EN_RECIPE_PARAM_TYPE.VALUE, true);

            int row = 0;
            string name = string.Empty;
            bool needRedraw = false;
            string removalString;
            if (m_nSelectedTabIndex == (int)SelectedGridView.Parallel)
            {
                if (m_dgViewInput.Rows.Count != _readingParallelInputs.Count)
                {
                    needRedraw = true;
                    m_dgViewInput.Rows.Clear();
                }

                foreach (var item in _readingParallelInputs.Keys)
                {
                    name = string.Empty;
                    if (false == m_InstanceOfDigital.GetParameter(true, item,
                        FrameOfSystem3.Config.ConfigDigitalIO.EN_PARAM_DIGITALIO.NAME, ref name))
                        continue;

                    if (needRedraw)
                    {
                        m_dgViewInput.Rows.Add();
                    }

                    removalString = string.Format("OHT_{0}_PIO_", PortId);
                    m_dgViewInput[COLUMN_INDEX_OF_INDEX, row].Value = item.ToString();
                    m_dgViewInput[COLUMN_INDEX_OF_NAME, row].Value = name.Replace(removalString, "");
                    m_dgViewInput.Rows[row].Selected = false;
                    ++row;
                }

                row = 0;
                needRedraw = false;
                if (m_dgViewOutput.Rows.Count != _readingParallelOutputs.Count)
                {
                    needRedraw = true;
                    m_dgViewOutput.Rows.Clear();
                }

                foreach (var item in _readingParallelOutputs.Keys)
                {
                    name = string.Empty;
                    if (false == m_InstanceOfDigital.GetParameter(false, item,
                        FrameOfSystem3.Config.ConfigDigitalIO.EN_PARAM_DIGITALIO.NAME, ref name))
                        continue;

                    if (needRedraw)
                    {
                        m_dgViewOutput.Rows.Add();
                    }

                    removalString = string.Format("OHT_{0}_PIO_", PortId);
                    m_dgViewOutput[COLUMN_INDEX_OF_INDEX, row].Value = item.ToString();
                    m_dgViewOutput[COLUMN_INDEX_OF_NAME, row].Value = name.Replace(removalString, "");
                    m_dgViewOutput.Rows[row].Selected = false;
                    ++row;
                }
            }
            else
            {
                row = 0;
                needRedraw = false;
                if (m_dgViewInput.Rows.Count != _readingDigitalInputs.Count)
                {
                    needRedraw = true;
                    m_dgViewInput.Rows.Clear();
                }

                foreach (var item in _readingDigitalInputs.Keys)
                {
                    name = string.Empty;
                    if (false == m_InstanceOfDigital.GetParameter(true, item,
                        FrameOfSystem3.Config.ConfigDigitalIO.EN_PARAM_DIGITALIO.NAME, ref name))
                        continue;

                    if (needRedraw)
                    {
                        m_dgViewInput.Rows.Add();
                    }

                    removalString = string.Format("LPM_{0}_", PortId);
                    m_dgViewInput[COLUMN_INDEX_OF_INDEX, row].Value = item.ToString();
                    m_dgViewInput[COLUMN_INDEX_OF_NAME, row].Value = name.Replace(removalString, "");
                    m_dgViewInput.Rows[row].Selected = false;
                    ++row;
                }

                row = 0;
                needRedraw = false;
                if (m_dgViewOutput.Rows.Count != _readingDigitalOutputs.Count)
                {
                    needRedraw = true;
                    m_dgViewOutput.Rows.Clear();
                }

                foreach (var item in _readingDigitalOutputs.Keys)
                {
                    name = string.Empty;
                    if (false == m_InstanceOfDigital.GetParameter(false, item,
                        FrameOfSystem3.Config.ConfigDigitalIO.EN_PARAM_DIGITALIO.NAME, ref name))
                        continue;

                    if (needRedraw)
                    {
                        m_dgViewOutput.Rows.Add();
                    }

                    removalString = string.Format("LPM_{0}_", PortId);
                    m_dgViewOutput[COLUMN_INDEX_OF_INDEX, row].Value = item.ToString();
                    m_dgViewOutput[COLUMN_INDEX_OF_NAME, row].Value = name.Replace(removalString, "");
                    m_dgViewOutput.Rows[row].Selected = false;
                    ++row;
                }
            }

            SlimLock.ExitWriteLock();
        }

        #region <Update Slot Map>
        #endregion

        #region <Update UI>
        private void UpdatePresentStatus()
        {
            LedPresentStatus = _loadPortManager.GetPresentState(MyIndex);
            if (LedPresentStatus != led_Present.Active)
            {
                led_Present.Active = LedPresentStatus;
            }
        }
        private void UpdatePlaceStatus()
        {
            LedPlaceStatus = _loadPortManager.GetPlacedState(MyIndex);
            if (LedPlaceStatus != led_Place.Active)
            {
                led_Place.Active = LedPlaceStatus;
            }
        }
        private void UpdateClampStatus()
        {
            LedClampStatus = _loadPortManager.GetClampingState(MyIndex);
            if (LedClampStatus != led_Clamped.Active)
            {
                led_Clamped.Active = LedClampStatus;
            }
        }
        private void UpdateDockStatus()
        {
            LedDockStatus = _loadPortManager.GetDockingState(MyIndex);
            if (LedDockStatus != led_Docked.Active)
            {
                led_Docked.Active = LedDockStatus;
            }
        }
        private void UpdateDoorStatus()
        {
            LedDoorStatus = _loadPortManager.GetDoorState(MyIndex);
            if (LedDoorStatus != led_Open.Active)
            {
                led_Open.Active = LedDoorStatus;
            }
        }
        private void UpdateFoupStatus()
        {
            LedFoupStatus = _loadPortManager.GetCarrierLoadingType(MyIndex).Equals(LoadPortLoadingMode.Foup);
            if (LedFoupStatus != led_Foup.Active)
            {
                led_Foup.Active = LedFoupStatus;
            }
        }
        private void UpdateCassetteStatus()
        {
            LedCassetteStatus = _loadPortManager.GetCarrierLoadingType(MyIndex).Equals(LoadPortLoadingMode.Cassette);
            if (LedCassetteStatus != led_Cassette.Active)
            {
                led_Cassette.Active = LedCassetteStatus;
            }
        }
        // 2024.11.13. by dwlim [ADD] ACCESS MODE 저장
        private void UpdateAutoStatus()
        {
            LedAutoStatus = _loadPortManager.GetAccessMode(MyIndex).Equals(LoadPortAccessMode.Auto);
            if (LedAutoStatus != led_Auto.Active)
            {
                led_Auto.Active = LedAutoStatus;
            }
        }
        private void UpdateManualStatus()
        {
            LedManualStatus = _loadPortManager.GetAccessMode(MyIndex).Equals(LoadPortAccessMode.Manual);
            if (LedManualStatus != led_Manual.Active)
            {
                led_Manual.Active = LedManualStatus;
            }
        }
        // 2024.11.13. by dwlim [END]
        // 2024.10.30. jhlim [MOD] Invoke 추가
        private delegate void OnUpdateSlotMap();
        private void UpdateSlotMap()
        {
            if (InvokeRequired)
            {
                OnUpdateSlotMap d = new OnUpdateSlotMap(UpdateSlotMap);
                BeginInvoke(d, null);
            }
            else
            {
                _carrierMapManager.UpdateControls(MyIndex);
            }
        }
        // 2024.10.30. jhlim [END]

        public void UpdateTotal()
        {
            UpdatePresentStatus();
            UpdatePlaceStatus();
            UpdateClampStatus();
            UpdateDockStatus();
            UpdateDoorStatus();
            UpdateFoupStatus();
            UpdateCassetteStatus();
            // 2024.11.13. by dwlim [ADD] ACCESS MODE 저장
            UpdateAutoStatus();
            UpdateManualStatus();
            // 2024.11.13. by dwlim [END]
            //_carrierMapManager.UpdateControls(MyIndex);
            UpdateSlotMap();
        }
        private void UpdateGridView()
        {
            SlimLock.EnterWriteLock();

            if (m_nSelectedTabIndex == (int)SelectedGridView.Parallel)
            {
                int rowIndex = 0;
                foreach (var item in _readingParallelInputs.Keys)
                {
                    bool result = m_InstanceOfDigital.ReadItem(true, item);
                    if (result != _readingParallelInputs[item])
                    {
                        _readingParallelInputs[item] = result;
                        if (result)
                        {
                            m_dgViewInput.Rows[rowIndex].Cells[COLUMN_INDEX_OF_PULSE].Style.BackColor = cClrInputOn;
                            m_dgViewInput.Rows[rowIndex].Cells[COLUMN_INDEX_OF_PULSE].Style.SelectionBackColor = cClrInputOn;
                        }
                        else
                        {
                            m_dgViewInput.Rows[rowIndex].Cells[COLUMN_INDEX_OF_PULSE].Style.BackColor = cClrFalse;
                            m_dgViewInput.Rows[rowIndex].Cells[COLUMN_INDEX_OF_PULSE].Style.SelectionBackColor = cClrFalse;
                        }
                    }
                    ++rowIndex;
                }

                rowIndex = 0;
                foreach (var item in _readingParallelOutputs.Keys)
                {
                    bool result = m_InstanceOfDigital.ReadItem(false, item);
                    if (result != _readingParallelOutputs[item])
                    {
                        _readingParallelOutputs[item] = result;
                        if (result)
                        {
                            m_dgViewOutput.Rows[rowIndex].Cells[COLUMN_INDEX_OF_PULSE].Style.BackColor = cClrInputOn;
                            m_dgViewOutput.Rows[rowIndex].Cells[COLUMN_INDEX_OF_PULSE].Style.SelectionBackColor = cClrInputOn;
                        }
                        else
                        {
                            m_dgViewOutput.Rows[rowIndex].Cells[COLUMN_INDEX_OF_PULSE].Style.BackColor = cClrFalse;
                            m_dgViewOutput.Rows[rowIndex].Cells[COLUMN_INDEX_OF_PULSE].Style.SelectionBackColor = cClrFalse;
                        }
                    }
                    ++rowIndex;
                }
            }
            else
            {
                int rowIndex = 0;
                foreach (var item in _readingDigitalInputs.Keys)
                {
                    bool result = m_InstanceOfDigital.ReadItem(true, item);
                    if (result != _readingDigitalInputs[item])
                    {
                        _readingDigitalInputs[item] = result;
                        if (result)
                        {
                            m_dgViewInput.Rows[rowIndex].Cells[COLUMN_INDEX_OF_PULSE].Style.BackColor = cClrInputOn;
                            m_dgViewInput.Rows[rowIndex].Cells[COLUMN_INDEX_OF_PULSE].Style.SelectionBackColor = cClrInputOn;
                        }
                        else
                        {
                            m_dgViewInput.Rows[rowIndex].Cells[COLUMN_INDEX_OF_PULSE].Style.BackColor = cClrFalse;
                            m_dgViewInput.Rows[rowIndex].Cells[COLUMN_INDEX_OF_PULSE].Style.SelectionBackColor = cClrFalse;
                        }
                    }

                    ++rowIndex;
                }

                rowIndex = 0;
                foreach (var item in _readingDigitalOutputs.Keys)
                {
                    bool result = m_InstanceOfDigital.ReadItem(false, item);
                    if (result != _readingDigitalOutputs[item])
                    {
                        _readingDigitalOutputs[item] = result;

                        if (result)
                        {
                            m_dgViewOutput.Rows[rowIndex].Cells[COLUMN_INDEX_OF_PULSE].Style.BackColor = cClrInputOn;
                            m_dgViewOutput.Rows[rowIndex].Cells[COLUMN_INDEX_OF_PULSE].Style.SelectionBackColor = cClrInputOn;
                        }
                        else
                        {
                            m_dgViewOutput.Rows[rowIndex].Cells[COLUMN_INDEX_OF_PULSE].Style.BackColor = cClrFalse;
                            m_dgViewOutput.Rows[rowIndex].Cells[COLUMN_INDEX_OF_PULSE].Style.SelectionBackColor = cClrFalse;
                        }
                    }

                    ++rowIndex;
                }
            }

            SlimLock.ExitWriteLock();
        }
        #endregion </Update UI>

        #region <UI Events>
        private void Click_TabButton(object sender, EventArgs e)
        {
            Control ctrl = sender as Control;

            switch (ctrl.TabIndex)
            {
                case 0: // Axis ADD
                    m_nSelectedTabIndex = 0;
                    m_tabOHT.ButtonClicked = true;
                    m_tabLPM.ButtonClicked = false;
                    break;

                case 1: // Axis ADD
                    m_nSelectedTabIndex = 1;
                    m_tabOHT.ButtonClicked = false;
                    m_tabLPM.ButtonClicked = true;
                    break;
            }

            InitList();
            //InitializeList(false);
            //InitializeList(true);
        }
        #endregion </UI Events>

        #region <Execute Actions>
        private CommandResults ExecuteActionsAsync(LoadPortCommands command)
        {
            CommandResults result = new CommandResults(command.ToString(), CommandResult.Error);
            TickCounter_.TickCounter tick = new TickCounter_.TickCounter();

            // Action 관련 Flag 초기화
            // Action 실행전 Flag 초기화는 항상 이루어져야한다.
            _loadPortManager.InitLoadPortAction(MyIndex);

            tick.SetTickCount(30000);
            while (true)
            {
                System.Threading.Thread.Sleep(100);

                if (tick.IsTickOver(true))
                {
                    result.CommandResult = CommandResult.Timeout;
                    return result;
                }

                result.CommandResult = CommandResult.Proceed;

                //if (false == result.Equals(EN_COMMAND_RESULT.PROCEED))
                //{
                //    return result;
                //}

                switch (command)
                {
                    case LoadPortCommands.Load:
                        result = _loadPortManager.LoadCarrierAtLoadPort(MyIndex);
                        break;
                    case LoadPortCommands.Unload:
                        result = _loadPortManager.UnloadCarrierAtLoadPort(MyIndex);
                        break;
                    case LoadPortCommands.Clamp:
                        result = _loadPortManager.ClampCarrierAtLoadPort(MyIndex);
                        break;
                    case LoadPortCommands.Unclamp:
                        result = _loadPortManager.ReleaseCarrierAtLoadPort(MyIndex);
                        break;
                    case LoadPortCommands.Dock:
                        result = _loadPortManager.DockCarrierAtLoadPort(MyIndex);
                        break;
                    case LoadPortCommands.Undock:
                        result = _loadPortManager.UnDockCarrierAtLoadPort(MyIndex);
                        break;
                    case LoadPortCommands.DoorOpen:
                        result = _loadPortManager.OpenCarrierAtLoadPort(MyIndex);
                        break;
                    case LoadPortCommands.DoorClose:
                        result = _loadPortManager.CloseCarrierAtLoadPort(MyIndex);
                        break;
                    case LoadPortCommands.Hello:
                        break;
                    case LoadPortCommands.Initialize:
                        result = _loadPortManager.InitializeLoadPort(MyIndex);
                        break;
                    case LoadPortCommands.Scan:
                        break;
                    case LoadPortCommands.ScanDown:
                        result = _loadPortManager.ScanCarrierAtLoadPort(MyIndex);
                        break;
                    case LoadPortCommands.GetMap:
                        result = _loadPortManager.GetMapCarrierAtLoadPort(MyIndex);
                        break;
                    case LoadPortCommands.ChangeToCassette:
                    case LoadPortCommands.ChangeToFoup:
                        {
                            LoadPortLoadingMode type;
                            if (command.Equals(LoadPortCommands.ChangeToCassette))
                                type = LoadPortLoadingMode.Cassette;
                            else
                                type = LoadPortLoadingMode.Foup;
                            result = _loadPortManager.ChangeLoadPortMode(MyIndex, type);
                        }
                        break;
                    // 2024.11.13. by dwlim [ADD] ACCESS MODE 저장
                    case LoadPortCommands.ChangeAccessModeToAuto:
                        result = _loadPortManager.ChangeLoadPortAccessModeToAuto(MyIndex);
                        break;
                    case LoadPortCommands.ChangeAccessModeToManual:
                        result = _loadPortManager.ChangeLoadPortAccessModeToManual(MyIndex);
                        break;
                    // 2024.11.13. by dwlim [END]
                    default:
                        {
                            result.CommandResult = CommandResult.Invalid;
                            result.Description = string.Format("Unknown Command {0}", command.ToString());
                        }
                        break;
                }

                if (false == result.CommandResult.Equals(CommandResult.Proceed))
                {
                    return result;
                }
            }
        }
        #endregion </Execute Actions>

        #endregion </Methods>

        private async void BtnCommandClicked(object sender, EventArgs e)
        {
            if (false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.IDLE) &&
                false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.PAUSE))
                return;
            
            if (!(sender is Component.CustomActionButton button))
                return;

            if (false == Enum.TryParse(button.Tag.ToString(), out LoadPortCommands command))
                return;

            this.Enabled = false;
            var waitResponse = System.Threading.Tasks.Task.Run(() => ExecuteActionsAsync(command));
            var result = await waitResponse;
            this.Enabled = true;

            string message = string.Format("Command : {0}\r\nResult : {1}", command.ToString(), result.CommandResult.ToString());
            if (false == result.CommandResult.Equals(CommandResult.Completed) &&
                false == result.CommandResult.Equals(CommandResult.Timeout))
            {
                message = string.Format("{0}\r\nDec : {1}", message, result.Description);
            }

            _messageBox.ShowMessage(message);
        }

        private void ToggleChanged(object sender, EventArgs e)
        {
            if (sender.Equals(toggleUseLoadPort))
            {
                //toggleUseLoadPort.Active;
                FrameOfSystem3.Recipe.PARAM_EQUIPMENT param = FrameOfSystem3.Recipe.PARAM_EQUIPMENT.UseLoadPort1 + MyIndex;
                bool enabled = toggleUseLoadPort.Active;
                _recipe.SetValue(FrameOfSystem3.Recipe.EN_RECIPE_TYPE.EQUIPMENT, param.ToString(), 0, FrameOfSystem3.Recipe.EN_RECIPE_PARAM_TYPE.VALUE, enabled.ToString());
                _loadPortManager.SetLoadPortEnabled(MyIndex, enabled);
            }
        }
    }
}
