﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EquipmentState_;
using TickCounter_;

using EFEM.Defines.Common;
using EFEM.Defines.MaterialTracking;
using EFEM.Modules.ProcessModule;
using EFEM.Modules.ProcessModule.Communicator;
using EFEM.MaterialTracking.LocationServer;
using EFEM.MaterialTracking;

namespace EFEM.CustomizedByProcessType.PWA500W
{
    public class ProcessModulePWA500W : BaseProcessModule
    {
        #region <Constructors>
        public ProcessModulePWA500W(int moduleIndex, BaseProcessModuleCommunicator communicator, string name, string[] portNames, bool simulation)
            : base(moduleIndex, communicator, name, portNames, simulation) 
        {
            HandlingLoadRequestedForSimulator = new ConcurrentDictionary<string, bool>();
            HandlingUnloadRequestedForSimulator = new ConcurrentDictionary<string, bool>();
            TickForLoading = new Dictionary<string, TickCounter>();
            TickForUnloading = new Dictionary<string, TickCounter>();

            for (int i = 0; i < LocationNames.Length; ++i)
            {
                if (LocationNames[i].Contains(Constants.LoadingToken))
                {
                    HandlingLoadRequestedForSimulator[LocationNames[i]] = false;
                    TickForLoading[LocationNames[i]] = new TickCounter();
                }
                else
                {
                    HandlingUnloadRequestedForSimulator[LocationNames[i]] = false;
                    TickForUnloading[LocationNames[i]] = new TickCounter();
                }
            }

            MinTicksForCoreHandling = 5000;
            MinTicksForSortHandling = 7000;

            Task.Run(() => InitCommunication());
        }
        #endregion </Constructors>

        #region <Fields>
        private const int MaxCapacityCore = 3;
        private const int MaxCapacityBin = 3;
        private const int ProcessingTime = 5;       // Sec
        private const string HandlingResultOk = "Ok";
        private readonly ConcurrentDictionary<string, bool> HandlingLoadRequestedForSimulator = null;
        private readonly ConcurrentDictionary<string, bool> HandlingUnloadRequestedForSimulator = null;

        private readonly uint MinTicksForCoreHandling;
        private readonly uint MinTicksForSortHandling;

        private readonly Dictionary<string, TickCounter> TickForLoading = null;
        private readonly Dictionary<string, TickCounter> TickForUnloading = null;
        #endregion </Fields>

        #region <Properties>
        #endregion </Properties>

        #region <Methods>

        #region <Override Methods>       
        public override void SetLoadingSignal(string location, bool enabled)
        {
            if (IsSimulation && false == enabled)
            {
                if (false == TickForLoading.ContainsKey(location))
                    return;

                if (location.Contains("Core"))
                {
                    TickForLoading[location].SetTickCount(MinTicksForCoreHandling);
                }
                else
                {
                    TickForLoading[location].SetTickCount(MinTicksForSortHandling);
                }
            }
            base.SetLoadingSignal(location, enabled);
        }

        public override void SetUnloadingSignal(string location, bool enabled)
        {
            if (IsSimulation && false == enabled)
            {
                if (false == TickForUnloading.ContainsKey(location))
                    return;

                if (location.Contains("Core"))
                {
                    TickForUnloading[location].SetTickCount(MinTicksForCoreHandling);
                }
                else
                {
                    TickForUnloading[location].SetTickCount(MinTicksForSortHandling);
                }
            }
            base.SetUnloadingSignal(location, enabled);
        }
        protected override void MappingCommunicatorPortByLocation(string[] locations, ref int[] ports)
        {
            int simulationOffset = 0;
            if (IsSimulation)
                simulationOffset = 5;

            for (int i = 0; i < locations.Length; ++i)
            {
                string location = locations[i];
                WCFClientIndex clientIndex = WCFClientIndex.Core_8_In;
                bool invalidLocation = false;
                switch (location)
                {
                    case Constants.ProcessModuleCore_8_InputName:
                        clientIndex = WCFClientIndex.Core_8_In;
                        break;
                    case Constants.ProcessModuleCore_8_OutputName:
                        clientIndex = WCFClientIndex.Core_8_Out;
                        break;
                    case Constants.ProcessModuleCore_12_InputName:
                        clientIndex = WCFClientIndex.Core_12_In;
                        break;
                    case Constants.ProcessModuleCore_12_OutputName:
                        clientIndex = WCFClientIndex.Core_12_Out;
                        break;
                    case Constants.ProcessModuleSort_12_InputName:
                        clientIndex = WCFClientIndex.Sort_12_In;
                        break;
                    case Constants.ProcessModuleSort_12_OutputName:
                        clientIndex = WCFClientIndex.Sort_12_Out;
                        break;

                    default:
                        invalidLocation = true;
                        break;
                }

                if (invalidLocation)
                    continue;

                ports[i] = (int)clientIndex + simulationOffset;
            }
        }
        protected override bool IsLoadingRequestReceived(ref List<string> locationNames)
        {
            if (IsSimulation)
            {
                return GetLoadingRequestForSimulator(ref locationNames);
            }

            return false;
        }
        protected override bool IsUnloadingRequestReceived(ref List<string> locationNames)
        {
            if (IsSimulation)
            {
                return GetUnloadingRequestForSimulator(ref locationNames);
            }

            return false;
        }
        protected override void Executing()
        {
            if (IsSimulation)
            {
                UpdateProcessStates();
                MoveSubstrateLocationLoadToUnloadForSimulator();
            }
        }

        #region <WCF>

        #region <Send>
        public override bool SendMessage(Location location, string title, string substrateName)
        {
            return SendRequestMessage(location, title, substrateName);
        }
        public override bool SendMessage(Location location, string title, Dictionary<string, string> messagePairs)
        {
            return SendRequestMessage(location, title, messagePairs);
        }
        public override CommunicationResult IsSendingCompleted(Location location, string title)
        {
            return _communicator.IsSendingCompleted(PortIdByLocation[location.Name], title);
        }
        public override bool GetSendingResult(Location location, string title, ref Dictionary<string, string> receivedData)
        {
            return false;
        }
        #endregion </Send>

        #region <Receive>
        public override void SetAckToReceivedMessage(Location location, string title, CommunicationResult result, string description)
        {
            _communicator.SetAckToReceivedMessage(PortIdByLocation[location.Name], title, result, description);
        }
        public override CommunicationResult IsMessageReceived(Location location, string title)
        {
            return _communicator.IsMessageReceived(PortIdByLocation[location.Name], title);
        }
        public override bool GetReceivedData(Location location, string title, ref Dictionary<string, string> receivedData)
        {
            return _communicator.GetReceivedData(PortIdByLocation[location.Name], title, ref receivedData);            
        }
        #endregion </Receive>

        #endregion </WCF>

        #endregion </Override Methods>

        #region <Internals>
        private void UpdateProcessStates()
        {
            DateTime currentTime = DateTime.Now;
            foreach (var item in Substrates)
            {
                if (false == item.Value.GetProcessingStatus().Equals(ProcessingStates.Processed))
                {
                    var elapsedTime = currentTime - item.Key;
                    if (elapsedTime.TotalSeconds >= ProcessingTime)
                    {
                        Substrates[item.Key].SetProcessingStatus(ProcessingStates.Processed);
                    }
                }
            }
        }
        private bool IsEntryLocation(string location)
        {
            return location.Contains(Constants.LoadingToken);
        }
        private bool IsExitLocation(string location)
        {
            return location.Contains(Constants.UnloadingToken);
        }
        
        #region <Simulation Only>
        private bool GetLoadingRequestForSimulator(ref List<string> locationNames)
        {
            foreach (var item in HandlingLoadRequestedForSimulator)
            {
                if (false == TickForLoading[item.Key].IsTickOver(true))
                    continue;

                if (item.Value)
                    locationNames.Add(item.Key);
            }

            return locationNames.Count > 0;
            //foreach (var item in SubstratesInHandlingLocation)
            //{
            //    // 출구가 비어있으면 해당 입구를 반환한다.
            //    if (item.Value == null && IsExitLocation(item.Key))
            //    {
            //        string handlingLocationName = string.Empty;
            //        if (item.Key.Contains(Constants.CoreName))
            //        {
            //            handlingLocationName = string.Format("PM1.{0}{1}", Constants.CoreName, Constants.LoadingToken);

            //        }
            //        else if (item.Key.Contains(Constants.SortName))
            //        {
            //            handlingLocationName = string.Format("PM1.{0}{1}", Constants.SortName, Constants.LoadingToken);
            //        }

            //        if (SubstratesInHandlingLocation.ContainsKey(handlingLocationName))
            //        {
            //            locationNames.Add(handlingLocationName);
            //        }
            //    }
            //}

            //return locationNames.Count > 0;
        }
        private bool GetUnloadingRequestForSimulator(ref List<string> locationNames)
        {
            foreach (var item in HandlingUnloadRequestedForSimulator)
            {
                if (false == TickForUnloading[item.Key].IsTickOver(true))
                    continue;

                if (item.Value)
                    locationNames.Add(item.Key);
            }

            return locationNames.Count > 0;
            //foreach (var item in SubstratesInHandlingLocation)
            //{
            //    if (item.Value != null && IsExitLocation(item.Key))
            //    {
            //        locationNames.Add(item.Key);
            //    }
            //}

            //return locationNames.Count > 0;
        }
        // 2025.02.13 dwlim [MOD] 500W의 Substrate Type에 맞게 수정 
        private void MoveSubstrateLocationLoadToUnloadForSimulator()
        {
            int countCore = 0, countCore_8 = 0, countCore_12 = 0, countBin = 0;
            int countCore_8_Completed = 0, countCore_12_Completed = 0, countBinCompleted = 0;
            foreach (var item in Substrates)
            {
                string subType = item.Value.GetAttribute(PWA500WSubstrateAttributes.SubstrateType);
                if (false == Enum.TryParse(subType, out SubstrateType convertedType))
                    continue;

                switch (convertedType)
                {
                    case SubstrateType.Core_8:
                        {
                            ++countCore_8;
                            if (item.Value.GetProcessingStatus().Equals(ProcessingStates.Processed))
                            {
                                ++countCore_8_Completed;
                            }
                        }
                        break;
                    case SubstrateType.Core_12:
                        {
                            ++countCore_12;
                            if (item.Value.GetProcessingStatus().Equals(ProcessingStates.Processed))
                            {
                                ++countCore_12_Completed;
                            }
                        }
                        break;
                    case SubstrateType.Bin_12:
                        {
                            ++countBin;
                            if (item.Value.GetProcessingStatus().Equals(ProcessingStates.Processed))
                            {
                                ++countBinCompleted;
                            }
                        }
                        break;
                    default:
                        break;
                }               
            }

            HandlingLoadRequestedForSimulator[Constants.ProcessModuleCore_8_InputName] = (countCore_8 < MaxCapacityCore);
            HandlingUnloadRequestedForSimulator[Constants.ProcessModuleCore_8_OutputName] = (countCore_8_Completed >= 1);
            //HandlingLoadRequestedForSimulator[Constants.ProcessModuleCore_12_InputName] = (countCore_12 < MaxCapacityCore);
            //HandlingUnloadRequestedForSimulator[Constants.ProcessModuleCore_12_OutputName] = (countCore_12_Completed >= 1);
            HandlingLoadRequestedForSimulator[Constants.ProcessModuleSort_12_InputName] = (countBin < MaxCapacityBin);
            HandlingUnloadRequestedForSimulator[Constants.ProcessModuleSort_12_OutputName] = (countBinCompleted >= 1);
        }
        // 2025.02.13 dwlim [END]
        #endregion </Simulation Only>

        #region <WCF>
        private void MakeStructureToSend(Substrate substrate, ref Dictionary<string, string> messageToSend)
        {
            messageToSend.Clear();

            messageToSend[PWA500WSubstrateAttributes.HandlingResult] = HandlingResultOk;
            if (substrate == null)
            {
                messageToSend[PWA500WSubstrateAttributes.SubstrateName] = string.Empty;
                messageToSend[PWA500WSubstrateAttributes.LotId] = string.Empty;
                messageToSend[PWA500WSubstrateAttributes.RecipeId] = string.Empty;
                messageToSend[PWA500WSubstrateAttributes.SubstrateType] = string.Empty;
                messageToSend[PWA500WSubstrateAttributes.RingId] = string.Empty;
                messageToSend[PWA500WSubstrateAttributes.PortId] = string.Empty;
                messageToSend[PWA500WSubstrateAttributes.SlotId] = string.Empty;
            }
            else
            {
                //var attributes = substrate.GetAttributesAll();

                messageToSend[PWA500WSubstrateAttributes.SubstrateName] = substrate.GetName();
                messageToSend[PWA500WSubstrateAttributes.LotId] = substrate.GetLotId();
                messageToSend[PWA500WSubstrateAttributes.RecipeId] = substrate.GetRecipeId();
                messageToSend[PWA500WSubstrateAttributes.RingId] = substrate.GetName();
                messageToSend[PWA500WSubstrateAttributes.PortId] = substrate.GetSourcePortId().ToString();
                messageToSend[PWA500WSubstrateAttributes.SlotId] = substrate.GetSourceSlot().ToString();
                
                // TODO : 이걸 왜 이렇게 했지? 나중에 다시 보자
                //switch (substrate.GetSourcePortId())
                //{
                //    case 1:
                //    case 2:
                //    case 3:
                //        messageToSend[PWA500WSubstrateAttributes.SubstrateType] = SubstrateType.Bin2.ToString();
                //        break;
                //    case 4:
                //        messageToSend[PWA500WSubstrateAttributes.SubstrateType] = SubstrateType.Empty.ToString();
                //        break;
                //    case 5:
                //    case 6:
                //        messageToSend[PWA500WSubstrateAttributes.SubstrateType] = SubstrateType.Core.ToString();
                //        break;
                //    default:
                //        break;
                //}

            }
        }
        private Dictionary<string, string> MakeMessagesBySubstrateName(Location location, string title, string substrateName)
        {
            Dictionary<string, string> messageToSend = new Dictionary<string, string>();

            Substrate substrate = null;
            if (title.Contains("Unloading"))
            {
                foreach (var item in Substrates)
                {
                    if (item.Value.GetName().Equals(substrateName))
                    {
                        substrate = item.Value;
                    }
                }

                //if (substrate == null)
                //    return null;
            }
            else
            {
                substrate = new Substrate("");
                if (false == SubstrateManager.Instance.GetSubstrateByName(substrateName, ref substrate))
                    return null;
            }
            
            MakeStructureToSend(substrate, ref messageToSend);

            return messageToSend;
        }
        private bool SendRequestMessage(Location location, string title, Dictionary<string, string> messagePairs)
        {
            if (false == Enum.TryParse(title, out RequestMessages _))
                return false;

            if (false == PortIdByLocation.ContainsKey(location.Name))
                return false;

            return _communicator.SendMessage(PortIdByLocation[location.Name], title, messagePairs);
        }
        private bool SendRequestMessage(Location location, string title, string substrateName)
        {
            if (false == Enum.TryParse(title, out RequestMessages _))
                return false;

            if (false == PortIdByLocation.ContainsKey(location.Name))
                return false;

            var structure = MakeMessagesBySubstrateName(location, title, substrateName);
            if (structure == null)
                return false;

            return _communicator.SendMessage(PortIdByLocation[location.Name], title, structure);
        }
        #endregion </WCF>

        #endregion </Internals>

        #endregion </Methods>
    }
}