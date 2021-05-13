#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.LiveCapture.GPUFrameSync
{
    /// <summary>
    /// Commands issued to the NVIDIA API, to manage the SwapGroup and SwapBarrier system.
    /// </summary>
    /// <remarks>
    /// These commands are issued on the render thread.
    /// </remarks>
    internal enum FrameSyncCommand
    {
        /// <summary>
        /// Enables the SwapGroup and Swap Barrier systems.
        /// </summary>
        Initialize,

        /// <summary>
        /// Queries the frame count.
        /// </summary>
        /// <remarks>
        /// Corresponds to the master sync node frame count,
        /// or the custom frame count in case the master is inactive.
        /// </remarks>
        QueryFrameCount,

        /// <summary>
        /// Resets the frame count.
        /// </summary>
        /// <remarks>
        /// Corresponds to the master sync node frame count,
        /// or the custom frame count in case the master is inactive.
        /// </remarks>
        ResetFrameCount,

        /// <summary>
        /// Disables the SwapGroup and Swap Barrier systems.
        /// </summary>
        Dispose,

        /// <summary>
        /// Activation of the Swap Group system.
        /// </summary>
        EnableSwapGroup,

        /// <summary>
        /// Activation of the Swap Barrier system.
        /// </summary>
        EnableSwapBarrier,

        /// <summary>
        /// Activation of the Master sync counter system.
        /// </summary>
        EnableSyncCounter
    };

    /// <summary>
    /// A class providing an interface with the NVIDIA SwapGroup and SwapBarrier API.
    /// </summary>
    public class GPUFrameSync
    {
#if !UNITY_2021_2_OR_NEWER
        static readonly NotSupportedException k_NotImplementedException = new NotSupportedException("GPUFrameSync requires Unity 2021.2 or newer.");
#endif
        static readonly NotSupportedException k_NotSupportedException = new NotSupportedException("GPUFrameSync only works in a Windows build.");

        /// <summary>
        /// Gets the unique instance of the class (Singleton).
        /// </summary>
        public static GPUFrameSync Instance => k_Instance;

        static readonly GPUFrameSync k_Instance = new GPUFrameSync();

        CommandBuffer m_CmdBuffer;

#if UNITY_2021_2_OR_NEWER
        internal static class Plugin
        {
#if UNITY_EDITOR_WIN
            internal const string k_LibD3D11 = "Packages/com.unity.live-capture/GPUFrameSync/Plugins/x86_64/GPUFrameSyncNvAPID3D11.dll";
            internal const string k_LibD3D12 = "Packages/com.unity.live-capture/GPUFrameSync/Plugins/x86_64/GPUFrameSyncNvAPID3D12.dll";
#elif UNITY_STANDALONE
            internal const string k_LibD3D11 = "GPUFrameSyncNvAPID3D11";
            internal const string k_LibD3D12 = "GPUFrameSyncNvAPID3D12";
#else
            internal const string k_LibD3D11 = "";
            internal const string k_LibD3D12 = ""
#error "System not implemented"
#endif

            [DllImport(k_LibD3D11, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public static extern IntPtr GetRenderEventFuncD3D11();

            [DllImport(k_LibD3D12, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public static extern IntPtr GetRenderEventFuncD3D12();
        }

        bool isCompatible => !Application.isEditor;
#endif

        /// <summary>
        /// Executes an NVIDIA frame sync command.
        /// </summary>
        /// <remarks>
        /// The command is executed asynchronously on the render thread.
        /// </remarks>
        /// <param name="id">The command to execute.</param>
        /// <param name="data">The custom data passed along the command.</param>
        internal void ExecuteCommand(FrameSyncCommand id, IntPtr data)
        {
#if UNITY_2021_2_OR_NEWER
            m_CmdBuffer.Clear();

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11)
            {
                m_CmdBuffer.IssuePluginEventAndData(Plugin.GetRenderEventFuncD3D11(), (int)id, data);
            }
            else if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12)
            {
                m_CmdBuffer.IssuePluginEventAndData(Plugin.GetRenderEventFuncD3D12(), (int)id, data);
            }

            Graphics.ExecuteCommandBuffer(m_CmdBuffer);
#else
            throw k_NotImplementedException;
#endif
        }

        /// <summary>
        /// Initializes the SwapGroup and SwapBarrier systems.
        /// </summary>
        /// <remarks>
        /// The SwapGroup and SwapBarrier systems are activated automatically upon initialization.
        /// </remarks>
        public void Initialize()
        {
#if UNITY_2021_2_OR_NEWER
            if (!isCompatible)
                throw k_NotSupportedException;

            m_CmdBuffer = new CommandBuffer();
            ExecuteCommand(FrameSyncCommand.Initialize, IntPtr.Zero);
#else
            throw k_NotImplementedException;
#endif
        }

        /// <summary>
        /// Releases the Swap Barrier and the Swap Group systems.
        /// </summary>
        public void Dispose()
        {
#if UNITY_2021_2_OR_NEWER
            if (!isCompatible)
                throw k_NotSupportedException;

            ExecuteCommand(FrameSyncCommand.Dispose, IntPtr.Zero);
            m_CmdBuffer.Dispose();
#else
            throw k_NotImplementedException;
#endif
        }
    }
}
#endif
