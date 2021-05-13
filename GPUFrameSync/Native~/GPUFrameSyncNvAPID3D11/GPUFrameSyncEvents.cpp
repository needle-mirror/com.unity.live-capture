#include "d3d11.h"
#include "dxgi.h"

#include "GPUFrameSync.h"
#include "GPUFrameSyncEvents.h"

#include "Unity/IUnityRenderingExtensions.h"
#include "Unity/IUnityGraphicsD3D11.h"

#include <sstream>
#include <fstream>

namespace GPUFrameSync
{
	static GPUFrameSync s_GPUFrameSync;
	static IUnityInterfaces* s_UnityInterfaces = NULL;
	static IUnityGraphicsD3D11* s_UnityGraphicsD3D11 = NULL;
	static IUnityGraphics* s_UnityGraphics = NULL;
	static ID3D11Device* s_D3D11Device = NULL;
	static IDXGISwapChain* s_D3D11SwapChain = NULL;
	static bool s_Initialized = false;

#ifdef DEBUG_LOG
	void WriteFileDebug_Advanced(const char* const message, const bool append)
	{
		std::ofstream myfile;

		if (append)
		{
			myfile.open("C:/NVIDIA_GPUFrameSync_DebugFile_New.txt", std::ios_base::app | std::ios_base::out);
		}
		else
		{
			myfile.open("C:/NVIDIA_GPUFrameSync_DebugFile_New.txt");
		}

		myfile << message;
		myfile.close();
	}
#endif

	// Override the function defining the load of the plugin
	extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
		UnityPluginLoad(IUnityInterfaces * unityInterfaces)
	{
		if (unityInterfaces)
		{
#ifdef DEBUG_LOG
			WriteFileDebug_Advanced("* Success: UnityPluginLoad\n", true);
#endif
			s_UnityInterfaces = unityInterfaces;
			s_UnityGraphics = s_UnityInterfaces->Get<IUnityGraphics>();
			if (s_UnityGraphics)
			{
				s_UnityGraphicsD3D11 = unityInterfaces->Get<IUnityGraphicsD3D11>();
				s_D3D11Device = s_UnityGraphicsD3D11->GetDevice();
				s_D3D11SwapChain = s_UnityGraphicsD3D11->GetSwapChain();
				s_UnityGraphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);
			}
		}
		else
		{
#ifdef DEBUG_LOG
			WriteFileDebug_Advanced("* Failed: UnityPluginLoad, unityInterfaces is null \n", true);
#endif
		}
	}

	// Freely defined function to pass a callback to plugin-specific scripts
	extern "C" UnityRenderingEventAndData UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
		GetRenderEventFuncD3D11()
	{
		return OnRenderEvent;
	}

	// Override the query method to use the `PresentFrame` callback
	// It has been added specially for the NvAPI plugin.
	extern "C" bool UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
		UnityRenderingExtQuery(UnityRenderingExtQueryType query)
	{
		if (!IsContextValid())
			return false;

		return (query == UnityRenderingExtQueryType::kUnityRenderingExtQueryOverridePresentFrame)
			? s_GPUFrameSync.Render(s_D3D11Device,
				s_D3D11SwapChain,
				s_UnityGraphicsD3D11->GetSyncInterval(),
				s_UnityGraphicsD3D11->GetPresentFlags())
			: false;
	}

	// Override function to receive graphics event 
	static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType)
	{
		if (eventType == kUnityGfxDeviceEventInitialize && !s_Initialized)
		{
#ifdef DEBUG_LOG
			WriteFileDebug_Advanced("---- Initialize File ----\n", false);
#endif
			s_Initialized = true;
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
		if (s_UnityGraphics == nullptr)
		{
#ifdef DEBUG_LOG
			WriteFileDebug_Advanced("* Failed: IsContextValid, s_UnityGraphics == nullptr \n", true);
#endif
			return false;
		}

		if (s_UnityGraphics->GetRenderer() != UnityGfxRenderer::kUnityGfxRendererD3D11)
		{
#ifdef DEBUG_LOG
			WriteFileDebug_Advanced("* Failed: s_UnityGraphics->GetRenderer() != UnityGfxRenderer::kUnityGfxRendererD3D11 \n", true);
#endif
			return false;
		}

		if (s_D3D11Device == nullptr)
		{
#ifdef DEBUG_LOG
			WriteFileDebug_Advanced("* Failed: IsContextValid, s_D3D11Device == nullptr \n", true);
#endif
			s_D3D11Device = s_UnityGraphicsD3D11->GetDevice();
		}

		if (s_D3D11SwapChain == nullptr)
		{
#ifdef DEBUG_LOG
			WriteFileDebug_Advanced("* Failed: IsContextValid, s_D3D11SwapChain == nullptr \n", true);
#endif
			s_D3D11SwapChain = s_UnityGraphicsD3D11->GetSwapChain();
		}

		return (s_D3D11Device != nullptr && s_D3D11SwapChain != nullptr);
	}

	// Enable Workstation SwapGroup & potentially join the SwapGroup / Barrier
	void Initialize()
	{
		if (!IsContextValid())
			return;

        s_GPUFrameSync.SetupWorkStation();
        s_GPUFrameSync.Initialize(s_D3D11Device, s_D3D11SwapChain);
	}

	// Query the actual frame count (master or custom one)
	void QueryFrameCount(int* const value)
	{
		if (!IsContextValid() || value == nullptr)
			return;

		auto frameCount = s_GPUFrameSync.QueryFrameCount(s_D3D11Device);
		*value = (int)frameCount;
	}

	// Reset the frame count (master or custom one)
	void ResetFrameCount()
	{
		if (!IsContextValid())
			return;

        s_GPUFrameSync.ResetFrameCount(s_D3D11Device);
	}

	// Leave the Barrier and Swap Group, disable the Workstation SwapGroup
	void Dispose()
	{
		if (!IsContextValid())
			return;

        s_GPUFrameSync.Dispose(s_D3D11Device, s_D3D11SwapChain);
        s_GPUFrameSync.DisposeWorkStation();
	}

	// Directly join or leave the Swap Group and Barrier
	void EnableSystem(const bool value)
	{
		if (!IsContextValid())
			return;

        s_GPUFrameSync.EnableSystem(s_D3D11Device, s_D3D11SwapChain, value);
	}

	// Toggle to join/leave the SwapGroup
	void EnableSwapGroup(const bool value)
	{
		if (!IsContextValid())
			return;

        s_GPUFrameSync.EnableSwapGroup(s_D3D11Device, s_D3D11SwapChain, value);
	}

	// Toggle to join/leave the Barrier
	void EnableSwapBarrier(const bool value)
	{
		if (!IsContextValid())
			return;

        s_GPUFrameSync.EnableSwapBarrier(s_D3D11Device, value);
	}

	// Enable or disable the Master Sync Counter
	void EnableSyncCounter(const bool value)
	{
		if (!IsContextValid())
			return;

        s_GPUFrameSync.EnableSyncCounter(value);
	}
}
