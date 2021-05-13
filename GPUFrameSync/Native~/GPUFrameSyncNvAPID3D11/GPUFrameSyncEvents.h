#pragma once

#include "Unity/IUnityGraphics.h"

namespace GPUFrameSync
{
	// Enum defining system callbacks
	enum class FrameSyncCommand
	{
		Initialize = 0,
		QueryFrameCount,
		ResetFrameCount,
		Dispose,
		EnableSwapGroup,
		EnableSwapBarrier,
		EnableSyncCounter
	};

	///////////////////////////////////////////////////////////////////////////////
	//
	// FUNCTION NAME:  OnGraphicsDeviceEvent
	//
	//! DESCRIPTION:   Overrided callbacks to handle the Device related events.
	//!
	//! WHEN TO USE:   Automatically called and used when the system is initialized or destroyed.
	//!
	//  SUPPORTED GFX: Direct3D 11
	//!
	//! \param [in]    eventType      Either specify that the Device has been initialized or destroyed.
	///////////////////////////////////////////////////////////////////////////////
	static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType);



	///////////////////////////////////////////////////////////////////////////////
	//
	// FUNCTION NAME:  OnRenderEvent
	//
	//! DESCRIPTION:   Overrided callbacks to handle the NvAPI related events.
	//!
	//! WHEN TO USE:   Called from C# to use a specific NvAPI functionality.
	//!
	//  SUPPORTED GFX: Direct3D 11
	//!
	//! \param [in]    eventID      The FrameSyncCommand that corresponds to the event.
	//! \param [in]    data         Buffer containing the data related to the event.
	///////////////////////////////////////////////////////////////////////////////
	static void UNITY_INTERFACE_API OnRenderEvent(int eventID, void* data);



	///////////////////////////////////////////////////////////////////////////////
	//
	// FUNCTION NAME:  IsContextValid
	//
	//! DESCRIPTION:   Verify if the D3D11 Device and the SwapChain are correct.
	//!
	//! WHEN TO USE:   Use it internally, before calling any other functions related to NvAPI.
	//!
	//  SUPPORTED GFX: Direct3D 11
	//!
	//! \retval ::true          The context is valid (D3D11 Device and the SwapChain)
	//! \retval ::false         The context is invalid (either the D3D11 Device or/and the SwapChain)
	///////////////////////////////////////////////////////////////////////////////
	bool IsContextValid();



	///////////////////////////////////////////////////////////////////////////////
	//
	// FUNCTION NAME:  SyncQueryFrameCount
	//
	//! DESCRIPTION:   Query the actual frame count in Runtime for the Master Sync system
	//                 or for the custom frame count system.
	//!
	//! WHEN TO USE:   After the system has been initialized, use it in runtime to
	//                 retrieve the actual frame count.
	//!
	//  SUPPORTED GFX: Direct3D 11
	//!
	//! \param [in]    value      Storage that will contain the frame count.
	///////////////////////////////////////////////////////////////////////////////
	void QueryFrameCount(int* value);



	///////////////////////////////////////////////////////////////////////////////
	//
	// FUNCTION NAME:  ResetFrameCount
	//
	//! DESCRIPTION:   Reset the frame count for the Master Sync system (NvAPI) or
	//                 for the custom frame count system.
	//!
	//! WHEN TO USE:   After the system has been initialized, use it in runtime as a toggle.
	//!
	//  SUPPORTED GFX: Direct3D 11
	//!
	///////////////////////////////////////////////////////////////////////////////
	void ResetFrameCount();



	///////////////////////////////////////////////////////////////////////////////
	//
	// FUNCTION NAME:  Initialize
	//
	//! DESCRIPTION:   Enable the Workstation SwapGroup and optionaly the use of
	//                 the Swap Group and the Swap Barrier systems (NvAPI).
	//!
	//! WHEN TO USE:   At the start of the program, after NvAPI_Initialize function.
	//!
	//  SUPPORTED GFX: Direct3D 11
	//!
	///////////////////////////////////////////////////////////////////////////////
	void Initialize();



	///////////////////////////////////////////////////////////////////////////////
	//
	// FUNCTION NAME:  Dispose
	//
	//! DESCRIPTION:   Disable the use of the Swap Group and the Swap Barrier systems
	//                 and disable the Workstation SwapGroup (NvAPI)
	//!
	//! WHEN TO USE:   At the end of the program.
	//!
	//  SUPPORTED GFX: Direct3D 11
	//!
	///////////////////////////////////////////////////////////////////////////////
	void Dispose();



    ///////////////////////////////////////////////////////////////////////////////
	//
	// FUNCTION NAME:  EnableSwapGroup
	//
	//! DESCRIPTION:   Enable or disable the use of the Swap Group system (NvAPI).
	//!
	//! WHEN TO USE:   After the system has been initialized, use it in runtime as a toggle.
	//!
	//  SUPPORTED GFX: Direct3D 11
	//!
	//! \param [in]    value      Value that corresponds to the activation or not.
	//
	///////////////////////////////////////////////////////////////////////////////
	void EnableSwapGroup(bool value);



	///////////////////////////////////////////////////////////////////////////////
	//
	// FUNCTION NAME:  EnableSwapBarrier
	//
	//! DESCRIPTION:   Enable or disable the use of the Swap Barrier system (NvAPI).
	//!
	//! WHEN TO USE:   After the system has been initialized, use it in runtime as a toggle.
	//!
	//  SUPPORTED GFX: Direct3D 11
	//!
	//! \param [in]    value      Value that corresponds to the activation or not.
	//
	///////////////////////////////////////////////////////////////////////////////
	void EnableSwapBarrier(bool value);



	///////////////////////////////////////////////////////////////////////////////
	//
	// FUNCTION NAME:  EnableSyncCounter
	//
	//! DESCRIPTION:   Enable or disable the use of the Master sync counter system (NvAPI).
	//!
	//! WHEN TO USE:   After the system has been initialized, use it in runtime as a toggle.
	//!
	//  SUPPORTED GFX: Direct3D 11
	//!
	//! \param [in]    value      Value that corresponds to the activation or not.
	//
	///////////////////////////////////////////////////////////////////////////////
	void EnableSyncCounter(bool value);
}
