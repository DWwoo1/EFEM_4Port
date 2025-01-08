using System.Text;
using System;

using Define.DefineEnumBase.Common;
using EFEM.Defines.Common;
using EFEM.Defines.RFID;
using RFIDOnly;

namespace EFEM.Modules.RFID.Controllers
{
    public class RfidSimulator : RRFIDReader
    {
        #region <Constructors>
        public RfidSimulator(int portId, EN_CONNECTION_TYPE interfaceType, int commIndex)
            : base(portId, interfaceType, commIndex)
        {
        }
        #endregion </Constructors>

        #region <Fields>
        private string _lotId;
        private string _carrierId;
        #endregion </Fields>

        #region <Properties>

        #endregion </Properties>

        #region <Methods>

        #region <Actions>
        protected override CommandResults DoReadLotId(ref string lotId)
        {
            return ExecuteCommand(RfidCommand.READ_LOT_ID, ref lotId);
        }
        protected override CommandResults DoReadCarrierId(ref string carrierId)
        {
            return ExecuteCommand(RfidCommand.READ_CARRIER_ID, ref carrierId);
        }
        protected override CommandResults DoWriteLotId(string lotId)
        {
            return ExecuteCommand(RfidCommand.WRITE_LOT_ID, ref lotId);
        }
        protected override CommandResults DoWriteCarrierId(string carrierId)
        {
            return ExecuteCommand(RfidCommand.WRITE_CARRIER_ID, ref carrierId);
        }
        #endregion </Actions>

        #region <Execute>
        public override void Execute()
        {
        }
        #endregion </Execute>

        #endregion </Methods>

        private CommandResults ExecuteCommand(RfidCommand command, ref string result)
        {
            switch (_actionStep)
            {
                case 0:
                    {
                        _timeChecker.SetTickCount(1000);
                        _result.CommandResult = CommandResult.Proceed;
                        _result.Description = string.Empty;
                        ++_actionStep;
                    }
                    break;
                case 1:
                    {
                        if (_timeChecker.IsTickOver(true))
                            break;

                        switch (command)
                        {
                            case RfidCommand.READ_LOT_ID:
                                if (string.IsNullOrEmpty(_lotId))
                                    _lotId = DateTime.Now.ToString("HHmmss");
                                result = _lotId;
                                break;
                            case RfidCommand.READ_CARRIER_ID:
                                if (string.IsNullOrEmpty(_carrierId))
                                    _carrierId = string.Format("CARRIER{0:d2}", PortId);
                                result = _carrierId;
                                break;
                            case RfidCommand.WRITE_LOT_ID:
                                _lotId = result;
                                break;
                            case RfidCommand.WRITE_CARRIER_ID:
                                _carrierId = result;
                                break;
                        }

                        _result.CommandResult = CommandResult.Completed;
                    }
                    break;
              
            }

            if (false == _result.CommandResult.Equals(CommandResult.Proceed))
            {
                _doingAction = RfidCommand.IDLE;
                _actionStep = 0;
            }

            return _result;
        }
        protected override void ParseMessages(byte[] receivedMessage, RfidCommand command)
        {
        }        
    }
}
