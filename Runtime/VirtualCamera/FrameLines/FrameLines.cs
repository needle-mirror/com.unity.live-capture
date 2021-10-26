using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
#if HDRP_10_2_OR_NEWER
using UnityEngine.Rendering.HighDefinition;

#endif

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// Map between Camera and FrameLines components.
    /// </summary>
    class FrameLinesMap : ComponentMap<Camera, FrameLines> {}

    /// <summary>
    /// A Component that displays frame lines which helps visualize the gate crop of the sensor
    /// and the aspect ratio crop of the target screen.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Live Capture/Virtual Camera/Frame Lines")]
    [RequireComponent(typeof(Camera))]
    [HelpURL(Documentation.baseURL + "ref-component-frame-lines" + Documentation.endURL)]
    public class FrameLines : MonoBehaviour
    {
        // Name used for profiling.
        internal const string k_ProfilingSamplerLabel = "Frame Lines";

        [SerializeField]
        FrameLinesSettings m_Settings;

        Camera m_Camera;
        bool m_GeometryIsValid;
        readonly FrameLinesDrawer m_FrameLinesDrawer = new FrameLinesDrawer();

        // Legacy render pipeline support.
        CommandBuffer m_LegacyCommandBuffer;
        bool m_AddedLegacyCommandBuffer;
        bool m_UsingLegacyRenderPipeline;
        bool m_LegacyCachedShouldRender;
        Vector2 m_CameraPixelSize;

        // Caching so that we can update the geometry when needed.
        FrameLinesSettings m_CachedSettings;
        float m_CachedScreenAspect;
        float m_CachedGateAspect;

#if HDRP_10_2_OR_NEWER
        CustomPassManager.Handle<HdrpFrameLinesPass> m_CustomPassHandle;
#endif
        /// <summary>
        /// The gate fit mode.
        /// </summary>
        public GateFit GateFit
        {
            get => m_Settings.GateFit;
            set => m_Settings.GateFit = value;
        }

        /// <summary>
        /// Whether or not to show the crop aspect ratio.
        /// </summary>
        public bool GateMaskEnabled
        {
            get => m_Settings.GateMaskEnabled;
            set => m_Settings.GateMaskEnabled = value;
        }

        /// <summary>
        /// Whether or not to show the crop aspect ratio.
        /// </summary>
        public bool AspectRatioEnabled
        {
            get => m_Settings.AspectRatioLinesEnabled;
            set => m_Settings.AspectRatioLinesEnabled = value;
        }

        /// <summary>
        /// Whether or not to show the center marker.
        /// </summary>
        public bool CenterMarkerEnabled
        {
            get => m_Settings.CenterMarkerEnabled;
            set => m_Settings.CenterMarkerEnabled = value;
        }

        /// <summary>
        /// The aspect ratio of the crop.
        /// </summary>
        public float CropAspect
        {
            get => m_Settings.AspectRatio;
            set => m_Settings.AspectRatio = value;
        }

        /// <summary>
        ///  Checks whether the frame lines should be rendered.
        /// </summary>
        /// <returns>True if the frame lines should render</returns>
        internal bool ShouldRender() => isActiveAndEnabled && m_GeometryIsValid;

        void Awake()
        {
            m_UsingLegacyRenderPipeline = GraphicsSettings.renderPipelineAsset == null;
            m_Camera = GetComponent<Camera>();
            Validate();
        }

        void Reset()
        {
            m_Settings = FrameLinesSettings.GetDefault();
        }

        void OnValidate()
        {
            m_Settings.Validate();
            Validate();
        }

        void OnEnable()
        {
            m_FrameLinesDrawer.Initialize();

            m_CachedSettings = m_Settings;

            // Force update
            m_CachedSettings.AspectFillOpacity = (m_CachedSettings.AspectFillOpacity + .5f) % 1f;

            if (m_UsingLegacyRenderPipeline)
            {
                m_LegacyCommandBuffer = new CommandBuffer { name = k_ProfilingSamplerLabel };

                AddLegacyCommandBufferIfNeeded();

                m_LegacyCachedShouldRender = ShouldRender();
                if (m_LegacyCachedShouldRender)
                {
                    Render(m_LegacyCommandBuffer);
                }
            }

#if HDRP_10_2_OR_NEWER
            m_CustomPassHandle = new CustomPassManager.Handle<HdrpFrameLinesPass>(CustomPassInjectionPoint.AfterPostProcess);
            m_CustomPassHandle.GetPass().name = k_ProfilingSamplerLabel;
#endif

            Debug.Assert(m_Camera != null);
        }

        void OnDisable()
        {
#if HDRP_10_2_OR_NEWER
            m_CustomPassHandle.Dispose();
#endif

            if (m_UsingLegacyRenderPipeline)
            {
                RemoveLegacyCommandBufferIfNeeded(true);
                m_LegacyCommandBuffer.Dispose();
                m_LegacyCommandBuffer = null;
            }

            m_FrameLinesDrawer.Dispose();
        }

        void OnDestroy()
        {
            FrameLinesMap.Instance.RemoveInstance(this);
        }

        void Validate()
        {
            FrameLinesMap.Instance.RemoveInstance(this);

            if (m_Camera != null)
            {
                FrameLinesMap.Instance.AddUniqueInstance(m_Camera, this);
            }
        }

        void AddLegacyCommandBufferIfNeeded()
        {
            Assert.IsFalse(m_AddedLegacyCommandBuffer);

            if (m_UsingLegacyRenderPipeline && m_Camera != null && m_LegacyCommandBuffer != null)
            {
                m_Camera.AddCommandBuffer(CameraEvent.AfterEverything, m_LegacyCommandBuffer);
                m_AddedLegacyCommandBuffer = true;
            }
        }

        void RemoveLegacyCommandBufferIfNeeded(bool disposing)
        {
            if (m_AddedLegacyCommandBuffer)
            {
                Assert.IsNotNull(m_LegacyCommandBuffer, $"{nameof(m_LegacyCommandBuffer)} disposed before being removed from the camera.");

                if (m_Camera == null)
                {
                    if (disposing)
                    {
                        m_AddedLegacyCommandBuffer = false;
                        return;
                    }

                    throw new InvalidOperationException($"{nameof(m_Camera)} disposed before command buffer was removed.");
                }

                m_Camera.RemoveCommandBuffer(CameraEvent.AfterEverything, m_LegacyCommandBuffer);
                m_AddedLegacyCommandBuffer = false;
            }
        }

        void LateUpdate()
        {
            UpdateCamera();
        }

        void UpdateCamera()
        {
            m_Camera.gateFit = m_Settings.GateFit == GateFit.Overscan ? Camera.GateFitMode.Overscan : Camera.GateFitMode.Fill;

            m_CameraPixelSize = new Vector2(m_Camera.pixelWidth, m_Camera.pixelHeight);

            var screenAspect = m_Camera.pixelWidth / (float)m_Camera.pixelHeight;
            var sensorSize = m_Camera.sensorSize;
            var gateAspect = sensorSize.x / sensorSize.y;

            m_GeometryIsValid = screenAspect > float.Epsilon && gateAspect > float.Epsilon;
            var geometryChanged = false;

            if (m_GeometryIsValid)
            {
                geometryChanged = UpdateGeometryIfNeeded(gateAspect, screenAspect);
            }

            if (m_UsingLegacyRenderPipeline)
            {
                var shouldRender = ShouldRender();
                if (shouldRender != m_LegacyCachedShouldRender || geometryChanged)
                {
                    m_LegacyCachedShouldRender = shouldRender;
                    m_LegacyCommandBuffer.Clear();
                    if (m_LegacyCachedShouldRender)
                    {
                        Render(m_LegacyCommandBuffer);
                    }
                }
            }
        }

        /// <summary>
        /// Draw active frame lines.
        /// </summary>
        /// <param name="cmd">The command buffer to append frame lines drawing commands to.</param>
        internal void Render(CommandBuffer cmd)
        {
            Assert.IsTrue(m_GeometryIsValid, $"{nameof(Render)} should not be called when geometry is not valid.");

            // Geometry is submitted in pixel space.
            var projection = Matrix4x4.Ortho(0, m_CameraPixelSize.x, 0, m_CameraPixelSize.y, 1, -100);

            cmd.SetViewProjectionMatrices(Matrix4x4.identity, projection);

            m_FrameLinesDrawer.Render(cmd);
        }

        bool UpdateGeometryIfNeeded(float gateAspect, float screenAspect)
        {
            var geometryNeedsUpdate =
                !Mathf.Approximately(gateAspect, m_CachedGateAspect) ||
                !Mathf.Approximately(screenAspect, m_CachedScreenAspect) ||
                m_CachedSettings != m_Settings;

            if (!geometryNeedsUpdate)
            {
                return false;
            }

            m_CachedGateAspect = gateAspect;
            m_CachedScreenAspect = screenAspect;
            m_CachedSettings = m_Settings;

            m_FrameLinesDrawer.Clear();

            // We start with a (1, 1) view size, we'll shrink it down according to gate and crop.
            // Coordinates are computed in NDC space.
            Vector2 gateViewSize;
            Vector2 cropViewSize;

            // The sensor gate mask is only visible using overscan.
            if (m_Settings.GateFit == GateFit.Overscan)
            {
                gateViewSize = ApplyAspectRatio(Vector2.one, screenAspect, gateAspect);
                // The crop mask is evaluated within the sensor gate.
                cropViewSize = ApplyAspectRatio(gateViewSize, screenAspect * gateViewSize.x / gateViewSize.y, m_Settings.AspectRatio);

                // In case the sensor gate mask is disabled but the crop mask is visible, render the sensor gate mask with the crop mask's opacity,
                // which amounts to having the crop mask cover the sensor gate mask area so that is reaches the edges of the screen.
                var showGateMask = m_Settings.GateMaskEnabled && m_Settings.GateMaskOpacity > 0f;
                var showCropFill = m_Settings.AspectRatioLinesEnabled && m_Settings.AspectFillOpacity > 0f;
                if (showGateMask || showCropFill)
                {
                    var opacity = showGateMask ? m_Settings.GateMaskOpacity : m_Settings.AspectFillOpacity;
                    // Gate mask letterbox.
                    m_FrameLinesDrawer.SetColor(new Color(0, 0, 0, opacity));

                    DrawLetterBox(Vector2.one, gateViewSize);
                }
            }
            else
            {
                // The sensor gate is ignored while evaluating the crop mask.
                gateViewSize = Vector2.one;
                cropViewSize = ApplyAspectRatio(Vector2.one, screenAspect, m_Settings.AspectRatio);
            }

            if (m_Settings.AspectRatioLinesEnabled)
            {
                // Crop mask letterbox.
                m_FrameLinesDrawer.SetColor(new Color(0, 0, 0, m_Settings.AspectFillOpacity));

                DrawLetterBox(gateViewSize, cropViewSize);

                // Also used for the marker.
                m_FrameLinesDrawer.SetColor(m_Settings.AspectLineColor);
                m_FrameLinesDrawer.SetLineWidth(m_Settings.AspectLineWidth);

                // Crop mask lines.
                if (m_Settings.AspectLineType != FrameLinesSettings.LineType.None)
                {
                    if (m_Settings.AspectLineType == FrameLinesSettings.LineType.Box)
                    {
                        var cropRect = Rect.MinMaxRect(-cropViewSize.x, -cropViewSize.y, cropViewSize.x, cropViewSize.y);

                        m_FrameLinesDrawer.DrawBox(NdcToPixels(cropRect));
                    }
                    else if (m_Settings.AspectLineType == FrameLinesSettings.LineType.Corner)
                    {
                        var cropRect = Rect.MinMaxRect(-cropViewSize.x, -cropViewSize.y, cropViewSize.x, cropViewSize.y);
                        var pixelBox = NdcToPixels(cropRect);
                        var extent = new Vector2(pixelBox.width * .1f, pixelBox.height * .1f);

                        m_FrameLinesDrawer.DrawCornerBox(pixelBox, extent);
                    }
                }
            }

            if (m_Settings.CenterMarkerEnabled)
            {
                m_FrameLinesDrawer.SetColor(m_Settings.AspectLineColor);
                m_FrameLinesDrawer.SetLineWidth(m_Settings.AspectLineWidth);

                // Marker.
                if (m_Settings.CenterMarkerType == FrameLinesSettings.MarkerType.Cross)
                {
                    var center = m_CameraPixelSize * .5f;
                    var inner = Mathf.Min(m_CameraPixelSize.x * .02f, m_CameraPixelSize.y * .02f);
                    var outer = Mathf.Min(m_CameraPixelSize.x * .06f, m_CameraPixelSize.y * .06f);
                    m_FrameLinesDrawer.DrawCross(center, inner, outer);
                }
                else if (m_Settings.CenterMarkerType == FrameLinesSettings.MarkerType.Dot)
                {
                    var center = m_CameraPixelSize * .5f;
                    var extent = Mathf.Max(2, Mathf.Min(m_CameraPixelSize.x * .02f, m_CameraPixelSize.y * .02f));

                    m_FrameLinesDrawer.DrawRect(Rect.MinMaxRect(
                        center.x - extent,
                        center.y - extent,
                        center.x + extent,
                        center.y + extent));
                }
            }

            m_FrameLinesDrawer.UpdateGeometry();

            return true;
        }

        void DrawLetterBox(Vector2 outerSize, Vector2 innerSize)
        {
            var screenAspect = outerSize.x / outerSize.y;
            var gateAspect = innerSize.x / innerSize.y;

            if (screenAspect > gateAspect) // Pillar box
            {
                var dx = outerSize.x - innerSize.x;

                m_FrameLinesDrawer.DrawRect(NdcToPixels(new Rect(-outerSize.x, -outerSize.y, dx, outerSize.y * 2))); // left

                m_FrameLinesDrawer.DrawRect(NdcToPixels(new Rect(innerSize.x, -outerSize.y, dx, outerSize.y * 2))); // right
            }
            else
            {
                var dy = outerSize.y - innerSize.y;

                m_FrameLinesDrawer.DrawRect(NdcToPixels(new Rect(-outerSize.x, -outerSize.y, outerSize.x * 2, dy))); // top

                m_FrameLinesDrawer.DrawRect(NdcToPixels(new Rect(-outerSize.x, innerSize.y, outerSize.x * 2, dy))); // bottom
            }
        }

        Rect NdcToPixels(Rect value)
        {
            var pxMin = (value.min + Vector2.one) * .5f * m_CameraPixelSize;
            var pxMax = (value.max + Vector2.one) * .5f * m_CameraPixelSize;

            return Rect.MinMaxRect(pxMin.x, pxMin.y, pxMax.x, pxMax.y);
        }

        static Vector2 ApplyAspectRatio(Vector2 size, float outerAspect, float innerAspect)
        {
            if (innerAspect < float.Epsilon || outerAspect < float.Epsilon)
            {
                return size;
            }

            if (outerAspect > innerAspect) // Vertical
            {
                size.x *= innerAspect / outerAspect;
            }
            else // Horizontal
            {
                size.y *= outerAspect / innerAspect;
            }

            return size;
        }
    }
}
