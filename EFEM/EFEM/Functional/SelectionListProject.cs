using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Define.DefineEnumProject.SelectionList;

namespace FrameOfSystem3.Functional
{
	public partial class SelectionList
	{
        // 2025.01.07. by dwlim [MOD] Process Module 분리로 인한 이름 변경
        private enum WaferType_PWA500BIN
        {
            Core,
            Empty,
            StageCenter,
            StageLeft,
            StageRight,
            //Bin1,
            //Bin2,
            //Bin3
        }
        // 2025.01.07. by dwlim [MOD] Process Module 분리로 인한 추가
        private enum WaferType_PWA500W
        {
            CORE_8,
            CORE_12,
            SORT_12,
        }

        private void MakeListByProjectEnum()
		{  
            m_DicOfList.Add(EN_SELECTIONLIST.ARM_TYPE, Enum.GetNames(typeof(EFEM.Defines.AtmRobot.RobotArmTypes)));
            m_DicOfList.Add(EN_SELECTIONLIST.WAFER_TYPE_PWA500BIN, Enum.GetNames(typeof(WaferType_PWA500BIN)));
            m_DicOfList.Add(EN_SELECTIONLIST.WAFER_TYPE_PWA500W, Enum.GetNames(typeof(WaferType_PWA500W)));

            m_DicOfList.Add(EN_SELECTIONLIST.SUBSTRATE_TRANSFER_STATE, Enum.GetNames(typeof(EFEM.Defines.MaterialTracking.SubstrateTransferStates)));
            m_DicOfList.Add(EN_SELECTIONLIST.SUBSTRATE_PROCESSING_STATE, Enum.GetNames(typeof(EFEM.Defines.MaterialTracking.ProcessingStates)));
            m_DicOfList.Add(EN_SELECTIONLIST.SUBSTRATE_ID_READING_STATE, Enum.GetNames(typeof(EFEM.Defines.MaterialTracking.IdReadingStates)));
            m_DicOfList.Add(EN_SELECTIONLIST.SUBSTRATE_TYPE, Enum.GetNames(typeof(EFEM.CustomizedByProcessType.PWA500BIN.SubstrateType)));
            // m_DicOfList.Add(EN_SELECTIONLIST.WORK_DIRECTION			, Enum.GetNames(typeof(Define.DefineEnumProject.Map.EN_WORK_DIRECTION)));
        }
    }
}
