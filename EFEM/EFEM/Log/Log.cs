using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading;
using System.IO;

using Socket_;
using Serial_;
using Motion_;
using Cylinder_;
using DigitalIO_;
using AnalogIO_;

using Define.DefineEnumProject;
using Define.DefineEnumBase.Log;
using Define.DefineConstant;

namespace FrameOfSystem3.Log
{
    /// <summary>
    /// 2021.01.15 by yjlee [ADD] Write and Read a log.
    /// </summary>
	public class LogManager
    {
        private const int SIZE_QUEUE                = 1 << 13;  // 8096

        #region Singleton
        private LogManager() {}
		private static LogManager m_instance        = new LogManager();
        public static LogManager GetInstance() { return m_instance; }
        #endregion

		#region Constants
		private readonly string FILE_NAME_TEMP_LOG	= "TemporaryLog";

		private const byte PREFIX					= 0x02;
		private const byte SUFFIX					= 0x03;

		private const byte PREFIX_REPLACE			= 0x5B;
		private const byte SUFFIX_REPLACE			= 0x5D;
		#endregion

		#region Variables
		private bool m_bInit                        = false;

        #region Instance
        private Socket m_instanceSocket             = null;
        private Recipe.Recipe m_instanceRecipe      = null;
        #endregion End - Instance

        #region Socket
        private int m_nIndexOfSocket                        = -1;
        #endregion End - Socket

        #region Delegate for writing log
        private DelegateWritingSocketLog m_dgWriteSocket            = null;
        private DelegateWritingSerialLog m_dgWriteSerial            = null;
        private DelegateWritingMotionLog m_dgWriteMotion            = null;
        private DelegateWritingCylinderLog m_dgWriteCylinder        = null;
        private DelegateWritingDigitalOutLog m_dgWriteDigitalOut    = null;
        private DelegateWritingAnalogOutLog m_dgWriteAnalogOut      = null;
        #endregion End - Delegate for writing log

        #region Mapping
        private string[] m_arMappingCFG             = null;
        private string[] m_arMappingXFR             = null;
        private string[] m_arMappingALM             = null;
        private string[] m_arMappingLEH             = null;
        private string[] m_arMappingPRC             = null;
        #endregion End - Mapping

        #region Logging
        private Timer m_timerForLogging             = null;
        private Queue<string> m_queueForBuffer      = new Queue<string>(SIZE_QUEUE);

		#region TempLogging
		private bool m_bChageSocketStatus			= false;
		private string m_strFullFilePath			= string.Empty;
		#endregion

		#endregion End - Logging

		#region Threading
		private ReaderWriterLockSlim m_rwLockForLogging = new ReaderWriterLockSlim();
        #endregion End - Threading

        #endregion

        #region Internal Interface

        #region Init
        /// <summary>
        /// 2021.01.18 by yjlee [ADD] Make the mapping arraies for the performance.
        /// </summary>
        private void MakeMappingArraies()
		{
			#region TempLogging
			m_strFullFilePath	= FilePath.FILEPATH_LOG + "\\" + FILE_NAME_TEMP_LOG + FileFormat.FILEFORMAT_LOG;
			#endregion

			#region XFR
			m_arMappingXFR      = new string[Enum.GetValues(typeof(EN_XFR_TYPE)).Length];

            foreach(EN_XFR_TYPE enType in Enum.GetValues(typeof(EN_XFR_TYPE)))
            {
                m_arMappingXFR[(int)enType]     = enType.ToString();
            }
            #endregion End - XFR

            #region CFG
            m_arMappingCFG      = new string[Enum.GetValues(typeof(EN_CFG_TYPE)).Length];

            foreach(EN_CFG_TYPE enType in Enum.GetValues(typeof(EN_CFG_TYPE)))
            {
                m_arMappingCFG[(int)enType]     = enType.ToString();
            }
            #endregion End - CFG

            #region ALM
            m_arMappingALM      = new string[Enum.GetValues(typeof(EN_ALM_TYPE)).Length];

            foreach(EN_ALM_TYPE enType in Enum.GetValues(typeof(EN_ALM_TYPE)))
            {
                m_arMappingALM[(int)enType]     = enType.ToString();
            }
            #endregion End - ALM

            #region LEH
            m_arMappingLEH      = new string[Enum.GetValues(typeof(EN_LEH_TYPE)).Length];

            foreach(EN_LEH_TYPE enType in Enum.GetValues(typeof(EN_LEH_TYPE)))
            {
                m_arMappingLEH[(int)enType]     = enType.ToString();
            }
            #endregion End - LEH

            #region PRC
            m_arMappingPRC      = new string[Enum.GetValues(typeof(EN_PRC_TYPE)).Length];

            foreach(EN_PRC_TYPE enType in Enum.GetValues(typeof(EN_PRC_TYPE)))
            {
                m_arMappingPRC[(int)enType]     = enType.ToString();
            }
            #endregion End - PRC
        }
        /// <summary>
        /// 2021.01.19 by yjlee [ADD] Set the delegate function.
        /// </summary>
        private void SetDelegateFunction()
        {
            m_dgWriteSocket     = new DelegateWritingSocketLog(WriteSocketLog);
            m_instanceSocket.SetLogFunction(ref m_dgWriteSocket);

            m_dgWriteSerial     = new DelegateWritingSerialLog(WriteSerialLog);
            Serial.GetInstance().SetLogFunction(ref m_dgWriteSerial);

            m_dgWriteMotion     = new DelegateWritingMotionLog(WriteMotionLog);
            Motion_.Motion.GetInstance().SetLogFunction(ref m_dgWriteMotion);

            m_dgWriteCylinder   = new DelegateWritingCylinderLog(WriteCylinderLog);
            Cylinder.GetInstance().SetLogFunction(ref m_dgWriteCylinder);

            m_dgWriteDigitalOut = new DelegateWritingDigitalOutLog(WriteDigitalOutputLog);
            DigitalIO.GetInstance().SetLogFunction(ref m_dgWriteDigitalOut);

            m_dgWriteAnalogOut  = new DelegateWritingAnalogOutLog(WriteAnalogOutputLog);
            AnalogIO.GetInstance().SetLogFunction(ref m_dgWriteAnalogOut);
        }
        #endregion End - Init

        #region Socket
        /// <summary>
        /// 2021.01.15 by yjlee [ADD] Initialize the socket.
        /// </summary>
        private bool InitSocket()
        {
            m_instanceSocket        = Socket.GetInstance();
            m_nIndexOfSocket        = (int)Define.DefineEnumProject.Socket.EN_SOCKET_INDEX.LOG;

            m_instanceRecipe        = Recipe.Recipe.GetInstance();

            return m_instanceSocket.Connect(m_nIndexOfSocket);
        }
        #endregion End - Socket

        #region Time
        /// <summary>
        /// 2021.01.15 by yjlee [ADD] Get the current time.
        /// </summary>
        private string GetCurrentTime()
        {
            return DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff");
        }
        #endregion

        #region Recipe
        /// <summary>
        /// 2021.01.18 by yjlee [ADD] Get the current process recipe name.
        /// </summary>
        private string GetCurrentProcessRecipe()
        {
            string strFileName      = string.Empty;

			m_instanceRecipe.GetProcessFileNameWithoutExtension(ref strFileName);

            return strFileName;
        }
        #endregion

        #region Logging
        /// <summary>
        /// 2021.01.15 by yjlee [ADD] Write the log message.
        /// </summary>
        private void WriteLog(ref string strMessage)
        {
            m_rwLockForLogging.EnterWriteLock();

            m_queueForBuffer.Enqueue(strMessage);

            m_rwLockForLogging.ExitWriteLock();
        }
        /// <summary>
        /// 2021.01.15 by yjlee [ADD] Send the log message through the socket.
        /// </summary>
        private void SendLog()
        {
            Queue<string> tempQueue     = new Queue<string>();

            m_rwLockForLogging.EnterWriteLock();

            while (m_queueForBuffer.Count > 0)
            {
				byte[] arMessage		= null;

				string strMessage       = m_queueForBuffer.Dequeue();

				//char[] chMessage        = strMessage.ToCharArray();

				// 2021.05.18 by twkang [ADD] 한글표시위해 Encoding 방식 변경
				//byte[] arMessage        = System.Text.Encoding.Unicode.GetBytes(chMessage);
				//byte[] arMessage        = System.Text.Encoding.ASCII.GetBytes(chMessage);

				ReplaceFixkey(strMessage, ref arMessage);
                
                int nLegnth             = arMessage.Length;
                byte[] arData           = new byte[nLegnth + 2];        // STX, ETX
                
                for(int nIndex = 0 ; nIndex < nLegnth ; ++ nIndex)
                {
                    arData[nIndex + 1]      = arMessage[nIndex];
                }
                
                arData[0]           = PREFIX; // Prefix, STX
                arData[nLegnth + 1] = SUFFIX; // Suffix, STX

                // 2021.01.18 by yjlee [ADD] Failed to send the log data.
                if(false == m_instanceSocket.Send(m_nIndexOfSocket, arData))
                {
                    tempQueue.Enqueue(strMessage);
                }
            }

            while(tempQueue.Count > 0)
            {
                m_queueForBuffer.Enqueue(tempQueue.Dequeue());
            }

            m_rwLockForLogging.ExitWriteLock();
        }
        /// <summary>
        /// 2021.01.15 by yjlee [ADD] Write the log data through the socket communication.
        /// </summary>
        private void WriteLogThroughSocket(object sender)
        {
            switch(m_instanceSocket.GetState(m_nIndexOfSocket))
            {
                case SOCKET_ITEM_STATE.CONNECTED:
                case SOCKET_ITEM_STATE.TIMEOUT_RECEIVE:
                case SOCKET_ITEM_STATE.WAITING_RECEIVE:
					if(m_bChageSocketStatus && CheckTemporaryLogFile(m_strFullFilePath))
					{
						m_bChageSocketStatus	= false;

						LoadTemporaryLog();
					}
                    SendLog();
                    break;
					
                default:
					SaveLogTemporary();
					m_bChageSocketStatus	= true;
                    break;
            }
        }

		/// <summary>
		/// 2021.06.04 by twkang [ADD] 임시 로그파일이 있는지 체크
		/// </summary>
		private bool CheckTemporaryLogFile(string strFullFilePath)
		{
			return System.IO.File.Exists(strFullFilePath);
		}

		/// <summary>
		/// 2021.06.04 by twkang [ADD] 임시 로그파일을 불러와서 버퍼에 넣어준다.
		/// </summary>
		private void LoadTemporaryLog()
		{
			string strData	= null;

			if(false == FileIOManager_.FileIOManager.GetInstance().Read(FilePath.FILEPATH_LOG
				, FILE_NAME_TEMP_LOG + FileFormat.FILEFORMAT_LOG
				, ref strData
				, FileIO.TIMEOUT_READ))
				return;

			string[] arData	= strData.Split('\n');

			m_rwLockForLogging.EnterWriteLock();

			for(int nIndex = 0, nEnd = arData.Length; nIndex < nEnd; ++nIndex)
			{
				if(string.Empty == arData[nIndex])
					continue;

				m_queueForBuffer.Enqueue(arData[nIndex]);
			}

			m_rwLockForLogging.ExitWriteLock();

			if(CheckTemporaryLogFile(m_strFullFilePath))
				File.Delete(m_strFullFilePath);
		}

		/// <summary>
		/// 2021.06.04 by twkang [ADD] 로그를 파일로 임시저장 해둔다
		/// </summary>
		private void SaveLogTemporary()
		{
			System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(FilePath.FILEPATH_LOG);
			
			// 해당 경로의 폴더가 존재하지 않을 경우
			if (false == dirInfo.Exists)
			{
				dirInfo.Create();
			}

			m_rwLockForLogging.EnterWriteLock();

			while(m_queueForBuffer.Count > 0)
			{
				string strData	= m_queueForBuffer.Dequeue() + "\n";

				FileIOManager_.FileIOManager.GetInstance().Write(FilePath.FILEPATH_LOG
				, FILE_NAME_TEMP_LOG + FileFormat.FILEFORMAT_LOG
				, strData
				, true
				, false);
			}

			m_rwLockForLogging.ExitWriteLock();
		}

		/// <summary>
		/// 2021.08.17 by twkang [ADD] Message 내용에 PreFix 또는 SuffIx 를 제거
		/// </summary>
		private void ReplaceFixkey(string strMessage, ref byte[] arData)
		{
			if (strMessage == string.Empty)
				return;

			char[] chMessage        = strMessage.ToCharArray();

			Queue<char> queueForTemp	= new Queue<char>();

			for (int nIndex = 0, nEnd = chMessage.Length; nIndex < nEnd; ++nIndex)
			{
				// Prefix, Suffix 와 동일한 문자는 누락시킨다.

				switch((byte)chMessage[nIndex])
				{
					case PREFIX:
						queueForTemp.Enqueue((char)PREFIX_REPLACE);
						break;
					case SUFFIX:
						queueForTemp.Enqueue((char)SUFFIX_REPLACE);
						break;
					default:
						queueForTemp.Enqueue(chMessage[nIndex]);
						break;
				}
			}

			chMessage = queueForTemp.ToArray();

			// 2021.05.18 by twkang [ADD] 한글표시를 위하여 ASCII 가 아닌 Unicode 방식을 사용
			// 2021.09.06 by twkang [MOD] UNICODE -> UTF8
			arData = System.Text.Encoding.UTF8.GetBytes(chMessage);
		}

        #region FNC
        /// <summary>
        /// 2021.01.20 by yjlee [ADD] Write the log message about controlling the motion.
        /// </summary>
        private void WriteMotionLog(string strID, MOTION_LOG_TYPE enMotionType, bool bStart, double dblEncoderPosition, double dblDestination, double dblVelocity)
        {
            string strMessage       = string.Empty;
            string strMotionType    = enMotionType.ToString();
            string strStatus        = bStart ? "START" : "END";

            switch(enMotionType)
            {
                case MOTION_LOG_TYPE.SPEED:
                case MOTION_LOG_TYPE.HOMING:
                case MOTION_LOG_TYPE.STOP:
                    strMessage  = string.Format("{0} '{1}' 'FNC' '{2}' '{3}' ('TYPE', 'MOTOR') ('ENCODER', {4:F4}) ('VELOCITY', {5})"
                                        , GetCurrentTime()
                                        , strID					// Device ID	(FRONT STAGE Z...)
                                        , strMotionType			// EVENT ID		(RELEATIVE...)
                                        , strStatus				// START, END
                                        , dblEncoderPosition	// DATA
                                        , dblVelocity);
                    break;

                default:
                    strMessage  = string.Format("{0} '{1}' 'FNC' '{2}' '{3}' ('TYPE', 'MOTOR') ('ENCODER', {4}) ('POSITION', {5}) ('VELOCITY', {6})"
                                        , GetCurrentTime()
                                        , strID
                                        , strMotionType
                                        , strStatus
                                        , dblEncoderPosition
                                        , dblDestination
                                        , dblVelocity);
                    break;
            }

            WriteLog(ref strMessage);
        }
        /// <summary>
        /// 2021.01.20 by yjlee [ADD] Write the log message about controlling the cylinder.
        /// </summary>
        private void WriteCylinderLog(string strID, string strType, bool bStart, int nDelayTime)
        {
            string strMessage   = string.Format("{0} '{1}' 'FNC' '{2}' '{3}' ('TYPE', 'SENSOR') ('DELAYTIME', {4})"
                                        , GetCurrentTime()
                                        , strID
                                        , strType
                                        , bStart ? "START" : "END"
                                        , nDelayTime);

            WriteLog(ref strMessage);
        }
        /// <summary>
        /// 2021.01.21 by yjlee [ADD] Write the analog output log.
        /// </summary>
        private void WriteAnalogOutputLog(string strID, string strTagID, double dblVoltage)
        {
            string strMessage   = string.Format("{0} '{1}' 'FNC' ('TYPE', 'ANALOG') ('{2}', '{3}')"
                                        , GetCurrentTime()
                                        , strID
										, strTagID
                                        , dblVoltage);

            WriteLog(ref strMessage);
        }
        /// <summary>
        /// 2021.01.21 by yjlee [ADD] Write the digital output log.
        /// </summary>
        private void WriteDigitalOutputLog(string strID, string strTagID, bool bOn)
        {
            string strMessage   = string.Format("{0} '{1}' 'FNC' ('TYPE', 'SENSOR') ('{2}', '{3}')"
                                        , GetCurrentTime()
                                        , strID
                                        , strTagID
                                        , bOn);

            WriteLog(ref strMessage);
        }
        #endregion End - FNC

        #region COMM
        /// <summary>
        /// 2021.01.19 by yjlee [ADD] Write the socket log.
        /// </summary>
        private void WriteSocketLog(string strID, PROTOCOL_TYPE enType, bool bSend, string strData)
        {
            string strMessage       = string.Empty;
            string strType          = string.Empty;

            switch(enType)
            {
                case PROTOCOL_TYPE.UDP:
                    strType         = "UDP/IP";
                    break;

                case PROTOCOL_TYPE.TCP_SERVER:
                case PROTOCOL_TYPE.TCP_CLIENT:
                    strType         = "TCP/IP";
                    break;
            }
            
            strMessage          = string.Format("{0} '{1}' 'COMM' '{2}' ('TYPE', '{3}') ('DATA', '{4}')"
                                        , GetCurrentTime()
                                        , strID
                                        , bSend ? "SEND" : "RECV"
                                        , strType
                                        , strData);

            WriteLog(ref strMessage);
        }
        /// <summary>
        /// 2021.01.19 by yjlee [ADD] Write the socket log.
        /// </summary>
        private void WriteSerialLog(string strID, bool bSend, string strData)
        {
            string strMessage       = string.Format("{0} '{1}' 'COMM' '{2}' ('TYPE', 'SERIAL') ('DATA', '{3}')"
                                        , GetCurrentTime()
                                        , strID
                                        , bSend ? "SEND" : "RECV"
                                        , strData);

            WriteLog(ref strMessage);
        }
        #endregion End - COMM

        #endregion End - Logging

        #endregion End - Internal Interface

        #region External Interface

        #region Init & Exit
        /// <summary>
        /// 2021.01.15 by yjlee [ADD] Initialize the instances for the log.
        /// </summary>
        public bool Init()
        {
            bool bReturnValue       = InitSocket();

            SetDelegateFunction();

            m_timerForLogging       = new Timer(WriteLogThroughSocket, null, 0, ThreadTimerInterval.THREADTIMER_INTERVAL_LOGGING);

            MakeMappingArraies();

			/// 시작시 True 설정
			m_bChageSocketStatus	= true;

            m_bInit                 = true;

            return bReturnValue;
        }
        /// <summary>
        /// 2021.01.15 by yjlee [ADD] Release the resources of the class.
        /// </summary>
        public void Exit()
        {
            m_timerForLogging.Dispose();

            SendLog();

            m_bInit             = false;
        }
        #endregion End - Init & Exit

        #region Logging

        #region FNC (Function)
        #endregion End - FNC (Function)

        #region XFR (Transfer)
        /// <summary>
        /// 2021.01.18 by yjlee [ADD] Write the transfer log.
        /// </summary>
        public void WriteTransferLog(string strID, EN_XFR_TYPE enType, string strAction, string strMaterialID, string strMaterialType, string strFrom, string strTo)
        {
			// 2021.05.21 by twkang [MOD] Message Format 수정
            string strMessage           = string.Format("{0} '{1}' 'XFR' '{2}' '{3}' '{4}' '{5}' '{6}' '{7}' ('Data', '')"
                , GetCurrentTime()
                , strID
                , strAction
                , m_arMappingXFR[(int)enType]
                , strMaterialID
                , strMaterialType
                , strFrom
                , strTo);

            WriteLog(ref strMessage);
        }
        #endregion End - XFR (Transfer)

        #region PRC (Process)
        /// <summary>
        /// 2021.01.18 by yjlee [ADD] Write the process log.
        /// </summary>
        public void WriteProcessLog(string strID, EN_PRC_TYPE enType, string strEventID, string strMaterialID, string strLotID)
        {
			// 2022.08.29 by junho [MOD] message format 수정
			// 2021.05.21 by twkang [MOD] Message Format 수정
            string strMessage           = string.Format("{0} '{1}' 'PRC' '{2}' '{3}' '{4}' '{5}' '{6}' ('LOT_ID', '{7}')"
                , GetCurrentTime()
                , strID
                , strEventID
                , m_arMappingPRC[(int)enType]
                , strMaterialID
                , strLotID
                , GetCurrentProcessRecipe()
				, strLotID);

            WriteLog(ref strMessage);
        }
		// 2021.08.01 by junho [ADD] Add Processs Log format
		public void WriteProcessLog(string taskName, string eventID, string data)
		{
			string message = string.Format("{0} '{1}' 'PRC' '{2}' ('Data', '{3}')"
				, GetCurrentTime()
				, taskName, eventID, data);

			WriteLog(ref message);
		}
        #endregion End - PRC (Process)

        #region LEH (Event)
        /// <summary>
        /// 2021.01.18 by yjlee [ADD] Write the event log.
        /// </summary>
		public void WriteEventLog(EN_LEH_TYPE enType, string strLotID, string carrierId, int workQty, string swVersion)
        {
			string strMessage = string.Empty;
			switch (enType)
			{
				case EN_LEH_TYPE.LOT_IN:
					strMessage = string.Format("{0} '{1}' 'LEH' '{2}' '{3}' '{4}' '{5}' ('WF_QTY' '{6}') ('RECIPE_ID', '{4}') ('SW_VERSION', '{7}')"
						, GetCurrentTime()				// 0
						, "SYSTEM"						// 1
						, m_arMappingLEH[(int)enType]	// 2
						, strLotID						// 3
						, GetCurrentProcessRecipe()		// 4
						, carrierId					// 5
						, workQty						// 6
						, swVersion);					// 7
					break;
				case EN_LEH_TYPE.LOT_OUT:
					strMessage = string.Format("{0} '{1}' 'LEH' '{2}' '{3}' '{4}' '{5}' ('WF_QTY' '{6}') ('RECIPE_ID', '{4}')"
						, GetCurrentTime()				// 0
						, "SYSTEM"						// 1
						, m_arMappingLEH[(int)enType]	// 2
						, strLotID						// 3
						, GetCurrentProcessRecipe()		// 4
						, carrierId					// 5
						, workQty);						// 6
					break;
				default: return;
			}

            WriteLog(ref strMessage);
        }
        #endregion End - LEH (Event)

        #region ALM (Alarm)
        /// <summary>
        /// 2021.01.15 by yjlee [ADD] Write an alarm log.
        /// </summary>
        public void WriteAlarmLog(ref string strID, EN_ALM_TYPE enType, ref string strGrade, ref int nAlarmCode, ref string strDescription)
        {
            string strMessage           = string.Format("{0} '{1}' 'ALM' '{2}' '{3}' '{4}' ('DESCRIPTION', '{5}')"
                , GetCurrentTime()
                , strID
                , strGrade
                , nAlarmCode
                , m_arMappingALM[(int)enType]
                , strDescription);

            WriteLog(ref strMessage);
        }
        #endregion End - ALM (Alarm)

        #region CFG (Configuration)
        /// <summary>
        /// 2021.01.18 by yjlee [ADD] Write a configuration log.
        /// </summary>
        public void WriteConfigurationLog(ref string strID, EN_CFG_TYPE enType, string strParameterName, string strPreviousValue, string strValue)
        {
            // 2021.01.26 by yjlee [ADD] Checking the initialization.
            if (false == m_bInit) { return; }

            string strMessage = string.Empty;

            switch (enType)
            {
                case EN_CFG_TYPE.SETTING:
                    strMessage = string.Format("{0} '{1}' 'CFG' '{2}' ('{3}', '{4}')"
                                        , GetCurrentTime()
                                        , strID
                                        , m_arMappingCFG[(int)enType]
                                        , strParameterName
                                        , strValue);
                    break;

                case EN_CFG_TYPE.CHANGE:
                    strMessage = string.Format("{0} '{1}' 'CFG' '{2}' ('CHANGE_ITEM', '{3}') ('VALUE', [{2}, '{4}', '{5}'])"
                                        , GetCurrentTime()
                                        , strID
                                        , m_arMappingCFG[(int)enType]
                                        , strParameterName
                                        , strPreviousValue
                                        , strValue);
                    break;

                case EN_CFG_TYPE.SAVE:
                    strMessage = string.Format("{0} '{1}' 'CFG' '{2}' ('PATH', '{3}')"
                                        , GetCurrentTime()
                                        , strID
                                        , m_arMappingCFG[(int)enType]
                                        , strValue);
                    break;
            }

            WriteLog(ref strMessage);
        }
        #endregion End - CFG (Configuration)

        #region COMM (Communication)
        #endregion End - COMM (Communication)

        #endregion End - Logging

        #endregion End - External Interface
    }
}
