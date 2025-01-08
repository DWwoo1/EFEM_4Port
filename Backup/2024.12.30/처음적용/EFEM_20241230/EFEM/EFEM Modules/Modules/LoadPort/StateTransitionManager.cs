using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EFEM.Defines.LoadPort;
using TransferStateOnly;

namespace EFEM.Modules.LoadPort
{
    public class StateTransitionManager
    {
        #region <Constructors>
        public StateTransitionManager(int portId, ref LoadPortStateInformation information)
        {
            PortId = portId;
            StateInformation = information;
            //_transferState = LoadPortTransferStates.OutOfService;

            TransferStateTransitioner = new TransferState(PortId, new OutOfService(PortId), StateInformation);
            CarrierIdStateTransitioner = new CarrierIdStateOnly.CarrierIdState(PortId, new CarrierIdStateOnly.IdNotRead(PortId), StateInformation);
            CarrierSlotMapTransitioner = new CarrierSlotMapStateOnly.CarrierSlotMapState(PortId, new CarrierSlotMapStateOnly.IdNotRead(PortId), StateInformation);
        }
        #endregion </Constructors>

        #region <Fields>
        private readonly int PortId;
        private readonly LoadPortStateInformation StateInformation = null;
        private readonly TransferState TransferStateTransitioner = null;
        private readonly CarrierIdStateOnly.CarrierIdState CarrierIdStateTransitioner = null;
        private readonly CarrierSlotMapStateOnly.CarrierSlotMapState CarrierSlotMapTransitioner = null;
        #endregion </Fields>

        #region <Properties>
        public LoadPortTransferStates TransferState
        {
            get
            {
                return TransferStateTransitioner.CurrentTransferState;
            }
        }
        public CarrierIdVerificationStates CarrierIdState
        {
            get
            {
                return CarrierIdStateTransitioner.CurrentCarrierIdState;
            }
        }
        public CarrierSlotMapVerificationStates CarrierSlotMapState
        {
            get
            {
                return CarrierSlotMapTransitioner.CurrentCarrierSlotMapState;
            }
        }

        #endregion </Properties>

        #region <Methods>

        #region <Execute>
        public void InitTransferState()
        {
            TransferStateTransitioner.InitState();
        }

        public void ExecuteTransition()
        {
            TransferStateTransitioner.TransitState(StateInformation);
            CarrierIdStateTransitioner.TransitState(TransferState, StateInformation);
            CarrierSlotMapTransitioner.TransitState(TransferState, CarrierIdState, StateInformation);
        }
        #endregion </Execute>

        #region <Internals>

        #region <States>
        #endregion </States>

        #endregion </Internals>

        #endregion </Methods>
    }
}

namespace TransferStateOnly
{
    public class TransferState
    {
        #region <Constructors>
        public TransferState(int portId, BaseTransferState initialState, LoadPortStateInformation initInfo)
        {
            PortId = portId;
            _currentState = initialState;
            _currentInformation = new LoadPortStateInformation();

            // 현재 값은 참조가 아닌 별도의 객체로 저장
            initInfo.CopyTo(ref _currentInformation);
        }
        #endregion </Constructors>

        #region <Fields>
        protected BaseTransferState _currentState;
        protected LoadPortStateInformation _currentInformation;

        protected readonly int PortId;
        #endregion </Fields>

        #region <Properties>
        public LoadPortStateInformation CurrentStateInformation
        {
            get
            {
                return _currentInformation;
            }
        }
        public LoadPortTransferStates CurrentTransferState { get; private set; }
        #endregion </Properties>

        #region <Events>
        // 상태 변화 통지용 이벤트
        public delegate void StateChangedHandler(BaseTransferState newState);
        //public event StateChangedHandler OnStateChanged;
        #endregion </Events>

        #region <Methods>
        public void InitState()
        {
            if (false == (_currentState is OutOfService))
            {
                _currentState = new OutOfService(PortId);
                //_currentState.TransitState(this, _currentInformation);
            }
        }

        public void TransitState(LoadPortStateInformation newInfo)
        {
            if (false == newInfo.Enabled)
            {
                InitState();
            }
            else
            {
                _currentState.TransitState(this, newInfo);
            }

            //OnStateChanged?.Invoke(_currentState);

            // 상태 전이 이후 현재 상태를 동기화한다.
            newInfo.CopyTo(ref _currentInformation);
            CurrentTransferState = _currentState.StateName;
        }

        public void SetState(BaseTransferState newState)
        {
            if (_currentState.GetType() != newState.GetType())
            {
                System.Console.WriteLine(string.Format("Transit State : {0} -> {1}", _currentState.GetType().Name, newState.GetType().Name));
                _currentState = newState;
            }
        }
        #endregion </Methods>
    }

    public abstract class BaseTransferState
    {
        #region <Constructors>
        public BaseTransferState(int portId /*, LoadPortStateInformation initialInfo*/)
        {
            PortId = portId;
        }
        #endregion </Constructors>

        #region <Fields>
        protected readonly int PortId;
        #endregion </Fields>

        #region <Properties>
        public LoadPortTransferStates StateName { get; protected set; }
        #endregion </Properties>

        #region <Methods>
        public abstract void TransitState(TransferState newState, LoadPortStateInformation newInfo);

        #region <Check loadport status>
        // 캐리어가 정확히 놓여있다.(Placed, Present)
        protected bool IsCarrierCorrectlyPlaced(TransferState currentState)
        {
            return (currentState.CurrentStateInformation.Placed && currentState.CurrentStateInformation.Present);
        }

        // 클램핑 중인 상태이다.
        protected bool IsCurrentlyClampingStatus(TransferState currentState, LoadPortStateInformation newInfo)
        {
            if (currentState.CurrentStateInformation.ClampState != newInfo.ClampState)
            {
                return newInfo.ClampState;
            }

            return false;
        }

        // 문이 열린 상태이다.
        protected bool IsCurrentlyOpeningStatus(TransferState currentState, LoadPortStateInformation newInfo)
        {
            if (currentState.CurrentStateInformation.DoorState != newInfo.DoorState)
            {
                return newInfo.DoorState;
            }

            return false;
        }

        // 캐리어가 로딩되는 중이다.
        protected bool IsCurrentlyLoadingStatus(TransferState currentState, LoadPortStateInformation newInfo)
        {
            if (currentState.CurrentStateInformation.DockState != newInfo.DockState)
            {
                // 도킹이 해제되는 중이다.
                return newInfo.DockState;
            }

            return false;
        }

        // 캐리어가 언로딩되는 중이다.
        protected bool IsCurrentlyUnloadingStatus(TransferState currentState, LoadPortStateInformation newInfo)
        {
            // 2024.07.04. jhlim [MOD] 홈이 안 잡힌 상태에서는 체크하지 않는다.
            if (currentState.CurrentStateInformation.DockState != newInfo.DockState &&
                newInfo.Initialized &&
                (newInfo.CarrierAccessingState.Equals(CarrierAccessStates.CarrierCompleted) ||
                newInfo.CarrierAccessingState.Equals(CarrierAccessStates.CarrierStopped)))
            {
                // 도킹이 해제되는 중이다.
                return (false == newInfo.DockState);
            }

            return false;
        }
        #endregion </Check loadport status>

        #endregion </Methods>
    }

    public class OutOfService : BaseTransferState
    {
        public OutOfService(int portId /*, LoadPortStateInformation initialInfo*/) : base(portId/*, initialInfo*/)
        {
            StateName = LoadPortTransferStates.OutOfService;
        }

        public override void TransitState(TransferState newState, LoadPortStateInformation newInfo)
        {
            if (newState.CurrentStateInformation.Initialized)
            {
                newState.SetState(new InService(PortId));
            }
        }
    }

    public class InService : BaseTransferState
    {
        public InService(int portId /*, LoadPortStateInformation initialInfo*/) : base(portId/*, initialInfo*/)
        {
            StateName = LoadPortTransferStates.InService;
        }

        public override void TransitState(TransferState newState, LoadPortStateInformation newInfo)
        {
            if (IsCarrierCorrectlyPlaced(newState))
            {
                newState.SetState(new TransferBlocked(PortId));
            }
            else
            {
                newState.SetState(new ReadyToLoad(PortId));
            }
        }
    }

    public class TransferBlocked : BaseTransferState
    {
        public TransferBlocked(int portId /*, LoadPortStateInformation initialInfo*/) : base(portId/*, initialInfo*/)
        {
            StateName = LoadPortTransferStates.TransferBlocked;
        }

        private int _seqNum;

        public override void TransitState(TransferState newState, LoadPortStateInformation newInfo)
        {
            if (IsCurrentlyUnloadingStatus(newState, newInfo))          // 언도킹 되면
            {                
                newState.SetState(new ReadyToUnload(PortId));
            }
            else if (false == IsCarrierCorrectlyPlaced(newState) &&
                (false == newState.CurrentStateInformation.DoorState && false == newState.CurrentStateInformation.DoorState))       // 제거되면
            {
                newState.SetState(new ReadyToLoad(PortId));
            }
            else
            {
                switch (_seqNum)
                {
                    case 0:
                        if (false == IsCurrentlyClampingStatus(newState, newInfo) && false == IsCurrentlyLoadingStatus(newState, newInfo))
                            break;
                        ++_seqNum; break;
                    case 1:
                        // TODO : Carrier Id Verification
                        // OK 될 때까지 태스크에서는 기다려야 한다.
                        // 태스크에서는 OK 되면 도킹, Failed 면 에러
                        break;
                    case 2:
                        // TODO : Carrier Slot Map Verification
                        // OK 될 때까지 태스크에서는 기다려야 한다.
                        // 태스크에서는 이후 작업 진행, 여기선 default or 다음 스텝에서 계속 리턴
                        break;
                    default:
                        break;
                }
            }
        }
    }

    public class ReadyToLoad : BaseTransferState
    {
        public ReadyToLoad(int portId /*, LoadPortStateInformation initialInfo*/) : base(portId/*, initialInfo*/)
        {
            StateName = LoadPortTransferStates.ReadyToLoad;
        }

        public override void TransitState(TransferState newState, LoadPortStateInformation newInfo)
        {
            if (IsCarrierCorrectlyPlaced(newState))
            {
                newState.SetState(new TransferBlocked(PortId));
            }
        }
    }

    public class ReadyToUnload : BaseTransferState
    {
        public ReadyToUnload(int portId /*, LoadPortStateInformation initialInfo*/) : base(portId/*, initialInfo*/)
        {
            StateName = LoadPortTransferStates.ReadyToUnload;
        }

        public override void TransitState(TransferState newState, LoadPortStateInformation newInfo)
        {
            if (false == IsCarrierCorrectlyPlaced(newState) && false == newInfo.DockState && false == newInfo.DoorState)
            {
                newState.SetState(new ReadyToLoad(PortId));
            }
        }
    }
}

namespace CarrierIdStateOnly
{
    public class CarrierIdState
    {
        #region <Constructors>
        public CarrierIdState(int portId, BaseCarrierIdState initialState, LoadPortStateInformation initInfo)
        {
            PortId = portId;
            _currentState = initialState;
            _currentInformation = new LoadPortStateInformation();

            // 현재 값은 참조가 아닌 별도의 객체로 저장
            initInfo.CopyTo(ref _currentInformation);
        }
        #endregion </Constructors>

        #region <Fields>
        protected BaseCarrierIdState _currentState;
        protected LoadPortStateInformation _currentInformation;
        protected readonly int PortId;
        #endregion </Fields>

        #region <Properties>
        public LoadPortStateInformation CurrentStateInformation
        {
            get
            {
                return _currentInformation;
            }
        }
        public CarrierIdVerificationStates CurrentCarrierIdState { get; private set; }
        #endregion </Properties>

        #region <Methods>
        public void InitState()
        {
            if (false == (_currentState is IdNotRead))
            {
                _currentState = new IdNotRead(PortId);
            }
        }

        public void TransitState(LoadPortTransferStates transferState, LoadPortStateInformation newInfo)
        {
            if (false == transferState.Equals(LoadPortTransferStates.TransferBlocked))
            {
                InitState();
            }
            else
            {
                _currentState.TransitState(this, newInfo);
            }

            // 상태 전이 이후 현재 상태를 동기화한다.
            newInfo.CopyTo(ref _currentInformation);
            CurrentCarrierIdState = _currentState.StateName;
        }

        public void SetState(BaseCarrierIdState newState)
        {
            if (_currentState.GetType() != newState.GetType())
            {
                System.Console.WriteLine(string.Format("Carrier Id State : {0} -> {1}", _currentState.GetType().Name, newState.GetType().Name));
                _currentState = newState;
            }
        }
        #endregion </Methods>
    }

    public abstract class BaseCarrierIdState
    {
        #region <Constructors>
        public BaseCarrierIdState(int portId /*, LoadPortStateInformation initialInfo*/)
        {
            PortId = portId;
        }
        #endregion </Constructors>

        #region <Fields>
        protected readonly int PortId;
        #endregion </Fields>

        #region <Properties>
        public CarrierIdVerificationStates StateName { get; protected set; }
        #endregion </Properties>

        #region <Methods>
        public abstract void TransitState(CarrierIdState newState, LoadPortStateInformation newInfo);

        #region <Check loadport status>
        // 캐리어가 정확히 놓여있다.(Placed, Present)
        protected bool IsCarrierCorrectlyPlaced(CarrierIdState currentState)
        {
            return (currentState.CurrentStateInformation.Placed && currentState.CurrentStateInformation.Present);
        }

        // 클램핑 중인 상태이다.
        protected bool IsCurrentlyClampingStatus(CarrierIdState currentState, LoadPortStateInformation newInfo)
        {
            if (currentState.CurrentStateInformation.ClampState != newInfo.ClampState)
            {
                return newInfo.ClampState;
            }

            return false;
        }

        // 캐리어가 로딩되는 중이다.
        protected bool IsCurrentlyLoadingStatus(CarrierIdState currentState, LoadPortStateInformation newInfo)
        {
            if (currentState.CurrentStateInformation.DockState != newInfo.DockState)
            {
                // 도킹이 해제되는 중이다.
                return newInfo.DockState;
            }

            return false;
        }

        // 문이 열린 상태이다.
        protected bool IsCurrentlyOpeningStatus(CarrierIdState currentState, LoadPortStateInformation newInfo)
        {
            if (currentState.CurrentStateInformation.DoorState != newInfo.DoorState)
            {
                return newInfo.DoorState;
            }

            return false;
        }

        // 캐리어가 언로딩되는 중이다.
        protected bool IsCurrentlyUnloadingStatus(CarrierIdState currentState, LoadPortStateInformation newInfo)
        {
            if (currentState.CurrentStateInformation.DockState != newInfo.DockState)
            {
                // 도킹이 해제되는 중이다.
                return (false == newInfo.DockState);
            }

            return false;
        }
        #endregion </Check loadport status>

        #endregion </Methods>
    }

    public class IdNotRead : BaseCarrierIdState
    {
        public IdNotRead(int portId /*, LoadPortStateInformation initialInfo*/) : base(portId/*, initialInfo*/)
        {
            StateName = CarrierIdVerificationStates.NotRead;
        }

        public override void TransitState(CarrierIdState newState, LoadPortStateInformation newInfo)
        {
            // TODO : 여기에서 시나리오 실행
            if (IsCurrentlyClampingStatus(newState, newInfo) || IsCurrentlyLoadingStatus(newState, newInfo))
            {
                newState.SetState(new WaitingForHost(PortId));
            }
        }
    }

    public class WaitingForHost : BaseCarrierIdState
    {
        public WaitingForHost(int portId /*, LoadPortStateInformation initialInfo*/) : base(portId/*, initialInfo*/)
        {
            StateName = CarrierIdVerificationStates.WaitingForHost;
        }

        public override void TransitState(CarrierIdState newState, LoadPortStateInformation newInfo)
        {
            newState.SetState(new VerificationOk(PortId));
        }
    }

    public class VerificationOk : BaseCarrierIdState
    {
        public VerificationOk(int portId /*, LoadPortStateInformation initialInfo*/) : base(portId/*, initialInfo*/)
        {
            StateName = CarrierIdVerificationStates.VerificationOk;
        }

        public override void TransitState(CarrierIdState newState, LoadPortStateInformation newInfo)
        {
        }
    }

    public class VerificationFailed : BaseCarrierIdState
    {
        public VerificationFailed(int portId /*, LoadPortStateInformation initialInfo*/) : base(portId/*, initialInfo*/)
        {
            StateName = CarrierIdVerificationStates.VerificationFailed;
        }

        public override void TransitState(CarrierIdState newState, LoadPortStateInformation newInfo)
        {
        }
    }

    //public class CarrierIdState
    //{
    //    private BaseCarrierIdState state;

    //    public CarrierIdState(TransferStateOnly.TransferState transferState)
    //    {
    //        transferState.OnStateChanged += UpdateState;
    //        state = new IdNotRead(this); // 초기 상태 설정
    //    }

    //    public void SetState(BaseCarrierIdState newState)
    //    {
    //        this.state = newState;
    //    }

    //    public void UpdateState(TransferStateOnly.BaseTransferState newState)
    //    {
    //        if (newState is TransferStateOnly.TransferBlocked)
    //        {
    //            state.TransitState();
    //        }            
    //    }
    //}

    //public abstract class BaseCarrierIdState
    //{
    //    protected CarrierIdState _currentCarrierIdState;

    //    public BaseCarrierIdState(CarrierIdState carrierIdState)
    //    {
    //        _currentCarrierIdState = carrierIdState;
    //    }

    //    public abstract void TransitState();
    //}

    //public class IdNotRead : BaseCarrierIdState
    //{
    //    public IdNotRead(CarrierIdState state) : base(state) { }

    //    public override void TransitState()
    //    {
    //        _currentCarrierIdState.SetState(new WaitingForHost(_currentCarrierIdState));
    //    }
    //}

    //public class WaitingForHost : BaseCarrierIdState
    //{
    //    public WaitingForHost(CarrierIdState state) : base(state) { }

    //    public override void TransitState()
    //    {
    //        _currentCarrierIdState.SetState(new VerificationOk(_currentCarrierIdState));
    //    }
    //}

    //public class VerificationOk : BaseCarrierIdState
    //{
    //    public VerificationOk(CarrierIdState state) : base(state) { }

    //    public override void TransitState()
    //    {
    //        _currentCarrierIdState.SetState(new VerificationFailed(_currentCarrierIdState));
    //    }
    //}

    //public class VerificationFailed : BaseCarrierIdState
    //{
    //    public VerificationFailed(CarrierIdState state) : base(state) { }

    //    public override void TransitState()
    //    {
    //    }
    //}
}

namespace CarrierSlotMapStateOnly
{
    public class CarrierSlotMapState
    {
        #region <Constructors>
        public CarrierSlotMapState(int portId, BaseCarrierSlotMapState initialState, LoadPortStateInformation initInfo)
        {
            PortId = portId;
            _currentState = initialState;
            _currentInformation = new LoadPortStateInformation();

            // 현재 값은 참조가 아닌 별도의 객체로 저장
            initInfo.CopyTo(ref _currentInformation);
        }
        #endregion </Constructors>

        #region <Fields>
        protected BaseCarrierSlotMapState _currentState;
        protected LoadPortStateInformation _currentInformation;
        protected readonly int PortId;
        #endregion </Fields>

        #region <Properties>
        public LoadPortStateInformation CurrentStateInformation
        {
            get
            {
                return _currentInformation;
            }
        }
        public CarrierSlotMapVerificationStates CurrentCarrierSlotMapState { get; private set; }
        #endregion </Properties>

        #region <Methods>
        public void InitState()
        {
            if (false == (_currentState is IdNotRead))
            {
                _currentState = new IdNotRead(PortId);
            }
        }

        public void TransitState(LoadPortTransferStates transferState, CarrierIdVerificationStates idState, LoadPortStateInformation newInfo)
        {
            if (false == transferState.Equals(LoadPortTransferStates.TransferBlocked)
                || false == idState.Equals(CarrierIdVerificationStates.VerificationOk))
            {
                InitState();
            }
            else
            {
                _currentState.TransitState(this, newInfo);
            }

            // 상태 전이 이후 현재 상태를 동기화한다.
            newInfo.CopyTo(ref _currentInformation);
            CurrentCarrierSlotMapState = _currentState.StateName;
        }

        public void SetState(BaseCarrierSlotMapState newState)
        {
            if (_currentState.GetType() != newState.GetType())
            {
                System.Console.WriteLine(string.Format("SlotMap State : {0} -> {1}", _currentState.GetType().Name, newState.GetType().Name));
                _currentState = newState;
            }
        }
        #endregion </Methods>
    }

    public abstract class BaseCarrierSlotMapState
    {
        #region <Constructors>
        public BaseCarrierSlotMapState(int portId /*, LoadPortStateInformation initialInfo*/)
        {
            PortId = portId;
        }
        #endregion </Constructors>

        #region <Fields>
        protected readonly int PortId;
        #endregion </Fields>

        #region <Properties>
        public CarrierSlotMapVerificationStates StateName { get; protected set; }
        #endregion </Properties>

        #region <Methods>
        public abstract void TransitState(CarrierSlotMapState newState, LoadPortStateInformation newInfo);

        #region <Check loadport status>
        // 캐리어가 정확히 놓여있다.(Placed, Present)
        protected bool IsCarrierCorrectlyPlaced(CarrierSlotMapState currentState)
        {
            return (currentState.CurrentStateInformation.Placed && currentState.CurrentStateInformation.Present);
        }

        // 클램핑 중인 상태이다.
        protected bool IsCurrentlyClampingStatus(CarrierSlotMapState currentState, LoadPortStateInformation newInfo)
        {
            if (currentState.CurrentStateInformation.ClampState != newInfo.ClampState)
            {
                return newInfo.ClampState;
            }

            return false;
        }

        // 문이 열린 상태이다.
        protected bool IsCurrentlyOpeningStatus(CarrierSlotMapState currentState, LoadPortStateInformation newInfo)
        {
            if (currentState.CurrentStateInformation.DoorState != newInfo.DoorState)
            {
                return newInfo.DoorState;
            }

            return false;
        }

        // 캐리어가 언로딩되는 중이다.
        protected bool IsCurrentlyUnloadingStatus(CarrierSlotMapState currentState, LoadPortStateInformation newInfo)
        {
            if (currentState.CurrentStateInformation.DockState != newInfo.DockState)
            {
                // 도킹이 해제되는 중이다.
                return (false == newInfo.DockState);
            }

            return false;
        }
        #endregion </Check loadport status>
        #endregion </Methods>
    }

    public class IdNotRead : BaseCarrierSlotMapState
    {
        public IdNotRead(int portId /*, LoadPortStateInformation initialInfo*/) : base(portId/*, initialInfo*/)
        {
            StateName = CarrierSlotMapVerificationStates.NotRead;
        }

        public override void TransitState(CarrierSlotMapState newState, LoadPortStateInformation newInfo)
        {
            // TODO : 여기에서 시나리오 실행
            //if (IsCurrentlyOpeningStatus(newState, newInfo))
            {
                newState.SetState(new WaitingForHost(PortId));
            }
        }
    }

    public class WaitingForHost : BaseCarrierSlotMapState
    {
        public WaitingForHost(int portId /*, LoadPortStateInformation initialInfo*/) : base(portId/*, initialInfo*/)
        {
            StateName = CarrierSlotMapVerificationStates.WaitingForHost;
        }

        public override void TransitState(CarrierSlotMapState newState, LoadPortStateInformation newInfo)
        {
            newState.SetState(new VerificationOk(PortId));
        }
    }

    public class VerificationOk : BaseCarrierSlotMapState
    {
        public VerificationOk(int portId /*, LoadPortStateInformation initialInfo*/) : base(portId/*, initialInfo*/)
        {
            StateName = CarrierSlotMapVerificationStates.VerificationOk;
        }

        public override void TransitState(CarrierSlotMapState newState, LoadPortStateInformation newInfo)
        {
        }
    }

    public class VerificationFailed : BaseCarrierSlotMapState
    {
        public VerificationFailed(int portId /*, LoadPortStateInformation initialInfo*/) : base(portId/*, initialInfo*/)
        {
            StateName = CarrierSlotMapVerificationStates.VerificationFailed;
        }

        public override void TransitState(CarrierSlotMapState newState, LoadPortStateInformation newInfo)
        {
        }
    }

    //public class CarrierIdState
    //{
    //    private BaseCarrierIdState state;

    //    public CarrierIdState(TransferStateOnly.TransferState transferState)
    //    {
    //        transferState.OnStateChanged += UpdateState;
    //        state = new IdNotRead(this); // 초기 상태 설정
    //    }

    //    public void SetState(BaseCarrierIdState newState)
    //    {
    //        this.state = newState;
    //    }

    //    public void UpdateState(TransferStateOnly.BaseTransferState newState)
    //    {
    //        if (newState is TransferStateOnly.TransferBlocked)
    //        {
    //            state.TransitState();
    //        }            
    //    }
    //}

    //public abstract class BaseCarrierIdState
    //{
    //    protected CarrierIdState _currentCarrierIdState;

    //    public BaseCarrierIdState(CarrierIdState carrierIdState)
    //    {
    //        _currentCarrierIdState = carrierIdState;
    //    }

    //    public abstract void TransitState();
    //}

    //public class IdNotRead : BaseCarrierIdState
    //{
    //    public IdNotRead(CarrierIdState state) : base(state) { }

    //    public override void TransitState()
    //    {
    //        _currentCarrierIdState.SetState(new WaitingForHost(_currentCarrierIdState));
    //    }
    //}

    //public class WaitingForHost : BaseCarrierIdState
    //{
    //    public WaitingForHost(CarrierIdState state) : base(state) { }

    //    public override void TransitState()
    //    {
    //        _currentCarrierIdState.SetState(new VerificationOk(_currentCarrierIdState));
    //    }
    //}

    //public class VerificationOk : BaseCarrierIdState
    //{
    //    public VerificationOk(CarrierIdState state) : base(state) { }

    //    public override void TransitState()
    //    {
    //        _currentCarrierIdState.SetState(new VerificationFailed(_currentCarrierIdState));
    //    }
    //}

    //public class VerificationFailed : BaseCarrierIdState
    //{
    //    public VerificationFailed(CarrierIdState state) : base(state) { }

    //    public override void TransitState()
    //    {
    //    }
    //}
}