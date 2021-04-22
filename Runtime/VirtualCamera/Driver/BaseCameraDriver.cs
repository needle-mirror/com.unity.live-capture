using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// Implements the Camera driver, that is, the component responsible for applying the virtual camera state
    /// to the current scene's camera and post effects.
    /// </summary>
    /// <remarks>
    /// Since we support multiple render pipelines and optionally Cinemachine, we split the code responsible
    /// for each pipeline and Cinemachine among different components. Components are added based on which packages
    /// are installed/used.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VirtualCameraActor))]
    [ExecuteAlways]
    abstract class BaseCameraDriver : MonoBehaviour, ICameraDriver
    {
        [SerializeField, HideInInspector]
        protected VirtualCameraActor m_VirtualCameraActor;

        float m_CachedFocusDistance;
        bool m_CachedFocusDistanceEnabled;

        /// <inheritdoc/>
        public abstract Camera GetCamera();

        protected abstract ICameraDriverImpl GetImplementation();

        protected abstract void OnAwake();

        bool TryGetImplementation(out ICameraDriverImpl impl)
        {
            impl = null;

            try
            {
                impl = GetImplementation();
            }
            catch {}

            return impl != null;
        }

        void Awake()
        {
            m_VirtualCameraActor = GetComponent<VirtualCameraActor>();
            OnAwake();
        }

        void LateUpdate()
        {
            if (TryGetImplementation(out var impl))
            {
                Assert.IsNotNull(m_VirtualCameraActor);
                var lens = m_VirtualCameraActor.lens;
                var cameraBody = m_VirtualCameraActor.cameraBody;

                impl.EnableDepthOfField(m_VirtualCameraActor.depthOfFieldEnabled);
                impl.SetFocusDistance(lens.focusDistance);
                impl.SetPhysicalCameraProperties(lens, cameraBody);

                if (FocusPlaneMap.instance.TryGetInstance(GetCamera(), out var focusPlane))
                    focusPlane.SetFocusDistance(lens.focusDistance);

                m_CachedFocusDistanceEnabled = m_VirtualCameraActor.depthOfFieldEnabled;
                m_CachedFocusDistance = lens.focusDistance;
            }
        }

        void OnDrawGizmos()
        {
            // Visualize focus distance.
            if (m_CachedFocusDistanceEnabled)
            {
                var cameraTransform = GetCamera()?.transform;
                if (cameraTransform != null)
                {
                    var position = cameraTransform.position;
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(position, position + cameraTransform.forward * m_CachedFocusDistance);
                }
            }
        }
    }
}
