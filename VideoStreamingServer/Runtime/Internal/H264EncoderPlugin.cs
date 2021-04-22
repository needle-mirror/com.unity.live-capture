using System;
using System.Runtime.InteropServices;

namespace Unity.LiveCapture.VideoStreaming.Server
{
    struct H264EncoderPlugin
    {
        [DllImport("H264Encoder", EntryPoint = "Create")]
        extern public static IntPtr CreateEncoder(uint width, uint height, uint frameRateNumerator, uint frameRateDenominator, uint averageBitRate, uint gopSize);

        [DllImport("H264Encoder", EntryPoint = "Destroy")]
        [return : MarshalAs(UnmanagedType.U1)]
        extern public static bool DestroyEncoder(IntPtr encoder);

        [DllImport("H264Encoder", EntryPoint = "Encode")]
        [return : MarshalAs(UnmanagedType.U1)]
        extern public unsafe static bool EncodeFrame(IntPtr encoder, byte* pixelData, ulong timeStampNs);

        [DllImport("H264Encoder", EntryPoint = "BeginConsume")]
        [return : MarshalAs(UnmanagedType.U1)]
        extern public static bool BeginConsumeEncodedBuffer(IntPtr encoder, out uint sizeOut);

        [DllImport("H264Encoder", EntryPoint = "EndConsume")]
        [return : MarshalAs(UnmanagedType.U1)]
        extern public unsafe static bool EndConsumeEncodedBuffer(
            IntPtr encoder,
            byte* dst,
            out ulong timeStampNs,
            [MarshalAs(UnmanagedType.U1)] out bool isKeyFrame);

        [DllImport("H264Encoder", EntryPoint = "GetSps")]
        extern public unsafe static uint GetSpsNAL(IntPtr encoder, byte* spsData);

        [DllImport("H264Encoder", EntryPoint = "GetPps")]
        extern public unsafe static uint GetPpsNAL(IntPtr encoder, byte* ppsData);
    }
}
