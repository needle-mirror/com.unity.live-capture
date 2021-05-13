#include <windows.h>

#include "d3d12.h"
#include "dxgi.h"

#include "GPUFrameSync.h"
#include "GPUFrameSyncEvents.h"

#include "Unity/IUnityRenderingExtensions.h"
#include "Unity/IUnityGraphicsD3D12.h"

namespace GPUFrameSync
{
	static GPUFrameSync s_GPUFrameSync;
	static IUnityInterfaces* s_UnityInterfaces = NULL;
	static IUnityGraphicsD3D12v7* s_UnityGraphicsD3D12 = NULL;
	static IUnityGraphics* s_UnityGraphics = NULL;
	static ID3D12Device* s_D3D12Device = NULL;
	static IDXGISwapChain* s_SwapChain = NULL;
	static bool s_Initialized = false;

	// Override the function defining the load of the plugin
	extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
		UnityPluginLoad(IUnityInterfaces * unityInterfaces)
	{
		if (unityInterfaces)
		{
			s_UnityInterfaces = unityInterfaces;

			s_UnityGraphics = s_UnityInterfaces->Get<IUnityGraphics>();
			if (s_UnityGraphics)
			{
				s_UnityGraphicsD3D12 = unityInterfaces->Get<IUnityGraphicsD3D12v7>();
				s_UnityGraphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);
			}
		}
	}

	// Freely defined function to pass a callback to plugin-specific scripts
	extern "C" UnityRenderingEventAndData UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
		GetRenderEventFuncD3D12()
	{
		return OnRenderEvent;
	}

	// Override the query method to use the `PresentFrame` callback.
	// It has been specially added for the NvAPI plugin.
	extern "C" bool UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
		UnityRenderingExtQuery(UnityRenderingExtQueryType query)
	{
		if (!IsContextValid())
			return false;

		return (query == UnityRenderingExtQueryType::kUnityRenderingExtQueryOverridePresentFrame)
			? s_GPUFrameSync.Render(s_D3D12Device,
				s_SwapChain,
				s_UnityGraphicsD3D12->GetSyncInterval(),
				s_UnityGraphicsD3D12->GetPresentFlags())
			: false;
	}

	// Override function to receive graphics event 
	static void UNITY_INTERFACE_API
		OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType)
	{
		if (eventType == kUnityGfxDeviceEventInitialize && !s_Initialized)
		{
			s_Initialized = true;
            s_GPUFrameSync.Prepare();
		}
		else if (eventType == kUnityGfxDeviceEventShutdown)
		{
			s_Initialized = false;
		}
	}

	// Plugin function to handle a specific rendering event
	static void UNITY_INTERFACE_API
		OnRenderEvent(int eventID, void* data)
	{
		switch (eventID)
		{
		case (int)FrameSyncCommand::Initialize:
			Initialize();
			break;
		case (int)FrameSyncCommand::QueryFrameCount:
			QueryFrameCount(static_cast<int* const>(data));
			break;
		case (int)FrameSyncCommand::ResetFrameCount:
			ResetFrameCount();
			break;
		case (int)FrameSyncCommand::Dispose:
			Dispose();
			break;
		case (int)FrameSyncCommand::EnableSwapGroup:
			EnableSwapGroup(static_cast<bool>(data));
			break;
		case (int)FrameSyncCommand::EnableSwapBarrier:
			EnableSwapBarrier(static_cast<bool>(data));
			break;
		case (int)FrameSyncCommand::EnableSyncCounter:
			EnableSyncCounter(static_cast<bool>(data));
			break;
		default:
			break;
		}
	}

	// Verify if the D3D11 Device and the Swap Chain are valid
	bool IsContextValid()
	{
		if (s_UnityGraphics->GetRenderer() != UnityGfxRenderer::kUnityGfxRendererD3D12)
			return false;

		if (s_D3D12Device == nullptr)
			s_D3D12Device = s_UnityGraphicsD3D12->GetDevice();

		if (s_SwapChain == nullptr)
			s_SwapChain = s_UnityGraphicsD3D12->GetSwapChain();

		return (s_D3D12Device != nullptr && s_SwapChain != nullptr);
	}

	// Enable Workstation SwapGroup & potentially join the SwapGroup / Barrier
	void Initialize()
	{
		if (!IsContextValid())
			return;

        s_GPUFrameSync.SetupWorkStation();
        s_GPUFrameSync.Initialize(s_D3D12Device, s_SwapChain);
	}

	// Query the actual frame count (master or custom one)
	void QueryFrameCount(int* const value)
	{
		if (!IsContextValid() || value == nullptr)
			return;

		auto frameCount = s_GPUFrameSync.QueryFrameCount(s_D3D12Device);
		*value = (int)frameCount;
	}

	// Reset the frame count (master or custom one)
	void ResetFrameCount()
	{
		if (!IsContextValid())
			return;

        s_GPUFrameSync.ResetFrameCount(s_D3D12Device);
	}

	// Leave the Barrier and Swap Group, disable the Workstation SwapGroup
	void Dispose()
	{
		if (!IsContextValid())
			return;

        s_GPUFrameSync.Dispose(s_D3D12Device, s_SwapChain);
        s_GPUFrameSync.DisposeWorkStation();
	}

	// Directly join or leave the Swap Group and Barrier
	void EnableSystem(const bool value)
	{
		if (!IsContextValid())
			return;

        s_GPUFrameSync.EnableSystem(s_D3D12Device, s_SwapChain, value);
	}

	// Toggle to join/leave the SwapGroup
	void EnableSwapGroup(const bool value)
	{
		if (!IsContextValid())
			return;

        s_GPUFrameSync.EnableSwapGroup(s_D3D12Device, s_SwapChain, value);
	}

	// Toggle to join/leave the Barrier
	void EnableSwapBarrier(const bool value)
	{
		if (!IsContextValid())
			return;

        s_GPUFrameSync.EnableSwapBarrier(s_D3D12Device, value);
	}

	// Enable or disable the Master Sync Counter
	void EnableSyncCounter(const bool value)
	{
		if (!IsContextValid())
			return;

        s_GPUFrameSync.EnableSyncCounter(value);
	}
}
