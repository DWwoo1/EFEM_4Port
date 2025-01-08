using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using DesignPattern_.Observer_;
using EquipmentState_;
using FrameOfSystem3.Recipe;
using FrameOfSystem3.SECSGEM.DefineSecsGem;
using FrameOfSystem3.SECSGEM.Scenario;
//using FrameOfSystem3.SECSGEM.SecsGemDll;	// TODO : secsgem dll

using TickCounter_;

namespace FrameOfSystem3.SECSGEM
{
	public class ScenarioOperator : IObserver
	{
		#region <Fields>
		private bool _initialized = false;
		private EquipmentState _subjectEquipmentState = null;

		private string _previousEquipmentState = String.Empty;
		private Communicator.SecsGemHandler _gemCommunicator = Communicator.SecsGemHandler.Instance;
		private ProcessingScenario _scenario = null;

		private Dictionary<long, string> _traceDataToUpdate = new Dictionary<long, string>();

		private bool _isExiting = false;
		private static ScenarioOperator _instance = null;
		private bool _isUse = false;
		#endregion </Fields>

		#region <Properties>
		public static ScenarioOperator Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new ScenarioOperator();
				}

				return _instance;
			}
		}
		public bool UseScenario
        {
			get
            {
				return _isUse;
            }
        }
		#endregion </Properties>

		#region External Interface

		#region Initialize & Exit
		public bool Initialize(ProcessingScenario scenario, SecsGem driver, string cfgPath, string recipePath)
		{
            _scenario = scenario;
			if (null == _scenario)
				return false;

            if (false == _gemCommunicator.Initialize(driver, cfgPath, recipePath))
				return false;

            Dictionary<string, StatusVariable> statusVariableList;
            Dictionary<long, List<StatusVariable>> reportList;
            Dictionary<string, CollectionEvent> collectionEventList;
            _gemCommunicator.MakeGemSpecification(cfgPath, out statusVariableList,
				out  reportList, out collectionEventList);

            _initialized = _scenario.Init(recipePath, string.Format(@"{0}\Config", cfgPath), statusVariableList, reportList, collectionEventList);

            //24.09.27 by wdw [ADD] EQUIPMNET CONSTANT 추가
            Dictionary<string, EquipmentConstant> equipmentConstantList;
            _gemCommunicator.MakeGemECVSpecification(cfgPath, out equipmentConstantList);
            _scenario.AddECVList(equipmentConstantList);

			if (_initialized)
			{
				RegisterSubject(EquipmentState.GetInstance());
				UpdateVariableItems();
			}

			_isUse = Recipe.Recipe.GetInstance().GetValue(EN_RECIPE_TYPE.COMMON, PARAM_COMMON.UseSecsGem.ToString(), false);

			return _initialized;
		}
		public void Exit()
		{
			_isExiting = true;
			
			if (_scenario != null)
				_scenario.Exit();

			_gemCommunicator.Exit();
		}

        public bool SetUse(bool trigger)
        {
            if (trigger && false == IsHostConnected(false))
            {
                _isUse = false;
                return false;
            }

            _isUse = trigger;
            return true;
        }

        #endregion

        #region Execute Scenario
        public void InitScenarioAll()
		{
			if (_scenario == null)
				return;

			_scenario.InitScenarioAll();
		}
        public void InitScenarioResultData(Enum scenario)
        {
            if (_scenario == null)
                return;

            if (_scenario.GetInstanceScenario(scenario) == null)
                return;

            _scenario.InitScenarioResultData(scenario);
        }

        public void SetScenarioActivation(Enum scenario, bool activation)
		{
			if (_scenario == null)
				return;

			if (_scenario.GetInstanceScenario(scenario) == null)
				return;

			_scenario.SetScenarioActivation(scenario, activation);
		}

        public bool UpdateScenarioParam(Enum scenario, Dictionary<string, string> values)
		{
			if (_scenario == null)
				return false;

			if (_scenario.GetInstanceScenario(scenario) == null)
				return true;

			_scenario.SetScenarioActivation(scenario, false);

			return _scenario.UpdateScenarioParams(scenario.ToString(), values);
		}
        public EN_SCENARIO_RESULT ExecuteScenario(Enum scenario)
		{
			if (_scenario == null)
                return EN_SCENARIO_RESULT.ERROR;

			if (_scenario.GetInstanceScenario(scenario) == null || false == _isUse)
				return EN_SCENARIO_RESULT.COMPLETED;

			if (false == _scenario.IsScenarioRunning(scenario))
			{
				InitScenarioResultData(scenario);

				_scenario.SetScenarioActivation(scenario, true);
			}

			return _scenario.ExecuteScenario(scenario);
		}
		#endregion

		#region Update State
		public void UpdateState(string strState)
		{
			if (_scenario == null) return;

			if (false == _previousEquipmentState.Equals(strState))
			{
				_previousEquipmentState = String.Format("{0}", strState);
				_scenario.EquipmentstateChanged(strState);
			}
		}
		#endregion

		#region Update Variable Items
        public void UpdateVariableItems()
        {
            if (_scenario == null) return;

            if (false == _initialized) return;

			if (false == _isUse)
				return;

			_scenario.UpdateVariablesAll();
        }
		#endregion

		#region ECID Parameter
		public void UpdateCommonParameters(PARAM_COMMON enParam, string strValue)
		{
			if (_scenario == null) return;

			_gemCommunicator.UpdateECVParameter((long)enParam + PARAM_RANGE.GetInstance().ECID_COMMON_START, strValue);
		}

		public void UpdateMachineParameters(PARAM_EQUIPMENT enParam, string strValue)
		{
			if (_scenario == null) return;

			_gemCommunicator.UpdateECVParameter((long)enParam + PARAM_RANGE.GetInstance().ECID_COMMON_START, strValue);
		}

        public bool UpdateECVParameters(string strECIDName, string strValue)
        {
            if (_scenario == null) return false;

            return _scenario.UpdateECVParameter(strECIDName, strValue);
        }
		#endregion

        #region <Alarms>
        public void ExecuteAlarmScenario(int alarmId, EN_GEM_ALARM_STATE state)
        {
            if (_scenario == null)
                return;

            if (false == UseScenario || _gemCommunicator.MaintenanceMode)
                return;

            _scenario.ExecuteReportAlarm(alarmId, state);
        }
        #endregion </Alarms>

        #region <Scenario Config>
        public List<string> GetScenarioList()
        {
            if (_scenario == null)
                return null;

            return _scenario.GetScenarioList();
        }

        public Enum ConvertScenarioByString(string scenarioName)
        {
            if (_scenario == null)
                return null;

            return _scenario.ConvertScenarioByName(scenarioName);
        }


        public List<string> GetScenarioParameterList(Enum scenario)
        {
            if (_scenario == null)
                return null;

            if (false == _scenario.IsScenarioEnabled(scenario))
                return null;

            return _scenario.GetScenarioParameterList(scenario);
        }

        public Dictionary<string, string> GetScenarioResultData(Enum scenario)
        {
            if (_scenario == null)
                return null;

            if (_scenario.GetInstanceScenario(scenario) == null)
                return null;

            return _scenario.GetScenarioResultData(scenario);
        }

        public int GetScenarioStep(Enum scenario)
        {
            if (_scenario == null)
                return DefineSecsGem.Contants.SCENARIO_STEP_END;

            if (_scenario.GetInstanceScenario(scenario) == null)
                return DefineSecsGem.Contants.SCENARIO_STEP_END;

            //ScenarioParamValues values = new ScenarioParamValues(arValues.ToList());

            if (false == _scenario.IsScenarioRunning(scenario))
            {
                return DefineSecsGem.Contants.SCENARIO_STEP_END;
            }

            return _scenario.GetScenarioStep(scenario);
        }

        public Dictionary<long, List<StatusVariable>> GetReportList()
        {
            if (_scenario == null)
                return null;

            return _scenario.GetReportList();
        }
        public Dictionary<string, StatusVariable> GetStatusVariableList()
        {
            if (_scenario == null)
                return null;

            return _scenario.GetStatusVariableList();
        }
        public Dictionary<string, CollectionEvent> GetCollectionEventList()
        {
            if (_scenario == null)
                return null;

            return _scenario.GetCollectionEventList();
        }

        #endregion <Scenario Config>
        #endregion

        #region Internal Interface

        #region Connect 여부 확인
        private bool IsHostConnected(bool bDoGenerateAlarm = true)
		{
			if (false == _isUse)
				return true;

			if (false == _initialized || false == _gemCommunicator.IsConnect)
			{
				if (true == bDoGenerateAlarm)
				{
					//Alarm_.Alarm.GetInstance().GenerateAlarm(0, 0, (int)Define.DefineEnumProject.Alarm.EN_SYSTEM_ALARM.HOST_CONNECTION_ALARM, false);
				}
				return false;
			}

			// 추후 이벤트 사용 여부를 설정해서 여기서 패스할 지 정해서 넘겨야하나??

			return true;
		}
		#endregion

		#endregion

		#region <Observer>
		// 설비 상태를 받아오기 위해 옵저버를 등록한다.
		public void RegisterSubject(Subject pSubject)
		{
			if (true == _initialized)
			{
				if (pSubject is EquipmentState)
				{
					_subjectEquipmentState = pSubject as EquipmentState;

					// 2022.08.19 by Thienvv [MOD] when state of Load is change, then update state of all Devices
					//UpdateUnitState(m_instEqpInfo.UnitStatus); // 2022.08.31 by Thienvv [DEL]
					UpdateState(_subjectEquipmentState.GetState().ToString());
				}

				pSubject.Attach(this);
			}
		}
		public void UpdateObserver(Subject pSubject)
		{
			if (true == _initialized)
			{
				if (pSubject is EquipmentState)
				{
					_subjectEquipmentState = pSubject as EquipmentState;

					UpdateState(_subjectEquipmentState.GetState().ToString());
					//UpdateUnitState(m_subjectEquipmentState.GetState().ToString()); // 2022.08.31 by Thienvv [DEL]
				}
			}
		}
		#endregion </Observer>

		#region <Execute>
		public void Execute()
        {
            if (false == _initialized)
                return;

			if (_isExiting)
				return;

            if (_scenario == null)
                return;

			if (false == IsHostConnected(false))
				return;

            _scenario.Execute();
			if (_isUse)
            {
                if (_scenario.UpdateTraceData(ref _traceDataToUpdate))
                {
                    if (_traceDataToUpdate != null && _traceDataToUpdate.Count > 0)
                    {
                        _gemCommunicator.UpdateVariables(_traceDataToUpdate.Keys.ToArray(), _traceDataToUpdate.Values.ToArray());
                    }
                }
            }

			_gemCommunicator.Execute();
		}
		#endregion </Execute>
	}

	namespace Communicator
	{
		using System.IO;
        using System.Threading.Tasks;

		//using Define.DefineConstant;
		using FrameOfSystem3.Recipe;
		using FrameOfSystem3.SECSGEM.DefineSecsGem;
		using System.Collections.Generic;
        using System.Collections.Concurrent;
        using System.IO.Compression;

        public class SecsGemHandler
        {
            #region <Fields>
            private SecsGem _gemDriver = null;
            private Recipe _recipe = Recipe.GetInstance();
            private static SecsGemHandler _instance = null;
            private AsyncLogger _asyncLogger = new AsyncLogger();
            //public event deleHandlerString CallbackDisplayLog;
            #endregion </Fields>

            #region <Properties>
            public bool IsConnect
            {
                get
                {
                    if (_gemDriver == null) return false;

                    return _gemDriver.Connect;
                }
            }

            public bool MaintenanceMode { get; set; }

            public static SecsGemHandler Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = new SecsGemHandler();
                    }

                    return _instance;
                }
            }
			// TODO : Here
            public bool IsExitingRequested { get; set; }
            #endregion </Properties>

            #region <Methods>

            #region <Delegate>
            public void AttachDisplayLog(deleHandlerString pFunc)
            {
                _asyncLogger.CallbackDisplayLog += pFunc;
            }

            //public void DisplaySecsGemLog(string strMessage)
            //{
            //    if (CallbackDisplayLog != null)
            //    {
            //        CallbackDisplayLog(strMessage);
            //    }
            //}
            public void LinkTerminalMessage(deleHandlerString pFunc)
            {
                if (_gemDriver == null) return;

                _gemDriver.CallbackTerminalMessage += new deleHandlerString(pFunc);
            }

            public void LinkShowOperatorCall(deleDisplayOperatorCallForm pFunc)
            {
                if (_gemDriver == null) return;

                _gemDriver.CallbackOperatorCall += new deleDisplayOperatorCallForm(pFunc);
            }

            public void LinkConnection(deleHandlerVoid pFunc)
            {
                if (_gemDriver == null) return;

                _gemDriver.CallbackUpdateVariables += new deleHandlerVoid(pFunc);
            }

            public void LinkControlState(deleHandlerString pFunc)
            {
                if (_gemDriver == null) return;

                _gemDriver.CallbackControlState += new deleHandlerString(pFunc);
            }

            public void LinkRemoteCommand(deleRemoteCommand pFunc)
            {
                if (_gemDriver == null) return;

                _gemDriver.CallbackRemoteCommand += new deleRemoteCommand(pFunc);
            }
            public void LinkEquipmentParameterChangeRequest(deleChangeEquipmentParameters pFunc)
            {
                if (_gemDriver == null) return;

                _gemDriver.CallbackChangeSystemParameter += new deleChangeEquipmentParameters(pFunc);
            }
            public void LinkClientToClientMessage(deleRecvClientToClientMessage pFunc)
            {
                if (_gemDriver == null) return;

                _gemDriver.CallBackClientToClientMessageReceived += new deleRecvClientToClientMessage(pFunc);
            }

            public void LinkSecsMessageReceived(deleSecsMessageReceived pFunc)
            {
                if (_gemDriver == null) return;

                _gemDriver.CallbackSecsMessageReceived += new deleSecsMessageReceived(pFunc);
            }

            public void LinkRecipeControlGrant(deleRecipeControlGrant pFunc)
            {
                if (_gemDriver == null) return;

                _gemDriver.CallbackCheckingRecipeControlGrant += new deleRecipeControlGrant(pFunc);
            }

            public void LinkUnFormattedRecipeControls(deleReqUPloadingUnformattedRecipeControl pUploadingFunc,
                deleReqDownloadingUnformattedRecipeControl pDownloadingFunc
                , deleReqUPloadingUnformattedRecipeAck pUploadingAck)
            {
                if (_gemDriver == null) return;

                _gemDriver.CallbackUploadingUnformattedRecipe += pUploadingFunc;
                _gemDriver.CallbackDownloadingUnformattedRecipe += pDownloadingFunc;
                _gemDriver.CallbackUploadingUnformattedRecipeAck += pUploadingAck;
            }

            public void LinkFormattedRecipeControls(deleReqUploadingFormattedRecipe pUploadingFunc,
               deleReqDownloadingFormattedRecipe pDownloadingFunc)
            {
                if (_gemDriver == null) return;

                _gemDriver.CallbackReqUploadingFormattedRecipe += pUploadingFunc;
                _gemDriver.CallbackReqDownloadingFormattedRecipe += pDownloadingFunc;
            }

            public void LinkRecipeFileIsDeleted(deleRecipeFileIsDeleted pFunc)
            {
                if (_gemDriver == null)
                    return;

                _gemDriver.CallbackRecipeFileIsDeleted += pFunc;
            }
            public void LinkTerminalMessageWithProcessingScenario(deleHandlerString pFunc)
            {
                if (_gemDriver == null) return;

                _gemDriver.CallbackTerminalMessage += new deleHandlerString(pFunc);
            }
            #endregion </Delegate>

            #region <Init/Exit>
            public bool Initialize(SecsGem driver, string cfgPath, string recipePath)
            {
                IsExitingRequested = false;
                _gemDriver = driver;

                if (null == _gemDriver)
                    return false;

                _gemDriver.CallbackLogging += new deleHandlerString(WriteLog);

                bool bResult = _gemDriver.Init(string.Format(@"{0}\{1}", cfgPath, DefineSecsGem.PATH.FILE_NAME_CFG));

                if (bResult == true)
                {
                    WriteLog("Communicator Initialize 성공");

                    _gemDriver.SetRecipePath(recipePath);
                }
                else
                {
                    _gemDriver.CallbackLogging -= new deleHandlerString(WriteLog);

                    WriteLog("Communicator Initialize 실패");
                }

                // 초기화
                MaintenanceMode = false;

                return bResult;
            }

            public void MakeGemSpecification(string configDirectory, out Dictionary<string, StatusVariable> statusVariableList, out Dictionary<long, List<StatusVariable>> reportList, out Dictionary<string, CollectionEvent> collectionEventList)
            {
                // SVID
                statusVariableList = new Dictionary<string, StatusVariable>();

                // RptId
                reportList = new Dictionary<long, List<StatusVariable>>();

                // CEID
                collectionEventList = new Dictionary<string, CollectionEvent>();

                _gemDriver.MakeGemSpecification(configDirectory, ref statusVariableList, ref reportList, ref collectionEventList);
            }

            //24.09.27 by wdw [ADD] EQUIPMNET CONSTANT 추가
            public void MakeGemECVSpecification(string configDirectory, out Dictionary<string, EquipmentConstant> equipmentConstantList)
            {
                // ECV
                equipmentConstantList = new Dictionary<string, EquipmentConstant>();
                _gemDriver.MakeGemECVSpecification(configDirectory, ref equipmentConstantList);

            }

            public void Exit()
            {
                if (_gemDriver == null) return;

                IsExitingRequested = true;

                WriteLog("Communicator Initialize 종료");

                _gemDriver.CallbackLogging -= new deleHandlerString(WriteLog);

                _asyncLogger.Exit();

                _gemDriver.Close();
            }
			// TODO : Here
            private bool IsValid()
            {
                if (_gemDriver == null ||
                    IsExitingRequested)
                    return true;

                return false;
            }
			#endregion </Init/Exit>

			#region <State>
			public void SetControlState(EN_CONTROL_STATE enControlState)
			{
				if (IsValid())
                    return;

				_gemDriver.SetInitControlState(enControlState);
                _gemDriver.SetControlState(enControlState);

				WriteLog(String.Format("SetControl state : {0}", enControlState.ToString()));
			}

			public EN_CONTROL_STATE GetControlState()
			{
                if (IsValid())
                    return EN_CONTROL_STATE.OFFLINE;
	
                return _gemDriver.GetControlState();
			}
	
			public void SetCommStateEnable()
			{
                if (IsValid())
                    return;

                _gemDriver.SetCommStateEnabled();
			}

			public void SetCommStateDisabled()
            {
                if (IsValid())
                    return;

                _gemDriver.SetCommStateDisabled();
			}

			public EN_COMM_STATE GetCommState()
			{
                if (IsValid())
                    return EN_COMM_STATE.DISABLED;
	
                return _gemDriver.GetCommState();
            }
            #endregion </State>

            #region <Alarm>
            public void SetAlarm(int nAlarm)
            {
                if (IsValid())
                    return;

                if (MaintenanceMode)
                    return;

                _gemDriver.SetAlarm(nAlarm);
            }
            public void ClearAlarm(int nAlarm)
            {
                if (IsValid())
                    return;

                if (MaintenanceMode)
                    return;

                _gemDriver.ClearAlarm(nAlarm);
            }
            #endregion </Alarm>

            #region <UserDefinedMessage>
            public bool SendClientToClientMessage(string device, string messageName, string sendingType, string scenarioName, string[] contentNames, string[] messages, EN_MESSAGE_RESULT result, bool useLogging)
            {
                if (IsValid())
                    return false;

                bool sendingResult =_gemDriver.SendClientToClientMessage(device, messageName, sendingType, scenarioName, contentNames, messages, result);
                
                if (useLogging)
                {
                    string messageOfLogging = String.Empty;
                    string message = String.Empty;
                    if(contentNames != null)
                    {
                        int count = contentNames.Length;
                        for(int i = 0; i < count; ++i)
                        {
                            message = String.Format(" [{0} : {1}] ", contentNames[i], messages[i]);

                            messageOfLogging = String.Format("{0},{1}", messageOfLogging, message);
                        }
                    }

                    if (messageOfLogging.Length > 1 && messageOfLogging.Substring(0, 1).Equals(","))
                        messageOfLogging = messageOfLogging.Remove(0, 1);

                    WriteLog(String.Format("Send Client Message > TargetDevice : {0}, MessageName : {1}, Type : {2}, Scenario : {3}, Content : {4}, Result : {5}", device, messageName, sendingType, scenarioName, messageOfLogging, result.ToString()));
                }
                
                return sendingResult;
            }
            #endregion </UserDefinedMessage>

            #region <Send Event>
            public void SendEvent(long eventID, long[] vids, string[] vidValues)
            {
                if (IsValid())
                    return;

                if (vids == null)
                    vids = new long[0];

                if (vidValues == null)
                    vidValues = new string[0];

                if (_gemDriver == null) return;

                if (vids != null && vidValues != null &&
                    vids.Length != vidValues.Length)
                {
                    string log = String.Format("[EQ ==> XGEM] Vid 개수 오류! >> CEID : {0}, Vid 수 : {1}, Value 수 : {2}", eventID, vids.Length, vidValues.Length);

                    WriteLog(log);

                    int length = Math.Min(vids.Length, vidValues.Length);

                    long[] vidsTemp = new long[length];
                    string[] valuesTemp = new string[length];

                    Array.Copy(vids, vidsTemp, length);
                    Array.Copy(vidValues, valuesTemp, length);

                    _gemDriver.SendEvent(eventID, vidsTemp, valuesTemp);
                }
                else
                {
                    _gemDriver.SendEvent(eventID, vids, vidValues);
                }
            }

            public bool IsSendingEventCompleted(long nEventID)
            {
                if (IsValid())
                    return false;

                return _gemDriver.IsEventDone(nEventID);
            }
            #endregion </SendEvent>

            #region <Send SecsMessage>
            public bool SendUserDefinedSecsMessage(long stream, long function, List<SemiObject> structure)
            {
                if (IsValid())
                    return false;

                return _gemDriver.SendUserDefinedSecsMessage(stream, function, structure);
            }
            #endregion </Send SecsMessage>

            #region <CallBack>
            public void ShowOperatorCallingMessage(string strMessage)
            {
                if (IsValid())
                    return;

                _gemDriver.ShowOperatorCall(EN_OPCALL_LEVEL.WARNING, "OPERATOR", true, strMessage);
            }

            // 2024.10.11. jhlim [DEL] ProcessingScenario로 이동
            //public void EquipmentParameterChanged(long[] arrIDs, string[] arrValues)
            //{
            //	if (_gemDriver == null) return;

            //	var paramRange = PARAM_RANGE.GetInstance();
            //	for (int i = 0; i < arrIDs.Length; ++i)
            //	{
            //                 if (EquipmentConstantList != null
            //                     && EquipmentConstantList.ContainsKey(arrIDs[i]))
            //                 {
            //                      PARAM_COMMON enCommonParam;
            //                     if(Enum.TryParse(EquipmentConstantList[arrIDs[i]].Name, out enCommonParam))
            //                     {
            //                             _recipe.SetValue(EN_RECIPE_TYPE.COMMON, enCommonParam.ToString(),
            //                                 0, EN_RECIPE_PARAM_TYPE.VALUE, arrValues[i]);
            //                     }

            //                     PARAM_EQUIPMENT enEquipmentParam;
            //                     if (Enum.TryParse(EquipmentConstantList[arrIDs[i]].Name, out enEquipmentParam))
            //                     {
            //                         _recipe.SetValue(EN_RECIPE_TYPE.EQUIPMENT, enEquipmentParam.ToString(),
            //                             0, EN_RECIPE_PARAM_TYPE.VALUE, arrValues[i]);
            //                     }
            //                 }
            //                 else
            //                 {
            //                     if (arrIDs[i] >= paramRange.ECID_START && arrIDs[i] <= paramRange.ECID_END)
            //                     {
            //                         // Common
            //                         if (arrIDs[i] >= paramRange.ECID_COMMON_START &&
            //                             arrIDs[i] <= paramRange.ECID_COMMON_END)
            //                         {
            //                             int nIndex = (int)arrIDs[i] - paramRange.ECID_COMMON_START;
            //                             PARAM_COMMON enParam = (PARAM_COMMON)nIndex;

            //                             _recipe.SetValue(EN_RECIPE_TYPE.COMMON, enParam.ToString(),
            //                                 0, EN_RECIPE_PARAM_TYPE.VALUE, arrValues[i]);
            //                         }

            //                         // Equip
            //                         if (arrIDs[i] >= paramRange.ECID_EQUIP_START &&
            //                             arrIDs[i] <= paramRange.ECID_EQUIP_END)
            //                         {
            //                             int nIndex = (int)arrIDs[i] - paramRange.ECID_EQUIP_START;

            //                             PARAM_EQUIPMENT enParam = (PARAM_EQUIPMENT)nIndex;

            //                             _recipe.SetValue(EN_RECIPE_TYPE.EQUIPMENT, enParam.ToString(), 0,
            //                                 EN_RECIPE_PARAM_TYPE.VALUE, arrValues[i]);
            //                         }
            //                     }
            //                 }
            //	}
            //}
            // 2024.10.11. jhlim [END]
            #endregion </CallBack>

            #region <Logging>
            public void WriteLog(string strLog)
			{
                _asyncLogger.EnqueueLog(strLog);
				//StreamWriter sw = null;
				//DateTime nowDate = DateTime.Now;
				//string  strPath = String.Format(@"{0}\{1:0000}\\{2:00}\\{3:00}\", PATH.FILEPATH_LOG, nowDate.Year, nowDate.Month, nowDate.Day);
				//string strFilePath = String.Format(@"{0}\Log.txt", strPath);
				//string strHeader = String.Format("[{0:d2}/{1:d2}-{2:d2}:{3:d2}:{4:d2}.{5:d3}]",
				//	DateTime.Now.Month,
				//	DateTime.Now.Day,
				//	DateTime.Now.Hour,
				//	DateTime.Now.Minute,
				//	DateTime.Now.Second,
				//	DateTime.Now.Millisecond);
				//string strMessage = String.Empty;

				//try
				//{
				//	if (Directory.Exists(strPath) == false)
				//	{
				//		Directory.CreateDirectory(strPath);
				//	}

				//	sw = new StreamWriter(strFilePath, true);
				//	strMessage = String.Format("{0} {1}", strHeader, strLog);
					
				//	sw.WriteLine(strMessage);
				//}
				//catch(Exception ex)
				//{
				//	if (sw != null)
				//	{
				//		strMessage = String.Format("{0} Exception -> {1}, {2}", strHeader, ex.Message, ex.StackTrace);
				//		sw.WriteLine(strMessage);
				//	}
				//}

				//if (sw != null) sw.Close();

				//Console.WriteLine(strMessage);

				//if (CallbackDisplayLog != null)
				//{
				//	CallbackDisplayLog(strMessage);
				//}
			}
			#endregion </Logging>

            #region <ECID>
            public void UpdateECVParameter(long nID, string strValue)
            {
                if (IsValid())
                    return;

                long[] arrIDs = { nID };
                string[] arrValues = { strValue };

                _gemDriver.UpdateECV(arrIDs, arrValues);
            }

            public void UpdateECVParameters(long[] arrIDs, string[] arrValues)
            {
                if (IsValid())
                    return;

                _gemDriver.UpdateECV(arrIDs, arrValues);
            }
            public void UpdateECVParameters(Dictionary<string, string> ecidValues)
            {
                if (IsValid())
                    return;

                _gemDriver.UpdateEquipmentConstants(ecidValues);
            }
            #endregion </ECID>

            #region <VID>
            public void UpdateVariable(long vid, List<SemiObject> value)
            {
                if (IsValid())
                    return;

                _gemDriver.UpdateVariable(vid, value);
            }
			public void UpdateVariable(long nID, string strValue)
			{
                if (IsValid())
                    return;

                long[] arrIDs = { nID };
				string[] arrValues = { strValue };

				_gemDriver.UpdateVariables(arrIDs, arrValues);
			}
            public void UpdateVariables(long[] arrIDs, string[] arrValues)
			{
                if (IsValid())
                    return;

                _gemDriver.UpdateVariables(arrIDs, arrValues);
			}
			#endregion </VID>

			#region Recipe
            public void SendRecipeUploadInquire(string recipeName)
            {
                if (IsValid())
                    return;

                _gemDriver.ReqUploadingRecipeInquire(recipeName);
            }
            public void SendRecipeUploadUnFormatted(string recipeName)
            {
                if (IsValid())
                    return;

                _gemDriver.ReqUploadingUnformattedRecipe(recipeName);
            }
            public void SendRecipeDownloadUnFormatted(string recipeName)
            {
                if (IsValid())
                    return;

                _gemDriver.ReqDownloadingUnformattedRecipe(recipeName);
            }
			#endregion

			#region <Gathering>
			public void Execute()
            {
                if (IsValid())
                    return;

                _gemDriver.Execute();

			}
			#endregion </Gathering>

			#endregion </Methods>
		}
        
        public class AsyncLogger
        {
            #region <Constructors>
            public AsyncLogger()
            {
                BasePath = PATH.FILEPATH_LOG;
                LogQueue = new ConcurrentQueue<Tuple<DateTime, string>>();

                // 로그 파일 경로가 존재하지 않으면 생성
                if (false == Directory.Exists(BasePath))
                {
                    Directory.CreateDirectory(BasePath);
                }

                _currentLogFilePath = string.Empty;

                Task.Run(() => ProcessLogsAsync());
            }
            #endregion </Constructors>

            #region <Fields>
            private readonly string BasePath;
            private readonly ConcurrentQueue<Tuple<DateTime, string>> LogQueue;
            private StreamWriter _streamWriter;
            private string _currentLogFilePath;            
            private bool _exiting = false;
            private string _temporaryPath = string.Empty;
            private string _temporaryDir = string.Empty;
            public event deleHandlerString CallbackDisplayLog;
            #endregion </Fields>

            #region <Methods>

            #region <External>
            public void EnqueueLog(string message)
            {
                var logEntry = Tuple.Create(DateTime.Now, message);

                LogQueue.Enqueue(logEntry);
            }
            public async void Exit()
            {
                _exiting = true;
             
                await WaitForCompletion();

                CloseStreamWriter();
            }
            #endregion </External>

            #region <Internal>
            private void CreateLogFilePath(DateTime date, ref string createdPath)
            {
                _temporaryDir = string.Format(@"{0}\{1:0000}\{2:00}\{3:00}", BasePath, date.Year, date.Month, date.Day);
                if (false == Directory.Exists(_temporaryDir))
                    Directory.CreateDirectory(_temporaryDir);

                createdPath = string.Format(@"{0}\Log.txt", _temporaryDir);
            }
            private void CreateStreamWriter(string path)
            {
                _streamWriter = new StreamWriter(path, true) { AutoFlush = true };
            }
            private void CloseStreamWriter()
            {
                if (_streamWriter != null)
                {
                    _streamWriter.Close();
                    _streamWriter.Dispose();
                    _streamWriter = null;
                }
            }
            private async Task ProcessLogsAsync()
            {
                while (true)
                {
                    await Task.Delay(1);

                    if (LogQueue.Count > 0)
                    {
                        if (LogQueue.TryDequeue(out Tuple<DateTime, string> logEntry))
                        {
                            WriteLog(logEntry.Item1, logEntry.Item2);
                        }
                    }
                    else
                    {
                        CloseStreamWriter();

                        // 이전 날짜 로그는 압축 및 제거
                        //await CleanUpLogsAsync();

                        if (_exiting)
                            return;
                    }
                }
            }
            private void WriteLog(DateTime logDate, string message)
            {
                try
                {
                    // 현재 날짜와 로그 날짜 비교
                    CreateLogFilePath(logDate, ref _temporaryPath);
                    if (false == _currentLogFilePath.Equals(_temporaryPath))
                    {
                        // 기존 StreamWriter 닫기
                        CloseStreamWriter();

                        // 새로운 날짜의 로그 파일 경로 설정
                        _currentLogFilePath = _temporaryPath;

                        // 새로운 StreamWriter 초기화
                        CreateStreamWriter(_currentLogFilePath);
                    }
                    else
                    {
                        if (_streamWriter == null)
                        {
                            CreateStreamWriter(_currentLogFilePath);
                        }
                    }

                    var logEntry = string.Format("[{0:d2}/{1:d2}-{2:d2}:{3:d2}:{4:d2}.{5:d3}] {6}",
                        logDate.Month,
                        logDate.Day,
                        logDate.Hour,
                        logDate.Minute,
                        logDate.Second,
                        logDate.Millisecond,
                        message);

                    _streamWriter.WriteLine(logEntry);
                    
                    if (CallbackDisplayLog != null)
                    {
                        CallbackDisplayLog(logEntry);
                    }
                }
                catch (Exception ex)
                {
                    if (_streamWriter != null)
                    {
                        _streamWriter.WriteLine($"##### { ex.Message } : { ex.StackTrace } #####");
                    }
                }
            }

            private async Task CleanUpLogsAsync()
            {
                string logRootPath = @"D:\Log"; // 로그 파일 경로
                string tempFolderPath = Path.Combine(logRootPath, "Temp");
                DateTime today = DateTime.Today;

                // 임시 폴더 생성
                Directory.CreateDirectory(tempFolderPath);

                var logFiles = Directory.GetFiles(logRootPath, "*.txt")
                    .Select(f => new FileInfo(f))
                    .Where(f => f.LastWriteTime.Date < today)
                    .ToList();

                foreach (var logFile in logFiles)
                {
                    string zipFileName = Path.Combine(logRootPath, $"{logFile.Name}.zip");
                    string tempFilePath = Path.Combine(tempFolderPath, logFile.Name);

                    // 파일을 임시 폴더로 복사
                    File.Copy(logFile.FullName, tempFilePath, true);

                    if (File.Exists(zipFileName))
                    {
                        using (var zipArchive = ZipFile.Open(zipFileName, ZipArchiveMode.Update))
                        {
                            zipArchive.CreateEntryFromFile(tempFilePath, logFile.Name);
                            Console.WriteLine($"기존 압축 파일에 추가: {logFile.Name}");
                        }
                    }
                    else
                    {
                        using (var zipArchive = ZipFile.Open(zipFileName, ZipArchiveMode.Create))
                        {
                            zipArchive.CreateEntryFromFile(tempFilePath, logFile.Name);
                            Console.WriteLine($"새 압축 파일 생성: {logFile.Name}");
                        }
                    }

                    // 원본 파일 삭제
                    File.Delete(logFile.FullName);
                    Console.WriteLine($"삭제 완료: {logFile.FullName}");
                }

                // 임시 폴더 정리
                Directory.Delete(tempFolderPath, true);
            }

            private async Task WaitForCompletion()
            {
                while (LogQueue.Count > 0)
                {
                    await Task.Delay(1);
                }
            }
            #endregion </Internal>
            #endregion </Methods>
        }
    }
}
