﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Define.DefineEnumProject.SelectionList;

namespace FrameOfSystem3.Functional
{
	public partial class SelectionList
	{
        private enum WaferType500Bin
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

        // 2025.02.08. by dwlim [MOD] 
        private enum WaferType500W
        {
            Core_8,
            Core_12,
            Sort_12
        }

        private void MakeListByProjectEnum()
		{  
            m_DicOfList.Add(EN_SELECTIONLIST.ARM_TYPE, Enum.GetNames(typeof(EFEM.Defines.AtmRobot.RobotArmTypes)));
            m_DicOfList.Add(EN_SELECTIONLIST.WAFER_TYPE_BIN, Enum.GetNames(typeof(WaferType500Bin)));
            m_DicOfList.Add(EN_SELECTIONLIST.WAFER_TYPE_500W, Enum.GetNames(typeof(WaferType500W)));

            m_DicOfList.Add(EN_SELECTIONLIST.SUBSTRATE_TRANSFER_STATE, Enum.GetNames(typeof(EFEM.Defines.MaterialTracking.SubstrateTransferStates)));
            m_DicOfList.Add(EN_SELECTIONLIST.SUBSTRATE_PROCESSING_STATE, Enum.GetNames(typeof(EFEM.Defines.MaterialTracking.ProcessingStates)));
            m_DicOfList.Add(EN_SELECTIONLIST.SUBSTRATE_ID_READING_STATE, Enum.GetNames(typeof(EFEM.Defines.MaterialTracking.IdReadingStates)));
            m_DicOfList.Add(EN_SELECTIONLIST.SUBSTRATE_TYPE, Enum.GetNames(typeof(EFEM.CustomizedByProcessType.PWA500BIN.SubstrateType)));
            // 2025.02.11 dwlim [ADD] 500W 추가
            m_DicOfList.Add(EN_SELECTIONLIST.SUBSTRATE_TYPE_500W, Enum.GetNames(typeof(EFEM.CustomizedByProcessType.PWA500W.SubstrateType)));
            // m_DicOfList.Add(EN_SELECTIONLIST.WORK_DIRECTION			, Enum.GetNames(typeof(Define.DefineEnumProject.Map.EN_WORK_DIRECTION)));
        }
    }
}
