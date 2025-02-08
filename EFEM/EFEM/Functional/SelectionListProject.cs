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
        // 2025.02.08. by dwlim [MOD] 
        private enum WaferType
        {
            Core_8,
            Core_12,
            Sort_12

            // PWA-500Bin꺼
            //Core,
            //Empty,
            //StageCenter,
            //StageLeft,
            //StageRight,
        }

        private void MakeListByProjectEnum()
		{  
            m_DicOfList.Add(EN_SELECTIONLIST.ARM_TYPE, Enum.GetNames(typeof(EFEM.Defines.AtmRobot.RobotArmTypes)));
            m_DicOfList.Add(EN_SELECTIONLIST.WAFER_TYPE, Enum.GetNames(typeof(WaferType)));

            m_DicOfList.Add(EN_SELECTIONLIST.SUBSTRATE_TRANSFER_STATE, Enum.GetNames(typeof(EFEM.Defines.MaterialTracking.SubstrateTransferStates)));
            m_DicOfList.Add(EN_SELECTIONLIST.SUBSTRATE_PROCESSING_STATE, Enum.GetNames(typeof(EFEM.Defines.MaterialTracking.ProcessingStates)));
            m_DicOfList.Add(EN_SELECTIONLIST.SUBSTRATE_ID_READING_STATE, Enum.GetNames(typeof(EFEM.Defines.MaterialTracking.IdReadingStates)));
            m_DicOfList.Add(EN_SELECTIONLIST.SUBSTRATE_TYPE, Enum.GetNames(typeof(EFEM.CustomizedByProcessType.PWA500BIN.SubstrateType)));
            // m_DicOfList.Add(EN_SELECTIONLIST.WORK_DIRECTION			, Enum.GetNames(typeof(Define.DefineEnumProject.Map.EN_WORK_DIRECTION)));
        }
    }
}
