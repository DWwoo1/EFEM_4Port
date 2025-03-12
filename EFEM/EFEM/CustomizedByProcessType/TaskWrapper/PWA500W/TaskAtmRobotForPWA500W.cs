using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TickCounter_;

using EFEM.Defines.Common;
using EFEM.Defines.AtmRobot;
using EFEM.Defines.LoadPort;
using EFEM.MaterialTracking.LocationServer;
using EFEM.MaterialTracking;
using EFEM.Defines.MaterialTracking;
using EFEM.CustomizedByProcessType.PWA500W;

using FrameOfSystem3.Recipe;
using FrameOfSystem3.SECSGEM.Scenario;

using Define.DefineEnumProject.Task.AtmRobot;
using FrameOfSystem3.SECSGEM.DefineSecsGem;

// ConfigTask에서 이 namespace를 가지고 클래스 타입을 가져오기 때문에 변경 불가
namespace FrameOfSystem3.Task
{
    class TaskAtmRobotForPWA500W : TaskAtmRobot
    {
        #region <Constructors>
        public TaskAtmRobotForPWA500W(int nIndexOfTask, string strTaskName)
            : base(nIndexOfTask, strTaskName, new TaskAtmRobotRecovery500W(strTaskName, nIndexOfTask))
        {
            Ticks = new TickCounter();
            ProcessModuleName = _processGroup.GetProcessModuleName(ProcessModuleIndex);
            _scenarioManager = ScenarioManagerForPWA500W_NRD.Instance;
        }
        #endregion </Constructors>

        #region <Fields>
        private CommandResults _commandResult = new CommandResults("", CommandResult.Invalid);
        private string _temporaryDescription = string.Empty;

        private readonly TickCounter Ticks = null;
        private const int ProcessModuleIndex = 0;
        private readonly string ProcessModuleName = string.Empty;

        // 시간 파라메터화가 필요한가?
        private const uint TimeoutShort = 30000;
        private const uint TimeoutLong = 60000;
        private CommandResults _result = new CommandResults("", CommandResult.Error);
        private int _subStepInterface;

        private const string ErrorDescriptionForInvalidSubstratePortInfo = "Invalid Substrate Port Info";
        private const string ErrorDescriptionForDoesntHaveCarrier = "Doesn't have carrier at loadport";
        private const string ErrorDescriptionForLoadPortNotEnabled = "Loadport is not enabled";
        private const string ErrorDescriptionForDoorIsNotOpened = "Carrier's door is not opened";
        private const string ErrorDescriptionForSlotIsFull = "All of the slot is full";

        private const string ErrorDescriptionForAssignSubstrateId = "Cannot getting a assigned substrate Id";
        private const string ErrorDescriptionForRequestPartId = "Cannot getting a assigned part Id";

        private string _newSubstrateId;
        private string _newPartId;
        private static ScenarioManagerForPWA500W_NRD _scenarioManager = null;
        #endregion </Fields>

        #region <Properties>
        #endregion </Properties>

        #region <Methods>

        #region <Overrids>

        #region <Input/Output>
        protected override bool GetBusySignalIndex(int index, ref int indexOfDigital)
        {
            indexOfDigital = (int)Define.DefineEnumProject.DigitalIO.PWA500W.EN_DIGITAL_IN.ROBOT_BUSY_STATUS;
            return true;
        }
        protected override bool GetAlarmSignalIndex(int index, ref int indexOfDigital)
        {
            indexOfDigital = (int)Define.DefineEnumProject.DigitalIO.PWA500W.EN_DIGITAL_IN.ROBOT_ALARM_STATUS;
            return true;
        }
        protected override bool GetServoSignalIndex(int index, ref int indexOfDigital)
        {
            indexOfDigital = (int)Define.DefineEnumProject.DigitalIO.PWA500W.EN_DIGITAL_IN.ROBOT_SERVO_ON_OFF_STATUS;
            return true;
        }
        #endregion </Input/Output>

        #region <Scenario>
        protected override void InitScenarioInfoPick()
        {
        }
        protected override bool UpdateParamToBeforePick()
        {
            return false;
        }
        protected override CommandResults ExecuteScenarioToBeforePick()
        {
            _commandResult.CommandResult = CommandResult.Completed;
            return _commandResult;
        }
        protected override bool UpdateParamToAfterPick()
        {
            return false;
        }
        protected override CommandResults ExecuteScenarioToAfterPick()
        {
            _commandResult.CommandResult = CommandResult.Completed;
            return _commandResult;
        }
        protected override void InitScenarioInfoPlace()
        {
        }
        protected override bool UpdateParamToBeforePlace()
        {
            return false;
        }
        protected override CommandResults ExecuteScenarioToBeforePlace()
        {
            _commandResult.CommandResult = CommandResult.Completed;
            return _commandResult;
        }
        protected override bool UpdateParamToAfterPlace()
        {
            return false;
        }
        protected override CommandResults ExecuteScenarioToAfterPlace()
        {
            _commandResult.CommandResult = CommandResult.Completed;
            return _commandResult;
        }
        #endregion </Scenario>

        #region <Material Handling With Process Module>

        private bool IsTickOver()
        {
            //return false;

            return Ticks.IsTickOver(false);
        }

        #region <Loading>
        protected override void InitMaterialHandlingInterface()
        {            
            _subStepInterface = 0;
        }

        protected override CommandResults IsApproachLoadingPrepared()
        {
            const string MethodName = "IsApproachLoadingPrepared";

            if (false == IsLoadingSignalStillActive(_workingInfo.Location.Name))
                return ReturnSkipped(MethodName);

            switch (_subStepInterface)
            {
                case 0:
                    {
                        //  1. Foup이 준비 되었는지 확인 후 준비 되지 않았으면 Skipped 리턴
                        //     준비되면 스메마 켜고 진행
                        bool prepared;
                        if (_workingInfo.Location is LoadPortLocation)
                        {
                            var lpLocation = _workingInfo.Location as LoadPortLocation;
                            prepared = _carrierServer.HasCarrier(lpLocation.PortId);
                            if (prepared)
                            {
                                // 스메마 켠다.
                                _processGroup.SetLoadingSignal(ProcessModuleIndex, _workingInfo.Location.Name, true);
                                ++_subStepInterface;
                            }
                            else
                            {
                                return ReturnSkipped(MethodName);
                            }
                        }
                        else if (_workingInfo.Location is ProcessModuleLocation)
                        {
                            // 스메마 켠다.
                            _processGroup.SetLoadingSignal(ProcessModuleIndex, _workingInfo.Location.Name, true);
                            ++_subStepInterface;
                        }
                        else
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_LOADING_DATA_INVALID, MethodName, _subStepInterface, "Location is invalid");
                        }
                    }
                    break;
                case 1:
                    {
                        // 2. AppreachLoading을 전송
                        if (false == _processGroup.SendMessage(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestApproachLoading.ToString(), _workingInfo.SubstrateName))
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_LOADING_SENDING_FAILED, MethodName,
                                _subStepInterface, RequestMessages.RequestApproachLoading.ToString());
                        }

                        Ticks.SetTickCount(TimeoutShort);
                        ++_subStepInterface;
                    }
                    break;
                case 2:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_LOADING_SENDING_COMPLETED_TIMEOUT_ACK, MethodName,
                                _subStepInterface, RequestMessages.RequestApproachLoading.ToString());
                        }

                        //  3. Ack 확인
                        var result = _processGroup.IsSendingCompleted(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestApproachLoading.ToString());
                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutLong);
                                ++_subStepInterface;
                                break;
                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_LOADING_SENDING_COMPLETED_BUT_NACK, MethodName,
                                    _subStepInterface, RequestMessages.RequestApproachLoading.ToString());

                            default:
                                break;
                        }

                    }
                    break;
                case 3:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_LOADING_RECEIVING_RESPONSE_MESSAGE_TIMEOUT, MethodName,
                                _subStepInterface, ResponseMessages.ResponseApproachLoading.ToString());
                        }

                        // 4. Response 확인
                        var result = _processGroup.IsMessageReceived(ProcessModuleIndex, _workingInfo.Location,
                            ResponseMessages.ResponseApproachLoading.ToString());
                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutShort);
                                ++_subStepInterface;
                                break;

                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                {
                                    return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_LOADING_RECEIVING_COMPLETED_BUT_ERROR, MethodName,
                                        _subStepInterface, ResponseMessages.ResponseApproachLoading.ToString());
                                }

                            default:
                                break;
                        }
                    }
                    break;

                case 4:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_BEFORE_LOADING_RECEIVING_RESPONSE_DATA_TIMEOUT, MethodName,
                                _subStepInterface, ResponseMessages.ResponseApproachLoading.ToString());
                        }

                        // 5. 데이터 확인
                        Dictionary<string, string> receivedData = new Dictionary<string, string>();
                        if (false == _processGroup.GetReceivedData(ProcessModuleIndex, _workingInfo.Location,
                            ResponseMessages.ResponseApproachLoading.ToString(), ref receivedData))
                            break;

                        // 6. Ack 전송 : 콜백에서 자동 Ack 나가니 현재 미구현
                        //if (false == _processGroup.SetAckReceivedMessage(ProcessModuleIndex, _workingInfo.Location,
                        //    ResponseMessages.ResponseApproachLoading.ToString(), CommunicationResult.Ack, string.Empty))
                        //{

                        //}

                        return ReturnCompleted();
                    }

                default:
                    break;
            }

            return ReturnProceed();
        }
        protected override CommandResults IsApproachLoadingCompleted()
        {
            const string MethodName = "IsApproachLoadingCompleted";

            // 상황이 바뀌었을 수 있다..
            if (false == IsLoadingSignalStillActive(_workingInfo.Location.Name))
                return ReturnSkipped(MethodName);

            return ReturnCompleted();
        }
        protected override CommandResults IsLoadingPrepared()
        {
            const string MethodName = "IsLoadingPrepared";

            // 상황이 바뀌었을 수 있다..
            if (false == IsLoadingSignalStillActive(_workingInfo.Location.Name))
                return ReturnSkipped(MethodName);

            switch (_subStepInterface)
            {
                case 0:
                    {
                        // 1. ActionLoading을 전송
                        if (false == _processGroup.SendMessage(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestActionLoading.ToString(), _workingInfo.SubstrateName))
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_ACTION_LOADING_SENDING_FAILED, MethodName,
                                _subStepInterface, RequestMessages.RequestActionLoading.ToString());
                        }

                        Ticks.SetTickCount(TimeoutShort);
                        ++_subStepInterface;
                    }
                    break;
                case 1:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_ACTION_LOADING_SENDING_COMPLETED_TIMEOUT_ACK, MethodName,
                                _subStepInterface, RequestMessages.RequestActionLoading.ToString());
                        }

                        //  2. Ack 확인
                        var result = _processGroup.IsSendingCompleted(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestActionLoading.ToString());
                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutLong);
                                ++_subStepInterface;
                                break;
                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                {
                                    return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_ACTION_LOADING_SENDING_COMPLETED_BUT_NACK, MethodName,
                                        _subStepInterface, RequestMessages.RequestActionLoading.ToString());
                                }
                            default:
                                break;
                        }

                    }
                    break;
                case 2:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_ACTION_LOADING_RECEIVING_RESPONSE_MESSAGE_TIMEOUT, MethodName,
                                _subStepInterface, ResponseMessages.ResponseActionLoading.ToString());
                        }

                        // 3. Response 확인
                        var result = _processGroup.IsMessageReceived(ProcessModuleIndex, _workingInfo.Location,
                            ResponseMessages.ResponseActionLoading.ToString());
                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutShort);
                                ++_subStepInterface;
                                break;

                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                {
                                    return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_ACTION_LOADING_RECEIVING_COMPLETED_BUT_ERROR, MethodName,
                                        _subStepInterface, ResponseMessages.ResponseActionLoading.ToString());
                                }

                            default:
                                break;
                        }
                    }
                    break;

                case 3:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_ACTION_LOADING_RECEIVING_RESPONSE_DATA_TIMEOUT, MethodName,
                                _subStepInterface, ResponseMessages.ResponseActionLoading.ToString());                            
                        }

                        // 4. 데이터 확인
                        Dictionary<string, string> receivedData = new Dictionary<string, string>();
                        if (false == _processGroup.GetReceivedData(ProcessModuleIndex, _workingInfo.Location,
                            ResponseMessages.ResponseActionLoading.ToString(), ref receivedData))
                            break;

                        // 6. Ack 전송 : 콜백에서 자동 Ack 나가니 현재 미구현
                        //if (false == _processGroup.SetAckReceivedMessage(ProcessModuleIndex, _workingInfo.Location,
                        //    ResponseMessages.ResponseActionLoading.ToString(), CommunicationResult.Ack, string.Empty))
                        //{

                        //}

                        return ReturnCompleted();
                    }

                default:
                    break;
            }

            return ReturnProceed();
        }
        protected override CommandResults IsLoadingCompleted()
        {
            const string MethodName = "IsLoadingCompleted";

            // 상황이 바뀌었을 수 있다..
            if (false == IsLoadingSignalStillActive(_workingInfo.Location.Name))
            {
                return ReturnSkipped(MethodName);
            }

            switch (_subStepInterface)
            {
                case 0:
                    {
                        // 1. ConfirmLoading 전송
                        if (false == _processGroup.SendMessage(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestConfirmLoading.ToString(), _workingInfo.SubstrateName))
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_AFTER_LOADING_SENDING_FAILED, MethodName,
                                _subStepInterface, RequestMessages.RequestConfirmLoading.ToString());
                        }

                            Ticks.SetTickCount(TimeoutShort);
                        ++_subStepInterface;
                    }
                    break;
                case 1:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_AFTER_LOADING_SENDING_COMPLETED_TIMEOUT_ACK, MethodName,
                                _subStepInterface, RequestMessages.RequestConfirmLoading.ToString());
                        }

                        //  2. Ack 확인
                        var result = _processGroup.IsSendingCompleted(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestConfirmLoading.ToString());
                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutLong);
                                ++_subStepInterface;
                                break;
                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                {
                                    return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_AFTER_LOADING_SENDING_COMPLETED_BUT_NACK, MethodName,
                                        _subStepInterface, RequestMessages.RequestConfirmLoading.ToString());
                                }
                            default:
                                break;
                        }

                    }
                    break;
                case 2:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_AFTER_LOADING_RECEIVING_RESPONSE_MESSAGE_TIMEOUT, MethodName,
                                _subStepInterface, ResponseMessages.ResponseConfirmLoading.ToString());
                        }

                        // 3. Response 확인
                        var result = _processGroup.IsMessageReceived(ProcessModuleIndex, _workingInfo.Location,
                            ResponseMessages.ResponseConfirmLoading.ToString());
                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutShort);
                                ++_subStepInterface;
                                break;

                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_AFTER_LOADING_RECEIVING_COMPLETED_BUT_ERROR, MethodName,
                                    _subStepInterface, ResponseMessages.ResponseConfirmLoading.ToString());

                            default:
                                break;
                        }
                    }
                    break;

                case 3:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_AFTER_LOADING_RECEIVING_RESPONSE_DATA_TIMEOUT, MethodName
                                , _subStepInterface, ResponseMessages.ResponseConfirmLoading.ToString());
                        }

                        // 4. 데이터 확인
                        Dictionary<string, string> receivedData = new Dictionary<string, string>();
                        if (false == _processGroup.GetReceivedData(ProcessModuleIndex, _workingInfo.Location,
                            ResponseMessages.ResponseConfirmLoading.ToString(), ref receivedData))
                            break;

                        // 5. Ack 전송 : 콜백에서 자동 Ack 나가니 현재 미구현
                        //if (false == _processGroup.SetAckReceivedMessage(ProcessModuleIndex, _workingInfo.Location,
                        //    ResponseMessages.ResponseConfirmLoading.ToString(), CommunicationResult.Ack, string.Empty))
                        //    return ReturnError(CommandResult.Error, MethodName,
                        //        _subStepInterface, string.Format("Response has failed : {0}", ResponseMessages.ResponseConfirmLoading.ToString()));

                        Ticks.SetTickCount(100);
                        ++_subStepInterface;
                    }
                    break;

                case 4:
                    {
                        if (false == IsTickOver())
                            break;

                        // 6. SMEMA OFF
                        _processGroup.SetLoadingSignal(ProcessModuleIndex, _workingInfo.Location.Name, false);

                        return ReturnCompleted();
                    }

                default:
                    break;
            }

            return ReturnProceed();
        }
        #endregion </Loading>

        #region <Unloading>
        private bool IsUnloadingSignalStillActive(string location)
        {
            // TODO :
            return true;

            //  1) PM의 스메마 확인 후 Off면 Skipped 리턴
            List<string> requestedLocation = new List<string>();
            if (false == _processGroup.IsUnloadingRequested(ProcessModuleIndex, ref requestedLocation))
                return false;

            if (false == requestedLocation.Contains(location))
                return false;

            return true;
        }

        private bool FindSubstrateByAttribute(string substrateName, string portId, string slot, ref Substrate substrate)
        {
            // 1. 해당 Substrate의 정보가 공정 설비에 존재하는 경우(정상)
            if (_substrateManager.GetSubstrateAtProcessModule(ProcessModuleName, substrateName, ref substrate))
                return true;

            // 2. 공정 설비에 있는 Substrate와 Source 정보들을 바탕으로 자재를 매칭(이름이 없고 포트번호, 슬롯번호가 존재하는 경우)
            #region <Find substrate by source info>
            List<Substrate> substrates = new List<Substrate>();
            if (false == _substrateManager.GetSubstratesAtProcessModule(ProcessModuleName, ref substrates))
                return false;

            substrate = null;
            for (int i = 0; i < substrates.Count; ++i)
            {
                if (substrates[i].GetSourcePortId().ToString().Equals(portId) && substrates[i].GetSourceSlot().ToString().Equals(slot))
                {
                    substrate = substrates[i];
                    break;
                }
                if (substrates[i].Equals(substrateName))
                {
                    substrate = substrates[i];
                    break;
                }
            }
            #endregion </Find substrate by source info>

            return substrate != null;
        }

        private bool FindPortInfo(int sourcePortId, SubstrateType substrateType, ref int targetPortId)
        {
            for (int i = 0; i < _loadPortManager.Count; ++i)
            {
                // 2024.09.03. jhlim [MOD] SubType을 UI에는 Center/Left/Right로 지정되도록 변경
                //FrameOfSystem3.Recipe.PARAM_EQUIPMENT paramName;
                //paramName = FrameOfSystem3.Recipe.PARAM_EQUIPMENT.LoadPortType1 + i;
                //string subTypeByRecipe = FrameOfSystem3.Recipe.Recipe.GetInstance().GetValue(FrameOfSystem3.Recipe.EN_RECIPE_TYPE.EQUIPMENT,
                //    paramName.ToString(),
                //    SubstrateType.Empty.ToString());

                //if (false == Enum.TryParse(subTypeByRecipe, out SubstrateType convertedSubType))
                //    continue;
                SubstrateType convertedSubType = _scenarioManager.GetSubstrateTypeByLoadPortIndex(i);
                // 2024.09.03. jhlim [END]

                if (false == substrateType.Equals(convertedSubType))
                    continue;
                if (substrateType == SubstrateType.Sort_12)
                {
                    if (FrameOfSystem3.Recipe.Recipe.GetInstance().GetValue(FrameOfSystem3.Recipe.EN_RECIPE_TYPE.COMMON, FrameOfSystem3.Recipe.PARAM_COMMON.UseCycleMode.ToString(), false))
                        substrateType = SubstrateType.Core_8;
                }

                if (substrateType == SubstrateType.Core_8 ||
                    substrateType == SubstrateType.Core_12 ||
                    substrateType == SubstrateType.Sort_12)
                {
                    if (sourcePortId > 0)
                    {
                        targetPortId = sourcePortId;
                        return true;
                    }
                }

                // 캐리어가 있고, Accessing 된 거만 찾는다.
                int temporaryPortId = _loadPortManager.GetLoadPortPortId(i);
                if (_carrierServer.HasCarrier(temporaryPortId))
                {
                    targetPortId = temporaryPortId;
                    return true;
                }
            }

            return false;
        }

        private bool CheckCarrierExistanceBySubstrateType(int sourcePortId, int sourceSlot, string substrateType, ref int targetPortId, ref int targetSlot, ref SubstrateType targetType)
        {
            if (false == Enum.TryParse(substrateType, out targetType))
                return false;

            bool result = FindPortInfo(sourcePortId, targetType, ref targetPortId);
            if (result)
            {
                switch (targetType)
                {
                    case SubstrateType.Core_8:
                    case SubstrateType.Core_12:
                        {
                            if (sourceSlot < 0)
                            {
                                targetSlot = -1;
                                int capacity = _carrierServer.GetCapacity(targetPortId);
                                for (int i = 0; i < capacity; ++i)
                                {
                                    if (false == _substrateManager.HasSubstrateAtLoadPort(targetPortId, i))
                                    {
                                        targetSlot = i;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                targetSlot = sourceSlot;
                            }

                            if (targetSlot < 0)
                                return false;

                            return _carrierServer.HasCarrier(targetPortId);
                        }
                    case SubstrateType.Sort_12:
                        {
                            // TODO : 하드코딩
                            if (Recipe.Recipe.GetInstance().GetValue(EN_RECIPE_TYPE.COMMON, PARAM_COMMON.UseCycleMode.ToString(),
                                false))
                            {
                                //targetPortId = 4;
                                targetType = SubstrateType.Core_8;
                            }

                            if (sourceSlot < 0)
                            {
                                targetSlot = -1;
                                int capacity = _carrierServer.GetCapacity(targetPortId);
                                for (int i = 0; i < capacity; ++i)
                                {
                                    if (false == _substrateManager.HasSubstrateAtLoadPort(targetPortId, i))
                                    {
                                        targetSlot = i;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                targetSlot = sourceSlot;
                            }

                            if (targetSlot < 0)
                                return false;

                            return _carrierServer.HasCarrier(targetPortId);
                        }

                    default:
                        return false;
                }
            }

            return false;
        }
        private bool CheckCarrierExistanceBySubstrateType2(int sourcePortId, string substrateType, ref int portId, ref SubstrateType targetType)
        {
            if (false == Enum.TryParse(substrateType, out targetType))
                return false;

            switch (targetType)
            {
                case SubstrateType.Core_8:
                case SubstrateType.Core_12:
                case SubstrateType.Sort_12:
                    portId = sourcePortId;
                    return _carrierServer.HasCarrier(sourcePortId);

                //case SubstrateType.Bin:
                //    {
                //        // TODO : 하드코딩
                //        if (Recipe.Recipe.GetInstance().GetValue(EN_RECIPE_TYPE.COMMON, PARAM_COMMON.UseCycleMode.ToString(),
                //            false))
                //        {
                //            portId = 4;
                //            targetType = SubstrateType.Core_8;
                //            return _carrierServer.HasCarrier(sourcePortId);
                //        }
                //        else
                //        {
                //            for (int i = 0; i < _loadPortManager.Count; ++i)
                //            {
                //                // 2024.09.03. jhlim [MOD] SubType을 UI에는 Center/Left/Right로 지정되도록 변경
                //                //FrameOfSystem3.Recipe.PARAM_EQUIPMENT paramName;
                //                //paramName = FrameOfSystem3.Recipe.PARAM_EQUIPMENT.LoadPortType1 + i;
                //                //string subTypeByRecipe = FrameOfSystem3.Recipe.Recipe.GetInstance().GetValue(FrameOfSystem3.Recipe.EN_RECIPE_TYPE.EQUIPMENT,
                //                //    paramName.ToString(),
                //                //    SubstrateType.Empty.ToString());

                //                //if (false == Enum.TryParse(subTypeByRecipe, out SubstrateType convertedSubType))
                //                //    continue;
                //                SubstrateType convertedSubType = _scenarioManager.GetSubstrateTypeByLoadPortIndex(i);
                //                // 2024.09.03. jhlim [END]

                //                if (false == targetType.Equals(convertedSubType))
                //                    continue;

                //                portId = _loadPortManager.GetLoadPortPortId(i);
                //                if (_carrierServer.HasCarrier(portId))
                //                    return true;
                //            }
                //        }

                //        return false;
                //    }

                default:
                    return false;
            }
        }
        private int FindPortIdBySubstrateType(int sourcePortId, string substrateType)
        {
            if (false == Enum.TryParse(substrateType, out SubstrateType targetType))
                return -1;

            switch (targetType)
            {
                case SubstrateType.Core_8:
                case SubstrateType.Core_12:
                case SubstrateType.Sort_12:
                    return sourcePortId;

                //case SubstrateType.Bin:
                //    {
                //        for (int i = 0; i < _loadPortManager.Count; ++i)
                //        {
                //            // 2024.09.03. jhlim [MOD] SubType을 UI에는 Center/Left/Right로 지정되도록 변경
                //            //FrameOfSystem3.Recipe.PARAM_EQUIPMENT paramName;
                //            //paramName = FrameOfSystem3.Recipe.PARAM_EQUIPMENT.LoadPortType1 + i;
                //            //string subTypeByRecipe = FrameOfSystem3.Recipe.Recipe.GetInstance().GetValue(FrameOfSystem3.Recipe.EN_RECIPE_TYPE.EQUIPMENT,
                //            //    paramName.ToString(),
                //            //    SubstrateType.Empty.ToString());

                //            //if (false == Enum.TryParse(subTypeByRecipe, out SubstrateType convertedSubType))
                //            //    continue;
                //            SubstrateType convertedSubType = _scenarioManager.GetSubstrateTypeByLoadPortIndex(i);
                //            // 2024.09.03. jhlim [END]

                //            if (false == targetType.Equals(convertedSubType))
                //                continue;

                //            return _loadPortManager.GetLoadPortPortId(i);
                //        }

                //        return -1;
                //    }

                default:
                    return -1;
            }
        }
        private bool FindWellknownProtInfoBySubstrateType(Substrate substrate, SubstrateType subType, ref int portId, ref int slot, ref string description)
        {
            description = string.Empty;
            portId = -1; slot = -1;
            // TODO : 2025.02.19. dwlim [MOD] Sort_12 Substrate Source와 Destination 다른 문제 수정했음
            portId = substrate.GetSourcePortId();
            slot = substrate.GetSourceSlot();
            return true;
            //switch (subType)
            //{
            //    case SubstrateType.Bin_12:
            //        {
            //            int lpIndex = -1;
            //            for (int i = 0; i < _loadPortManager.Count; ++i)
            //            {
            //                // 2024.09.03. jhlim [MOD] SubType을 UI에는 Center/Left/Right로 지정되도록 변경
            //                //FrameOfSystem3.Recipe.PARAM_EQUIPMENT paramName;
            //                //paramName = FrameOfSystem3.Recipe.PARAM_EQUIPMENT.LoadPortType1 + i;
            //                //string subTypeByRecipe = FrameOfSystem3.Recipe.Recipe.GetInstance().GetValue(FrameOfSystem3.Recipe.EN_RECIPE_TYPE.EQUIPMENT,
            //                //    paramName.ToString(),
            //                //    SubstrateType.Empty.ToString());

            //                //if (false == Enum.TryParse(subTypeByRecipe, out SubstrateType convertedSubType))
            //                //    continue;
            //                SubstrateType convertedSubType = _scenarioManager.GetSubstrateTypeByLoadPortIndex(i);
            //                // 2024.09.03. jhlim [END]

            //                if (false == subType.Equals(convertedSubType))
            //                    continue;

            //                lpIndex = i;
            //                break;
            //            }

            //            if (lpIndex < 0)
            //            {
            //                description = ErrorDescriptionForInvalidSubstratePortInfo;
            //                return false;
            //            }

            //            portId = _loadPortManager.GetLoadPortPortId(lpIndex);
            //            if (portId <= 0)
            //            {
            //                description = ErrorDescriptionForInvalidSubstratePortInfo;
            //                return false;
            //            }

            //            if (false == _carrierServer.HasCarrier(portId))
            //            {
            //                description = ErrorDescriptionForDoesntHaveCarrier;
            //                return false;
            //            }

            //            if (false == _loadPortManager.IsLoadPortEnabled(lpIndex))
            //            {
            //                description = ErrorDescriptionForLoadPortNotEnabled;
            //                return false;
            //            }

            //            if (false == _loadPortManager.GetDoorState(lpIndex))
            //            {
            //                description = ErrorDescriptionForDoorIsNotOpened;
            //                return false;
            //            }

            //            int capacity = _carrierServer.GetCapacity(portId);
            //            var substrates = _substrateManager.GetSubstratesAtLoadPort(portId);
            //            var loadingMode = _loadPortManager.GetCarrierLoadingType(lpIndex);
            //            for (int i = 0; i < capacity; ++i)
            //            {
            //                if (i == 0 && loadingMode.Equals(LoadPortLoadingMode.Cassette))
            //                    continue;

            //                if (false == substrates.ContainsKey(i))
            //                {
            //                    slot = i;
            //                    return true;
            //                }
            //            }

            //            description = ErrorDescriptionForSlotIsFull;
            //            return false;
            //        }

            //    default:
            //        portId = substrate.GetSourcePortId();
            //        slot = substrate.GetSourceSlot();
            //        return true;
            //}
        }

        // Type과 Sub정보를 이용해서 Port 번호를 받아온다.
        private int FindUnknownPortInfoBySubstrateType(Substrate substrate, SubstrateType subType)
        {
            switch (subType)
            {
                case SubstrateType.Core_8:
                    {
                        // Core Port는 두갠데...
                        for (int i = 0; i < _loadPortManager.Count; ++i)
                        {
                            if (i.Equals((int)LoadPortType.Core_8_1) || i.Equals((int)LoadPortType.Core_8_2))
                            {
                                int portId = _loadPortManager.GetLoadPortPortId(i);

                                if (_carrierServer.GetCarrierAccessingStatus(portId).Equals(CarrierAccessStates.InAccessed))
                                {
                                    return portId;
                                }
                            }
                        }
                        return -1;
                    }
                case SubstrateType.Core_12:
                    {
                        // Core Port는 두갠데...
                        for (int i = 0; i < _loadPortManager.Count; ++i)
                        {
                            if (i.Equals((int)LoadPortType.Core_12))
                            {
                                int portId = _loadPortManager.GetLoadPortPortId(i);

                                if (_carrierServer.GetCarrierAccessingStatus(portId).Equals(CarrierAccessStates.InAccessed))
                                {
                                    return portId;
                                }
                            }
                        }
                        return -1;
                    }
                case SubstrateType.Sort_12:
                    {
                        return FindDestinationPortBySubstrateType(substrate, subType);
                    }

                default:
                    return -1;
            }
        }
        // Type과 Sub정보를 이용해서 Slot 번호를 받아온다.
        private int FindUnknownSlotInfoByPortId(int portId, Substrate substrate, SubstrateType subType)
        {
            int capacity = _carrierServer.GetCapacity(portId);
            int lpIndex = _loadPortManager.GetLoadPortIndexByPortId(portId);

            switch (subType)
            {
                case SubstrateType.Core_8:
                case SubstrateType.Core_12:
                case SubstrateType.Sort_12:
                    {
                        bool notAvailableSlotFirst = (_loadPortManager.GetCarrierLoadingType(lpIndex).Equals(LoadPortLoadingMode.Cassette));
                        for (int i = 0; i < capacity; ++i)
                        {
                            if (notAvailableSlotFirst && i == 0)
                                continue;

                            if (_substrateManager.HasSubstrateAtLoadPort(portId, i))
                                continue;

                            if (false == _substrateManager.IsSourceSlotReserved(portId, i))
                            {
                                return i;
                            }
                        }

                        // 둘 데가 없다..
                        return -1;
                    }

                default:
                    return -1;
            }
        }

        private int FindDestinationPortBySubstrateType(Substrate substrate, SubstrateType subType)
        {
            if (subType.Equals(SubstrateType.Core_8) ||
                subType.Equals(SubstrateType.Core_12) ||
                subType.Equals(SubstrateType.Sort_12))
            {
                return substrate.GetSourcePortId();
            }

            //int portId = -1;
            for (int i = 0; i < _loadPortManager.Count; ++i)
            {
                // 2024.09.03. jhlim [MOD] SubType을 UI에는 Center/Left/Right로 지정되도록 변경
                //FrameOfSystem3.Recipe.PARAM_EQUIPMENT paramName;
                //paramName = FrameOfSystem3.Recipe.PARAM_EQUIPMENT.LoadPortType1 + i;
                //string subTypeByRecipe = FrameOfSystem3.Recipe.Recipe.GetInstance().GetValue(FrameOfSystem3.Recipe.EN_RECIPE_TYPE.EQUIPMENT,
                //    paramName.ToString(),
                //    SubstrateType.Empty.ToString());

                //if (false == Enum.TryParse(subTypeByRecipe, out SubstrateType convertedSubType))
                //    continue;
                SubstrateType convertedSubType = _scenarioManager.GetSubstrateTypeByLoadPortIndex(i);
                // 2024.09.03. jhlim [END]

                if (false == subType.Equals(convertedSubType))
                    continue;

                switch (convertedSubType)
                {
                    case SubstrateType.Sort_12:
                        return _loadPortManager.GetLoadPortPortId(i);

                    default:
                        break;
                }
            }

            return -1;
        }

        protected override CommandResults IsApproachUnloadingPrepared()
        {
            const string MethodName = "IsApproachUnloadingPrepared";

            // 상황이 바뀌었을 수 있다..
            if (false == IsUnloadingSignalStillActive(_workingInfo.Location.Name))
                return ReturnSkipped(MethodName);

            switch (_subStepInterface)
            {
                case 0:
                    {
                        // 1. ActionUnloading을 전송
                        if (false == _processGroup.SendMessage(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestApproachUnloading.ToString(), _workingInfo.SubstrateName))
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_SENDING_FAILED, MethodName,
                                _subStepInterface, RequestMessages.RequestApproachUnloading.ToString());
                        }

                        Ticks.SetTickCount(TimeoutShort);
                        ++_subStepInterface;
                    }
                    break;
                case 1:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_BEFORE_UNLOADING_SENDING_COMPLETED_TIMEOUT_ACK, MethodName,
                                _subStepInterface, RequestMessages.RequestApproachUnloading.ToString());
                        }

                        //  2. Ack 확인
                        var result = _processGroup.IsSendingCompleted(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestApproachUnloading.ToString());

                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutLong);
                                ++_subStepInterface;
                                break;
                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                {
                                    return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_SENDING_COMPLETED_BUT_NACK, MethodName,
                                        _subStepInterface, RequestMessages.RequestApproachUnloading.ToString());
                                }
                            default:
                                break;
                        }

                    }
                    break;
                case 2:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_RESPONSE_MESSAGE_TIMEOUT, MethodName,
                                _subStepInterface, ResponseMessages.ResponseApproachUnloading.ToString());
                        }

                        // 3. Response 확인
                        var result = _processGroup.IsMessageReceived(ProcessModuleIndex, _workingInfo.Location,
                            ResponseMessages.ResponseApproachUnloading.ToString());
                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutShort);
                                ++_subStepInterface;
                                break;

                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_COMPLETED_BUT_ERROR, MethodName,
                                    _subStepInterface, ResponseMessages.ResponseApproachUnloading.ToString());

                            default:
                                break;
                        }
                    }
                    break;

                case 3:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_RESPONSE_DATA_TIMEOUT, MethodName,
                                _subStepInterface, ResponseMessages.ResponseApproachUnloading.ToString());
                        }

                        // 4. 데이터 확인
                        Dictionary<string, string> receivedData = new Dictionary<string, string>();
                        if (false == _processGroup.GetReceivedData(ProcessModuleIndex, _workingInfo.Location,
                            ResponseMessages.ResponseApproachUnloading.ToString(), ref receivedData))
                            break;

                        #region <Data 확인>
                        Substrate substrate = new Substrate("");
                        SubstrateType convertedType;
                        string substrateName, lotId, recipeId, ringId, portId, slot, subType;
                        if (false == _processGroup.IsSimulationMode(ProcessModuleIndex))
                        {
                            if (false == receivedData.TryGetValue(PWA500WSubstrateAttributes.SubstrateName, out substrateName))
                            {
                                return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_COMPLETED_BUT_DATA_INVALID, MethodName,
                                    _subStepInterface, string.Format("{0} : {1}",
                                    ResponseMessages.ResponseApproachUnloading.ToString(), PWA500WSubstrateAttributes.SubstrateName));
                            }

                            if (false == receivedData.TryGetValue(PWA500WSubstrateAttributes.LotId, out lotId))
                            {
                                return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_COMPLETED_BUT_DATA_INVALID, MethodName,
                                    _subStepInterface, string.Format("{0} : {1}",
                                    ResponseMessages.ResponseApproachUnloading.ToString(), PWA500WSubstrateAttributes.LotId));
                            }

                            if (false == receivedData.TryGetValue(PWA500WSubstrateAttributes.RecipeId, out recipeId))
                            {
                                return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_COMPLETED_BUT_DATA_INVALID, MethodName,
                                    _subStepInterface, string.Format("{0} : {1}",
                                    ResponseMessages.ResponseApproachUnloading.ToString(), PWA500WSubstrateAttributes.RecipeId));
                            }

                            if (false == receivedData.TryGetValue(PWA500WSubstrateAttributes.RingId, out ringId))
                            {
                                return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_COMPLETED_BUT_DATA_INVALID, MethodName,
                                    _subStepInterface, string.Format("{0} : {1}",
                                    ResponseMessages.ResponseApproachUnloading.ToString(), PWA500WSubstrateAttributes.RingId));
                            }

                            if (false == receivedData.TryGetValue(PWA500WSubstrateAttributes.PortId, out portId))
                            {
                                return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_COMPLETED_BUT_DATA_INVALID, MethodName,
                                    _subStepInterface, string.Format("{0} : {1}",
                                    ResponseMessages.ResponseApproachUnloading.ToString(), PWA500WSubstrateAttributes.PortId));
                            }

                            if (false == receivedData.TryGetValue(PWA500WSubstrateAttributes.SlotId, out slot))
                            {
                                return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_COMPLETED_BUT_DATA_INVALID, MethodName,
                                    _subStepInterface, string.Format("{0} : {1}",
                                    ResponseMessages.ResponseApproachUnloading.ToString(), PWA500WSubstrateAttributes.SlotId));
                            }

                            // TODO : 
                            if (false == receivedData.TryGetValue(PWA500WSubstrateAttributes.SubstrateType, out subType))
                            {
                                return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_COMPLETED_BUT_DATA_INVALID, MethodName,
                                    _subStepInterface, string.Format("{0} : {1}",
                                    ResponseMessages.ResponseApproachUnloading.ToString(), PWA500WSubstrateAttributes.SubstrateType));
                            }

                            if (subType == "Core")
                            {
                                convertedType = SubstrateType.Core_12;
                            }
                            else
                            {
                                convertedType = SubstrateType.Sort_12;
                            }

                            //if (false == receivedData.TryGetValue(PWA500WSubstrateAttributes.SubstrateType, out subType) ||
                            //    false == Enum.TryParse(subType, out convertedType))
                            //{
                            //    return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_COMPLETED_BUT_DATA_INVALID, MethodName,
                            //        _subStepInterface, string.Format("{0} : {1}",
                            //        ResponseMessages.ResponseApproachUnloading.ToString(), PWA500WSubstrateAttributes.SubstrateType));
                            //}
                        }
                        else
                        {
                            var processModuleName = _processGroup.GetProcessModuleName(ProcessModuleIndex);

                            List<string> requestedLocation = new List<string>();
                            _processGroup.IsUnloadingRequested(ProcessModuleIndex, ref requestedLocation);
                            // 2025.02.24. dwlim [MOD] Simulation Mode 수정 => Sort 1장 완료하려면 Core 2장 완료해야함
                            var substrates = new List<Substrate>();
                            var unloadingSubstrates = new List<Substrate>();
                            _substrateManager.GetSubstratesAtProcessModule(processModuleName, ref substrates);

                            bool existUnloadingCore = false;
                            if (requestedLocation.Count != 0)
                            {
                                for (int i = 0; i < requestedLocation.Count; i++)
                                {
                                    if (false == requestedLocation[i].Contains(SubstrateType.Core_8.ToString()))
                                        continue;
                                    foreach (var item in substrates)
                                    {
                                        if (item.GetLocation().Name.Contains(SubstrateType.Core_8.ToString()))
                                        {
                                            if (false == item.GetProcessingStatus().Equals(ProcessingStates.Processed))
                                                continue;

                                            unloadingSubstrates.Add(item);
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    if (unloadingSubstrates.Count == 0)
                                        break;

                                    substrate = unloadingSubstrates.First();
                                    existUnloadingCore = true;
                                }
                                if (false == existUnloadingCore)
                                {
                                    foreach (var item in substrates)
                                    {
                                        if (item.GetLocation().Name.Contains(SubstrateType.Sort_12.ToString()))
                                        {
                                            if (false == item.GetProcessingStatus().Equals(ProcessingStates.Processed))
                                                continue;

                                            unloadingSubstrates.Add(item);
                                        }
                                    }
                                    if (unloadingSubstrates.Count == 0)
                                        break;

                                    substrate = unloadingSubstrates.First();
                                }
                            }
                            // 2025.02.24. dwlim [End]
                            substrateName = substrate.GetName();
                            lotId = substrate.GetLotId();
                            recipeId = substrate.GetRecipeId();
                            portId = substrate.GetSourcePortId().ToString();
                            slot = substrate.GetSourceSlot().ToString();
                            subType = substrate.GetAttribute(PWA500WSubstrateAttributes.SubstrateType);
                            Enum.TryParse(subType, out convertedType);
                            switch (convertedType)
                            {
                                case SubstrateType.Core_8:
                                    convertedType = SubstrateType.Core_8;
                                    break;
                                case SubstrateType.Core_12:
                                    convertedType = SubstrateType.Core_12;
                                    break;
                                default:
                                    convertedType = SubstrateType.Sort_12;
                                    break;
                            }
                            subType = convertedType.ToString();
                            ringId = substrate.GetAttribute(PWA500WSubstrateAttributes.RingId);
                        }
                        bool hasCarrier = false;

                        if (false == FindSubstrateByAttribute(substrateName, portId, slot, ref substrate))
                        {
                            string substrateTemporaryName;
                            if (string.IsNullOrEmpty(ringId) && string.IsNullOrEmpty(substrateName))
                                substrateTemporaryName = "Unknown";
                            else if (string.IsNullOrEmpty(substrateName))
                                substrateTemporaryName = ringId;
                            else
                                substrateTemporaryName = substrateName;

                            _substrateManager.CreateSubstrate(substrateTemporaryName, _workingInfo.Location);

                            substrate = new Substrate("");
                            bool result = false;

                            if (_workingInfo.Location is ProcessModuleLocation pmLocation)
                            {
                                result = _substrateManager.GetSubstrateAtProcessModule(substrateTemporaryName, pmLocation, ref substrate);
                            }

                            if (result)
                            {
                                var attr = substrate.GetAttributesAll();
                                string[] keys = attr.Keys.ToArray();
                                for (int i = 0; i < keys.Length; ++i)
                                {
                                    if (attr[keys[i]] == null)
                                    {
                                        substrate.SetAttribute(keys[i], string.Empty);
                                    }
                                }

                                substrate.SetAttribute(BaseSubstrateAttributeKeys.Name, substrateTemporaryName);
                                substrate.SetAttribute(PWA500WSubstrateAttributes.RingId, substrateTemporaryName);
                                substrate.SetAttribute(BaseSubstrateAttributeKeys.ProcessingState, ProcessingStates.Processed.ToString());
                                substrate.SetAttribute(BaseSubstrateAttributeKeys.TransPortState, SubstrateTransferStates.AtWork.ToString());
                                substrate.SetAttribute(PWA500WSubstrateAttributes.SubstrateType, subType);
                                substrate.SetAttribute(BaseSubstrateAttributeKeys.LotId, lotId);
                                substrate.SetAttribute(PWA500WSubstrateAttributes.ParentLotId, lotId);
                                substrate.SetAttribute(BaseSubstrateAttributeKeys.RecipeId, recipeId);

                                int port = FindUnknownPortInfoBySubstrateType(substrate, convertedType);
                                int slotIndex = FindUnknownSlotInfoByPortId(port, substrate, convertedType);

                                substrate.SetAttribute(BaseSubstrateAttributeKeys.SourcePortId, port.ToString());
                                substrate.SetAttribute(BaseSubstrateAttributeKeys.SourceSlot, slotIndex.ToString());
                                substrate.SetAttribute(BaseSubstrateAttributeKeys.DestinationPortId, port.ToString());
                                substrate.SetAttribute(BaseSubstrateAttributeKeys.DestinationSlot, slotIndex.ToString());
                                substrateName = substrateTemporaryName;

                                _processGroup.AssignSubstrate(_workingInfo.Location.Name, substrate);

                            }
                            else
                            {
                                return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_COMPLETED_BUT_DATA_INVALID, MethodName,
                                    _subStepInterface, string.Format("Find substrate has failed by Receiving data : {0}, [{1},{2},{3}]", ResponseMessages.ResponseApproachUnloading.ToString(),
                                    ringId, portId, slot));
                            }
                        }

                        // 찾았으면 Set
                        substrate.SetName(substrateName);
                        //substrate.SetLotId(lotId);
                        substrate.SetRecipeId(recipeId);

                        if (false == int.TryParse(portId, out int sourcePortId))
                            sourcePortId = -1;
                        substrate.SetSourcePortId(sourcePortId);

                        if (false == int.TryParse(slot, out int sourceSlot))
                            sourceSlot = -1;
                        substrate.SetSourceSlot(sourceSlot);

                        substrate.SetProcessingStatus(ProcessingStates.Processed);
                        substrate.SetAttribute(PWA500WSubstrateAttributes.SubstrateType, convertedType.ToString());

                        //if (sourcePortId > 0)
                        //{
                            int targetPortId = 0, targetSlot = 0;
                            string description = string.Empty;
                            // 2024.08.22. jhlim [MOD] 공정설비에서 자재를 찾도록 수정
                            hasCarrier = FindWellknownProtInfoBySubstrateType(substrate, convertedType, ref targetPortId, ref targetSlot, ref description);
                            if (hasCarrier)
                            {
                                substrate.SetDestinationPortId(targetPortId);
                                substrate.SetDestinationSlot(targetSlot);
                            }

                            // 2024.08.22. jhlim [END]
                            //hasCarrier = CheckCarrierExistanceBySubstrateType(_substrateToPick.GetSourcePortId(), _substrateToPick.GetSourceSlot(), subType, ref targetPortId, ref targetSlot, ref convertedType);
                            //if (targetPortId > 0)
                            //{
                            //    _substrateToPick.SetProcessingStatus(ProcessingStates.Processed);
                            //    _substrateToPick.SetDestinationPortId(targetPortId);
                            //    _substrateToPick.SetDestinationSlot(targetSlot);
                            //    switch (convertedType)
                            //    {
                            //        case SubstrateType.Bin1:
                            //        case SubstrateType.Bin2:
                            //        case SubstrateType.Bin3:
                            //            {
                            //                _substrateToPick.SetDestinationPortId(targetPortId);
                            //                _substrateToPick.SetDestinationSlot(targetSlot);
                            //                _substrateToPick.SetAttribute(PWA500BINSubstrateAttributes.SubstrateType, convertedType.ToString());
                            //            }
                            //            break;

                            //        default:
                            //            break;
                            //    }
                            //}
                        //}

                        _workingInfo.SubstrateName = substrate.GetName();
                        //if (ManualWorkingInfo.Count > 0)
                        //{
                        //    ManualWorkingInfo.First().Value.SubstrateName = substrate.GetName();
                        //}
                        #endregion </Data 확인>

                        // 5. Ack 전송 : 콜백에서 자동 Ack 나가니 현재 미구현
                        //if (false == _processGroup.SetAckReceivedMessage(ProcessModuleIndex, _workingInfo.Location,
                        //    ResponseMessages.ResponseApproachUnloading.ToString(), CommunicationResult.Ack, string.Empty))
                        //{

                        //}

                        string handlingResult = hasCarrier ? PWA500WSubstrateAttributes.HandlingResultOk : PWA500WSubstrateAttributes.HandlingResultNg;
                        receivedData[PWA500WSubstrateAttributes.HandlingResult] = handlingResult;

                        if (false == _processGroup.SendMessage(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestStartUnloading.ToString(), receivedData))
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_SENDING_FAILED, MethodName,
                                _subStepInterface, RequestMessages.RequestStartUnloading.ToString());
                        }

                        if (sourcePortId > 0 && false == hasCarrier)
                        {
                            int lpIndex = _loadPortManager.GetLoadPortIndexByPortId(sourcePortId);
                            if (description.Equals(ErrorDescriptionForDoesntHaveCarrier))
                            {
                                // Carrier가 없고, Auto면 올 때까지 대기해야 하므로 스킵
                                if (_loadPortManager.GetAccessMode(lpIndex).Equals(LoadPortAccessMode.Auto))
                                {
                                    return ReturnSkipped(MethodName);
                                }
                                else
                                {
                                    return ReturnToError(CommandResult.Error, EN_ALARM.ATM_ROBOT_DOES_NOT_HAVE_CARRIER, MethodName, _subStepInterface, description);
                                }
                            }
                            else
                            {
                                return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_COMPLETED_BUT_DATA_INVALID, MethodName,
                                    _subStepInterface, description);
                            }                            
                            //return ReturnSkipped(MethodName, description);
                        }

                        Ticks.SetTickCount(TimeoutShort);
                        ++_subStepInterface;
                    }
                    break;

                case 4:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_BEFORE_UNLOADING_SENDING_COMPLETED_TIMEOUT_ACK, MethodName,
                                _subStepInterface, RequestMessages.RequestStartUnloading.ToString());
                        }

                        //  2. Ack 확인
                        var result = _processGroup.IsSendingCompleted(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestStartUnloading.ToString());

                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutLong);
                                ++_subStepInterface;
                                break;
                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                {
                                    return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_SENDING_COMPLETED_BUT_NACK, MethodName,
                                        _subStepInterface, RequestMessages.RequestStartUnloading.ToString());
                                }
                            default:
                                break;
                        }
                    }
                    break;

                case 5:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_RESPONSE_DATA_TIMEOUT, MethodName,
                                _subStepInterface, ResponseMessages.ResponseStartUnloading.ToString());
                        }

                        // TODO : 메시지를 왜 확인 안 했더라??
                        // 3. Response 확인 
                        //var result = _processGroup.IsMessageReceived(ProcessModuleIndex, _workingInfo.Location,
                        //    ResponseMessages.ResponseStartUnloading.ToString());
                        //switch (result)
                        //{
                        //    case CommunicationResult.Ack:
                        //        _processGroup.SetUnloadingSignal(ProcessModuleIndex, _workingInfo.Location, true);
                        //        return ReturnCompleted();

                        //    case CommunicationResult.Nack:
                        //    case CommunicationResult.Error:
                        //        return CommandResult.Error;

                        //    default:
                        //        break;
                        //}
                        Dictionary<string, string> resp = new Dictionary<string, string>();
                        if (false == _processGroup.GetReceivedData(ProcessModuleIndex, _workingInfo.Location, ResponseMessages.ResponseStartUnloading.ToString(), ref resp))
                            break;

                        _processGroup.SetUnloadingSignal(ProcessModuleIndex, _workingInfo.Location.Name, true);

                        return ReturnCompleted();
                    }

                default:
                    break;
            }

            return ReturnProceed();
        }
        protected override CommandResults IsApproachUnloadingCompleted()
        {
            const string MethodName = "IsApproachUnloadingCompleted";

            // 상황이 바뀌었을 수 있다..
            if (false == IsUnloadingSignalStillActive(_workingInfo.Location.Name))
                return ReturnSkipped(MethodName);

            return ReturnCompleted();
        }
        protected override CommandResults IsUnloadingPrepared()
        {
            const string MethodName = "IsUnloadingPrepared";

            // 상황이 바뀌었을 수 있다..
            if (false == IsUnloadingSignalStillActive(_workingInfo.Location.Name))
                return ReturnSkipped(MethodName);

            switch (_subStepInterface)
            {
                case 0:
                    {
                        // 1. ActionUnloading을 전송
                        if (false == _processGroup.SendMessage(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestActionUnloading.ToString(), _workingInfo.SubstrateName))
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_ACTION_UNLOADING_SENDING_FAILED, MethodName,
                                _subStepInterface, RequestMessages.RequestActionUnloading.ToString());
                        }

                        Ticks.SetTickCount(TimeoutShort);
                        ++_subStepInterface;
                    }
                    break;
                case 1:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_ACTION_UNLOADING_SENDING_COMPLETED_TIMEOUT_ACK, MethodName,
                                _subStepInterface, RequestMessages.RequestActionUnloading.ToString());
                        }

                        //  2. Ack 확인
                        var result = _processGroup.IsSendingCompleted(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestActionUnloading.ToString());

                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutLong);
                                ++_subStepInterface;
                                break;
                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                {
                                    return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_ACTION_UNLOADING_SENDING_COMPLETED_BUT_NACK, MethodName,
                                        _subStepInterface, RequestMessages.RequestActionUnloading.ToString());
                                }
                            default:
                                break;
                        }

                    }
                    break;
                case 2:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_ACTION_UNLOADING_RECEIVING_RESPONSE_MESSAGE_TIMEOUT, MethodName,
                                _subStepInterface, ResponseMessages.ResponseActionUnloading.ToString());
                        }

                        // 3. Response 확인
                        var result = _processGroup.IsMessageReceived(ProcessModuleIndex, _workingInfo.Location,
                            ResponseMessages.ResponseActionUnloading.ToString());
                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutShort);
                                ++_subStepInterface;
                                break;

                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                {
                                    return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_ACTION_UNLOADING_RECEIVING_COMPLETED_BUT_ERROR, MethodName,
                                        _subStepInterface, ResponseMessages.ResponseActionUnloading.ToString());
                                }

                            default:
                                break;
                        }
                    }
                    break;

                case 3:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_ACTION_UNLOADING_RECEIVING_RESPONSE_DATA_TIMEOUT, MethodName,
                                _subStepInterface, ResponseMessages.ResponseActionUnloading.ToString());
                        }

                        // 4. 데이터 확인
                        Dictionary<string, string> receivedData = new Dictionary<string, string>();
                        if (false == _processGroup.GetReceivedData(ProcessModuleIndex, _workingInfo.Location,
                            ResponseMessages.ResponseActionUnloading.ToString(), ref receivedData))
                            break;

                        // 5. Ack 전송 : 콜백에서 자동 Ack 나가니 현재 미구현
                        //if (false == _processGroup.SetAckReceivedMessage(ProcessModuleIndex, _workingInfo.Location,
                        //    ResponseMessages.ResponseActionUnloading.ToString(), CommunicationResult.Ack, string.Empty))
                        //{

                        //}

                        return ReturnCompleted();
                    }

                default:
                    break;
            }

            return ReturnProceed();
        }
        protected override CommandResults IsUnloadingCompleted()
        {
            const string MethodName = "IsUnloadingCompleted";

            // 상황이 바뀌었을 수 있다..
            if (false == IsUnloadingSignalStillActive(_workingInfo.Location.Name))
            {
                return ReturnSkipped(MethodName);
            }

            switch (_subStepInterface)
            {
                case 0:
                    {
                        // 1. ConfirmUnloading 전송
                        if (false == _processGroup.SendMessage(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestConfirmUnloading.ToString(), _workingInfo.SubstrateName))
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_AFTER_UNLOADING_SENDING_FAILED, MethodName,
                                _subStepInterface, RequestMessages.RequestConfirmUnloading.ToString());
                        }

                        Ticks.SetTickCount(TimeoutShort);
                        ++_subStepInterface;
                    }
                    break;
                case 1:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_AFTER_UNLOADING_SENDING_COMPLETED_TIMEOUT_ACK, MethodName,
                                _subStepInterface, RequestMessages.RequestConfirmUnloading.ToString());
                        }

                        //  2. Ack 확인
                        var result = _processGroup.IsSendingCompleted(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestConfirmUnloading.ToString());

                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutLong);
                                ++_subStepInterface;
                                break;
                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                {
                                    return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_AFTER_UNLOADING_SENDING_COMPLETED_BUT_NACK, MethodName,
                                        _subStepInterface, RequestMessages.RequestConfirmUnloading.ToString());
                                }
                            default:
                                break;
                        }

                    }
                    break;
                case 2:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_AFTER_UNLOADING_RECEIVING_RESPONSE_MESSAGE_TIMEOUT, MethodName,
                                _subStepInterface, ResponseMessages.ResponseConfirmUnloading.ToString());
                        }

                        // 3. Response 확인
                        var result = _processGroup.IsMessageReceived(ProcessModuleIndex, _workingInfo.Location,
                            ResponseMessages.ResponseConfirmUnloading.ToString());
                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutShort);
                                ++_subStepInterface;
                                break;

                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                {
                                    return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_AFTER_UNLOADING_RECEIVING_COMPLETED_BUT_ERROR, MethodName,
                                        _subStepInterface, ResponseMessages.ResponseConfirmUnloading.ToString());
                                }

                            default:
                                break;
                        }
                    }
                    break;

                case 3:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_AFTER_UNLOADING_RECEIVING_RESPONSE_DATA_TIMEOUT, MethodName,
                                _subStepInterface, ResponseMessages.ResponseConfirmUnloading.ToString());
                        }

                        // 4. 데이터 확인
                        //Dictionary<string, string> receivedData = new Dictionary<string, string>();
                        //if (false == _processGroup.GetReceivedData(ProcessModuleIndex, _workingInfo.Location,
                        //    ResponseMessages.ResponseConfirmUnloading.ToString(), ref receivedData))
                        //    break;

                        //// 5. Ack 전송 : 콜백에서 자동 Ack 나가니 현재 미구현
                        //if (false == _processGroup.SetAckReceivedMessage(ProcessModuleIndex, _workingInfo.Location,
                        //    ResponseMessages.ResponseConfirmUnloading.ToString(), CommunicationResult.Ack))
                        //    return CommandResult.Error;

                        Ticks.SetTickCount(100);
                        ++_subStepInterface;
                    }
                    break;
                case 4:
                    {
                        if (false == IsTickOver())
                            break;

                        // 6. SMEMA OFF
                        _processGroup.SetUnloadingSignal(ProcessModuleIndex, _workingInfo.Location.Name, false);

                        return ReturnCompleted();
                    }

                default:
                    break;
            }

            return ReturnProceed();
        }
        #endregion </Unloading>

        #endregion </Material Handling With Process Module>

        #region <Recovery Data>
        protected override void UpdateRecoveryDataBeforePick()
        {
        }

        protected override void UpdateRecoveryDataAfterPick()
        {
        }
        protected override void UpdateRecoveryDataBeforePlace()
        {
        }
        protected override void UpdateRecoveryDataAfterPlace()
        {
        }
        #endregion </Recovery Data>

        #endregion </Overrids>

        #region <Internals>
        #region <Scenario>
        #endregion </Scenario>

        #region <Material Handling Interface>
        private bool GetNextSlotInformationToPlace(int lpIndex, ref int slot)
        {
            int portId = _loadPortManager.GetLoadPortPortId(lpIndex);
            if (false == _carrierServer.HasCarrier(portId))
                return false;

            //if (false == _substrateManager.HasAnySubstrateInLoadPort(portId))
            //    return false;

            slot = -1;
            bool notAvailableSlotFirst = (_loadPortManager.GetCarrierLoadingType(lpIndex).Equals(LoadPortLoadingMode.Cassette));
            int capacity = _carrierServer.GetCapacity(portId);
            for (int i = 0; i < capacity; ++i)
            {
                if (notAvailableSlotFirst && i == 0)
                    continue;

                if (false == _substrateManager.HasSubstrateAtLoadPort(portId, i))
                {
                    slot = i;
                    break;
                }
            }

            return (slot >= 0);
        }
        private void InitSubStepFlag()
        {
            _subStepInterface = 0;
        }
        private CommandResults ReturnToError(CommandResult result, EN_ALARM alarmCode,string methodName, int step, string description)
        {
            InitSubStepFlag();

            _result.ActionName = methodName;
            _result.AlarmCode = (int)alarmCode;
            _result.CommandResult = CommandResult.Error;
            _result.Description = description;

            return _result;
        }
        private CommandResults ReturnSkipped(string methodName)
        {
            InitSubStepFlag();

            _result.ActionName = methodName;
            _result.CommandResult = CommandResult.Skipped;
            _result.Description = string.Empty;

            return _result;
        }
        private CommandResults ReturnProceed()
        {
            //InitSubStepFlag();

            _result.ActionName = string.Empty;
            _result.CommandResult = CommandResult.Proceed;
            _result.Description = string.Empty;

            return _result;
        }
        private CommandResults ReturnCompleted()
        {
            InitSubStepFlag();

            _result.ActionName = string.Empty;
            _result.CommandResult = CommandResult.Completed;
            _result.Description = string.Empty;

            return _result;
        }
        private bool IsLoadingSignalStillActive(string location)
        {
            // TODO :
            return true;

            //  1) PM의 스메마 확인 후 Off면 Skipped 리턴
            List<string> requestedLocation = new List<string>();
            if (false == _processGroup.IsLoadingRequested(ProcessModuleIndex, ref requestedLocation))
                return false;

            if (false == requestedLocation.Contains(location))
                return false;
            
            return true;
        }

        #endregion </Material Handling Interface>

        #endregion </Internals>

        #endregion </Methods>
    }

    class TaskAtmRobotRecovery500W : Work.RecoveryData
    {
        public TaskAtmRobotRecovery500W(string taskName, int nPortCount)
            : base(taskName, nPortCount)
        {
        }

        protected override void LoadData(ref FileComposite_.FileComposite fComp, string sRootName)
        {
        }
        protected override void SaveData(ref FileComposite_.FileComposite fComp, string sRootName)
        {
        }
    }
}
