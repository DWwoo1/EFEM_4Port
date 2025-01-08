using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Threading;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

using Define.DefineConstant;

namespace FrameOfSystem3
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            bool bNotRunning;
			string entryAssemblyName = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;
            Mutex mutexApp = new Mutex(true, entryAssemblyName, out bNotRunning);

            if (bNotRunning)
            {
                // 1. Make a directory for the exception logs.
                MakeDefaultDirectory();

                // 2. Register the exceptions
                Application.ThreadException                     += new ThreadExceptionEventHandler(WriteLogOnThreadException);
                AppDomain.CurrentDomain.FirstChanceException    += new EventHandler<FirstChanceExceptionEventArgs>(WriteLogOnOccurredException);

                // 3. Init taskoperator.
                Task.TaskOperator.GetInstance().Init();

                // 4. Run Main Form
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);                
                Application.Run(new Views.Form_Frame());

                // 5. Exit taskoperator.
                Task.TaskOperator.GetInstance().Exit();

                mutexApp.ReleaseMutex();
            }
            else
            {
                MessageBox.Show("This software is already running.");
                Application.Exit();
            }
        }

        #region Exceptions
        /// <summary>
        /// 2020.02.05 by yjlee [ADD] This function will be called when the exception on thread occur.
        /// </summary>
        private static void WriteLogOnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            string strFileName      = "ThreadException_" + GetCurrentDate() + ".txt";

            WriteLog(strFileName
                , e.Exception.ToString());

        }
        /// <summary>
        /// 2020.02.05 by yjlee [ADD] This function will be called when all exception occur.
        /// </summary>
        private static void WriteLogOnOccurredException(object source, FirstChanceExceptionEventArgs e)
        {
            string strFileName = "Exception_" + GetCurrentDate() + ".txt";

            if (e.Exception is System.Net.Sockets.SocketException)
            {

            }
            else if (e.Exception is System.ServiceModel.CommunicationException)
            {

            }
            else if (e.Exception is System.Reflection.TargetInvocationException)
            {

            }
            else
            {
                WriteLog(strFileName
                , string.Format("### OCCURRED EXCEPTION : {0}\r\n{1}"
                , e.Exception.Message
                , Environment.StackTrace));
            }
        }
        //private static void WriteLogOnOccurredException(object source, FirstChanceExceptionEventArgs e)
        //{
        //    string strFileName      = "Exception_" + GetCurrentDate() + ".txt";

        //    WriteLog(strFileName
        //        , string.Format("### OCCURRED EXCEPTION : {0}\r\n{1}"
        //        , e.Exception.Message
        //        , Environment.StackTrace));
        //}

        #endregion

        #region Write Logs
        private static ReaderWriterLockSlim m_rwLockForLogging      = new ReaderWriterLockSlim();

        /// <summary>
        /// 2020.02.05 by yjlee [ADD] Write a log for the exceptions.
        /// </summary>
        private static void WriteLog(string strFileName, string strData)
        {
            m_rwLockForLogging.EnterWriteLock();
			
            try
            {
                using(System.IO.StreamWriter outputFile = new System.IO.StreamWriter(FilePath.FILEPATH_EXCEPTION_LOG
                    + "\\" + strFileName
                    , true))
                {
                    outputFile.WriteLine("######### OCCURRUNCE TIME : " + GetCurrentTime());
                    outputFile.WriteLine(strData + "\r\n");
                }
            }
            finally
            {
                m_rwLockForLogging.ExitWriteLock();
            }
        }
        #endregion

        #region Directory
        /// <summary>
        /// 2020.02.05 by yjlee [ADD] Make a default directory for the exception log.
        /// </summary>
        private static void MakeDefaultDirectory()
        {
            System.IO.DirectoryInfo dirInfor = new System.IO.DirectoryInfo(FilePath.FILEPATH_EXCEPTION_LOG);

            // 해당 경로의 폴더가 존재하지 않을 경우
            if(false == dirInfor.Exists)
            {
                dirInfor.Create();
            }
        }
        #endregion

        #region Time
        /// <summary>
        /// 2020.02.05 by yjlee [ADD] Get a current date as string type.
        /// </summary>
        private static string GetCurrentDate()
        {
            DateTime nowDate    = System.DateTime.Now;

            return string.Format("{0:0000}{1:00}{2:00}"
                , nowDate.Year
                , nowDate.Month
                , nowDate.Day);
        }
        /// <summary>
        /// 2020.02.05 by yjlee [ADD] Get a current time as string type.
        /// </summary>
        private static string GetCurrentTime()
        {
            DateTime nowDate    = System.DateTime.Now;

            return string.Format("{0:00}h {1:00}m {2:00}s {3:000}ms"
                , nowDate.Hour
                , nowDate.Minute
                , nowDate.Second
                , nowDate.Millisecond);
        }
        #endregion
    }
}

