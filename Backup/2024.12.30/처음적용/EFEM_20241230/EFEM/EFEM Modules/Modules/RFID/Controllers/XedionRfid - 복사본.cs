using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RFIDOnly;
using EFEM.Defines.Common;
using EFEM.Defines.RFID;
using Define.DefineEnumBase.Common;

namespace EFEM.Modules.RFID.Controllers
{
    public class XedionRfid : RRFIDReader
    {
        #region <Constructors>
        public XedionRfid(int portId, EN_CONNECTION_TYPE interfaceType, int commIndex)
            : base(portId, interfaceType, commIndex)
        {
            _transaction = new XedionTransaction(commIndex);
        }
        #endregion </Constructors>

        #region <Fields>
        private readonly XedionTransaction _transaction = null;
        private byte[] _receiveMessage;
        private bool WriteCommandAckFlag = false;
        #endregion </Fields>

        #region <Properties>

        #endregion </Properties>

        #region <Methods>

        #region <Actions>
        public override void InitAction()
        {
            base.InitAction();
            _transaction.ErrorFlag = false;
            _transaction.ByteLotId = null;
            WriteCommandAckFlag = false;
        }

        protected override CommandResults DoReadLotId(ref string lotId)
        {
            if (_transaction.ErrorFlag == true)
            {
                _transaction.ErrorFlag = false;
                return new CommandResults(RfidCommand.READ_LOT_ID.ToString(), CommandResult.Error, _transaction.ErrorDescription);
            }

            var result = ExecuteReadLotId(RfidCommand.READ_LOT_ID);
            if (result.CommandResult == CommandResult.Completed)
            {
                lotId = _lotId;
            }
            return result;
            //return new CommandResults(RfidCommand.READ_LOT_ID.ToString(), CommandResult.Completed);
        }
        protected override CommandResults DoReadCarrierId(ref string CarrierId)
        {
            if (_transaction.ErrorFlag == true)
            {
                _transaction.ErrorFlag = false;
                return new CommandResults(RfidCommand.READ_CARRIER_ID.ToString(), CommandResult.Error, _transaction.ErrorDescription);
            }
            if (_transaction.ByteCarrierId != null)
            {
                CarrierId = StringHexToStringAscii(Encoding.ASCII.GetString(_transaction.ByteCarrierId));
                _transaction.ByteCarrierId = null;
                _actionStep = 0;
                return new CommandResults(RfidCommand.READ_CARRIER_ID.ToString(), CommandResult.Completed);
            }

            return ExecuteCommand(RfidCommand.READ_CARRIER_ID);
            //return new CommandResults(RfidCommand.READ_CARRIER_ID.ToString(), CommandResult.Completed);
        }
        protected override CommandResults DoWriteLotId(string lotId)
        {
            if (String.IsNullOrEmpty(_transaction.ErrorDescription) == false)
            {
                _transaction.ErrorDescription = string.Empty;
                return new CommandResults(RfidCommand.WRITE_LOT_ID.ToString(), CommandResult.Error, _transaction.ErrorDescription);
            }
            if (WriteCommandAckFlag == true)
            {
                WriteCommandAckFlag = false;
                return new CommandResults(RfidCommand.WRITE_LOT_ID.ToString(), CommandResult.Completed);
            }
            _transaction.LotID = lotId;
            return ExecuteCommand(RfidCommand.WRITE_LOT_ID);
            //return new CommandResults(RfidCommand.WRITE_LOT_ID.ToString(), CommandResult.Completed);
        }
        protected override CommandResults DoWriteCarrierId(string carrierId)
        {
            if (String.IsNullOrEmpty(_transaction.ErrorDescription) == false)
            {
                _transaction.ErrorDescription = string.Empty;
                return new CommandResults(RfidCommand.WRITE_CARRIER_ID.ToString(), CommandResult.Error, _transaction.ErrorDescription);
            }
            if (WriteCommandAckFlag == true)
            {
                WriteCommandAckFlag = false;
                return new CommandResults(RfidCommand.WRITE_CARRIER_ID.ToString(), CommandResult.Completed);
            }
            _transaction.CarrierID = carrierId;
            return ExecuteCommand(RfidCommand.WRITE_CARRIER_ID);
            //return new CommandResults(RfidCommand.WRITE_CARRIER_ID.ToString(), CommandResult.Completed);
			
        }
        #endregion </Actions>

        #region <Internals>
        private string StringHexToStringAscii(string inputHex)
        {
            string hexValue = string.Empty;
            int convertHex;
            char convertAscii;
            string result = string.Empty;
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < inputHex.Length; i += 2)
            {
                hexValue = inputHex.Substring(i, 2);
                convertHex = Convert.ToInt32(hexValue, 16);
                convertAscii = Convert.ToChar(convertHex);
                stringBuilder.Append(convertAscii);
            }
            result = stringBuilder.ToString();
            return result;
        }
        #endregion </Internals>

        #region <Methods>
        private bool SendMessage(RfidCommand command, string page = "")
        {
            byte[] data = null;

            if (page.Equals(""))
            {
                _transaction.LotIdPage = LotIdAddress.ToString();
            }
            else
            {
                _transaction.LotIdPage = page;
            }

            if (_transaction.LotIdPage.Length == 1)
            {
                _transaction.LotIdPage = _transaction.LotIdPage.Insert(0, "0");
            }

            _transaction.CarrierIdPage = CarrierIdAddress.ToString();
            if (_transaction.CarrierIdPage.Length == 1)
            {
                _transaction.CarrierIdPage = _transaction.CarrierIdPage.Insert(0, "0");
            }

            switch (command)
            {
                case RfidCommand.READ_LOT_ID:
                    _transaction.LotID = string.Empty;
                    data = _transaction.ReadLotId(command);
                    break;
                case RfidCommand.READ_CARRIER_ID:
                    _transaction.CarrierID = string.Empty;
                    data = _transaction.ReadCarrierId(command);
                    break;
                case RfidCommand.WRITE_LOT_ID:
                    data = _transaction.WriteLotId(command);
                    break;
                case RfidCommand.WRITE_CARRIER_ID:
                    data = _transaction.WriteCarrierId(command);
                    break;
                default:
                    break;
            }

            if (data == null)
                return false;

            return DoAction(command, data);
        }
        #endregion </Methods>

        #region <Execute>
        public override void Execute()
        {
            if (_result.CommandResult.Equals(CommandResult.Proceed))
            {
                if (_comm == null)
                {
                    return;
                }

				if (false == _comm.IsConnected)
                {
                    _comm.OpenPort();
                    return;
                }
                if (false == _comm.ReadByteData(ref _receiveMessage))
                    return;
                ParseMessages(_receiveMessage, _doingAction);
            }
        }

        string _lotId = string.Empty;
        private CommandResults ExecuteReadLotId(RfidCommand command)
        {
            switch (_actionStep)
            {
                case 0:
                    {
                        _timeChecker.SetTickCount(5000);
                        _result.CommandResult = CommandResult.Proceed;
                        _result.Description = string.Empty;
                        _transaction.ErrorDescription = string.Empty;
                        _lotId = string.Empty;
                        ++_actionStep;
                    }
                    break;
                case 1:
                    {
                        if (SendMessage(command))
                        {
                            _doingAction = command;
                            ++_actionStep;
                        }
                    }
                    break;
                case 2:
                    if (TimeIsOver())
                    {
                        _result.CommandResult = CommandResult.Timeout;
                        break;
                    }

                    if (_transaction.ByteLotId != null)
                    {
                        _doingAction = RfidCommand.IDLE;
                        _lotId = StringHexToStringAscii(Encoding.ASCII.GetString(_transaction.ByteLotId));
                        _timeChecker.SetTickCount(5000);
                        _transaction.ByteLotId = null;
                        ++_actionStep;
                    }
                    break;
                case 3:
                    {
                        if (SendMessage(command, "6"))
                        {
                            _doingAction = command;
                            ++_actionStep;
                        }
                    }
                    break;
                case 4:
                    {
                        if (TimeIsOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }

                        if (_transaction.ByteLotId != null)
                        {
                            string tempLotId = StringHexToStringAscii(Encoding.ASCII.GetString(_transaction.ByteLotId));
                            _lotId = string.Format("{0}{1}", _lotId, tempLotId);
                            _transaction.ByteLotId = null;
                            _result.CommandResult = CommandResult.Completed;
                        }
                    }
                    break;
                default:
                    _result.CommandResult = CommandResult.Invalid;
                    _result.Description = string.Format("Invalid Seq Num : {0}", _actionStep);
                    break;
            }
            if (false == _result.CommandResult.Equals(CommandResult.Proceed))
            {
                _doingAction = RfidCommand.IDLE;
                _actionStep = 0;
            }
            return _result;
        }
        private CommandResults ExecuteCommand(RfidCommand command)
        {
            switch (_actionStep)
            {
                case 0:
                    {
                        _timeChecker.SetTickCount(5000);
                        _result.CommandResult = CommandResult.Proceed;
                        _result.Description = string.Empty;
                        _transaction.ErrorDescription = string.Empty;
                        ++_actionStep;
                    }
                    break;
                case 1:
                    {
                        if (SendMessage(command))
                        {
                            _doingAction = command;
                            ++_actionStep;
                        }
                    }
                    break;
                case 2:
                    if (TimeIsOver())
                    {
                        _result.CommandResult = CommandResult.Timeout;
                        break;
                    }
                    break;

                default:
                    _result.CommandResult = CommandResult.Invalid;
                    _result.Description = string.Format("Invalid Seq Num : {0}", _actionStep);
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
            int count = receivedMessage.Length - 4;

            switch (command)
            {
                case RfidCommand.READ_LOT_ID:
                    _transaction.ParseMessages(receivedMessage, RfidCommand.READ_LOT_ID);
                    break;
                case RfidCommand.READ_CARRIER_ID:
                    _transaction.ParseMessages(receivedMessage, RfidCommand.READ_CARRIER_ID);
                    break;
                case RfidCommand.WRITE_LOT_ID:
                    if (_transaction.ParseMessages(receivedMessage, RfidCommand.WRITE_LOT_ID))
                    {
                        WriteCommandAckFlag = true;
                    }
                    break;
                case RfidCommand.WRITE_CARRIER_ID:
                    if (_transaction.ParseMessages(receivedMessage, RfidCommand.WRITE_CARRIER_ID))
                    {
                        WriteCommandAckFlag = true;
                    }
                    break;
                default:
                    break;
            }
        }
        private bool TimeIsOver()
        {
            if (_timeChecker.IsTickOver(true))
            {
                _actionStep = 0;
                return true;
            }
            return false;
        }
        #endregion </Execute>

        #endregion </Methods>
    }

    #region <Transaction Class>
    public class XedionTransaction
    {
        #region <Constructors>
        public XedionTransaction(int commIndex)
        {
            _portId = (uint)(1 + commIndex - (int)Define.DefineEnumProject.Serial.EN_SERIAL_INDEX.RFID_FOUP_1);
        }
        #endregion </Constructors>

		#region <Fields>

        #region <Const>
        private const char StartSign = (char)83;
        private const char EndSign = (char)13;
        private const char ReaderID = '0';
        private const string Page98 = "98";     // ID 비트 0에서 3까지의 빈 표지나 종료 기호까지 모든 페이지 읽기
        private const string Page99 = "99";     // 전체 태그 읽기

        #region <Command to Reader>
        private const string ReadData = "X";
        private const string WriteData = "W";
        private const string RequestParameter = "G";
        private const string SetParameter = "P";
        private const string HeartBeat = "H";
        private const string Reset = "N";
        private const string TransponderMode = "M";
        private const string LockPage = "L";
        private const string CheckVersionAndSerialNumber = "I";
        private const string WriteSerialNumber = "S";
        private const string CheckVersion = "V";
        #endregion </Command To Reader>

        #region <Command from Reader>
        private const char ReadDataAck = 'x';
        private const char WriteDataAck = 'w';
        private const char SetParameterAck = 'p';
        private const char RequestParameterAck = 'g';
        private const char HeartbeatAck = 'h';
        private const char ResetAck = 'n';
        private const char ErrorMessage = 'e';
        private const char TransponderModeAck = 'm';
        private const char LockPageAck = 'l';
        private const char CheckVersionAndSerialNumberAck = 'i';
        private const char CheckVersionAck = 'v';
        #endregion </Command from Reader>

        #endregion </Const>

        #endregion </Fields>

        #region <Enumerations>
        // 번역기 돌려서 해석이 힘들수 있음..
        public enum CommandToReader
        {
            X,  // 외부에서 트리거된 읽기 시작
            W,  // TAG에 쓰기
            G,  // 매개변수 요청
            P,  // 매개변수 요청
            H,  // HEART BEAT 시작
            N,  // 소프트웨어 리셋 시작
            M,  // TRANSPONDER MODE 설정/읽기 (단일/다중)
            L,  // 한 페이지 잠금
            I,  // 버전 및 시리얼 번호 조회
            S,  // 시리얼 번호 쓰기(비밀번호 필요)
            V,  // 버전 조회
        }

        public enum CommandFromReader
        {
            x,  // 페이지에서 데이터(외부에서 트리거 된 읽기)
            w,  // TAG에 쓰기 후 응답
            p,  // 매개변수 설정 중 응답
            g,  // 매개변수 읽기 요청에 대한 응답
            h,  // HEART BEAT 후 응답
            n,  // 소프트웨어 또는하드웨어 리셋 후 응답
            e,  // 실패 메시지
            m,  // TRANSPONDER MODE 설정 후 응답/확인
            l,  // 한 페이지 잠금 후 피드백
            i,  // 버전 및 시리얼 번호 조회에 대한 응답
            v,  // 버전 조회에 대한 응답
        }
		public enum CategoriesOfData
        {
            Header,
            Data,
            EndOfPackage,
        }
        #endregion </Enumerations>

        #region <Fields>
        private uint _portId;
        public string LotIdPage;
        public string CarrierIdPage;
        public string ErrorDescription = string.Empty;
        public bool ErrorFlag = false;
        //private string MatchPage;
        public string LotID;
        public byte[] ByteLotId;
        public string CarrierID;
        public byte[] ByteCarrierId;
        private int ReceiveDataCount;
        private string ReceivePage;
        #endregion </Fields>

		#region <Properties>
        private string[] pageData = new string[17];

        public string[] PageData
        {
            get
            {
                return pageData;
            }

            private set
            {
                pageData = value;
            }
        }
        public ushort LastPage { get; private set; }

        #endregion </Properties>

        #region <Methods>
		public byte[] ReadLotId(RfidCommand command)
        {
            return CreateCommand(ReaderID, RfidCommand.READ_LOT_ID, LotIdPage);
        }
        public byte[] ReadCarrierId(RfidCommand command)
        {
            return CreateCommand(ReaderID, RfidCommand.READ_CARRIER_ID, CarrierIdPage);
        }
        public byte[] WriteLotId(RfidCommand command)
        {
            string[] convertArray = new string[9];
            convertArray[0] = LotIdPage;

            for (int i = 1; i < convertArray.Length ; i++)
            {
                if (LotID.Length >= i)
                {
                    convertArray[i] = ((int)LotID[i - 1]).ToString("X2");
                }
                else
                {
                    convertArray[i] = " ";
                }
            }
            return CreateCommand(ReaderID, RfidCommand.WRITE_LOT_ID, convertArray);
        }
        public byte[] WriteCarrierId(RfidCommand command)
        {
            string[] convertArray = new string[9];
            convertArray[0] = CarrierIdPage;

            for (int i = 1; i < convertArray.Length; i++)
            {
                if (CarrierID.Length >= i)
                {
                    convertArray[i] = ((int)CarrierID[i - 1]).ToString("X2");
                }
                else
                {
                    convertArray[i] = " ";
                }
            }
            return CreateCommand(ReaderID, RfidCommand.WRITE_CARRIER_ID, convertArray);
        }
        public byte[] CreateCommand(char readerId, RfidCommand foupCommand, params string[] parameters)
        {
            ErrorDescription = string.Empty;
            string command = string.Empty;
            string header = string.Empty, endPackage = string.Empty;
            string temp = string.Empty;
            StringBuilder stringBuilder = new StringBuilder();
			switch (foupCommand)
            {
                case RfidCommand.READ_LOT_ID:
                case RfidCommand.READ_CARRIER_ID:
                    command = ReadData.ToString();
                    break;
                case RfidCommand.WRITE_LOT_ID:
                case RfidCommand.WRITE_CARRIER_ID:
                    command = WriteData.ToString();
                    break;
                default:
                    break;
            }
            stringBuilder.Append(command);
            stringBuilder.Append(readerId);

            foreach (string parameter in parameters)
            {
                if (command.Equals(WriteData))
                {
                    stringBuilder.Append(parameter);
                }
                else
                {
                    stringBuilder.Append(parameter);
                }
            }

            string message = stringBuilder.ToString();
            MakePackageHeader(ref header, message.Length);
            MakeEndingOfPackage(ref endPackage, header, message);
			string strMessage = header + message + endPackage;

            byte[] sendMessage = Encoding.ASCII.GetBytes(strMessage);

            return sendMessage;
        }
        private bool MakePackageHeader(ref string packageHeader, int length)
        {
            try
            {
                string strHexHeader = string.Format("{0:X2}", length);
			    char[] charMsgLength = new char[2];
                charMsgLength = strHexHeader.ToCharArray();

                packageHeader = StartSign.ToString();
                foreach (char c in charMsgLength)
                {
                    packageHeader += c.ToString();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private bool MakeEndingOfPackage(ref string endOfPackage, string header, string message)
        {
            try
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(header + message + EndSign);

                string ConvertByteToString = "";
                int ConvertStingToInt = 0;
                int tempXORCheckSum = 0;
                int tempXORAdditionCheckSum = 0;
                char[] arrChecksumXOR = new char[2];
                char[] arrChecksumAddition = new char[2];

                foreach (byte b in inputBytes)
                {
                    ConvertByteToString = string.Format("{0:X2}", b);
                    ConvertStingToInt = Convert.ToInt32(ConvertByteToString, 16);
                    if (b == StartSign)
                    {
                        tempXORCheckSum = ConvertStingToInt;
                        tempXORAdditionCheckSum = tempXORCheckSum;
                    }
                    else
                    {
                        tempXORCheckSum ^= ConvertStingToInt;
                        tempXORAdditionCheckSum += ConvertStingToInt;
                    }
                }

                arrChecksumXOR = string.Format("{0:X2}", tempXORCheckSum).ToCharArray();

                string sTempXORAddtionalCheckSum = "";
                sTempXORAddtionalCheckSum = string.Format("{0:X2}", tempXORAdditionCheckSum);

                if (sTempXORAddtionalCheckSum.Length > 2)
                {
                    arrChecksumAddition = (sTempXORAddtionalCheckSum.Substring(sTempXORAddtionalCheckSum.Length - 2, sTempXORAddtionalCheckSum.Length - 1)).ToCharArray();
                }
                else
                {
                    arrChecksumAddition = sTempXORAddtionalCheckSum.ToCharArray();
                }

                endOfPackage = EndSign.ToString();
                foreach (char c in arrChecksumXOR)
                {
                    endOfPackage += c.ToString();
                }
                foreach (char c in arrChecksumAddition)
                {
                    endOfPackage += c.ToString();
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool ParseMessages(byte[] receivedMessage, RfidCommand command)
        {
            if (receivedMessage == null || command == RfidCommand.IDLE)
            {
                return false;
            }
            if (MatchReceiveMessage(receivedMessage, command))
            {
                return true;
            }
            return false;
        }
        private bool MatchReceiveMessage(byte[] receivedMessage, RfidCommand receiveCommand)
        {
            byte[] compareHeader = SearchForData(receivedMessage, receiveCommand, CategoriesOfData.Header);
            byte[] compareData = SearchForData(receivedMessage, receiveCommand, CategoriesOfData.Data);
            byte[] compareEndOfPackage = SearchForData(receivedMessage, receiveCommand, CategoriesOfData.EndOfPackage);
            List<byte> byteResult = new List<byte>();
            string temp = string.Empty;
            string command = string.Empty;
            string header = string.Empty, endPackage = string.Empty;

            StringBuilder stringBuilder = new StringBuilder();

            if (compareData[0] == ErrorMessage)
            {
                ErrorDescription = GetErrorMessage((char)compareData[2]);
                ErrorFlag = true;
                return false;
            }

            if (MatchReceiveData(compareData, receivedMessage, receiveCommand) == false)
            {
                return false;
            }
            if (MatchReceiveHeader(compareHeader, compareData.Length) == false)
            {
                return false;
            }
            
            string message = stringBuilder.ToString();

            if (MatchReceiveEndOfPackage(compareEndOfPackage))
            {
                switch (receiveCommand)
                {
                    case RfidCommand.READ_LOT_ID:
                        for (int i = 4; i < compareData.Length; i++)
                        {
                            byteResult.Add(compareData[i]);
                        }
                        ByteLotId = byteResult.ToArray();
                        break;
                    case RfidCommand.READ_CARRIER_ID:
                        for (int i = 4; i < compareData.Length; i++)
                        {
                            byteResult.Add(compareData[i]);
                        }
                        ByteCarrierId = byteResult.ToArray();
                        break;
                    case RfidCommand.WRITE_LOT_ID:
                        break;
                    case RfidCommand.WRITE_CARRIER_ID:
                        break;
                    default:
                        break;
                }
                return true;
            }
            return false;
        }
        private bool MatchReceiveData(byte[] receiveData, byte[] receivedMessage, RfidCommand command)
        {
            byte[] checkMatchingCommand = new byte[1];
            byte[] checkMatchingPage;
            string checkCommand = string.Empty;
            string checkPage = string.Empty;

            switch (command)
            {
                case RfidCommand.READ_LOT_ID:
                    checkPage = LotIdPage;
                    checkCommand = ReadDataAck.ToString();
                    break;
                case RfidCommand.READ_CARRIER_ID:
                    checkPage = CarrierIdPage;
                    checkCommand = ReadDataAck.ToString();
                    break;
                case RfidCommand.WRITE_LOT_ID:
                case RfidCommand.WRITE_CARRIER_ID:
                    checkCommand = WriteDataAck.ToString();
                    break;
                default:
                    break;
            }

            checkMatchingCommand = Encoding.ASCII.GetBytes(checkCommand);
            checkMatchingPage = Encoding.ASCII.GetBytes(checkPage);
            // Message 4번째 문자부터 Data 시작
            if (receivedMessage[3] == checkMatchingCommand[0])
            {
                if (checkCommand == ReadDataAck.ToString())
                {
                    if (ReceivePage == checkPage)
                    {
                        return true;
                    }
                    return false;
                }
                if (checkCommand == WriteDataAck.ToString())
                {
                    return true;
                }
                return false;
            }
            else
            {
                return false;
            }
        }
        private bool MatchReceiveHeader(byte[] packageHeader, int length)
        {
//             string stringLength = ((char)packageHeader[2]).ToString();
//             int checkLength = Convert.ToInt32(stringLength,16);

            if (packageHeader[0] == StartSign)
            {
                if (ReceiveDataCount == length)
                {
                    return true;
                }
            }
            return false;
        }
        private bool MatchReceiveEndOfPackage(byte[] inputBytes)
        {
            if (inputBytes[0] == EndSign)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private byte[] SearchForData(byte[] receivedMessage, RfidCommand command, CategoriesOfData category)
        {
            //byte[] foundData;
            List<byte> dynamicByteArray = new List<byte>();
            byte[] result;
            string receiveDataCount = ((char)receivedMessage[1]).ToString() + ((char)receivedMessage[2]).ToString();
            ReceiveDataCount = Convert.ToInt32(receiveDataCount, 16);
            switch (category)
            {
                case CategoriesOfData.Header:
                    for (int i = 0; i < 3; i++)
                    {
                        dynamicByteArray.Add(receivedMessage[i]);
                    }
                    result = new byte[dynamicByteArray.ToArray().Length];
                    result = dynamicByteArray.ToArray();

                    dynamicByteArray.Clear();
                    return result;
                case CategoriesOfData.Data:
                    for (int i = 3; i < 3 + ReceiveDataCount; i++)
                    {
                        dynamicByteArray.Add(receivedMessage[i]);
                    }
                    result = new byte[dynamicByteArray.ToArray().Length];
                    result = dynamicByteArray.ToArray();
                    if (command == RfidCommand.READ_LOT_ID || command == RfidCommand.READ_CARRIER_ID)
                    {
                        if (result[0] != ErrorMessage && result.Length > 3)
                        {
                            ReceivePage = ((char)result[2]).ToString() + ((char)result[3]).ToString();
                        }
                    }
                    dynamicByteArray.Clear();
                    return result;
                case CategoriesOfData.EndOfPackage:
                    for (int i = 3 + ReceiveDataCount; i < receivedMessage.Length; i++)
                    {
                        dynamicByteArray.Add(receivedMessage[i]);
                    }
                    result = new byte[dynamicByteArray.ToArray().Length];
                    result = dynamicByteArray.ToArray();

                    dynamicByteArray.Clear();
                    return result;
                default:
                    return null;
            }
        }
        public string GetErrorMessage(char errorCode)
        {
            string msg = "";
            switch (errorCode)
            {
                case '0':
                    msg = "none";    // 오류 없음
                    break;
                case '1':
                    msg = "auto fail";    // 자동 읽기가 불가능합니다.
                    break;
                case '2':
                    msg = "ex fail";    // 외부 트리거된 읽기가 불가능합니다.
                    break;
                case '3':
                    msg = "write fail";    // 태그로 데이터 전송이 불가능합니다.
                    break;
                case '4':
                    msg = "no tag";    // 태그가 없거나 안테나가 설치되어 있지 않습니다.
                    break;
                case '5':
                    msg = "invalid";    // 잘못된 매개 변수 또는 명령입니다.
                    break;
                case '6':
                    msg = "unknown";    // 알 수 없는 오류
                    break;
                case '7':
                    msg = "unconfig";    // 장치가 구성되지 않았습니다.
                    break;
                case '8':
                    msg = "check";    // 패리티 또는 체크섬 오류를 확인하세요.
                    break;
                case '9':
                    msg = "void ackn";    // 유효한 확인이 없습니다.
                    break;
                case 'A':								// ERROR MESSAGE 아래 3개중 하나
                    msg = "locked, msg len, invalid";    // 잠긴 페이지에는 쓸 수 없습니다.
                    break;								// 메시지가 너무 깁니다. , 잘못된 매개 변수 또는 명령입니다.
                case 'B':
                    msg = "no ackn";    // 확인해야 할 메시지가 이미 전송되었지만 지정된 시간 내에 터미널에서 확인되지 않았습니다.
                    break;
            }
            return (msg);
        }
        #endregion </Methods>
    }
    #endregion </Transaction Class>

}
