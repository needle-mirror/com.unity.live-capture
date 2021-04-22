using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace Unity.LiveCapture.VideoStreaming.Server
{
    /// <summary>
    /// The configuration options for <see cref="H264Encoder"/>.
    /// </summary>
    struct H264EncoderSettings : IEquatable<H264EncoderSettings>
    {
        /// <summary>
        /// The width in pixels of the output video.
        /// </summary>
        public int width;

        /// <summary>
        /// The height in pixels of the output video.
        /// </summary>
        public int height;

        /// <summary>
        /// The frame rate in Hz of the output video.
        /// </summary>
        public int frameRate;

        /// <summary>
        /// The target bit rate of the output video in kilobits per second, controlling the video quality.
        /// </summary>
        public int bitRate;

        /// <summary>
        /// The number of frames in each group of pictures, which consists of one keyframe (I-frame) followed
        /// by delta frames (P-frames and B-frames). Larger values will result in higher quality for a given
        /// bit rate, but the stream will take longer to recover after a dropped frame.
        /// </summary>
        public int gopSize;

        public bool Equals(H264EncoderSettings other)
        {
            return
                width == other.width &&
                height == other.height &&
                frameRate == other.frameRate &&
                bitRate == other.bitRate &&
                gopSize == other.gopSize;
        }

        public override bool Equals(object obj)
        {
            return obj is H264EncoderSettings other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = width;
                hashCode = (hashCode * 397) ^ height;
                hashCode = (hashCode * 397) ^ frameRate;
                hashCode = (hashCode * 397) ^ bitRate;
                hashCode = (hashCode * 397) ^ gopSize;
                return hashCode;
            }
        }

        public static bool operator==(H264EncoderSettings a, H264EncoderSettings b) => a.Equals(b);
        public static bool operator!=(H264EncoderSettings a, H264EncoderSettings b) => !a.Equals(b);
    }

    /// <summary>
    /// An encoder that can convert NV12 frames to H264 video.
    /// </summary>
    class H264Encoder : IDisposable
    {
        /// <summary>
        /// Used to temporarily pin a buffer with a using block.
        /// </summary>
        struct PinnedBufferScope : IDisposable
        {
            GCHandle m_Handle;

            public unsafe byte* pointer => (byte*)m_Handle.AddrOfPinnedObject();

            public PinnedBufferScope(ArraySegment<byte> buffer)
            {
                m_Handle = GCHandle.Alloc(buffer.Array, GCHandleType.Pinned);
            }

            public void Dispose()
            {
                if (m_Handle.IsAllocated)
                {
                    m_Handle.Free();
                    m_Handle = default;
                }
            }
        }

        H264EncoderSettings m_Settings;
        IntPtr m_Encoder;

        ~H264Encoder()
        {
            Dispose();
        }

        /// <summary>
        /// Destroys the native encoder instance.
        /// </summary>
        public void Dispose()
        {
            if (m_Encoder != IntPtr.Zero)
            {
#if UNITY_EDITOR_WIN
                H264EncoderPlugin.DestroyEncoder(m_Encoder);
#endif
                m_Encoder = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Configures a new native encoder instance.
        /// </summary>
        /// <param name="settings">The configuration of the encoder.</param>
        public void Setup(in H264EncoderSettings settings)
        {
            if (m_Settings != settings)
                Dispose();

            if (m_Encoder == IntPtr.Zero)
            {
                m_Settings = settings;

#if UNITY_EDITOR_WIN
                m_Encoder = H264EncoderPlugin.CreateEncoder(
                    (uint)settings.width,
                    (uint)settings.height,
                    (uint)settings.frameRate, 1,
                    (uint)settings.bitRate * 1000,
                    (uint)settings.gopSize);
#endif
            }
        }

        /// <summary>
        /// Encodes an image into the video stream.
        /// </summary>
        /// <param name="imageData">An image in NV12 format with a width and height matching those configured using <see cref="Setup"/>.</param>
        /// <param name="timeStamp">The time in nanoseconds the image was sampled at since the start of the video stream.</param>
        /// <param name="frame">Used to return the encoded image frame.</param>
        public void Encode(in NativeArray<byte> imageData, ulong timeStamp, H264EncodedFrame frame)
        {
            if (m_Encoder == IntPtr.Zero)
                throw new InvalidOperationException("Encoder is disposed and needs to be setup before encoding a frame.");

            var expectedSize = (m_Settings.width * m_Settings.height * 3) / 2;

            if (imageData.Length != expectedSize)
                throw new ArgumentException($"NV12 image buffer is {imageData.Length} bytes long, but the encoder expects {expectedSize} bytes.", nameof(imageData));

            var success = EncodeFrame(imageData, timeStamp, frame);

            if (!success)
                Debug.LogError($"Error encoding frame at t = {timeStamp / 1000000} ms");
        }

        unsafe bool EncodeFrame(in NativeArray<byte> imageData, ulong timeStamp, H264EncodedFrame frame)
        {
            Profiler.BeginSample("EncodeFrame");
            var success = H264EncoderPlugin.EncodeFrame(m_Encoder, (byte*)imageData.GetUnsafeReadOnlyPtr(), timeStamp);
            Profiler.EndSample();

            if (!success)
                return false;

            Profiler.BeginSample("BeginConsumeEncodedBuffer");
            success = H264EncoderPlugin.BeginConsumeEncodedBuffer(m_Encoder, out var bufferSize);
            Profiler.EndSample();

            if (!success)
                return false;

            frame.SetSize(ref frame.imageNalu, (int)bufferSize);
            bool isKeyFrame;

            using (var buffer = new PinnedBufferScope(frame.imageNalu))
            {
                Profiler.BeginSample("EndConsumeEncodedBuffer");
                success = H264EncoderPlugin.EndConsumeEncodedBuffer(m_Encoder, buffer.pointer, out var bufferTimeStampNs, out isKeyFrame);
                Profiler.EndSample();
            }

            if (!success)
                return false;

            if (isKeyFrame)
            {
                var sz = H264EncoderPlugin.GetSpsNAL(m_Encoder, (byte*)0);
                frame.SetSize(ref frame.spsNalu, (int)sz);
                using (var buffer = new PinnedBufferScope(frame.spsNalu))
                {
                    H264EncoderPlugin.GetSpsNAL(m_Encoder, buffer.pointer);
                }

                sz = H264EncoderPlugin.GetPpsNAL(m_Encoder, (byte*)0);
                frame.SetSize(ref frame.ppsNalu, (int)sz);
                using (var buffer = new PinnedBufferScope(frame.ppsNalu))
                {
                    H264EncoderPlugin.GetPpsNAL(m_Encoder, buffer.pointer);
                }
            }
            else
            {
                frame.SetSize(ref frame.spsNalu, 0);
                frame.SetSize(ref frame.ppsNalu, 0);
            }

            return true;
        }
    }
}
