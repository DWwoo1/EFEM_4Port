using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FrameOfSystem3.Views.Config
{
    public partial class Config_Tool : UserControlForMainView.CustomView
    {
        #region CONSTANT

        private const int DGVIEW_TOOL_INDEX = 0;
        private const int DGVIEW_TOOL_NAME = 1;

        private const int DGVIEW_ITEM_NAME = 0;
        private const int DGVIEW_ITEM_VALUE = 1;

        private const int SELECT_NONE = -1;
        private const int STARTING_INDEX = 0;

        private readonly Color c_clrTrue = Color.DodgerBlue;
        private readonly Color c_clrFalse = Color.White;
        #endregion

        #region CONSTRUCTOR
        public Config_Tool()
        {
            InitializeComponent();

			m_InstanceOfCalculator				= Functional.Form_Calculator.GetInstance();
			m_InstanceOfKeyboard				= Functional.Form_Keyboard.GetInstance();
			m_InstanceOfSelectionList			= Functional.Form_SelectionList.GetInstance();
			m_InstanceOfMessageBox				= Functional.Form_MessageBox.GetInstance();

            m_ConfigTool = FrameOfSystem3.Config.ConfigTool.GetInstance();
		}
        #endregion

		#region FIELD
		private int m_nSelectedToolListRow = -1;
		private int m_nSelectedToolListIndex = -1;

		Functional.Form_MessageBox m_InstanceOfMessageBox					= null;
		Functional.Form_Keyboard m_InstanceOfKeyboard						= null;
		Functional.Form_Calculator m_InstanceOfCalculator					= null;
		Functional.Form_SelectionList m_InstanceOfSelectionList				= null;

        FrameOfSystem3.Config.ConfigTool m_ConfigTool = null;              
		#endregion

		#region OVERRIDE
        protected override void ProcessWhenActivation()
        {
            UpdateToolList();

			if(SELECT_NONE != m_nSelectedToolListRow)
			{
                m_dgViewToolList.Rows[m_nSelectedToolListRow].Selected = true;
			}
        }
        protected override void ProcessWhenDeactivation()
        {
        }
        public override void CallFunctionByTimer()
        {
            if (m_nSelectedToolListIndex.Equals(SELECT_NONE))
                return;

            UpdateMonitoringData(m_nSelectedToolListIndex);
        }
        #endregion

		#region METHOD
        private void UpdateToolList()
        {
            m_dgViewToolList.Rows.Clear();

            string[] arToolList = null;

            if (m_ConfigTool.GetToolList(ref arToolList))
            {
                for (int nIndex = 0, nEnd = arToolList.Length; nIndex < nEnd; ++nIndex)
                {
                    string sIndex = arToolList[nIndex];

                    m_dgViewToolList.Rows.Add();

                    m_dgViewToolList[DGVIEW_TOOL_INDEX, nIndex].Value = sIndex;
                    
                    string sItemValue = string.Empty;

                    m_ConfigTool.GetToolName(sIndex, ref sItemValue);

                    m_dgViewToolList[DGVIEW_TOOL_NAME, nIndex].Value = sItemValue;
                }

                m_nSelectedToolListIndex = int.Parse(arToolList[STARTING_INDEX].ToString());

                m_nSelectedToolListRow = STARTING_INDEX;

                UpdateItemList(m_nSelectedToolListIndex);
            }
        }
        private void UpdateItemList(int nSelectedIndex)
        {
            // ITEM LIST
            m_dgViewItemList.Rows.Clear();

            string[] arItemNames = null;
            string[] arItemValues = null;

            m_ConfigTool.GetItemsValue(nSelectedIndex.ToString(), ref arItemNames, ref arItemValues);

            for (int nIndex = 0, nEnd = arItemValues.Length; nIndex < nEnd; ++nIndex)
            {
                m_dgViewItemList.Rows.Add();

                m_dgViewItemList[DGVIEW_ITEM_NAME, nIndex].Value = arItemNames[nIndex];
                m_dgViewItemList[DGVIEW_ITEM_VALUE, nIndex].Value = arItemValues[nIndex];
            }

            // MONITORING DATA
            m_dgViewMonitoringData.Rows.Clear();

            string[] arMonitoringNames = null;
            string[] arMonitoringValues = null;

            m_ConfigTool.GetMonitoringData(nSelectedIndex.ToString(), ref arMonitoringNames, ref arMonitoringValues);

            for (int nIndex = 0, nEnd = arMonitoringValues.Length; nIndex < nEnd; ++nIndex)
            {
                m_dgViewMonitoringData.Rows.Add();

                m_dgViewMonitoringData[DGVIEW_ITEM_NAME, nIndex].Value = arMonitoringNames[nIndex];
                m_dgViewMonitoringData[DGVIEW_ITEM_VALUE, nIndex].Value = arMonitoringValues[nIndex];
            }
        }
        private void UpdateMonitoringData(int nSelectedIndex)
        {
            string[] arMonitoringNames = null;
            string[] arMonitoringValues = null;

            m_ConfigTool.GetMonitoringData(nSelectedIndex.ToString(), ref arMonitoringNames, ref arMonitoringValues);

            for (int nIndex = 0, nEnd = arMonitoringValues.Length; nIndex < nEnd; ++nIndex)
            {
                m_dgViewMonitoringData[DGVIEW_ITEM_NAME, nIndex].Value = arMonitoringNames[nIndex];

				if(nIndex == 6)
					m_dgViewMonitoringData[DGVIEW_ITEM_VALUE, nIndex].Value = arMonitoringValues[nIndex].Substring(0, 8);
				else
					m_dgViewMonitoringData[DGVIEW_ITEM_VALUE, nIndex].Value = arMonitoringValues[nIndex];
            }
        }
		#endregion

		#region UI EVENT
        private void Click_dgViewToolList(object sender, DataGridViewCellEventArgs e)
        {
            int nRowIndex = e.RowIndex;

            if (nRowIndex < 0 || nRowIndex >= m_dgViewToolList.RowCount)
                return;

            if (m_nSelectedToolListRow.Equals(nRowIndex)) 
                return;

            m_nSelectedToolListIndex = int.Parse(m_dgViewToolList[DGVIEW_TOOL_INDEX, nRowIndex].Value.ToString());

            m_nSelectedToolListRow = nRowIndex;

            UpdateItemList(m_nSelectedToolListIndex);
        }
		private void Click_Configuration(object sender, EventArgs e)
		{
            /*
			Control ctrl			= sender as Control;

			string strResult		= string.Empty;
			int nValue				= -1;

			string[] strArrName		= null;
			int[] nArrIndex			= null;
			switch(ctrl.TabIndex)
			{
				case 0: // NAME
					if(m_InstanceOfKeyboard.CreateForm(m_labelName.Text))
					{
						m_InstanceOfKeyboard.GetResult(ref strResult);
						if(m_InstanceOfInterrupt.SetParameter(m_nSelectedToolListIndex, FrameOfSystem3.Config.ConfigInterrupt.EN_PARAM_INTERRUPT.NAME, strResult))
						{
							m_labelName.Text	= strResult;
							m_dgViewInterrupt[COLUMN_INDEX_OF_NAME, m_nSelectedToolListRow].Value	= strResult;
						}
					}
					break;
				case 2:	// CONDITION
					if(m_InstanceOfSelectionList.CreateForm(m_lblCondition.Text, Define.DefineEnumProject.SelectionList.EN_SELECTIONLIST.EQUIPMENT_STATE
						, m_labelCondition.Text, true))
					{
						m_InstanceOfSelectionList.GetResult(ref strResult);
						if(m_InstanceOfInterrupt.SetParameter(m_nSelectedToolListIndex, FrameOfSystem3.Config.ConfigInterrupt.EN_PARAM_INTERRUPT.CONDITION, strResult))
						{
							m_labelCondition.Text	= strResult;
							m_dgViewInterrupt[COLUMN_INDEX_OF_CONDITION, m_nSelectedToolListRow].Value	= strResult;
						}
					}
					break;
				case 3:	// ALARM CODE
					if(m_InstanceOfCalculator.CreateForm(m_labelAlarmCode.Text, MIN_OF_PARAM, MAX_OF_PARAM))
					{
						m_InstanceOfCalculator.GetResult(ref nValue);
						strResult		= nValue.ToString();
						if(m_InstanceOfInterrupt.SetParameter(m_nSelectedToolListIndex, FrameOfSystem3.Config.ConfigInterrupt.EN_PARAM_INTERRUPT.CODE_ALARM, strResult))
						{
							m_labelAlarmCode.Text = strResult;
							m_dgViewInterrupt[COLUMN_INDEX_OF_CODE, m_nSelectedToolListRow].Value	= strResult;
						}
					}
					break;
				case 4:	// ACTION
                    if (m_InstanceOfSelectionList.CreateForm(m_lblAction.Text, Define.DefineEnumProject.SelectionList.EN_SELECTIONLIST.INTERRUPT_ACTION, m_labelAction.Text))
                    {
                        m_InstanceOfSelectionList.GetResult(ref strResult);
                        if (m_InstanceOfInterrupt.SetParameter(m_nSelectedToolListIndex, FrameOfSystem3.Config.ConfigInterrupt.EN_PARAM_INTERRUPT.ACTION, strResult))
                        {
                            m_labelAction.Text = strResult;
                            m_dgViewInterrupt[COLUMN_INDEX_OF_ACTION, m_nSelectedToolListRow].Value = strResult;
                        }
                    }
					break;
			}
            */
		}

		private void Click_Reset(object sender, EventArgs e)
		{
			if (m_nSelectedToolListIndex < 0) return;

			m_ConfigTool.ResetItem(m_nSelectedToolListIndex);
		}
		#endregion
    }
}
