#if HDRP_10_2_OR_NEWER
using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Unity.LiveCapture.VirtualCamera
{
    [Serializable]
    class HdrpCoreCameraDriverComponent : ICameraDriverComponent
    {
        DepthOfField m_DepthOfField;

        /// <summary>
        /// Configure the driver component based on a gameObject to attach a post-processing volume to.
        /// </summary>
        /// <param name="gameObject">The gameObject meant to hold the post-processing volume.</param>
        public void SetRoot(GameObject gameObject)
        {
            m_DepthOfField = VolumeComponentUtility.GetOrAddVolumeComponent<DepthOfField>(gameObject, false);
            VolumeComponentUtility.UpdateParameterIfNeeded(m_DepthOfField.focusMode, DepthOfFieldMode.UsePhysicalCamera);
        }

        /// <inheritdoc/>
        public bool SetDamping(Damping dampingData) { return false; }

        /// <inheritdoc/>
        public bool EnableDepthOfField(bool value)
        {
            m_DepthOfField.active = value;
            return true;
        }

        /// <inheritdoc/>
        public bool SetFocusDistance(float value)
        {
            VolumeComponentUtility.UpdateParameterIfNeeded(m_DepthOfField.focusDistance, value);
            return true;
        }

        /// <inheritdoc/>
        public bool SetPhysicalCameraProperties(Lens lens, LensIntrinsics intrinsics, CameraBody cameraBody) { return false; }
    }
}
#endif
