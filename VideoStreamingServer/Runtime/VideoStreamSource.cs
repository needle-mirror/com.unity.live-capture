#if URP_10_2_OR_NEWER || HDRP_10_2_OR_NEWER
#define USING_SCRIPTABLE_RENDER_PIPELINE
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
#if URP_10_2_OR_NEWER
using UnityEngine.Rendering.Universal;
#endif
#if HDRP_10_2_OR_NEWER
using UnityEngine.Rendering.HighDefinition;

#endif

namespace Unity.LiveCapture.VideoStreaming.Server
{
    /// <summary>
    /// Consumes the output of a <see cref="VideoStreamSource"/>.
    /// </summary>
    interface IVideoStreamSink
    {
        /// <summary>
        /// Attempt to minimize the latency of retrieving rendered frames from the GPU at the cost of performance.
        /// </summary>
        bool ShouldPrioritizeLatency();

        /// <summary>
        /// Gets the target frame rate in Hz of the video stream.
        /// </summary>
        /// <returns>The frame rate.</returns>
        int GetFrameRate();

        /// <summary>
        /// Gets the target resolution of the video stream.
        /// </summary>
        /// <returns>The resolution in pixels.</returns>
        Vector2Int GetResolution();

        /// <summary>
        /// Called by the <see cref="VideoStreamSource"/> this sink has been registered to when a new frame
        /// is ready to consume.
        /// </summary>
        /// <param name="frame">A completed request containing the frame data. The frame's resolution may not
        /// exactly match that specified by <see cref="GetResolution"/> in order to better accommodate the encoder.</param>
        void ConsumeFrame(VideoFrameRequest frame);
    }

    /// <summary>
    /// Component that manages a camera in order generate a stream of <see cref="VideoFrameRequest"/> in NV12 format.
    /// Generated frames are consumed by one or many <see cref="IVideoStreamSink"/> instances registered with <see cref="RegisterSink"/>.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    class VideoStreamSource : MonoBehaviour
    {
        /// <summary>
        /// The encoded video resolution width and height are constrained to a multiple of this value.
        /// </summary>
        internal static readonly int k_BlockSize = 4;

        /// <summary>
        /// The smallest supported encoding resolution.
        /// </summary>
        internal static readonly Vector2Int k_MinEncoderResolution = new Vector2Int(64, 64);

        /// <summary>
        /// The largest supported encoding resolution. The official H.264 standard only guarantees support
        /// for resolutions up to 2048 x 2048, but in practice encoders support much higher resolutions.
        /// </summary>
        internal static readonly Vector2Int k_MaxEncoderResolution = new Vector2Int(4096, 2048);

        const string k_VerticalFlipKeyword = "VERTICAL_FLIP";
        static readonly int k_SrcScaleOffsetProperty = Shader.PropertyToID("_SrcTex_ScaleOffset");
        static readonly int k_DstTexelSizeProperty = Shader.PropertyToID("_DstTex_TexelSize");

        class SinkState
        {
            readonly IVideoStreamSink m_Sink;
            readonly Queue<VideoFrameRequest> m_FrameStream = new Queue<VideoFrameRequest>();
            float m_StartTime = 0f;
            int m_LastFrameIndex = -1;
            int m_LastFrameRate = 0;

            public SinkState(IVideoStreamSink sink)
            {
                m_Sink = sink;
            }

            /// <summary>
            /// Polls the asynchronous frame requests and updates the timing data.
            /// </summary>
            public void Update()
            {
                if (GetNextFrame(out var frame))
                    m_Sink.ConsumeFrame(frame);

                var frameRate = m_Sink.GetFrameRate();
                if (m_LastFrameRate != frameRate)
                {
                    m_LastFrameRate = frameRate;
                    m_LastFrameIndex = -1;
                    m_StartTime = Time.realtimeSinceStartup;
                }
            }

            /// <summary>
            /// Gets if the sink needs another frame of video yet.
            /// </summary>
            public bool NeedsNextFrame() => GetFrameIndex() > m_LastFrameIndex;

            /// <summary>
            /// Prepares to consume the frame data from the provided request.
            /// May complete synchronously, blocking execution, depending on the sink configuration.
            /// </summary>
            /// <param name="request">The request containing the next video frame for this sink.</param>
            /// <param name="width">The width of the video frame.</param>
            /// <param name="height">The height of the video frame.</param>
            public void EnqueueFrame(AsyncGPUReadbackRequest request, int width, int height)
            {
                var frame = new VideoFrameRequest(request, width, height, GetElapsedTime());

                // Using AsyncGPUReadback asynchronously introduces a few frames of latency,
                // so we optionally allow reading the result back synchronously.
                if (m_Sink.ShouldPrioritizeLatency())
                {
                    request.WaitForCompletion();

                    if (request.done && !request.hasError)
                        m_Sink.ConsumeFrame(frame);

                    while (m_FrameStream.Count > 0)
                        m_FrameStream.Dequeue();
                }
                else
                {
                    m_FrameStream.Enqueue(frame);

                    // in case the frames are not being consumed we should remove the oldest requests
                    while (m_FrameStream.Count > 6)
                        m_FrameStream.Dequeue();
                }

                m_LastFrameIndex = GetFrameIndex();
            }

            bool GetNextFrame(out VideoFrameRequest frame)
            {
                frame = default;
                var isFrameValid = false;

                while (m_FrameStream.Count > 0)
                {
                    var nextFrame = m_FrameStream.Peek();

                    if (nextFrame.hasError)
                    {
                        m_FrameStream.Dequeue();
                    }
                    else if (nextFrame.isDone)
                    {
                        frame = nextFrame;
                        isFrameValid = true;
                        m_FrameStream.Dequeue();
                    }
                    else
                    {
                        break;
                    }
                }

                return isFrameValid;
            }

            int GetFrameIndex() => (int)(GetElapsedTime() * m_Sink.GetFrameRate());

            float GetElapsedTime() => Time.realtimeSinceStartup - m_StartTime;
        }

        readonly Dictionary<IVideoStreamSink, SinkState> m_SinkStates = new Dictionary<IVideoStreamSink, SinkState>();

        [SerializeField]
        bool m_Crop;

        Camera m_Camera;
        RenderTexture m_CaptureTarget;
        Material m_RgbToNV12Material;
        CommandBuffer m_LegacyCaptureCommandBuffer;
        bool m_AddedLegacyCommandBuffer;
        bool m_UsingLegacyRenderPipeline;

        // May seen redundant but warranted by our lifecycle management,
        // we automatically deactivate on Awake, leading to OnDisable being called before OnEnable has executed.
        bool m_Initialized;

        /// <summary>
        /// Connect this source to a new output.
        /// </summary>
        /// <param name="sink">The sink to output to.</param>
        public void RegisterSink(IVideoStreamSink sink)
        {
            if (sink != null && !m_SinkStates.ContainsKey(sink))
            {
                m_SinkStates.Add(sink, new SinkState(sink));
                enabled = true;
            }
        }

        /// <summary>
        /// Disconnects this source from an output.
        /// </summary>
        /// <param name="sink">The sink to disconnect.</param>
        public void DeregisterSink(IVideoStreamSink sink)
        {
            if (sink != null && m_SinkStates.Remove(sink) && m_SinkStates.Count == 0)
                enabled = false;
        }

        void Awake()
        {
            m_Camera = GetComponent<Camera>();

            hideFlags = HideFlags.DontSave;
            enabled = false;
        }

        void OnEnable()
        {
            m_UsingLegacyRenderPipeline = true;
            m_RgbToNV12Material = new Material(Shader.Find("Video/RGBToNV12"))
            {
                hideFlags = HideFlags.HideAndDontSave
            };

#if USING_SCRIPTABLE_RENDER_PIPELINE
            m_UsingLegacyRenderPipeline = false;
            CameraCaptureBridge.enabled = true;
            CameraCaptureBridge.AddCaptureAction(m_Camera, Capture);
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
#endif

            // Add capture command buffer for the legacy render pipeline.
            // We DO NOT support a camera swap on runtime.
            if (m_UsingLegacyRenderPipeline)
            {
                m_LegacyCaptureCommandBuffer = new CommandBuffer();
                m_Camera.AddCommandBuffer(CameraEvent.AfterEverything, m_LegacyCaptureCommandBuffer);
                m_AddedLegacyCommandBuffer = true;

                m_RgbToNV12Material.EnableKeyword(k_VerticalFlipKeyword);
            }

            m_Initialized = true;
        }

        void OnDisable()
        {
            if (!m_Initialized)
            {
                return;
            }

            if (m_AddedLegacyCommandBuffer)
            {
                m_Camera.RemoveCommandBuffer(CameraEvent.AfterEverything, m_LegacyCaptureCommandBuffer);
                m_LegacyCaptureCommandBuffer.Release();
                m_AddedLegacyCommandBuffer = false;
            }

#if USING_SCRIPTABLE_RENDER_PIPELINE
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;

            // We don't disable the capture bridge since we have no way to know if something else is using it.
            CameraCaptureBridge.RemoveCaptureAction(m_Camera, Capture);
#endif

            if (Application.isPlaying)
                Destroy(m_RgbToNV12Material);
            else
                DestroyImmediate(m_RgbToNV12Material);

            Destroy(ref m_CaptureTarget);

            m_Initialized = false;
        }

        void Capture(RenderTargetIdentifier source, CommandBuffer cmd)
        {
            cmd.Blit(source, m_CaptureTarget);
        }

        void Update()
        {
            var reallocatedTarget = ResizeTexture(ref m_CaptureTarget, m_Camera.pixelWidth, m_Camera.pixelHeight);

            if (m_UsingLegacyRenderPipeline && reallocatedTarget)
            {
                m_LegacyCaptureCommandBuffer.Clear();
                m_LegacyCaptureCommandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, m_CaptureTarget);
            }

            foreach (var sinkState in m_SinkStates)
                sinkState.Value.Update();
        }

#if USING_SCRIPTABLE_RENDER_PIPELINE
        void OnEndCameraRendering(ScriptableRenderContext context, Camera currentCamera)
        {
            if (currentCamera == m_Camera && m_CaptureTarget != null)
            {
                CaptureVideoFrames();
            }
        }

#endif

        void OnPostRender()
        {
            if (m_CaptureTarget != null)
            {
                CaptureVideoFrames();
            }
        }

        void CaptureVideoFrames()
        {
            Profiler.BeginSample($"{nameof(VideoStreamSource)}.{nameof(CaptureVideoFrames)}");

            var captureResolution = new Vector2Int(m_CaptureTarget.width, m_CaptureTarget.height);

            foreach (var sinkState in m_SinkStates)
            {
                var sink = sinkState.Key;
                var state = sinkState.Value;

                if (!state.NeedsNextFrame())
                    continue;

                var resolution = CalculateEncoderResolution(sink.GetResolution());
                var width = resolution.x;
                var height = resolution.y;

                // The NV12 texture needs extra height scaling for the chroma sub-sampling (See the shader for details).
                var nv12Texture = RenderTexture.GetTemporary(width, height * 3 / 2, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);

                ComputeCopyTransformation(captureResolution, resolution, m_Crop, out var scale, out var offset);

                m_RgbToNV12Material.SetVector(k_SrcScaleOffsetProperty, new Vector4(scale.x, scale.y, offset.x, offset.y));
                m_RgbToNV12Material.SetVector(k_DstTexelSizeProperty, new Vector4(1f / width, 1f / height, width, height));

                Graphics.Blit(m_CaptureTarget, nv12Texture, m_RgbToNV12Material);

                var request = AsyncGPUReadback.Request(nv12Texture);
                state.EnqueueFrame(request, width, height);

                RenderTexture.ReleaseTemporary(nv12Texture);
            }

            Profiler.EndSample();
        }

        /// <summary>
        /// Computes the scaling and offset transformation needed to copy a texture to another
        /// while preserving the aspect ratio of the source texture.
        /// </summary>
        /// <param name="srcRes">Resolution of the source texture.</param>
        /// <param name="dstRes">Resolution of the destination texture.</param>
        /// <param name="crop">We crop the source texture if true, otherwise use letterboxing.</param>
        /// <param name="scale">The scaling transformation to apply.</param>
        /// <param name="offset">The translation transformation to apply.</param>
        public static void ComputeCopyTransformation(Vector2 srcRes, Vector2 dstRes, bool crop, out Vector2 scale, out Vector2 offset)
        {
            var srcAspect = srcRes.x / srcRes.y;
            var dstAspect = dstRes.x / dstRes.y;

            var dstRegionSize = (srcAspect > dstAspect) ^ crop ? new Vector2(dstRes.x, dstRes.x / srcAspect) : new Vector2(dstRes.y * srcAspect, dstRes.y);

            var dstRegionOffset = (dstRes - dstRegionSize) * 0.5f;
            var dstRegion = new Rect(dstRegionOffset, dstRegionSize);

            scale = dstRes / dstRegion.size;
            offset = -dstRegion.position / dstRegion.size;
        }

        static bool ResizeTexture(ref RenderTexture texture, int width, int height)
        {
            if (texture != null && (texture.width != width || texture.height != height))
            {
                Destroy(ref texture);
            }

            if (texture == null)
            {
                texture = new RenderTexture(width, height, 32, RenderTextureFormat.ARGB32);
                texture.name = "Camera Capture";
                texture.hideFlags = HideFlags.HideAndDontSave;
                return true;
            }

            return false;
        }

        static void Destroy(ref RenderTexture renderTexture)
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
                renderTexture = null;
            }
        }

        /// <summary>
        /// Transforms a video resolution to one that is valid for the encoder.
        /// </summary>
        /// <param name="resolution">The resolution to constrain.</param>
        /// <returns>The resolution of the encoded video.</returns>
        internal static Vector2Int CalculateEncoderResolution(Vector2Int resolution)
        {
            var blockCount = new Vector2Int(
                Mathf.FloorToInt(resolution.x / (float)k_BlockSize),
                Mathf.FloorToInt(resolution.y / (float)k_BlockSize)
            );

            return Vector2Int.Min(Vector2Int.Max(blockCount * k_BlockSize, k_MinEncoderResolution), k_MaxEncoderResolution);
        }
    }
}
