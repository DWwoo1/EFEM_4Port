using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Sys3Controls;

using EFEM.Modules;
using EFEM.Defines.Common;
using EFEM.Defines.AtmRobot;
using EFEM.MaterialTracking.LocationServer;
using Define.DefineEnumProject.DigitalIO;

namespace FrameOfSystem3.Views.Setup.AtmRobot
{
    public partial class SetupSubViewAtmRobot : ParameterPanel
    {
        #region <Constructors>
        public SetupSubViewAtmRobot(int atmIndex)
        {
            InitializeComponent();
            InitializeComboBox();

            _selectionList = Functional.Form_SelectionList.GetInstance();
            var processModuleLocations = ProcessModules.GetProcessModuleLocations(0);
            
            for (int i = 0; i < _loadPortManager.Count; ++i)
            {
                LocationNames.Add(_loadPortManager.GetLoadPortName(i));
            }
            for (int i = 0; i < processModuleLocations.Length; ++i)
            {
                LocationNames.Add(processModuleLocations[i]);
            }
            _selectedLocationIndex = 0;

            _lLed.Add(led_UpperArm);
            _lLed.Add(led_LowerArm);
            _lLed.Add(led_DoorUnlocked);
            _lLed.Add(led_Servo);

            foreach (var item in _lLed)
            {
                item.ColorActive = cActivated;
                item.ColorNormal = cNormal;
                item.Active = false;
            }

            _locationServer = LocationServer.Instance;
            m_InstanceOfDigital = FrameOfSystem3.Config.ConfigDigitalIO.GetInstance();

            _myIndex = atmIndex;
            _robotNum = _myIndex + 1;
            this.Tag = string.Format("ATM ROBOT {0}", _myIndex + 1);

            for (int i = 1; i <= 25; i++)
            {
                cboxPnPSlot.Items.Add(i.ToString());
            }
            for (int i = 1; i <= 25; i++)
            {
                cboxGotoSlot.Items.Add(i.ToString());
            }

            labelSpeedLowValue.Text = "10";
            labelSpeedHighValue.Text = "10";
            cboxPnPSlot.SelectedIndex = 0;

            cboxGotoRadial.SelectedIndex = 1;
            cboxGotoArm.SelectedIndex = 0;
            cboxGotoSlot.SelectedIndex = 0;
            cboxGotoAction.SelectedIndex = 0;

            DoorLockOutput = (int)Define.DefineEnumProject.DigitalIO.EN_DIGITAL_OUT.EFEM_DOOR_OPEN_CLOSE;
            //DoorLockInput = (int)Define.DefineEnumProject.DigitalIO.EN_DIGITAL_IN.EFEM_DOOR_CLOSE;

            this.Dock = DockStyle.Fill;
        }
        #endregion </Constructors>

        #region <Const>
        //GridView 칼럼 인덱스 번호
        private const int HEIGHT_OF_ROWS = 30;
        private const int COLUMN_INDEX_OF_INDEX = 0;
        private const int COLUMN_INDEX_OF_NAME = 1;
        private const int COLUMN_INDEX_OF_PULSE = 2;
        private readonly string MIN_OF_PARAM = "0";
        private readonly string MAX_OF_PARAM = "100";
        private readonly int DoorLockOutput = -1;
        //private readonly int DoorLockInput = 0;
        private double resultLow;
        private double resultHigh;
        #endregion</Const>

        #region <Fields>

        #region Color
        private readonly Color cActivated = Color.LimeGreen;
        private readonly Color cNormal = Color.DimGray;

        private readonly Color cClrTrue = Color.DodgerBlue;
        private readonly Color cClrFalse = Color.White;

        private readonly Color cClrInputOn = Color.LimeGreen;
        private readonly Color cClrOutputOn = Color.Red;
        #endregion

        private RobotArmTypes _pnpSelectedArm = RobotArmTypes.UpperArm;
        private static Functional.Form_SelectionList _selectionList = null;
        private readonly PanelInterface _panelInstance = new PanelInterface();
        private readonly List<Sys3LedLabelWithText> _lLed = new List<Sys3LedLabelWithText>();
        private bool _ledBackupUpperArmStatus = false;
        private bool _ledBackupLowerArmStatus = false;
        private bool _ledBackupDoorUnlockedStatus = false;
        private bool _ledBackupServoStatus = false;

        private int _selectedLocationIndex = 0;
        private readonly List<string> LocationNames = new List<string>();

        private readonly int _myIndex;
        FrameOfSystem3.Config.ConfigDigitalIO m_InstanceOfDigital = null;
        private readonly AtmRobotManager _robotManager = AtmRobotManager.Instance;
        private readonly Functional.Form_MessageBox _messageBox = Functional.Form_MessageBox.GetInstance();

        private int RobotIndexCount;
        private int _robotNum;

        private static LoadPortManager _loadPortManager = LoadPortManager.Instance;
        private readonly ProcessModuleGroup ProcessModules = ProcessModuleGroup.Instance;
        private readonly Functional.Form_Calculator m_InstanceOfCalculator = Functional.Form_Calculator.GetInstance();

        private static LocationServer _locationServer = null;
        #endregion </Fields>

        #region <Types>
        enum RobotRadialTypes
        {
            Extend,
            Retract
        }
        enum RobotActionTypes
        {
            Down,
            Up,
        }
        #endregion </Types>

        #region <Properties>
        #endregion </Properties>

        #region <Methods>

        #region <Inhert Interfaces>
        public override void CallFunctionByTimer()
        {
            UpdateTotal();
            UpdateInputPulse(_myIndex);
        }
        protected override void ProcessWhenActivation()
        {
            InitializeList();
            UpdatePnpConfig();
        }
        #endregion </Inhert Interfaces>

        #region <Update UI>
        private void UpdatePnpConfig()
        {
            lblSelectedPnpArm.Text = _pnpSelectedArm.ToString();
            lblSelectedLocation.Text = LocationNames[_selectedLocationIndex];
        }
        private void UpdateUpperArmStatus()
        {
            _ledBackupUpperArmStatus = _robotManager.GetWaferPresenceUpperArm(_myIndex);
            if (_ledBackupUpperArmStatus != led_UpperArm.Active)
            {
                led_UpperArm.Active = _ledBackupUpperArmStatus;
            }
        }
        private void UpdateLowerArmStatus()
        {
            _ledBackupLowerArmStatus = _robotManager.GetWaferPresenceLowerArm(_myIndex);
            if (_ledBackupLowerArmStatus != led_LowerArm.Active)
            {
                led_LowerArm.Active = _ledBackupLowerArmStatus;
            }
        }
        private void UpdateDoorUnlockedStatus()
        {
            _ledBackupDoorUnlockedStatus = GetDoorState();
            if (_ledBackupDoorUnlockedStatus != led_DoorUnlocked.Active)
            {
                led_DoorUnlocked.Active = _ledBackupDoorUnlockedStatus;
            }
        }
        private void UpdateServoStatus()
        {
            _ledBackupServoStatus = GetServoState();
            if (_ledBackupServoStatus != led_Servo.Active)
            {
                led_Servo.Active = _ledBackupServoStatus;
            }
        }
        public void UpdateTotal()
        {
            UpdateUpperArmStatus();
            UpdateLowerArmStatus();
            UpdateDoorUnlockedStatus();
            UpdateServoStatus();
        }

        private void InitializeComboBox()
        {
            foreach (var RadialType in Enum.GetValues(typeof(RobotRadialTypes)))
            {
                cboxGotoRadial.Items.Add(RadialType);
            }
            foreach (var gotoarmType in Enum.GetValues(typeof(RobotArmTypes)))
            {
                cboxGotoArm.Items.Add(gotoarmType);
            }
            foreach (var gotoactionType in Enum.GetValues(typeof(RobotActionTypes)))
            {
                cboxGotoAction.Items.Add(gotoactionType);
            }

        }
        private void InitializeList()
        {
            RobotIndexCount = 0;
            //int[] arIndex = null;

            //if (false == m_InstanceOfDigital.GetListOfItem(true, ref arIndex))
            //{
            //    return;
            //}

            m_dgViewInputRobot1.Rows.Clear();

            for (int nIndex = (int)EN_DIGITAL_IN.ROBOT_LOWER_ARM_RETRACT;
                nIndex <= (int)EN_DIGITAL_IN.ROBOT_SERVO_ON_OFF_STATUS;
                ++nIndex)
            {
                AddItemOnList(true, nIndex/*arIndex[nIndex]*/);
            }
        }
        private void UpdateInputPulse(int robotIndex)
        {
            string strLpIndex = (robotIndex + 1).ToString();
            string strValue = string.Empty;
            //int[] arIndex = null;

            //if (false == m_InstanceOfDigital.GetListOfItem(true, ref arIndex))
            //{
            //    return;
            //}

            for (int nIndex = (int)EN_DIGITAL_IN.ROBOT_LOWER_ARM_RETRACT;
              nIndex <= (int)EN_DIGITAL_IN.ROBOT_SERVO_ON_OFF_STATUS;
              ++nIndex)
            {
                int rowIndex = nIndex - (int)EN_DIGITAL_IN.ROBOT_LOWER_ARM_RETRACT;
                if (m_dgViewInputRobot1.Rows.Count <= rowIndex)
                    continue;
                if (m_InstanceOfDigital.ReadItem(true, nIndex))
                {
                    m_dgViewInputRobot1.Rows[rowIndex].Cells[COLUMN_INDEX_OF_PULSE].Style.BackColor = cClrInputOn;
                    m_dgViewInputRobot1.Rows[rowIndex].Cells[COLUMN_INDEX_OF_PULSE].Style.SelectionBackColor = cClrInputOn;
                }
                else
                {
                    m_dgViewInputRobot1.Rows[rowIndex].Cells[COLUMN_INDEX_OF_PULSE].Style.BackColor = cClrFalse;
                    m_dgViewInputRobot1.Rows[rowIndex].Cells[COLUMN_INDEX_OF_PULSE].Style.SelectionBackColor = cClrFalse;
                }
            }
        }
        private void AddItemOnList(bool bInput, int nIndexOfItem)
        {

            System.Windows.Forms.DataGridView dgViewList = m_dgViewInputRobot1;

            int nIndexOfRow = dgViewList.Rows.Count;
            dgViewList.Rows.Add();

            string strValue = string.Empty;

            dgViewList.Rows[nIndexOfRow].Height = HEIGHT_OF_ROWS;

            dgViewList[COLUMN_INDEX_OF_INDEX, nIndexOfRow].Value = nIndexOfRow.ToString();
            dgViewList.Rows[nIndexOfRow].Cells[COLUMN_INDEX_OF_INDEX].Style.BackColor = Color.Silver;
            dgViewList.Rows[nIndexOfRow].Cells[COLUMN_INDEX_OF_INDEX].Style.SelectionBackColor = Color.Silver;
            dgViewList.Rows[nIndexOfRow].Cells[COLUMN_INDEX_OF_INDEX].Style.SelectionForeColor = Color.Black;

            m_InstanceOfDigital.GetParameter(bInput, nIndexOfItem, FrameOfSystem3.Config.ConfigDigitalIO.EN_PARAM_DIGITALIO.NAME, ref strValue);
            {
                dgViewList[COLUMN_INDEX_OF_NAME, RobotIndexCount].Value = strValue;
                RobotIndexCount++;
                dgViewList.Rows[nIndexOfRow].Selected = false;
            }
        }

        #endregion </Update UI>

        #region <UI Events>a
        private async void BtnCommandClickedPnP(object sender, EventArgs e)
        {
            if (false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.IDLE) &&
                false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.PAUSE))
                return;

            Component.CustomActionButton button = sender as Component.CustomActionButton;
            if (button == null)
                return;

            //if (false == Enum.TryParse(button.Tag.ToString(), out RobotCommands command))
            //    return;


            //var targetLocation = RobotLocationTypes.LP1_Foup; 

            #region Pick_Place Location

            RobotCommands command;
            if (sender.Equals(btnManualPick))
                command = RobotCommands.Pick;
            else if (sender.Equals(btnManualPlace))
                command = RobotCommands.Place;
            else
                return;

            int slot = cboxPnPSlot.SelectedIndex;
            Location targetLocation = null;
            if(LocationNames[_selectedLocationIndex].Contains("PM1"))
            {
                ProcessModuleLocation pmLocation = new ProcessModuleLocation("", "");
                string moduleName = ProcessModuleGroup.Instance.GetProcessModuleName(0);
                if (_locationServer.GetProcessModuleLocation(moduleName, LocationNames[_selectedLocationIndex], ref pmLocation))
                {
                    targetLocation = pmLocation;
                }
            }
            else
            {
                int portId = _loadPortManager.GetLoadPortPortId(LocationNames[_selectedLocationIndex]);
                LoadPortLocation lpLocation = new LoadPortLocation(portId, slot, "");
                if(_locationServer.GetLoadPortSlotLocation(portId, cboxPnPSlot.SelectedIndex, ref lpLocation))
                {
                    targetLocation = lpLocation;
                }

            }

            if (targetLocation == null)
                return;
            
            var armType = _pnpSelectedArm;

            if (_selectedLocationIndex <= 5)
            {
                //targetLocation = _loadPortManager.GetCurrentLocationName(_selectedLocationIndex);
            }
            else
            {
                slot = 0;
            }

            #endregion

            this.Enabled = false;
            var waitResponse = System.Threading.Tasks.Task.Run(() => ExecuteActionsAsync(command, armType, targetLocation));
            var result = await waitResponse;
            this.Enabled = true;

            string message = string.Format("Command : {0}\r\nResult : {1}", command.ToString(), result.ToString());
            _messageBox.ShowMessage(message);
        }

        //private async void BtnCommandClickedPnP(object sender, EventArgs e)
        //{
        //    if (false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.IDLE) &&
        //        false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.PAUSE))
        //        return;
        //    Component.CustomActionButton button = sender as Component.CustomActionButton;
        //    if (button == null)
        //        return;

        //    //if (false == Enum.TryParse(button.Tag.ToString(), out RobotCommands command))
        //    //    return;


        //    //var targetLocation = RobotLocationTypes.LP1_Foup; 

        //    #region Pick_Place Location

        //    // 연속 실행해야 하니 위치와 커맨드를 리스트로 제작
        //    List<RobotCommands> command = new List<RobotCommands>()
        //    {
        //        RobotCommands.Pick,
        //        RobotCommands.Place
        //    };

        //    List<string> locations = new List<string>();

        //    var loadPortLocation = cboxPnPLocation.SelectedItem.ToString();
        //    var pmLocation = button.Tag.ToString();
        //    var armType = (RobotArmTypes)cboxPnPArm.SelectedIndex;
        //    int slot = cboxPnPSlot.SelectedIndex + 1;

        //    if (button.Equals(btnCoreInLoad))
        //    {
        //        locations.Add(loadPortLocation);
        //        locations.Add(pmLocation);
        //    }
        //    else if (button.Equals(btnCoreInUnload))
        //    {
        //        locations.Add(pmLocation);
        //        locations.Add(loadPortLocation);
        //    }
        //    else if (button.Equals(btnCoreOutLoad))
        //    {
        //        locations.Add(loadPortLocation);
        //        locations.Add(pmLocation);
        //    }
        //    else if (button.Equals(btnCoreOutUnload))
        //    {
        //        locations.Add(pmLocation);
        //        locations.Add(loadPortLocation);
        //    }
        //    else if (button.Equals(btnSortingInLoad))
        //    {
        //        locations.Add(loadPortLocation);
        //        locations.Add(pmLocation);
        //    }
        //    else if (button.Equals(btnSortingInUnload))
        //    {
        //        locations.Add(pmLocation);
        //        locations.Add(loadPortLocation);
        //    }
        //    else if (button.Equals(btnSortingOutLoad))
        //    {
        //        locations.Add(loadPortLocation);
        //        locations.Add(pmLocation);
        //    }
        //    else if (button.Equals(btnSortingOutUnload))
        //    {
        //        locations.Add(pmLocation);
        //        locations.Add(loadPortLocation);
        //    }

        //    for (int i = 0; i < locations.Count; ++i)
        //    {
        //        if (false == _locationServer.CheckLocationValidity(locations[i]))
        //        {
        //            _messageBox.ShowMessage(string.Format("Invalid Location : {0}", locations[i]));
        //            return;
        //        }
        //    }
        //    #endregion

        //    this.Enabled = false;

        //    bool waferPresenceUpper = false;
        //    bool waferPresenceLower = false;
        //    var waitResponseStatus = System.Threading.Tasks.Task.Run(() => ExecuteStateAsync(RobotCommands.GetWaferPresence, ref waferPresenceUpper, ref waferPresenceLower));
        //    var resultStatus = await waitResponseStatus;

        //    bool passPickup = false;
        //    // 1. 자재가 있으면 Pick x : passPickup = true;
        //    //
        //    if (armType == RobotArmTypes.UpperArm && waferPresenceUpper)
        //    {
        //        passPickup = true;
        //    }
        //    else if (armType == RobotArmTypes.LowerArm && waferPresenceLower)
        //    {
        //        passPickup = true;
        //    }

        //    string message = string.Empty;
        //    for (int i = 0; i < command.Count; ++i)
        //    {
        //        if (command[i] == RobotCommands.Pick && passPickup)
        //            continue;

        //        var waitResponse = System.Threading.Tasks.Task.Run(() => ExecuteActionsAsync(command[i], armType, locations[i], slot));
        //        var result = await waitResponse;

        //        if (string.IsNullOrEmpty(message))
        //        {
        //            message = string.Format("Command : {0} => Result : {1}", command[i].ToString(), result.ToString());
        //        }
        //        else
        //        {
        //            message = string.Format("{0}\r\nCommand : {1} => Result : {2}", message, command[i].ToString(), result.ToString());
        //        }

        //        bool isError = false;
        //        switch (result)
        //        {
        //            case CommandResult.Proceed: // 올 일이 없다.
        //                break;
        //            case CommandResult.Completed:   // 실행 완료
        //                break;
        //            //case CommandResult.Timeout:
        //            //case CommandResult.Error:
        //            //case CommandResult.Invalid:
        //            default:
        //                isError = true;
        //                break;
        //        }
        //        if (isError)
        //        {
        //            break;
        //        }
        //    }
        //    this.Enabled = true;

        //    _messageBox.ShowMessage(message);
        //}
        private async void BtnCommandClickedGoto(object sender, EventArgs e)
        {
            if (false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.IDLE) &&
                false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.PAUSE))
                return;
            Component.CustomActionButton button = sender as Component.CustomActionButton;
            if (button == null)
                return;

            //if (false == Enum.TryParse(button.Tag.ToString(), out RobotCommands command))
            //    return;

            //var targetLocation = RobotLocationTypes.LP1_Foup; 

            #region GOTO Location

            RobotCommands command = RobotCommands.Idle;
            var armType = (RobotArmTypes)cboxGotoArm.SelectedIndex;
            int Slot = cboxGotoSlot.SelectedIndex + 1;
            var radial = (RobotRadialTypes)cboxGotoRadial.SelectedIndex;
            var action = (RobotActionTypes)cboxGotoAction.SelectedIndex;

            if (radial == RobotRadialTypes.Extend && action == RobotActionTypes.Down)
            {
                command = RobotCommands.MoveToPick;
            }
            else if (radial == RobotRadialTypes.Retract && action == RobotActionTypes.Down)
            {
                command = RobotCommands.ApproachForPick;
            }
            else if (radial == RobotRadialTypes.Extend && action == RobotActionTypes.Up)
            {
                command = RobotCommands.MoveToPlace;
            }
            else if (radial == RobotRadialTypes.Retract && action == RobotActionTypes.Up)
            {
                command = RobotCommands.ApproachForPlace;
            }

            int slot = cboxGotoSlot.SelectedIndex;
            Location targetLocation = null;
            string locationName = button.Tag.ToString();
            if(locationName.Contains("PM1"))
            {
                ProcessModuleLocation pmLocation = new ProcessModuleLocation("", "");
                string moduleName = ProcessModuleGroup.Instance.GetProcessModuleName(0);
                if (_locationServer.GetProcessModuleLocation(moduleName, locationName, ref pmLocation))
                {
                    targetLocation = pmLocation;
                }

            }
            else
            {
                int portId = _loadPortManager.GetLoadPortPortId(locationName);
                LoadPortLocation lpLocation = new LoadPortLocation(portId, slot, "");
                if (_locationServer.GetLoadPortSlotLocation(portId, cboxPnPSlot.SelectedIndex, ref lpLocation))
                {
                    targetLocation = lpLocation;
                }

            }
            

            if (command.Equals(RobotCommands.Idle))
                return;
            #endregion

            this.Enabled = false;
            var waitResponse = System.Threading.Tasks.Task.Run(() => ExecuteActionsAsync(command, armType, targetLocation));
            var result = await waitResponse;
            this.Enabled = true;

            string message = string.Format("Command : {0}\r\nResult : {1}", command.ToString(), result.ToString());
            _messageBox.ShowMessage(message);
        }
        private async void BtnCommandClickedGetSpeed(object sender, EventArgs e)
        {
            if (false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.IDLE) &&
                false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.PAUSE))
                return;
            Component.CustomActionButton button = sender as Component.CustomActionButton;
            if (button == null)
                return;

            RobotCommands command = RobotCommands.Idle;

            if (button.Name.Equals("btnSpeedHighGet"))
            {
                command = RobotCommands.GetHighSpeed;
            }
            else if (button.Name.Equals("btnSpeedLowGet"))
            {
                command = RobotCommands.GetLowSpeed;
            }

            var armType = (RobotArmTypes)cboxGotoArm.SelectedIndex;
            double speedValue = 0.0;

            var waitResponseSpeed = System.Threading.Tasks.Task.Run(() => ExecuteGetSpeedAsync(command, armType, ref speedValue));
            var resultSpeed = await waitResponseSpeed;

            this.Enabled = true;

            if (command.Equals(RobotCommands.GetHighSpeed))
            {
                labelSpeedHighValue.Text = speedValue.ToString();
            }
            else
            {
                labelSpeedLowValue.Text = speedValue.ToString();
            }

            string message = string.Format("Command : {0}\r\nResult : {1}", command.ToString(), resultSpeed.ToString());
            _messageBox.ShowMessage(message);
        }
        private async void BtnCommandClickedSetSpeed(object sender, EventArgs e)
        {
            if (false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.IDLE) &&
                false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.PAUSE))
                return;
            Component.CustomActionButton button = sender as Component.CustomActionButton;
            if (button == null)
                return;

            RobotCommands command = RobotCommands.Idle;

            double settingSpeed = 0.0;
            string textValue = string.Empty;
            if (button.Name.Equals("btnSpeedHighSet"))
            {
                command = RobotCommands.SetHighSpeed;
                textValue = labelSpeedHighValue.Text;
            }
            else if (button.Name.Equals("btnSpeedLowSet"))
            {
                command = RobotCommands.SetLowSpeed;

                textValue = labelSpeedLowValue.Text;
            }

            if (false == double.TryParse(textValue, out settingSpeed))
            {
                _messageBox.ShowMessage(string.Format("Invalid setting speed : {0}", textValue));
                return;
            }

            var armType = (RobotArmTypes)cboxGotoArm.SelectedIndex;
            
            var waitResponseSpeed = System.Threading.Tasks.Task.Run(() => ExecuteSetSpeedAsync(command, armType, settingSpeed));
            var resultSpeed = await waitResponseSpeed;

            this.Enabled = true;

            string message = string.Format("Command : {0}\r\nResult : {1}", command.ToString(), resultSpeed.ToString());
            _messageBox.ShowMessage(message);

        }
        private void BtnCommandClickedSpeedValue(object sender, EventArgs e)
        {
            if (false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.IDLE) &&
                false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.PAUSE))
                return;

            Control ctrl = sender as Control;
            string speedValue = string.Empty;

            switch (ctrl.TabIndex)
            {
                case 0:
                    string labelTextLow = labelSpeedLowValue.Text;

                    if (double.TryParse(labelTextLow, out resultLow))
                    {
                        if (m_InstanceOfCalculator.CreateForm(resultLow.ToString(), MIN_OF_PARAM, MAX_OF_PARAM))
                        {
                            m_InstanceOfCalculator.GetResult(ref speedValue);
                            labelSpeedLowValue.Text = speedValue;
                        }
                    }
                    break;

                case 1:
                    string labelTextHigh = labelSpeedHighValue.Text;

                    if (double.TryParse(labelTextHigh, out resultHigh))
                    {
                        if (m_InstanceOfCalculator.CreateForm(resultHigh.ToString(), MIN_OF_PARAM, MAX_OF_PARAM))
                        {
                            m_InstanceOfCalculator.GetResult(ref speedValue);
                            labelSpeedHighValue.Text = speedValue;
                        }
                    }

                    break;
                default:
                    break;
            }
        }
        private async void BtnCommandClickedGetWaferPresence(object sender, EventArgs e)
        {
            if (false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.IDLE) &&
                false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.PAUSE))
                return;
            Component.CustomActionButton button = sender as Component.CustomActionButton;
            if (button == null)
                return;

            RobotCommands command = RobotCommands.Idle;

            if (button.Name.Equals("btnArmGet"))
            {
                command = RobotCommands.GetWaferPresence;
            }

            bool waferPresenceUpper = false;
            bool waferPresenceLower = false;
            var waitResponsestate = System.Threading.Tasks.Task.Run(() => ExecuteStateAsync(command, ref waferPresenceUpper, ref waferPresenceLower));
            var resultstate = await waitResponsestate;
            

            this.Enabled = true;

            string message = string.Format("Command : {0}\r\nResult : {1}", command.ToString(), resultstate.ToString());
            _messageBox.ShowMessage(message);

        }
        private void BtnCommandClickedDoorLock(object sender, EventArgs e)
        {
            if (false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.IDLE) &&
                false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.PAUSE))
                return;

            if (sender.Equals(btnDoorLock))
            {
                SetDoorLock();
            }
            else if (sender.Equals(btnDoorUnlock))
            {
                SetDoorUnlock();
            }
        }
        private void BtnPnpConfigClicked(object sender, EventArgs e)
        {
            Sys3Label label = sender as Sys3Label;
            if (label == null)
                return;

            if (label.Equals(lblSelectedPnpArm))
            {
                if (_selectionList.CreateForm("Select Arm", Define.DefineEnumProject.SelectionList.EN_SELECTIONLIST.ARM_TYPE, _pnpSelectedArm.ToString()))
                {
                    string selectedArm = string.Empty;
                    _selectionList.GetResult(ref selectedArm);
                    if (Enum.TryParse(selectedArm, out _pnpSelectedArm))
                    {
                        UpdatePnpConfig();
                    }
                }
            }
            else if (label.Equals(lblSelectedLocation))
            {
                if (_selectionList.CreateForm("Select Location", LocationNames.ToArray(), LocationNames[_selectedLocationIndex]))
                {
                    _selectionList.GetResult(ref _selectedLocationIndex);
                    UpdatePnpConfig();
                }
            }
        }
        #endregion </UI Events>

        #region <Execute Actions>
        private CommandResult ExecuteActionsAsync(RobotCommands command, RobotArmTypes armType, Location location)
        {
            CommandResults result = new CommandResults(command.ToString(),CommandResult.Error);
            TickCounter_.TickCounter tick = new TickCounter_.TickCounter();

            // Action 관련 Flag 초기화
            // Action 실행전 Flag 초기화는 항상 이루어져야한다.
            _robotManager.InitAtmRobotAction(_myIndex);

            tick.SetTickCount(30000);
            while (true)
            {
                System.Threading.Thread.Sleep(100);

                if (tick.IsTickOver(true))
                {
                    result.ActionName = RobotCommands.Idle.ToString();
                    return CommandResult.Timeout;
                }

                result.CommandResult = CommandResult.Proceed;

                //if (false == result.Equals(EN_COMMAND_RESULT.PROCEED))
                //{
                //    return result;
                //}


                switch (command)
                {
                    case RobotCommands.Initialize:
                        result = _robotManager.InitializeAtmRobot(_myIndex);
                        break;
                    case RobotCommands.ApproachForPick:
                        result = _robotManager.ApproachForPick(_myIndex, armType, location);
                        break;
                    case RobotCommands.MoveToPick:
                        result = _robotManager.MoveToPick(_myIndex, armType, location);
                        break;
                    case RobotCommands.Pick:
                        result = _robotManager.Pick(_myIndex, armType, location, true);
                        break;
                    case RobotCommands.ApproachForPlace:
                        result = _robotManager.ApproachForPlace(_myIndex, armType, location);
                        break;
                    case RobotCommands.MoveToPlace:
                        result = _robotManager.MoveToPlace(_myIndex, armType, location);
                        break;
                    case RobotCommands.Place:
                        result = _robotManager.Place(_myIndex, armType, location, true);
                        break;
                    case RobotCommands.AmpOn:
                        result = _robotManager.ServoOn(_myIndex);
                        break;
                    case RobotCommands.AmpOff:
                        result = _robotManager.ServoOff(_myIndex);
                        break;
                    case RobotCommands.ClearError:
                        result = _robotManager.Clear(_myIndex);
                        break;
                    case RobotCommands.Grip:
                        result = _robotManager.Grip(_myIndex, armType);
                        break;
                    case RobotCommands.Ungrip:
                        result = _robotManager.Ungrip(_myIndex, armType);
                        break;
                    case RobotCommands.GetError:
                        break;
                    case RobotCommands.Hello:
                        break;
                    default:
                        return CommandResult.Invalid;
                }

                if (false == result.CommandResult.Equals(CommandResult.Proceed))
                {
                    return result.CommandResult;
                }
            }
        }
        private CommandResult ExecuteStateAsync(RobotCommands command, ref bool waferStateUpper, ref bool waferStateLower)
        {

            CommandResults result = new CommandResults(command.ToString(), CommandResult.Error);
            TickCounter_.TickCounter tick = new TickCounter_.TickCounter();

            // Action 관련 Flag 초기화
            // Action 실행전 Flag 초기화는 항상 이루어져야한다.
            _robotManager.InitAtmRobotAction(_myIndex);

            tick.SetTickCount(30000);
            while (true)
            {
                System.Threading.Thread.Sleep(100);

                if (tick.IsTickOver(true))
                {
                    result.ActionName = RobotCommands.Idle.ToString();
                    return CommandResult.Timeout;
                }

                result.CommandResult = CommandResult.Proceed;
                result = _robotManager.GetWaferPresence(_myIndex);
                
                if (false == result.CommandResult.Equals(CommandResult.Proceed))
                {
                    waferStateUpper = _robotManager.GetWaferPresenceUpperArm(_myIndex);
                    waferStateLower = _robotManager.GetWaferPresenceLowerArm(_myIndex);
                    return result.CommandResult;
                }
            }
        }
        private CommandResult ExecuteGetSpeedAsync(RobotCommands command, RobotArmTypes armtype, ref double speedValue)
        {

            CommandResults result = new CommandResults(command.ToString(), CommandResult.Error);
            TickCounter_.TickCounter tick = new TickCounter_.TickCounter();

            // Action 관련 Flag 초기화
            // Action 실행전 Flag 초기화는 항상 이루어져야한다.
            _robotManager.InitAtmRobotAction(_myIndex);

            tick.SetTickCount(30000);
            while (true)
            {
                System.Threading.Thread.Sleep(100);

                if (tick.IsTickOver(true))
                {
                    result.ActionName = RobotCommands.Idle.ToString();
                    return CommandResult.Timeout;
                }

                result.CommandResult = CommandResult.Proceed;

                //if (false == result.Equals(EN_COMMAND_RESULT.PROCEED))
                //{
                //    return result;
                //}
                double speed = 0.0;

                switch (command)
                {
                    case RobotCommands.GetHighSpeed:
                        result = _robotManager.GetHighSpeed(_myIndex, armtype, ref speed);
                        break;
                    case RobotCommands.GetLowSpeed:
                        result = _robotManager.GetLowSpeed(_myIndex, armtype, ref speed);
                        break;
                    default:
                        return CommandResult.Invalid;
                }

                if (false == result.CommandResult.Equals(CommandResult.Proceed))
                {
                    speedValue = speed;
                    return result.CommandResult;
                }
            }
        }

        private CommandResult ExecuteSetSpeedAsync(RobotCommands command, RobotArmTypes armtype, double settingSpeed)
        {

            CommandResults result = new CommandResults(command.ToString(), CommandResult.Error);
            TickCounter_.TickCounter tick = new TickCounter_.TickCounter();

            // Action 관련 Flag 초기화
            // Action 실행전 Flag 초기화는 항상 이루어져야한다.
            _robotManager.InitAtmRobotAction(_myIndex);

            tick.SetTickCount(30000);
            while (true)
            {
                System.Threading.Thread.Sleep(100);

                if (tick.IsTickOver(true))
                {
                    result.ActionName = RobotCommands.Idle.ToString();
                    return CommandResult.Timeout;
                }

                result.CommandResult = CommandResult.Proceed;

                switch (command)
                {
                    case RobotCommands.SetHighSpeed:
                        result = _robotManager.SetHighSpeed(_myIndex, armtype, settingSpeed);
                        break;
                    case RobotCommands.SetLowSpeed:
                        result = _robotManager.SetLowSpeed(_myIndex, armtype, settingSpeed);
                        break;
                    default:
                        return CommandResult.Invalid;
                }

                if (false == result.CommandResult.Equals(CommandResult.Proceed))
                {
                    return result.CommandResult;
                }
            }
        }
        private async void BtnCommandETC(object sender, EventArgs e)
        {
            if (false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.IDLE) &&
                false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.PAUSE))
                return;
            Component.CustomActionButton button = sender as Component.CustomActionButton;
            if (button == null)
                return;

            //if (false == Enum.TryParse(button.Tag.ToString(), out RobotCommands command))
            //    return;

           //var targetLocation = RobotLocationTypes.LP1_Foup; 

            #region GOTO Location

            RobotCommands command = RobotCommands.Idle;
            var armType = (RobotArmTypes)cboxGotoArm.SelectedIndex;
            int Slot = cboxGotoSlot.SelectedIndex + 1;
            var radial = (RobotRadialTypes)cboxGotoRadial.SelectedIndex;

            if (button.Equals(btnRobotInitialize))
            {
                command = RobotCommands.Initialize;
            }
            else if (button.Equals(btnRobotClear))
            {
                command = RobotCommands.ClearError;
            }
            else if (button.Equals(btnGrip))
            {
                command = RobotCommands.Grip;
            }
            else if (button.Equals(btnUngrip))
            {
                command = RobotCommands.Ungrip;
            }
            else if (button.Equals(btnRobotServoOn))
            {
                command = RobotCommands.AmpOn;
            }
            else if (button.Name.Equals("btnRobotServoOff"))
            {
                command = RobotCommands.AmpOff;
            }

            if (command.Equals(RobotCommands.Idle))
                return;
            #endregion

            this.Enabled = false;
            var waitResponse = System.Threading.Tasks.Task.Run(() => ExecuteActionsAsync(command, armType, null));
            var result = await waitResponse;
            this.Enabled = true;

            string message = string.Format("Command : {0}\r\nResult : {1}", command.ToString(), result.ToString());
            _messageBox.ShowMessage(message);
        }

        #endregion </Execute Actions>

        #region <ETC>
        private void SetDoorLock()
        {
            DigitalIO_.DigitalIO.GetInstance().WriteOutput(DoorLockOutput, true);
        }
        private void SetDoorUnlock()
        {
            DigitalIO_.DigitalIO.GetInstance().WriteOutput(DoorLockOutput, false);
        }
        private bool GetDoorState()
        {
            const int DoorClosedIndex = (int)Define.DefineEnumProject.DigitalIO.EN_DIGITAL_IN.EFEM_DOOR_CLOSE;

            return m_InstanceOfDigital.ReadItem(true, DoorClosedIndex);
        }
        private bool GetServoState()
        {
            const int ServoIndex = (int)Define.DefineEnumProject.DigitalIO.EN_DIGITAL_IN.ROBOT_SERVO_ON_OFF_STATUS;

            return m_InstanceOfDigital.ReadItem(true, ServoIndex);
        }
        #endregion </ETC>

        #endregion </Methods>   
    }
}
