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
    /// Map between Camera and FilmFormat components.
    /// </summary>
    class FilmFormatMap : ComponentMap<Camera, FilmFormat> {}

    /// <summary>
    /// A Component that displays a mask which helps visualize the gate crop of the sensor
    /// and the aspect ratio crop of the target screen.
    /// </summary>
    /// <remarks>
    /// The mask is rendered as a geometry of 4 quads.
    /// FilmFormat instances are managed via a static API,
    /// render pipeline code tries to fetch a FilmFormat instance based on the rendering camera.
    /// </remarks>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Live Capture/Virtual Camera/Film Format")]
    public class FilmFormat : MonoBehaviour
    {
        // Name used for profiling.
        internal const string k_ProfilingSamplerLabel = "Film Format";

        // Minimal aspect ratio for crop.
        const float k_MinAspectRatio = 0.01f;

        // Color property for the solid color material.
        static readonly int k_MainColorProp = Shader.PropertyToID("_Color");

        // Mesh vertices container.
        static readonly Vector3[] k_Vertices = new Vector3[16];

        // Used temporarily when updating geometry.
        static readonly Vector4[] k_Rects = new Vector4[4];

        // Mesh indices, we're using quads so vertices simply are drawn in order.
        static readonly int[] k_Indices = new[]
        {
            0, 1, 2, 3,
            4, 5, 6, 7,
            8, 9, 10, 11,
            12, 13, 14, 15
        };

        [SerializeField, Tooltip("The camera to use to render the film format mask.")]
        Camera m_Camera;
        [SerializeField, AspectRatio, Tooltip("The aspect ratio of the crop mask.")]
        float m_CropAspect = 1.78f;
        [SerializeField]
        bool m_ShowGateMask = true;
        [SerializeField]
        bool m_ShowCropMask;
        [SerializeField, Tooltip("The opacity of the mask."), Range(0.1f, 1f)]
        float m_Opacity = 0.5f;

        Mesh m_Mesh;
        Material m_Material;
        MaterialPropertyBlock m_MaterialPropertyBlock;
        bool m_GeometryIsValid;

        // Caching so that we can update the geometry when needed.
        float m_CachedScreenAspect;
        float m_CachedGateAspect;
        float m_CachedCropAspect;
        bool m_CachedShowCropMask;

#if HDRP_10_2_OR_NEWER
        CustomPassManager.Handle<HdrpFilmFormatPass> m_CustomPassHandle;
#endif
        /// <summary>
        /// Sets the camera to use to render the film format mask.
        /// </summary>
        /// <param name="camera">The camera to set</param>
        public void SetCamera(Camera camera)
        {
            if (m_Camera != camera)
            {
                m_Camera = camera;
                Validate();
            }
        }

        /// <summary>
        /// Whether or not to display the film gate mask.
        /// </summary>
        public bool showGateMask
        {
            get => m_ShowGateMask;
            set => m_ShowGateMask = value;
        }

        /// <summary>
        /// The opacity of the mask.
        /// </summary>
        public float opacity
        {
            get => m_Opacity;
            set => m_Opacity = value;
        }

        /// <summary>
        /// Toggle crop mask visibility. Note that the gate mask must be enabled to render the crop mask.
        /// </summary>
        public bool showCropMask
        {
            get => m_ShowCropMask;
            set => m_ShowCropMask = value;
        }

        /// <summary>
        /// The aspect ratio of the crop mask.
        /// </summary>
        public float cropAspect
        {
            get => m_CropAspect;
            set => m_CropAspect = value;
        }

        /// <summary>
        ///  Checks whether the crop mask should render.
        /// </summary>
        /// <returns>True if the mask should render</returns>
        public bool ShouldRender() => isActiveAndEnabled && m_GeometryIsValid && m_ShowGateMask && m_Opacity > Mathf.Epsilon;

        void Awake()
        {
            m_Mesh = new Mesh();
            m_Material = AdditionalCoreUtils.CreateEngineMaterial("Hidden/LiveCapture/UnlitTransparentColored");
            m_Camera = GetComponent<Camera>();
        }

        void OnDestroy()
        {
            FilmFormatMap.instance.RemoveInstance(this);
            AdditionalCoreUtils.Destroy(m_Material);
            AdditionalCoreUtils.Destroy(m_Mesh);
        }

        void OnValidate()
        {
            m_CropAspect = Mathf.Max(k_MinAspectRatio, m_CropAspect);

            Validate();
        }

        void OnEnable()
        {
            m_MaterialPropertyBlock = new MaterialPropertyBlock();

#if HDRP_10_2_OR_NEWER
            m_CustomPassHandle = new CustomPassManager.Handle<HdrpFilmFormatPass>(CustomPassInjectionPoint.AfterPostProcess);
            m_CustomPassHandle.GetPass().name = k_ProfilingSamplerLabel;
#endif
        }

        void OnDisable()
        {
#if HDRP_10_2_OR_NEWER
            m_CustomPassHandle.Dispose();
#endif
        }

        void Update()
        {
            if (m_Camera != null)
            {
                UpdateCamera();
            }
        }

        void UpdateCamera()
        {
            Debug.Assert(m_Camera != null);

            m_Camera.gateFit = Camera.GateFitMode.Overscan;

            var screenAspect = m_Camera.pixelWidth / (float)m_Camera.pixelHeight;
            var sensorSize = m_Camera.sensorSize;
            var gateAspect = sensorSize.x / sensorSize.y;

            m_GeometryIsValid = screenAspect > float.Epsilon && gateAspect > float.Epsilon;

            if (m_GeometryIsValid && m_ShowGateMask)
                UpdateGeometryIfNeeded(gateAspect, screenAspect);
        }

        void Validate()
        {
            FilmFormatMap.instance.RemoveInstance(this);

            if (m_Camera != null)
            {
                FilmFormatMap.instance.AddUniqueInstance(m_Camera, this);
            }

            Refresh();
        }

        void Refresh()
        {
            m_CachedShowCropMask = !m_ShowCropMask; // Force geometry update.
        }

        /// <summary>
        /// Draw active mask(s).
        /// </summary>
        /// <param name="cmd">The command buffer to append mask drawing commands to.</param>
        /// <param name="material">The material used to render mask(s), typically depends on the render pipeline in use.</param>
        internal void Render(CommandBuffer cmd)
        {
            if (!m_GeometryIsValid)
                return;

            if (!m_ShowGateMask)
            {
                Debug.LogWarning($"[{nameof(FilmFormat)}.Render] should not be called when {nameof(m_ShowGateMask)} is set to false.");
                return;
            }

            m_MaterialPropertyBlock.SetColor(k_MainColorProp, new Color(0, 0, 0, m_Opacity));

            // Geometry is already described in clip space.
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            cmd.DrawMesh(m_Mesh, Matrix4x4.identity, m_Material, 0, 0, m_MaterialPropertyBlock);
        }

        /// <summary>
        /// Update mask(s) geometries if aspect ratios have changed.
        /// </summary>
        /// <param name="gateAspect">Gate (Sensor Size) aspect ratio.</param>
        /// <param name="screenAspect">Screen aspect ratio.</param>
        /// <remarks>
        /// Note that the crop aspect ratio is not passed since it is a serialized field.
        /// The geometry changes at low frequency so no need to be granular, that is, update masks separately.
        /// </remarks>
        void UpdateGeometryIfNeeded(float gateAspect, float screenAspect)
        {
            // Geometry needs to be recomputed if any of the 3 aspect ratios have changed.
            var geometryNeedsUpdate =
                gateAspect != m_CachedGateAspect ||
                screenAspect != m_CachedScreenAspect ||
                m_CropAspect != m_CachedCropAspect ||
                m_CachedShowCropMask != m_ShowCropMask;

            m_CachedGateAspect = gateAspect;
            m_CachedScreenAspect = screenAspect;
            m_CachedCropAspect = m_CropAspect;
            m_CachedShowCropMask = m_ShowCropMask;

            if (geometryNeedsUpdate)
            {
                // Start with a (1, 1) view size, we'll shrink it down according to gate and crop.
                var viewSize = Vector2.one;

                // Apply gate mask.
                viewSize = ApplyAspectRatio(viewSize, gateAspect, screenAspect);
                if (showCropMask) // Optionally add crop mask.
                    viewSize = ApplyAspectRatio(viewSize, m_CropAspect, screenAspect * viewSize.x / viewSize.y);

                UpdateMaskMesh(viewSize, m_Mesh);
            }
        }

        /// <summary>
        /// Update a mesh geometry corresponding to a mask.
        /// </summary>
        /// <param name="size">Normalized size of the mask.</param>
        /// <param name="mesh">The mesh storing the geometry corresponding to the mask.</param>
        /// <remarks>
        /// Note that the mask area is assumed to be centered,
        /// otherwise a rectangle would need to be passed instead of a size.
        /// </remarks>
        static void UpdateMaskMesh(Vector2 size, Mesh mesh)
        {
            k_Rects[0] = new Vector4(-1, -1, 1 - size.x, 2); // left
            k_Rects[1] = new Vector4(-size.x, -1, 2 * size.x, 1 - size.y); // top
            k_Rects[2] = new Vector4(size.x, -1, 1 - size.x, 2); // right
            k_Rects[3] = new Vector4(-size.x, size.y, 2 * size.x, 1 - size.y); // bottom

            for (var i = 0; i != 4; ++i)
                WriteRect(k_Rects[i], k_Vertices, i * 4);

            mesh.vertices = k_Vertices;
            mesh.SetIndices(k_Indices, MeshTopology.Quads, 0);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(false);
        }

        /// <summary>
        /// Write a rectangle geometry to an array of vertices, to be drawn as quads.
        /// </summary>
        /// <param name="rect">Rectangle to be added to the geometry, described as a vector4 (x, y, width, height).</param>
        /// <param name="vertices">Array of vertices to write to.</param>
        /// <param name="startIndex">Index to start writing vertices at.</param>
        static void WriteRect(Vector4 rect, Vector3[] vertices, int startIndex)
        {
            vertices[startIndex] = new Vector3(rect.x, rect.y, 0);
            vertices[startIndex + 1] = new Vector3(rect.x, rect.y + rect.w, 0);
            vertices[startIndex + 2] = new Vector3(rect.x + rect.z, rect.y + rect.w, 0);
            vertices[startIndex + 3] = new Vector3(rect.x + rect.z, rect.y, 0);
        }

        static Vector2 ApplyAspectRatio(Vector2 size, float innerAspect, float outerAspect)
        {
            if (innerAspect < float.Epsilon || outerAspect < float.Epsilon)
                return size;

            if (outerAspect > innerAspect) // Vertical
                size.x *= innerAspect / outerAspect;
            else // Horizontal
                size.y *= outerAspect / innerAspect;

            return size;
        }
    }
}
