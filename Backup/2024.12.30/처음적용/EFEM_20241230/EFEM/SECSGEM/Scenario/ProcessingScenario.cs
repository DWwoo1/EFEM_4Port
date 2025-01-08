using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

using TickCounter_;

using FrameOfSystem3.Recipe;
using FrameOfSystem3.Functional;
using FrameOfSystem3.SECSGEM.Scenario;
using FrameOfSystem3.SECSGEM.DefineSecsGem;

namespace FrameOfSystem3.SECSGEM
{
    public abstract class ProcessingScenario
    {
        #region <Variables>
        protected Recipe.Recipe _recipe = Recipe.Recipe.GetInstance();
        private Communicator.SecsGemHandler _gemHandler = Communicator.SecsGemHandler.Instance;

        protected PARAM_RANGE _paramRange = PARAM_RANGE.GetInstance();
        protected readonly ConcurrentDictionary<Enum, ScenarioBaseClass> ScenarioList = new ConcurrentDictionary<Enum, ScenarioBaseClass>();

        protected string _recipePath;

        protected EN_CONTROL_STATE _controlState = EN_CONTROL_STATE.OFFLINE;

        protected readonly Dictionary<long, List<StatusVariable>> ReportList = new Dictionary<long, List<StatusVariable>>();
        protected readonly Dictionary<string, StatusVariable> StatusVariableList = new Dictionary<string, StatusVariable>();
        protected readonly Dictionary<string, CollectionEvent> CollectionEventList = new Dictionary<string, CollectionEvent>();
        protected readonly Dictionary<string, EquipmentConstant> EquipmentConstantList = new Dictionary<string, EquipmentConstant>();// key : Name;

        private bool _isTraceDataAvailable = false;
        private uint _traceDataInterval;
        private readonly TickCounter TickForTraceData = new TickCounter();
        private readonly Dictionary<long, string> TraceDataToUpdate = new Dictionary<long, string>();
        private Dictionary<long, string> _traceDataFromProcessingScenario = new Dictionary<long, string>();
        #endregion </Variables>

        #region <External Methods>
        public ScenarioBaseClass GetInstanceScenario(Enum scenario)
        {
            if (false == ScenarioList.ContainsKey(scenario))
                return null;

            return ScenarioList[scenario];
        }
        public List<string> GetScenarioList()
        {
            List<string> scenarioList = new List<string>();
            foreach (var item in ScenarioList)
            {
                scenarioList.Add(item.Key.ToString());
            }

            return scenarioList;
        }
        public Dictionary<long, List<StatusVariable>> GetReportList()
        {
            return new Dictionary<long, List<StatusVariable>>(ReportList);
        }
        public Dictionary<string, StatusVariable> GetStatusVariableList()
        {
            return new Dictionary<string, StatusVariable>(StatusVariableList);
        }
        public Dictionary<string, CollectionEvent> GetCollectionEventList()
        {
            return new Dictionary<string, CollectionEvent>(CollectionEventList);
        }
        public bool IsScenarioRunning(Enum scenario)
        {
            if (false == ScenarioList.ContainsKey(scenario))
                return false;

            return ScenarioList[scenario].Activate;
        }

        public void InitScenarioAll()
        {
            foreach (var item in ScenarioList)
            {
                SetScenarioActivation(item.Key, false);
            }
        }

        public void SetScenarioActivation(Enum scenario, bool activated)
        {
            if (false == ScenarioList.ContainsKey(scenario))
                return;

            ScenarioList[scenario].Activate = activated;
        }

        public EN_SCENARIO_RESULT ExecuteScenario(Enum scenario)
        {
            if (false == ScenarioList.ContainsKey(scenario))
                return EN_SCENARIO_RESULT.PROCEED;

            return ScenarioList[scenario].ExecuteScenario();
        }

        public void InitScenarioResultData(Enum scenario)
        {
            if (false == ScenarioList.ContainsKey(scenario))
                return;

            ScenarioList[scenario].InitResultData();
        }

        public int GetScenarioStep(Enum scenario)
        {
            if (false == ScenarioList.ContainsKey(scenario))
                return DefineSecsGem.Contants.SCENARIO_STEP_END;

            return ScenarioList[scenario].Step;
        }

        public bool UpdateTraceData(ref Dictionary<long, string> dataToUpdate)
        {
            if (false == _isTraceDataAvailable)
                return false;

            if (false == TickForTraceData.IsTickOver(true))
                return false;

            if (false == GetTraceDataValue(ref _traceDataFromProcessingScenario))
                return false;

            TickForTraceData.SetTickCount(_traceDataInterval);
         
            // 갱신이 필요한 변수만 넘긴다.
            return CompareTraceDataToUpdate(ref dataToUpdate);
        }
        #endregion </External Methods>

        #region <Virtual Methods>
        public virtual bool Init(string recipePath, string configPath, Dictionary<string, StatusVariable> statusVariableList,
                Dictionary<long, List<StatusVariable>> reportList, Dictionary<string, CollectionEvent> collectionEventList)
        {
            if (statusVariableList != null)
            {
                foreach (var item in statusVariableList)
                {
                    StatusVariableList[item.Key] = item.Value;
                }
            }

            if (reportList != null)
            {
                foreach (var item in reportList)
                {
                    ReportList[item.Key] = item.Value;
                }
            }

            if (collectionEventList != null)
            {
                foreach (var item in collectionEventList)
                {
                    CollectionEventList[item.Key] = item.Value;
                }
            }

            _recipePath = recipePath;

            _gemHandler.LinkConnection(UpdateVariablesAll);
            _gemHandler.LinkControlState(ControlStateChanged);
            _gemHandler.LinkRemoteCommand(CallBackRemoteCommand);
            _gemHandler.LinkEquipmentParameterChangeRequest(EquipmentParameterChangeRequested);
            _gemHandler.LinkClientToClientMessage(CallBackClientToClientMessage);
            _gemHandler.LinkSecsMessageReceived(SecsMessageReceived);
            _gemHandler.LinkRecipeControlGrant(CheckingRecipeControlGrant);
            _gemHandler.LinkFormattedRecipeControls(UploadingFormattedRecipeReceived, DownloadingFormattedRecipeReceived);
            _gemHandler.LinkUnFormattedRecipeControls(UploadingUnFormattedRecipeReceived, DownloadingUnFormattedRecipeReceived, UploadingUnFormattedRecipeAckReceived);
            _gemHandler.LinkRecipeFileIsDeleted(RecipeFileIsDeleted);
            _gemHandler.LinkTerminalMessageWithProcessingScenario(OnTerminalMessageReceived);

            MakeScenarioByConfigFiles(configPath);
            MakeCustomScenario();

            #region <Trace Data Scenario>
            List<long> traceDataIds = new List<long>();
            _isTraceDataAvailable = MakeScenarioToTraceData(ref _traceDataInterval, ref traceDataIds);
            _isTraceDataAvailable &= (traceDataIds != null && traceDataIds.Count > 0);
            
            if (_isTraceDataAvailable)
            {
                TickForTraceData.SetTickCount(_traceDataInterval);
            }
            #endregion </Trace Data Scenario>
          
            return true;
        }
        public virtual void Exit() { }
        public virtual bool AddECVList(Dictionary<string, EquipmentConstant> equipmentConstantList)
        {
            if (equipmentConstantList != null)
            {
                foreach (var item in equipmentConstantList)
                {
                    EquipmentConstantList[item.Key] = item.Value;
                }
            }

            return true;
        }
        protected virtual void MakeScenario(Enum typeOfScenario, ScenarioBaseClass scenario)
        {
            ScenarioList.TryAdd(typeOfScenario, scenario);
        }
        public virtual bool SendClientToClientMessage(string device, string messageName, string sendingType, string scenarioName, string[] contentNames, string[] messages, EN_MESSAGE_RESULT result, bool useLogging)
        {
            return _gemHandler.SendClientToClientMessage(device, messageName, sendingType, scenarioName, contentNames, messages, result, useLogging);
        }
        public virtual EN_PPGRANT CheckingRecipeControlGrant(string recipeName)
        {
            var state = EquipmentState_.EquipmentState.GetInstance().GetState();
            switch (state)
            {
                case EquipmentState_.EQUIPMENT_STATE.EXECUTING:
                    // 경우에 따라서 레시피 조작이 가능한지 여부 판단 후 코드 리턴
                    //
                    // return EN_PPGRANT.OK; or return EN_PPGRANT.BUSY;
                    break;

                case EquipmentState_.EQUIPMENT_STATE.FINISHING:
                case EquipmentState_.EQUIPMENT_STATE.INITIALIZE:
                case EquipmentState_.EQUIPMENT_STATE.READY:
                case EquipmentState_.EQUIPMENT_STATE.SETUP:
                    return EN_PPGRANT.BUSY;


                case EquipmentState_.EQUIPMENT_STATE.IDLE:
                case EquipmentState_.EQUIPMENT_STATE.PAUSE:
                    return EN_PPGRANT.OK;

                default:
                    return EN_PPGRANT.BUSY;
            }

            return EN_PPGRANT.BUSY;
        }
        public virtual void ExecuteReportAlarm(int alarmId, EN_GEM_ALARM_STATE state)
        {
            if (state.Equals(EN_GEM_ALARM_STATE.OCCURED))
            {
                _gemHandler.SetAlarm(alarmId);
            }
            else
            {
                _gemHandler.ClearAlarm(alarmId);
            }
        }
        public virtual void EquipmentParameterChangeRequested(string[] ecNameList, string[] valueList)
        {
            //var paramRange = PARAM_RANGE.GetInstance();
            for (int i = 0; i < ecNameList.Length; ++i)
            {
                string ecName = ecNameList[i];
                if (EquipmentConstantList != null && EquipmentConstantList.ContainsKey(ecName))
                {
                    PARAM_COMMON enCommonParam;
                    if (Enum.TryParse(ecName, out enCommonParam))
                    {
                        _recipe.SetValue(EN_RECIPE_TYPE.COMMON, enCommonParam.ToString(),
                            0, EN_RECIPE_PARAM_TYPE.VALUE, valueList[i]);
                    }

                    PARAM_EQUIPMENT enEquipmentParam;
                    if (Enum.TryParse(ecName, out enEquipmentParam))
                    {
                        _recipe.SetValue(EN_RECIPE_TYPE.EQUIPMENT, enEquipmentParam.ToString(),
                            0, EN_RECIPE_PARAM_TYPE.VALUE, valueList[i]);
                    }
                }
                
                //else
                //{
                //    if (arrIDs[i] >= paramRange.ECID_START && arrIDs[i] <= paramRange.ECID_END)
                //    {
                //        // Common
                //        if (arrIDs[i] >= paramRange.ECID_COMMON_START &&
                //            arrIDs[i] <= paramRange.ECID_COMMON_END)
                //        {
                //            int nIndex = (int)arrIDs[i] - paramRange.ECID_COMMON_START;
                //            PARAM_COMMON enParam = (PARAM_COMMON)nIndex;

                //            _recipe.SetValue(EN_RECIPE_TYPE.COMMON, enParam.ToString(),
                //                0, EN_RECIPE_PARAM_TYPE.VALUE, valueList[i]);
                //        }

                //        // Equip
                //        if (arrIDs[i] >= paramRange.ECID_EQUIP_START &&
                //            arrIDs[i] <= paramRange.ECID_EQUIP_END)
                //        {
                //            int nIndex = (int)arrIDs[i] - paramRange.ECID_EQUIP_START;

                //            PARAM_EQUIPMENT enParam = (PARAM_EQUIPMENT)nIndex;

                //            _recipe.SetValue(EN_RECIPE_TYPE.EQUIPMENT, enParam.ToString(), 0,
                //                EN_RECIPE_PARAM_TYPE.VALUE, valueList[i]);
                //        }
                //    }
                //}
            }
        }
        protected virtual void OnTerminalMessageReceived(string message)
        {

        }
        #endregion </Virtual Methods>

        #region <Abstract Methods>

        #region <Callback Message 관련>
        public abstract bool RemoteCommandReceived(string rcmdName, string[] cpNames, string[] cpValues, ref long[] results);
        public abstract bool ClientToClientMessageReceived(string device, string messageName, string sendingType, string scenarioName, string[] contentNames, string[] messages, EN_MESSAGE_RESULT result, ref bool useLogging);
        public abstract bool SecsMessageReceived(UserDefinedSecsMessage receivedSecsMessage, ref UserDefinedSecsMessage secsMessageToSend);
        #endregion </Callback Message 관련>
        
        #region <Variable 관련>
        public abstract void UpdateVariablesAll();
        public abstract bool UpdateECVParameter(string strECVName, string strValue);
        #endregion </Variable 관련>

        #region <Status 관련>
        public abstract void ControlStateChanged(string state);
		public abstract void EquipmentstateChanged(string state);
        #endregion </Status 관련>

        #region <Scenario 관련>
        public abstract Enum ConvertScenarioByName(string scenarioName);
        protected abstract void MakeCustomScenario();
        protected abstract bool MakeScenarioToTraceData(ref uint interval, ref List<long> traceDataVariableIds);
        protected abstract void MakeScenarioByConfigFiles(string configPath);
        public abstract bool IsScenarioEnabled(Enum scenario);
        public abstract List<string> GetScenarioParameterList(Enum scenario);
        public abstract Dictionary<string, string> GetScenarioResultData(Enum scenario);
        public abstract bool UpdateScenarioParams(string scenarioName, Dictionary<string, string> param);
        #endregion </Scenario 관련>

        #region <UnFormatted Recipe>
        public abstract bool UploadingUnFormattedRecipeReceived(string recipeName, ref string recipeFullPath);
        public abstract EN_ACK7 DownloadingUnFormattedRecipeReceived(string recipeName, string recipeFullPath);
        public abstract void UploadingUnFormattedRecipeAckReceived(string recipeName, EN_ACK7 recipeUploadAck);

        public abstract void RecipeFileIsDeleted(string[] deletedFileList);
        #endregion </UnFormatted Recipe>

        #region <Formatted Recipe>
        public abstract bool UploadingFormattedRecipeReceived(string recipeName, out Dictionary<string, SemiObject[]> recipeBodies);
        public abstract bool DownloadingFormattedRecipeReceived(string recipeName, Dictionary<string, string[]> recipeBodies);
        #endregion </Formatted Recipe>

        #region <주기호출>
        public abstract void Execute();
        #endregion </주기호출>
        #region <Trace Data>
        protected abstract bool GetTraceDataValue(ref Dictionary<long, string> dataToUpdate);
        #endregion </Trace Data>

        #endregion </Abstract Methods>

        #region <Protected/Private Methods>

        #region <Interface with Gem Driver>
        protected void SetControlState(EN_CONTROL_STATE state)
        {
            _gemHandler.SetControlState(state);
        }
        protected EN_CONTROL_STATE GetControlState()
        {
            return _gemHandler.GetControlState();
        }
        protected void SendEvent(long nEventID, long[] arrVids, string[] arrVidValues)
        {
            _gemHandler.SendEvent(nEventID, arrVids, arrVidValues);
        }
        protected void SendSecsMessage(long stream, long function, List<SemiObject> messageStructure)
        {
            _gemHandler.SendUserDefinedSecsMessage(stream, function, messageStructure);
        }
        protected bool IsSendingEventCompleted(long nEventID)
        {
            return _gemHandler.IsSendingEventCompleted(nEventID);
        }
        protected void UpdateVariable(long[] ids, string[] values)
        {
            _gemHandler.UpdateVariables(ids, values);
        }
        protected void UpdateEquipmentConstants(long[] ids, string[] values)
        {
            _gemHandler.UpdateECVParameters(ids, values);
        }
        protected void UpdateEquipmentConstants(string[] ecidNames, string[] values)
        {
            var ecidValues = new Dictionary<string, string>();
            for(int i = 0; i < ecidNames.Length; ++i)
            {
                ecidValues[ecidNames[i]] = values[i];
            }
            _gemHandler.UpdateECVParameters(ecidValues);
        }
        #endregion </Interface with Gem Driver>

        #region <Callback Wrapper>
        private bool CallBackClientToClientMessage(string device, string messageName, string sendingType, string scenarioName, string[] contentNames, string[] messages, EN_MESSAGE_RESULT result)
        {
            bool useLogging = true;

            bool resultCallback = ClientToClientMessageReceived(device, messageName, sendingType, scenarioName, contentNames, messages, result, ref useLogging);
            if (useLogging)
            {
                string messageOfLogging = String.Empty;
                string message = String.Empty;
                if (contentNames != null)
                {
                    int count = contentNames.Length;
                    for (int i = 0; i < count; ++i)
                    {
                        message = String.Format(" [{0} : {1}] ", contentNames[i], messages[i]);

                        messageOfLogging = String.Format("{0},{1}", messageOfLogging, message);
                    }
                }

                if (messageOfLogging.Length > 1 && messageOfLogging.Substring(0, 1).Equals(","))
                    messageOfLogging = messageOfLogging.Remove(0, 1);

                _gemHandler.WriteLog(String.Format("Received Client Message > TargetDevice : {0}, MessageName : {1}, Type : {2}, Scenario : {3}, Content : {4}, Result : {5}",
                    device, messageName, sendingType, scenarioName, messageOfLogging, result.ToString()));
            }

            return resultCallback;
        }

        private bool CallBackRemoteCommand(string rcmdName, string[] cpNames, string[] cpValues, ref long[] results)
        {
            return RemoteCommandReceived(rcmdName, cpNames, cpValues, ref results);
        }
        #endregion </Callback Wrapper>

        #region <Scenario>
        protected void UpdateScenarioParams(Enum scenario, ScenarioParamValues values)
        {
            if (false == ScenarioList.ContainsKey(scenario))
                return;

            ScenarioList[scenario].UpdateParamValues(values);
        }
        #endregion </Scenario>

        #region <Recipe>
        protected void UpdateScenarioPermission(Enum scenario, bool result)
        {
            if (false == ScenarioList.ContainsKey(scenario))
                return;

            ScenarioList[scenario].UpdatePermission(result);
        }
        protected string AddExtensionToFileName(string fileName)
        {
            string ex = System.IO.Path.GetExtension(fileName);
            string extension = Define.DefineConstant.FileFormat.FILEFORMAT_RECIPE;
            
            // 중간에 '.'가 들어가면 확장자가 다를 수 있다.
            if (String.IsNullOrEmpty(ex) || false == ex.Equals(extension))
            {
                fileName += Define.DefineConstant.FileFormat.FILEFORMAT_RECIPE;
            }

            return fileName;
        }
        protected bool LoadRecipe(string recipeName)
        {
            string ppid = AddExtensionToFileName(recipeName);
            _gemHandler.WriteLog(string.Format("> target file {0}\\{1}", _recipePath, ppid));

            if (false == FunctionsETC.FileExistCheck(_recipePath, ppid))
            {
                _gemHandler.WriteLog("> file not found");
                return false;
            }

            string path = _recipePath;
            string strErrorMsg = string.Empty;
            if (false == _recipe.LoadProcessRecipe(ref path, ref ppid, ref strErrorMsg))
            {
                _gemHandler.WriteLog(string.Format("> recipe load fail : {0}" + strErrorMsg));
                return false;
            }
            return true;
        }
        #endregion </Recipe>

        #region <Logging>
        protected void WriteLog(string logToWrite)
        {
            _gemHandler.WriteLog(logToWrite);
        }
        #endregion </Logging>

        #region <TraceData>
        private bool CompareTraceDataToUpdate(ref Dictionary<long, string> dataToUpdate)
        {
            dataToUpdate.Clear();
            foreach (var item in _traceDataFromProcessingScenario)
            {
                if (false == TraceDataToUpdate.TryGetValue(item.Key, out string value) ||
                    false == item.Value.Equals(value))
                {
                    dataToUpdate[item.Key] = item.Value;
                }

                TraceDataToUpdate[item.Key] = item.Value;
            }

            return dataToUpdate.Count > 0;
        }
        #endregion </TraceData>

        #endregion </Internal Methods>
    }
}