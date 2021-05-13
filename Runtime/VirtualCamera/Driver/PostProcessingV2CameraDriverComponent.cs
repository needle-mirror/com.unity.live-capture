#if PP_3_0_3_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.PostProcessing;

namespace Unity.LiveCapture.VirtualCamera
{
    [Serializable]
    class PostProcessingV2CameraDriverComponent : ICameraDriverComponent
    {
        DepthOfField m_DepthOfField;
        Camera m_Camera;

        /// <summary>
        /// Configures the driver component based on a Camera instance.
        /// </summary>
        /// <param name="camera">The camera instance used by the driver.</param>
        public void SetCamera(Camera camera)
        {
            m_Camera = camera;
            Assert.IsNotNull(m_Camera);

            var postProcessLayer = AdditionalCoreUtils.GetOrAddComponent<PostProcessLayer>(m_Camera.gameObject);
            postProcessLayer.volumeTrigger = m_Camera.transform;
            postProcessLayer.volumeLayer = LayerMask.GetMask(LayerMask.LayerToName(m_Camera.gameObject.layer));

            var postProcessVolume = AdditionalCoreUtils.GetOrAddComponent<PostProcessVolume>(m_Camera.gameObject);
            postProcessVolume.isGlobal = false;
            postProcessVolume.priority = 1;

            var sphereCollider = AdditionalCoreUtils.GetOrAddComponent<SphereCollider>(m_Camera.gameObject);
            sphereCollider.radius = 0.01f;
            sphereCollider.isTrigger = true;

            var profile = postProcessVolume.profile;
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<PostProcessProfile>();
                profile.hideFlags = HideFlags.DontSave;
                postProcessVolume.profile = profile;
            }

            m_DepthOfField = profile.GetSetting<DepthOfField>();
            if (m_DepthOfField == null)
            {
                m_DepthOfField = profile.AddSettings<DepthOfField>();
                m_DepthOfField.hideFlags = HideFlags.DontSave;
            }

            UpdateParameterIfNeeded(m_DepthOfField.enabled, false);
        }

        /// <inheritdoc/>
        public bool EnableDepthOfField(bool value)
        {
            UpdateParameterIfNeeded(m_DepthOfField.enabled, value);
            return true;
        }

        /// <inheritdoc/>
        public bool SetDamping(Damping dampingData)
        {
            return false;
        }

        /// <inheritdoc/>
        public bool SetFocusDistance(float focusDistance)
        {
            UpdateParameterIfNeeded(m_DepthOfField.focusDistance, focusDistance);
            return true;
        }

        /// <inheritdoc/>
        public bool SetPhysicalCameraProperties(Lens lens, LensIntrinsics intrinsics, CameraBody cameraBody)
        {
            CompositeCameraDriverImpl.UpdateCamera(m_Camera, lens, intrinsics, cameraBody);

            UpdateParameterIfNeeded(m_DepthOfField.focalLength, lens.FocalLength);
            UpdateParameterIfNeeded(m_DepthOfField.aperture, lens.Aperture);

            return true;
        }

        static void UpdateParameterIfNeeded<T>(ParameterOverride<T> parameter, T value)
        {
            if (EqualityComparer<T>.Default.Equals(value, parameter.value))
                return;

            parameter.value = value;
            parameter.overrideState = true;
        }
    }
}
#endif
