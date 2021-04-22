using System;
using System.Collections.Concurrent;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.LiveCapture.VideoStreaming.Server
{
    /// <summary>
    /// A server that takes images and encodes them to a video stream transported to clients via RTSP.
    /// </summary>
    class VideoStreamingServer : IDisposable
    {
        /// <summary>
        /// Checks if the video streaming server is supported on the current platform.
        /// </summary>
        /// <returns>True if supported; false otherwise.</returns>
        public static bool IsSupported()
        {
#if UNITY_EDITOR_WIN
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// The maximum number of frames which can be waiting to be encoded on the
        /// encoder thread. Larger vales prioritize stream continuity over latency
        /// when encoding becomes a bottleneck.
        /// </summary>
        const int k_MaxBufferedFrameCount = 3;

        // we want a low gop size to recover quickly if packets are dropped
        const int k_GopSize = 2;

        struct BufferedFrame
        {
            public H264EncoderSettings settings;
            public NativeArray<byte> data;
            public ulong timeStamp;
        }

        H264Encoder m_Encoder = new H264Encoder();
        bool m_Disposed;

        Thread m_Thread;
        RtspServer m_Server;
        BlockingCollection<BufferedFrame> m_BufferedFrames;

        /// <summary>
        /// Gets if the server is currently running.
        /// </summary>
        public bool isRunning { get; private set; }

        /// <summary>
        /// The port the server is running on.
        /// </summary>
        public int port => isRunning ? m_Server.port : 0;

        /// <summary>
        /// Creates a new <see cref="VideoStreamingServer"/> instance.
        /// </summary>
        public VideoStreamingServer()
        {
            if (!IsSupported())
                throw new PlatformNotSupportedException($"{nameof(VideoStreamingServer)} is not supported on the current platform!");
        }

        /// <summary>
        /// The finalizer which disposes the server if <see cref="Dispose"/> is not called on the instance.
        /// </summary>
        ~VideoStreamingServer()
        {
            Dispose(false);
        }

        /// <summary>
        /// Starts the video server.
        /// </summary>
        /// <param name="port">The TCP port to listen for connecting clients on. If 0 is provided, the server lets
        /// the OS pick a free port.</param>
        public void Start(int port = 0)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(VideoStreamingServer));

            Debug.Assert(!isRunning);

            m_BufferedFrames = new BlockingCollection<BufferedFrame>();

            try
            {
                m_Server = new RtspServer(port, null, null);
                m_Server.StartListen();

                isRunning = true;

                m_Thread = new Thread(ServerLoop);
                m_Thread.Start();
            }
            catch (Exception)
            {
                DisposeServer();
                throw;
            }
        }

        /// <summary>
        /// Shuts down the video server.
        /// </summary>
        public void Stop()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(VideoStreamingServer));

            DisposeServer();
        }

        /// <summary>
        /// Releases the resources held by this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            DisposeServer();
            m_Encoder.Dispose();

            m_Disposed = true;
        }

        void DisposeServer()
        {
            isRunning = false;
            m_BufferedFrames.CompleteAdding();

            m_Thread?.Join();
            m_Thread = null;

            m_Server?.Dispose();
            m_Server = null;

            while (m_BufferedFrames.TryTake(out var frame))
                frame.data.Dispose();
        }

        /// <summary>
        /// Enqueue a frame for encoding into the stream.
        /// </summary>
        /// <param name="frame">The frame to encode.</param>
        /// <param name="frameRate">The frame rate in Hz of the video stream.</param>
        /// <param name="bitRate">The target bit rate of the video stream in kilobits per second.</param>
        public void EnqueueFrame(VideoFrameRequest frame, int frameRate, int bitRate)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(VideoStreamingServer));

            if (!isRunning)
                return;

            m_BufferedFrames.Add(new BufferedFrame
            {
                settings = new H264EncoderSettings
                {
                    width = frame.width,
                    height = frame.height,
                    frameRate = frameRate,
                    bitRate = bitRate,
                    gopSize = k_GopSize,
                },

                // We need to copy the frame data, since the request data could be cleared if the frame ends
                // before the encoder finishes using the data.
                data = new NativeArray<byte>(frame.GetData(), Allocator.TempJob),

                timeStamp = (ulong)(frame.elapsedTime * 1000000000),
            });

            // This is required so that if the encoding is slower than the rate at which
            // frames are generated the queue does not grow infinitely and the stream become
            // increasingly delayed.
            while (m_BufferedFrames.Count > k_MaxBufferedFrameCount)
            {
                if (m_BufferedFrames.TryTake(out var f))
                    f.data.Dispose();
            }
        }

        void ServerLoop()
        {
            Profiler.BeginThreadProfiling("Video Streaming Servers", $"Server Port: {port}");

            try
            {
                var encodedFrame = new H264EncodedFrame();

                while (!m_BufferedFrames.IsCompleted)
                {
                    if (m_BufferedFrames.TryTake(out var frame, Timeout.Infinite))
                    {
                        if (m_Server.RefreshConnectionList())
                        {
                            Profiler.BeginSample($"Encode Frame");
                            m_Encoder.Setup(frame.settings);
                            m_Encoder.Encode(frame.data, frame.timeStamp, encodedFrame);
                            Profiler.EndSample();

                            Profiler.BeginSample($"Send NALUs");
                            m_Server.SendNALUs(frame.timeStamp, encodedFrame.spsNalu, encodedFrame.ppsNalu, encodedFrame.imageNalu);
                            Profiler.EndSample();
                        }

                        frame.data.Dispose();
                    }
                }
            }
            catch (ThreadAbortException)
            {
                // Thrown then the thead is stopped
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                Profiler.EndThreadProfiling();
            }
        }
    }
}
