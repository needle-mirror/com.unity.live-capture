using System;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    [Serializable]
    class VanillaCameraDriverComponent : ICameraDriverComponent
    {
        Camera m_Camera;

        /// <summary>
        /// The camera this driver component acts on.
        /// </summary>
        public Camera Camera
        {
            set { m_Camera = value; }
        }

        /// <inheritdoc/>
        public bool EnableDepthOfField(bool value)
        {
            return false;
        }

        /// <inheritdoc/>
        public bool SetDamping(Damping dampingData)
        {
            return false;
        }

        /// <inheritdoc/>
        public bool SetFocusDistance(float focusDistance)
        {
            return false;
        }

        /// <inheritdoc/>
        public bool SetPhysicalCameraProperties(Lens lens, LensIntrinsics intrinsics, CameraBody cameraBody)
        {
            CompositeCameraDriverImpl.UpdateCamera(m_Camera, lens, intrinsics, cameraBody);
            return true;
        }
    }
}
