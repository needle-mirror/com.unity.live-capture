#if URP_10_2_OR_NEWER
using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.Universal;

namespace Unity.LiveCapture.VirtualCamera
{
    [Serializable]
    class UrpCameraDriverComponent : ICameraDriverComponent
    {
#pragma warning disable 649
        [SerializeField, Tooltip("Use high quality Depth Of Field. Trades performance for visual quality.")]
        bool m_UseHighQualityDepthOfField;
#pragma warning restore 649

        DepthOfField m_DepthOfField;
        Camera m_Camera;
        UniversalAdditionalCameraData m_UniversalCameraData;

        /// <summary>
        /// Configure the driver component based on a Camera instance.
        /// </summary>
        /// <param name="camera">The camera instance used by the driver.</param>
        public void SetCamera(Camera camera)
        {
            m_Camera = camera;
            Assert.IsNotNull(m_Camera, $"{nameof(UrpCameraDriverComponent)} expects a GameObject holding a Camera.");

            m_UniversalCameraData = m_Camera.GetComponent<UniversalAdditionalCameraData>();
            // It can be that the additional camera data has not been added yet.
            if (m_UniversalCameraData == null)
                m_UniversalCameraData = m_Camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            m_UniversalCameraData.renderPostProcessing = false;

            m_DepthOfField = VolumeComponentUtility.GetOrAddVolumeComponent<DepthOfField>(m_Camera.gameObject, false);
            VolumeComponentUtility.UpdateParameterIfNeeded(m_DepthOfField.mode, DepthOfFieldMode.Bokeh);
            VolumeComponentUtility.UpdateParameterIfNeeded(m_DepthOfField.bladeCurvature, 1);
        }

        /// <inheritdoc/>
        public bool SetDamping(Damping dampingData) { return false; }

        /// <inheritdoc/>
        public bool EnableDepthOfField(bool value)
        {
            // So far DoF is the only post process we support on URP
            m_UniversalCameraData.renderPostProcessing = value;
            m_DepthOfField.active = value;
            return true;
        }

        /// <inheritdoc/>
        public bool SetFocusDistance(float value)
        {
            VolumeComponentUtility.UpdateParameterIfNeeded(m_DepthOfField.highQualitySampling, m_UseHighQualityDepthOfField);
            VolumeComponentUtility.UpdateParameterIfNeeded(m_DepthOfField.focusDistance, value);
            return true;
        }

        /// <inheritdoc/>
        public bool SetPhysicalCameraProperties(Lens lens, CameraBody cameraBody)
        {
            CompositeCameraDriverImpl.UpdateCamera(m_Camera, lens, cameraBody);

            VolumeComponentUtility.UpdateParameterIfNeeded(m_DepthOfField.highQualitySampling, m_UseHighQualityDepthOfField);
            VolumeComponentUtility.UpdateParameterIfNeeded(m_DepthOfField.focalLength, lens.focalLength);
            VolumeComponentUtility.UpdateParameterIfNeeded(m_DepthOfField.aperture, lens.aperture);
            VolumeComponentUtility.UpdateParameterIfNeeded(m_DepthOfField.bladeCount, lens.bladeCount);

            return true;
        }
    }
}
#endif
