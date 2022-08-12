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
        const string k_ProfileName = "Camera Driver Profile";

        [SerializeField, HideInInspector]
        PostProcessProfile m_Profile;
        DepthOfField m_DepthOfField;
        
        /// <summary>
        /// Camera to be driven by this component.
        /// </summary>
        public Camera Camera { get; set; }

        void PrepareIfNeeded()
        {
            Assert.IsNotNull(Camera);

            if (!Camera.gameObject.TryGetComponent<PostProcessLayer>(out var postProcessLayer))
            {
                postProcessLayer = AdditionalCoreUtils.GetOrAddComponent<PostProcessLayer>(Camera.gameObject);
                postProcessLayer.volumeTrigger = Camera.transform;
                postProcessLayer.volumeLayer = LayerMask.GetMask(LayerMask.LayerToName(Camera.gameObject.layer));
            }

            if (!Camera.gameObject.TryGetComponent<PostProcessVolume>(out var volume))
            {
                volume = AdditionalCoreUtils.GetOrAddComponent<PostProcessVolume>(Camera.gameObject);
                volume.isGlobal = false;
                volume.priority = 1;
            }

            if (!Camera.gameObject.TryGetComponent<SphereCollider>(out var sphereCollider))
            {
                sphereCollider = AdditionalCoreUtils.GetOrAddComponent<SphereCollider>(Camera.gameObject);
                sphereCollider.radius = 0.01f;
                sphereCollider.isTrigger = true;
            }

            // Profile instances will end up being shared when duplicating objects through serialization.
            // We have to invalidate the profile when we don't know who created it.
            if (!VolumeProfileTracker.Instance.TryRegisterProfileOwner(m_Profile, volume))
            {
                m_Profile = null;
            }

            if (m_Profile == null)
            {
                m_Profile = volume.profile;

                if (string.IsNullOrEmpty(m_Profile.name))
                {
                    m_Profile.name = k_ProfileName;
                }

                VolumeProfileTracker.Instance.TryRegisterProfileOwner(m_Profile, volume);
            }

            volume.profile = m_Profile;

            m_DepthOfField = m_Profile.GetSetting<DepthOfField>();

            if (m_DepthOfField == null)
            {
                m_DepthOfField = m_Profile.AddSettings<DepthOfField>();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            VolumeProfileTracker.Instance.UnregisterProfile(m_Profile);
            AdditionalCoreUtils.DestroyIfNeeded(ref m_Profile);
            AdditionalCoreUtils.DestroyIfNeeded(ref m_DepthOfField);
        }

        /// <inheritdoc/>
        public void EnableDepthOfField(bool value)
        {
            PrepareIfNeeded();

            UpdateParameterIfNeeded(m_DepthOfField.enabled, value);
        }

        /// <inheritdoc/>
        public void SetDamping(Damping dampingData) {}

        /// <inheritdoc/>
        public void SetFocusDistance(float focusDistance)
        {
            PrepareIfNeeded();

            UpdateParameterIfNeeded(m_DepthOfField.focusDistance, focusDistance);
        }

        /// <inheritdoc/>
        public void SetPhysicalCameraProperties(Lens lens, LensIntrinsics intrinsics, CameraBody cameraBody)
        {
            PrepareIfNeeded();

            UpdateParameterIfNeeded(m_DepthOfField.focalLength, lens.FocalLength);
            UpdateParameterIfNeeded(m_DepthOfField.aperture, lens.Aperture);
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
