using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using EFEM.Modules;
using EFEM.Defines.LoadPort;
using EFEM.Defines.MaterialTracking;
using EFEM.MaterialTracking;
using EFEM.MaterialTracking.LocationServer;

namespace FrameOfSystem3.Views.MapManager
{
    public delegate void DelegateCellClicked(int clickedMapIndex, Queue<int> polints);

    class CarrierMapManager
    {
        #region <Constructors>
        private CarrierMapManager()
        {
            _loadPortManager = LoadPortManager.Instance;
            CarrierMapControls = new ConcurrentDictionary<int, CarrierMap>();
            for (int i = 0; i < _loadPortManager.Count; ++i)
            {
                CarrierMapControls.TryAdd(i, new CarrierMap(i));
            }
        }
        #endregion </Constructors>

        #region <Fields>
        private static CarrierMapManager _instance = null;
        private static LoadPortManager _loadPortManager = null;
        private readonly ConcurrentDictionary<int, CarrierMap> CarrierMapControls = null;        
        #endregion </Fields>

        #region <Properties>
        public static CarrierMapManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new CarrierMapManager();

                return _instance;
            }
        }
        #endregion </Properties>

        #region <Methods>
        public void AssignMapControls(int lpIndex, ref Sys3Controls.Sys3Map mapControl, DelegateCellClicked callbackCellClicked = null)
        {
            if (false == CarrierMapControls.ContainsKey(lpIndex))
                return;

            CarrierMap map = CarrierMapControls[lpIndex];
            map.AddMap(ref mapControl, callbackCellClicked);

            CarrierMapControls[lpIndex] = map;
        }

        public void UpdateControls(int lpIndex)
        {
            if (false == CarrierMapControls.ContainsKey(lpIndex))
                return;

            CarrierMapControls[lpIndex].RefreshMaps();
        }
        public void DisableHighlight(int lpIndex, ref Sys3Controls.Sys3Map map)
        {
            if (false == CarrierMapControls.ContainsKey(lpIndex))
                return;

            CarrierMapControls[lpIndex].DisableHighlight(ref map);
        }
        #endregion </Methods>

    }

    class CarrierMap
    {
        #region <Constructors>
        public CarrierMap(int lpIndex)
        {
            Index = lpIndex;
            _loadPortManager = LoadPortManager.Instance;
            _carrierServer = CarrierManagementServer.Instance;
            _substrateManager = SubstrateManager.Instance;

            PortId = _loadPortManager.GetLoadPortPortId(Index);
            //_temporaryCarrier = new Carrier(PortId);

            MapControls = new List<Sys3Controls.Sys3Map>();
            ControlVisibilities = new List<bool>();

            SlotColors = new Dictionary<CarrierSlotMapStates, Color>();
            ProcessingColors = new Dictionary<ProcessingStates, Color>();
            SubstrateTransferColors = new Dictionary<SubstrateTransferStates, Color>();

            InitColors();

            Locking = new object();
            _locationServer = LocationServer.Instance;

            _eventHandlers = new Dictionary<Sys3Controls.Sys3Map, DelegateCellClicked>();
            _callbackCellClicked = new Sys3Controls.Sys3Map.DelegateGettingCellCoordinatesWithMap(CallbackCellClicked);
            ClickedCell = new Queue<int>();

            DataToUpdate = new Dictionary<int, MapData>();
            for (int i = 0; i < 25; ++i)
            {
                DataToUpdate[i] = new MapData();
            }
        }
        #endregion </Constructors>

        #region <Fields>
        private readonly int Index;
        private readonly int PortId;
        //private Carrier _carrier = null;
        //private Carrier _temporaryCarrier = null;
        private static LoadPortManager _loadPortManager = null;
        private static LocationServer _locationServer = null;
        private static CarrierManagementServer _carrierServer = null;
        private static SubstrateManager _substrateManager = null;

        private readonly List<Sys3Controls.Sys3Map> MapControls = null;
        private readonly List<bool> ControlVisibilities = null;
        private readonly Dictionary<Sys3Controls.Sys3Map, DelegateCellClicked> _eventHandlers = null;
        private readonly Dictionary<CarrierSlotMapStates, Color> SlotColors = null;
        private readonly Dictionary<ProcessingStates, Color> ProcessingColors = null;
        private readonly Dictionary<SubstrateTransferStates, Color> SubstrateTransferColors = null;
        private readonly Queue<int> ClickedCell = null;
        private readonly object Locking;
        private readonly Dictionary<int, MapData> DataToUpdate = null;
        //private CarrierSlotMapStates[] _slotStates = null;
        //private Substrate[] _substrates = null;

        private Color _temporaryColor;
        private Sys3Controls.Sys3Map.DelegateGettingCellCoordinatesWithMap _callbackCellClicked = null;
        #endregion </Fields>

        #region <Properties>
        #endregion </Properties>

        #region <Methods>
        private void InitColors()
        {
            Color colorNormal = Color.Silver;
            var colors = (CarrierSlotMapStates[])Enum.GetValues(typeof(CarrierSlotMapStates));
            foreach (var item in colors)
            {
                Color color = Color.Transparent;
                switch (item)
                {
                    case CarrierSlotMapStates.Undefined:
                    case CarrierSlotMapStates.Empty:
                        color = Color.White;
                        break;
                    case CarrierSlotMapStates.NotEmpty:
                        color = Color.DarkViolet;
                        break;
                    case CarrierSlotMapStates.CorrectlyOccupied:
                        color = colorNormal;
                        break;
                    case CarrierSlotMapStates.DoubleSlotted:
                        color = Color.DarkViolet;
                        break;
                    case CarrierSlotMapStates.CrossSlotted:
                        color = Color.Brown;
                        break;
                    default:
                        break;
                }

                if (color.Equals(Color.Transparent))
                    continue;

                SlotColors.Add(item, color);
            }

            var colors2 = (ProcessingStates[])Enum.GetValues(typeof(ProcessingStates));
            foreach (var item in colors2)
            {
                Color color = Color.Transparent;
                switch (item)
                {
                    case ProcessingStates.NeedsProcessing:
                        color = colorNormal;
                        break;
                    case ProcessingStates.InProcess:
                        color = Color.Blue;
                        break;
                    case ProcessingStates.Processed:
                        color = Color.Green;
                        break;
                    case ProcessingStates.Rejected:
                        color = Color.Orange;
                        break;
                    case ProcessingStates.Stopped:
                    case ProcessingStates.Aborted:
                    case ProcessingStates.Skipped:
                        color = Color.LightYellow;
                        break;
                    case ProcessingStates.Lost:
                        color = Color.Red;
                        break;
                    default:
                        break;
                }
                
                if (color.Equals(Color.Transparent))
                    continue;

                ProcessingColors.Add(item, color);
            }

            var colors3 = (SubstrateTransferStates[])Enum.GetValues(typeof(SubstrateTransferStates));
            foreach (var item in colors3)
            {
                Color color = Color.Transparent;
                switch (item)
                {
                    case SubstrateTransferStates.AtSource:
                        color = colorNormal;
                        break;
                    case SubstrateTransferStates.AtWork:
                        color = Color.Blue;
                        break;
                    case SubstrateTransferStates.AtDestination:
                        color = Color.LimeGreen;
                        break;
                    default:
                        break;
                }

                if (color.Equals(Color.Transparent))
                    continue;

                SubstrateTransferColors.Add(item, color);
            }
        }

        public void AddMap(ref Sys3Controls.Sys3Map map, DelegateCellClicked cellClicked = null)
        {
            if (cellClicked != null)
            {
                map.SetCallbackFunctionForGettingCell(ref _callbackCellClicked);
                _eventHandlers[map] = cellClicked;
            }

            MapControls.Add(map);
            ControlVisibilities.Add(map.Visible);
        }
        
        public void DisableHighlight(ref Sys3Controls.Sys3Map map)
        {
            for(int i = 0; i < MapControls.Count; ++i)
            {
                if (MapControls[i].Equals(map))
                {
                    MapControls[i].SetSingleCellHighlighted(-1, -1);
                    break;
                }
            }
        }

        public void RefreshMaps()
        {
            lock (Locking)
            {
                
                if (false == _carrierServer.HasCarrier(PortId))
                {
                    InitializeMap();
                }
                else
                {
                    UpdateMap();
                }
            }
        }

        private void InitializeMap()
        {
            // 캐리어가 없는 경우 맵 초기화
            for (int i = 0; i < MapControls.Count; ++i)
            {
                if (MapControls[i] == null)
                    continue;

                if (ControlVisibilities[i] != MapControls[i].Visible)
                {
                    ControlVisibilities[i] = MapControls[i].Visible;
                }

                if (false == MapControls[i].Visible)
                    continue;

                if (MapControls[i].MapSize.Height != 1)
                {
                    MapControls[i].MapSize = new Size(1, 1);
                    MapControls[i].SetCellColor(0, 0, MapControls[i].BackGroundColor);
                }
            }
        }

        private void UpdateMap()
        {
            #region <SlotMapState>
            // null 이거나 길이가 다르면 새로 초기화
            //if (_slotStates == null || _carrierServer.GetCapacity(PortId) != _slotStates.Length)
            //{
            //    _slotStates = new CarrierSlotMapStates[_carrierServer.GetCapacity(PortId)];
            //}
            //bool areEquals = Enumerable.SequenceEqual(_slotStates, _carrierServer.GetCarrierSlotMap(PortId));

            //// 다르면 배열 복사
            //if (false == areEquals)
            //{
            //    var originalSlots = _carrierServer.GetCarrierSlotMap(PortId);
            //    _slotStates = new CarrierSlotMapStates[originalSlots.Length];
            //    Array.Copy(originalSlots, _slotStates, originalSlots.Length);
            //}
            #endregion </SlotMapState>

            var slotStates = _carrierServer.GetCarrierSlotMap(PortId);
            //var substrates = _carrierServer.GetSubstrates(PortId);

            if (slotStates == null || slotStates.Length <= 0)// || substrates == null)
                return;

            //if (slotStates.Length != substrates.Length)
            //    return;

            int capacity = slotStates.Length;

            for (int slot = 0; slot < capacity; ++slot)
            {
                var state = slotStates[slot];
                
                if (DataToUpdate[slot] == null)
                {
                    DataToUpdate[slot] = new MapData();
                }

                if (false == _substrateManager.HasSubstrateAtLoadPort(PortId, slot))
                {
                    DataToUpdate[slot].CellColor = GetCellColorBySlotState(state);
                    DataToUpdate[slot].CellText = _substrateManager.GetSubstrateNameByDestinationPortId(PortId, slot);
                }
                else
                {
                    LoadPortLocation lpLocation = new LoadPortLocation(PortId, slot, "");
                    if (_locationServer.GetLoadPortSlotLocation(PortId, slot, ref lpLocation))
                    {
                        SubstrateTransferStates transferStatus = SubstrateTransferStates.AtSource;
                        ProcessingStates processingStatus = ProcessingStates.NeedsProcessing;
                        if (_substrateManager.GetTransferStatus(lpLocation, "", ref transferStatus) &&
                            _substrateManager.GetProcessingStatus(lpLocation, "", ref processingStatus))
                        {
                            DataToUpdate[slot].CellColor = GetCellColorByTransferState(transferStatus, processingStatus);
                            DataToUpdate[slot].CellText = _substrateManager.GetSubstrateNameAtLoadPort(lpLocation.PortId, lpLocation.Slot);                            
                        }
                        else
                        {
                            DataToUpdate[slot].CellColor = GetCellColorBySlotState(state);
                            DataToUpdate[slot].CellText = _substrateManager.GetSubstrateNameByDestinationPortId(lpLocation.PortId, lpLocation.Slot);
                        }
                    }
                    else
                    {
                        DataToUpdate[slot].CellColor = GetCellColorBySlotState(state);
                        DataToUpdate[slot].CellText = string.Empty;
                    }
                }
            }

            for (int i = 0; i < MapControls.Count; ++i)
            {
                if (MapControls[i] == null)
                    continue;

                if (ControlVisibilities[i] != MapControls[i].Visible)
                {
                    ControlVisibilities[i] = MapControls[i].Visible;
                }

                if (false == MapControls[i].Visible)
                    continue;

                if (MapControls[i].MapSize.Height != capacity)
                {
                    MapControls[i].MapSize = new Size(1, capacity);
                }

                for (int slot = 0; slot < capacity; ++slot)
                {
                    int cellIndex = capacity - slot - 1;

                    if (DataToUpdate.TryGetValue(slot, out MapData mapData))
                    {
                        MapControls[i].SetCellColor(0, cellIndex, mapData.CellColor);
                        MapControls[i].SetCellText(0, cellIndex, mapData.CellText);
                    }
                }
            }


            //for (int i = 0; i < MapControls.Count; ++i)
            //{
            //    if (MapControls[i] == null)
            //        continue;

            //    if (ControlVisibilities[i] != MapControls[i].Visible)
            //    {
            //        ControlVisibilities[i] = MapControls[i].Visible;
            //    }

            //    if (false == MapControls[i].Visible)
            //        continue;

            //    if (MapControls[i].MapSize.Height != capacity)
            //    {
            //        MapControls[i].MapSize = new Size(1, capacity);
            //    }

            //    for (int slot = 0; slot < capacity; ++slot)
            //    {
            //        int cellIndex = capacity - slot - 1;
            //        var state = slotStates[slot];

            //        if (false == _substrateManager.HasSubstrateAtLoadPort(PortId, slot))
            //        {
            //            FillCellColorBySlotState(i, cellIndex, state);
            //            MapControls[i].SetCellText(0, cellIndex, _substrateManager.GetSubstrateNameByDestinationPortId(PortId, slot));
            //        }
            //        else
            //        {
            //            LoadPortLocation lpLocation = new LoadPortLocation(PortId, slot, "");
            //            if (_locationServer.GetLoadPortSlotLocation(PortId, slot, ref lpLocation))
            //            {
            //                SubstrateTransferStates transferStatus = SubstrateTransferStates.AtSource;
            //                ProcessingStates processingStatus = ProcessingStates.NeedsProcessing;
            //                if (_substrateManager.GetTransferStatus(lpLocation, "", ref transferStatus) &&
            //                    _substrateManager.GetProcessingStatus(lpLocation, "", ref processingStatus))
            //                {
            //                    FillCellColorByTransferState(i, cellIndex, transferStatus, processingStatus);

            //                    // Substrate 이름 표기
            //                    MapControls[i].SetCellText(0, cellIndex, _substrateManager.GetSubstrateNameAtLoadPort(lpLocation.PortId, lpLocation.Slot));
            //                }
            //                else
            //                {
            //                    FillCellColorBySlotState(i, cellIndex, state);
            //                    MapControls[i].SetCellText(0, cellIndex, _substrateManager.GetSubstrateNameByDestinationPortId(lpLocation.PortId, lpLocation.Slot));
            //                    //if (state.Equals(CarrierSlotMapStates.CorrectlyOccupied))
            //                    //{
            //                    //}
            //                    //else
            //                    //{
            //                    //    MapControls[i].SetCellText(0, cellIndex, string.Empty);
            //                    //}
            //                }
            //            }
            //            else
            //            {
            //                FillCellColorBySlotState(i, cellIndex, state);
            //            }
            //        }
            //    }
            //}
        }

        private void FillCellColorByTransferState(int controlIndex, int slot, SubstrateTransferStates transferStatus, ProcessingStates processingStatus)
        {
            switch (processingStatus)
            {
                case ProcessingStates.NeedsProcessing:
                case ProcessingStates.InProcess:
                 case ProcessingStates.Processed:
                    {
                        switch (transferStatus)
                        {
                            case SubstrateTransferStates.AtWork:
                            case SubstrateTransferStates.AtSource:
                                {
                                    if (ProcessingColors.TryGetValue(processingStatus, out _temporaryColor))
                                    {
                                        MapControls[controlIndex].SetCellColor(0, slot, _temporaryColor);
                                    }
                                }
                                break;
                            case SubstrateTransferStates.AtDestination:
                                {
                                    if (SubstrateTransferColors.TryGetValue(transferStatus, out _temporaryColor))
                                    {
                                        MapControls[controlIndex].SetCellColor(0, slot, _temporaryColor);
                                    }
                                }
                                break;

                            default:
                                break;
                        }
                    }
                    break;
                case ProcessingStates.Rejected:
                case ProcessingStates.Stopped:
                case ProcessingStates.Aborted:
                case ProcessingStates.Skipped:
                case ProcessingStates.Lost:
                    {
                        if (ProcessingColors.TryGetValue(processingStatus, out _temporaryColor))
                        {
                            MapControls[controlIndex].SetCellColor(0, slot, _temporaryColor);
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        private void FillCellColorBySlotState(int controlIndex, int slot, CarrierSlotMapStates state)
        {
            switch (state)
            {
                case CarrierSlotMapStates.CorrectlyOccupied:
                    {
                        // 있었는데요, 없었습니다.
                        if (SlotColors.TryGetValue(CarrierSlotMapStates.Empty, out _temporaryColor))
                        {
                            MapControls[controlIndex].SetCellColor(0, slot, _temporaryColor);
                        }
                    }
                    break;
                default:
                    {
                        if (SlotColors.TryGetValue(state, out _temporaryColor))
                        {
                            MapControls[controlIndex].SetCellColor(0, slot, _temporaryColor);
                        }
                    }
                    break;
            }
        }
        private Color GetCellColorByTransferState(SubstrateTransferStates transferStatus, ProcessingStates processingStatus)
        {
            switch (processingStatus)
            {
                case ProcessingStates.NeedsProcessing:
                case ProcessingStates.InProcess:
                case ProcessingStates.Processed:
                    {
                        switch (transferStatus)
                        {
                            case SubstrateTransferStates.AtWork:
                            case SubstrateTransferStates.AtSource:
                                {
                                    if (ProcessingColors.TryGetValue(processingStatus, out _temporaryColor))
                                    {
                                        return _temporaryColor;
                                    }
                                }
                                break;
                            case SubstrateTransferStates.AtDestination:
                                {
                                    if (SubstrateTransferColors.TryGetValue(transferStatus, out _temporaryColor))
                                    {
                                        return _temporaryColor;
                                    }
                                }
                                break;

                            default:
                                break;
                        }
                    }
                    break;
                case ProcessingStates.Rejected:
                case ProcessingStates.Stopped:
                case ProcessingStates.Aborted:
                case ProcessingStates.Skipped:
                case ProcessingStates.Lost:
                    {
                        if (ProcessingColors.TryGetValue(processingStatus, out _temporaryColor))
                        {
                            return _temporaryColor;
                        }
                    }
                    break;
                default:
                    break;
            }

            return Color.White;
        }
        private Color GetCellColorBySlotState(CarrierSlotMapStates state)
        {
            switch (state)
            {
                case CarrierSlotMapStates.CorrectlyOccupied:
                    {
                        // 있었는데요, 없었습니다.
                        if (SlotColors.TryGetValue(CarrierSlotMapStates.Empty, out _temporaryColor))
                        {
                            return _temporaryColor;
                        }
                    }
                    break;
                default:
                    {
                        if (SlotColors.TryGetValue(state, out _temporaryColor))
                        {
                            return _temporaryColor;
                        }
                    }
                    break;
            }

            return Color.White;
        }
        private void CallbackCellClicked(Sys3Controls.Sys3Map sender, Queue<Point> clickedPoint)
        {
            if (false == _carrierServer.HasCarrier(PortId))
                return;

            lock (Locking)
            {
                if (_eventHandlers.ContainsKey(sender))
                {
                    int capacity = _carrierServer.GetCapacity(PortId);

                    ClickedCell.Clear();
                    foreach (var item in clickedPoint)
                    {
                        int cellIndex = capacity - item.Y - 1;
                        ClickedCell.Enqueue(cellIndex);
                    }

                    if (sender.UseClick)
                    {
                        Point point = clickedPoint.Last();
                        if (point == null)
                            return;

                        sender.SetSingleCellHighlighted(point.X, point.Y);
                    }
                    _eventHandlers[sender](Index, ClickedCell);
                }
            }
        }
        #endregion </Methods>
    }

    class MapData
    {
        #region <Properties>
        public Color CellColor { get; set; }
        public string CellText { get; set; }
        public bool SetHighlight { get; set; }
        #endregion </Properties>
    }
}
