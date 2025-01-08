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
        private enum WaferType
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

        private void MakeListByProjectEnum()
		{  
            m_DicOfList.Add(EN_SELECTIONLIST.ARM_TYPE, Enum.GetNames(typeof(EFEM.Defines.AtmRobot.RobotArmTypes)));
            m_DicOfList.Add(EN_SELECTIONLIST.WAFER_TYPE, Enum.GetNames(typeof(WaferType)));


            m_DicOfList.Add(EN_SELECTIONLIST.SUBSTRATE_TRANSFER_STATE, Enum.GetNames(typeof(EFEM.Defines.MaterialTracking.SubstrateTransferStates)));
            m_DicOfList.Add(EN_SELECTIONLIST.SUBSTRATE_PROCESSING_STATE, Enum.GetNames(typeof(EFEM.Defines.MaterialTracking.ProcessingStates)));
            m_DicOfList.Add(EN_SELECTIONLIST.SUBSTRATE_ID_READING_STATE, Enum.GetNames(typeof(EFEM.Defines.MaterialTracking.IdReadingStates)));
            m_DicOfList.Add(EN_SELECTIONLIST.SUBSTRATE_TYPE, Enum.GetNames(typeof(EFEM.CustomizedByCustomer.PWA500BIN.SubstrateType)));
            // m_DicOfList.Add(EN_SELECTIONLIST.WORK_DIRECTION			, Enum.GetNames(typeof(Define.DefineEnumProject.Map.EN_WORK_DIRECTION)));
        }
    }
}
