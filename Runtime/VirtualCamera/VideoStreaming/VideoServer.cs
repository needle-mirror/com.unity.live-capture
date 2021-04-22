using System;
using Unity.LiveCapture.VideoStreaming.Server;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// A server which receives frames from a camera and encodes them into a networked video stream.
    /// </summary>
    [Serializable]
    class VideoServer : IVideoStreamSink
    {
        /// <summary>
        /// Checks if video streaming is supported on the current platform.
        /// </summary>
        /// <returns>
        /// true if supported; otherwise, false.
        /// </returns>
        public static bool IsSupported() => VideoStreamingServer.IsSupported();

        Camera m_Camera;
        Camera m_ActiveCamera;
        VideoStreamingServer m_VideoStreamingServer;

        /// <summary>
        /// The target resolution of the video stream.
        /// </summary>
        /// <remarks>
        /// The actual stream resolution may be different depending on the stream settings or the camera rendering resolution.
        /// </remarks>
        public Vector2Int baseResolution { get; set; } = new Vector2Int(1280, 720);

        /// <summary>
        /// The camera this server will capture video frames from.
        /// </summary>
        public Camera camera
        {
            get => m_Camera;
            set => m_Camera = value;
        }

        /// <summary>
        /// Is the server is currently running.
        /// </summary>
        public bool isRunning => m_VideoStreamingServer != null && m_VideoStreamingServer.isRunning;

        /// <summary>
        /// The port the server is running on.
        /// </summary>
        public int port => isRunning ? m_VideoStreamingServer.port : 0;

        /// <summary>
        /// Updates the server.
        /// </summary>
        public void Update()
        {
            if (isRunning)
            {
                if (m_Camera != m_ActiveCamera)
                {
                    SetActiveCamera(m_Camera);
                }
            }
        }

        /// <summary>
        /// Starts the video server, allowing clients to connect.
        /// </summary>
        public void StartServer()
        {
            if (VideoStreamingServer.IsSupported() && m_VideoStreamingServer == null)
            {
                m_VideoStreamingServer = new VideoStreamingServer();
            }

            if (m_VideoStreamingServer != null && !m_VideoStreamingServer.isRunning)
            {
                try
                {
                    m_VideoStreamingServer.Start();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// Stops the video server.
        /// </summary>
        public void StopServer()
        {
            if (m_VideoStreamingServer != null && m_VideoStreamingServer.isRunning)
            {
                try
                {
                    m_VideoStreamingServer.Stop();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                SetActiveCamera(null);

                m_VideoStreamingServer.Dispose();
                m_VideoStreamingServer = null;
            }
        }

        /// <summary>
        /// Gets the target bit-rate in kilobits per second.
        /// </summary>
        /// <returns>
        /// The target bit-rate in kilobits per second.
        /// </returns>
        public int GetBitRate()
        {
            // A typical video stream uses about 0.14 bits per pixel, but since we limit compression features
            // and GOP size we need a larger bitrate to achieve a similar quality image.
            const float baseBitsPerPixel = 0.28f;

            var rez = GetResolution();

            return Mathf.CeilToInt(baseBitsPerPixel * rez.x * rez.y * VideoServerSettings.instance.frameRate *
                Mathf.Lerp(0.1f, 1.5f, VideoServerSettings.instance.quality / 100f) / 1000f);
        }

        /// <inheritdoc />
        public bool ShouldPrioritizeLatency()
        {
            return VideoServerSettings.instance.prioritizeLatency;
        }

        /// <inheritdoc/>
        public int GetFrameRate()
        {
            return VideoServerSettings.instance.frameRate;
        }

        /// <inheritdoc/>
        public Vector2Int GetResolution()
        {
            var res = (Vector2)baseResolution;
            return Vector2Int.RoundToInt(res * VideoServerSettings.instance.resolutionScale);
        }

        /// <inheritdoc/>
        public void ConsumeFrame(VideoFrameRequest frame)
        {
            if (m_VideoStreamingServer != null)
                m_VideoStreamingServer.EnqueueFrame(frame, GetFrameRate(), GetBitRate());
        }

        void SetActiveCamera(Camera camera)
        {
            if (m_ActiveCamera != camera)
            {
                UnregisterSink();
            }

            m_ActiveCamera = camera;
            RegisterSink();
        }

        void RegisterSink()
        {
            // Only add video stream sources if the server is supported, otherwise
            // there will be added overhead for no reason.
            if (m_ActiveCamera != null && IsSupported())
            {
                if (!m_ActiveCamera.TryGetComponent<VideoStreamSource>(out var newSource))
                    newSource = m_ActiveCamera.gameObject.AddComponent<VideoStreamSource>();

                newSource.RegisterSink(this);
            }
        }

        void UnregisterSink()
        {
            if (m_ActiveCamera != null && m_ActiveCamera.TryGetComponent<VideoStreamSource>(out var oldSource))
            {
                oldSource.DeregisterSink(this);
            }
        }
    }
}
