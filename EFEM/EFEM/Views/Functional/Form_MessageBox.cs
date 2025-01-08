using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FrameOfSystem3.Views.Functional
{
    /// <summary>
    /// 2018.06.23 by yjlee [ADD] OK , CANCEL을 위한 확인용 메시지 폼이다.
    /// </summary>
    public partial class Form_MessageBox : Form
    {
        #region 싱글톤
        private static Form_MessageBox _Instance = null;
        public static Form_MessageBox GetInstance()
        {
            if (_Instance == null)
            {
                _Instance = new Form_MessageBox();
            }

            return _Instance;
        }
        private Form_MessageBox()
        {
            DoubleBuffered = true;

			m_InstanceOfLanguage	= FrameOfSystem3.Config.ConfigLanguage.GetInstance();

            InitializeComponent();
			DEFAULT_SIZE_TEXTBOX = new Size(panel_TextBox.Size.Width, panel_TextBox.Size.Height);
			DEFAULT_SIZE_PANEL = new Size(panel_Main.Size.Width, panel_Main.Size.Height);
			DEFAULT_SIZE_FORM = new Size(this.Size.Width, this.Size.Height);
        }
        #endregion

		#region 상수
		readonly string OK_BUTTON_TEXT = "OK";
		readonly string CANCEL_BUTTON_TEXT = "CANCEL";
		readonly string YES_BUTTON_TEXT = "YES";
		readonly string NO_BUTTON_TEXT = "NO";

		readonly Size DEFAULT_SIZE_TEXTBOX;
		readonly Size DEFAULT_SIZE_PANEL;
		readonly Size DEFAULT_SIZE_FORM;
		#endregion

		#region 변수

		#region Instance
		FrameOfSystem3.Config.ConfigLanguage m_InstanceOfLanguage		= null;
		#endregion

		#region GUI
		private bool m_bMouseDownTitle					= false;
		private Point m_pMouseDownCoordinate			= new Point();
		#endregion

		#endregion

		#region Internal Interface
		/// <summary>
		/// 2021.08.24 by twkang [ADD] 이벤트 처리
		/// </summary>
		private void ProcessingEvent(Keys enInputedKey)
		{
			switch(enInputedKey)
			{
				case Keys.Enter:
					this.DialogResult = System.Windows.Forms.DialogResult.OK;
					break;
				case Keys.Escape:
					this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
					break;
				default:
					return;
			}

			this.Close();
		}
		/// <summary>
		/// 2024.01.17 by junho [ADD] button style 추가
		/// </summary>
		private void ChangeStyle(EN_STYLE style)
		{
			switch(style)
			{
				case EN_STYLE.YesNo:
					m_btnOK.Text = m_InstanceOfLanguage.TranslateWord(YES_BUTTON_TEXT);
					m_btnCancel.Text = m_InstanceOfLanguage.TranslateWord(NO_BUTTON_TEXT);
					m_btnCancel.Visible = true;
					m_btnOK.Location = new Point(140, 138);
					break;
				case EN_STYLE.OkOnly:
					m_btnOK.Text = m_InstanceOfLanguage.TranslateWord(OK_BUTTON_TEXT);
					m_btnCancel.Text = m_InstanceOfLanguage.TranslateWord(CANCEL_BUTTON_TEXT);
					m_btnCancel.Visible = false;
					m_btnOK.Location = new Point(200, 138);
					break;
				default:
					m_btnOK.Text = m_InstanceOfLanguage.TranslateWord(OK_BUTTON_TEXT);
					m_btnCancel.Text = m_InstanceOfLanguage.TranslateWord(CANCEL_BUTTON_TEXT);
					m_btnCancel.Visible = true;
					m_btnOK.Location = new Point(140, 138);
					break;
			}
		}
		private void AdjustFormSize()
		{
			// 메시지 내용에 따라 폼 크기 조정
			Size textSize = TextRenderer.MeasureText(m_labelMessage.Text, m_labelMessage.Font
				, new Size(Screen.PrimaryScreen.Bounds.Width - 200, 0), TextFormatFlags.WordBreak);

			textSize.Width += 20;	// 텍스트 너비에 여유 공간 20 추가
			textSize.Height += 50; // 텍스트 높이에 여유 공간 50 추가

			// 크기 계산
			int offsetWidth = (textSize.Width > DEFAULT_SIZE_TEXTBOX.Width) ? textSize.Width - DEFAULT_SIZE_TEXTBOX.Width : 0;
			int offsetHeight = (textSize.Height > DEFAULT_SIZE_TEXTBOX.Height) ? textSize.Height - DEFAULT_SIZE_TEXTBOX.Height : 0;

			// 폼 크기 조정
			this.Size = new Size(DEFAULT_SIZE_FORM.Width + offsetWidth, DEFAULT_SIZE_FORM.Height + offsetHeight);
			panel_Main.Size = new Size(DEFAULT_SIZE_PANEL.Width + offsetWidth, DEFAULT_SIZE_PANEL.Height + offsetHeight);
		}
		#endregion

		#region 외부 인터페이스
		/// <summary>
        /// 2018.06.23 by yjlee [ADD] 메시지 창을 화면에 출력한다.
        /// </summary>
        /// <param name="strMessage"></param>
        /// <returns></returns>
        public bool ShowMessage(string strMessage, string strTitle = "CONFIRMATION MESSAGE", EN_STYLE style = EN_STYLE.OkCancel)
        {
			if (IsFormOpening())
				return false;

            m_labelMessage.Text = strMessage;

            m_TitleBar.Text		= m_InstanceOfLanguage.TranslateWord(strTitle);
			ChangeStyle(style);
			AdjustFormSize();

			m_labelMessage.Text = m_InstanceOfLanguage.TranslateWord(m_labelMessage.Text);

            this.CenterToScreen();

            if(!this.Modal)
                this.ShowDialog();

            if (this.DialogResult == System.Windows.Forms.DialogResult.OK)
            {
                return true;
            }

            return false;
        }
		public bool ShowMessage(string message, EN_STYLE style, string title = "CONFIRMATION MESSAGE")
		{
			return ShowMessage(message, title, style);
        }

		private delegate bool DelegateShowWarningMessage(string strMessage, EN_STYLE style, string strTitle = "WARNING MESSAGE", int indexOfBuzzer = -1, bool bBuzzer = false);
		public bool ShowWarningMessage(string strMessage, EN_STYLE style = EN_STYLE.OkCancel, string strTitle = "WARNING MESSAGE", int indexOfBuzzer = -1, bool bBuzzer = false)
        {
            try
            {
                if (this.InvokeRequired)
                {
                    DelegateShowWarningMessage d = new DelegateShowWarningMessage(ShowWarningMessage);
                    IAsyncResult result = this.BeginInvoke(d, new object[] { strMessage, strTitle });
                    return d.EndInvoke(result);
                }
                else
                {
					if (bBuzzer && indexOfBuzzer >= 0)
					{
						DigitalIO_.DigitalIO.GetInstance().WriteOutput(indexOfBuzzer, bBuzzer);
					}

                    m_labelMessage.Text = strMessage;
                    m_labelMessage.BackGroundColor = Color.OrangeRed;
                    m_labelMessage.MainFontColor = Color.White;

                    m_TitleBar.Text = m_InstanceOfLanguage.TranslateWord(strTitle);
                    //m_btnOK.Text = m_InstanceOfLanguage.TranslateWord(OK_BUTTON_TEXT);
                    //m_btnCancel.Text = m_InstanceOfLanguage.TranslateWord(CANCEL_BUTTON_TEXT);

					ChangeStyle(style);
					AdjustFormSize();

					CenterToScreen();
                    TopMost = true;
                    if (false == IsFormOpening())
                    {
                        ShowDialog();
                    }

                    if (bBuzzer && indexOfBuzzer >= 0)
                        DigitalIO_.DigitalIO.GetInstance().WriteOutput(indexOfBuzzer, false);

                    m_labelMessage.BackGroundColor = Color.WhiteSmoke;
                    m_labelMessage.MainFontColor = Color.Black;

                    if (DialogResult == System.Windows.Forms.DialogResult.OK)
                    {
                        return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
        private bool IsFormOpening()
        {
            FormCollection fc = Application.OpenForms;
            bool bIsFormOpened = false;
            foreach (Form frm in fc)
            {
                //iterate through
                if (frm.Name == "Form_MessageBox")
                {
                    bIsFormOpened = true;
                    break;
                }
            }

            return bIsFormOpened;
        }
        #endregion

        #region UI 이벤트 처리
        /// <summary>
        /// 2018.06.23 by yjlee [ADD] OK 또는 CANCEL 버튼을 눌렀을 경우 처리
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Click_OkorCancel(object sender, EventArgs e)
        {
            Control ctr = sender as Control;

            switch (ctr.TabIndex)
            {
                case 0: // OK
					ProcessingEvent(Keys.Enter);
                    break;
                case 1: // CANCEL
					ProcessingEvent(Keys.Escape);
                    break;
            }
        }
		/// <summary>
		/// 2021.07.28 by twkang [ADD] TitleBar Mouse Down 이벤트
		/// </summary>
		private void MouseDown_Title(object sender, MouseEventArgs e)
		{
			m_bMouseDownTitle = true;
			m_pMouseDownCoordinate = e.Location;
		}
		/// <summary>
		/// 2021.07.28 by twkang [ADD] TitleBar Mouse Move 이벤트
		/// </summary>
		private void MouseMove_Title(object sender, MouseEventArgs e)
		{
			if (m_bMouseDownTitle)
			{
				this.SetDesktopLocation(MousePosition.X - m_pMouseDownCoordinate.X, MousePosition.Y - m_pMouseDownCoordinate.Y);
			}
		}
		/// <summary>
		/// 2021.07.28 by twkang [ADD] TitleBar Mouse Up 이벤트
		/// </summary>
		private void MouseUp_Title(object sender, MouseEventArgs e)
		{
			m_bMouseDownTitle = false;
		}

		/// <summary>
		/// 2021.08.24 by twkang [ADD] 키입력 이벤트
		/// </summary>
		private void KeyDown_Event(object sender, KeyEventArgs e)
		{
			ProcessingEvent(e.KeyCode);
		}
        #endregion

		private void Form_MessageBox_KeyDown(object sender, KeyEventArgs e)
		{
			int nKeyCode = (int)e.KeyCode;

			switch (e.KeyCode)
			{
				case Keys.Escape: // Esc 입력 시
				case Keys.Back:	// 백스페이스 입력 시
					ProcessingEvent(Keys.Escape);
					break;
				case Keys.Enter: // 엔터 입력 시
					ProcessingEvent(Keys.Enter);
					break;
				default:
					break;
			}
		}

		public enum EN_STYLE
		{
			OkCancel,
			OkOnly,
			YesNo,
		}
    }
}
