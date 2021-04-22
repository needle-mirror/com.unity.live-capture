#if HDRP_10_2_OR_NEWER || URP_10_2_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
#if VP_CINEMACHINE_2_4_0
using Cinemachine;
using Cinemachine.PostFX;
#endif
using UnityEngine.Rendering;

namespace Unity.LiveCapture.VirtualCamera
{
    static class VolumeComponentUtility
    {
        /// <summary>
        /// Update the value of a Volume Parameter if needed, that is if its current value differs from the desired one.
        /// </summary>
        /// <param name="parameter">Volume parameter to be updated.</param>
        /// <param name="value">Desired value of the parameter.</param>
        /// <typeparam name="T">Type of the value.</typeparam>
        internal static void UpdateParameterIfNeeded<T>(VolumeParameter<T> parameter, T value)
        {
            if (EqualityComparer<T>.Default.Equals(value, parameter.value))
                return;

            parameter.value = value;
            parameter.overrideState = true;
        }

        /// <summary>
        /// Returns a reference to a volume component, fetched from a volume held by the gameObject, and added if not already present.
        /// </summary>
        /// <param name="target">Game object holding the volume.</param>
        /// <param name="isGlobalVolume">Whether or not the volume is global.</param>
        /// <typeparam name="T">Type of the volume component.</typeparam>
        /// <returns>The volume component.</returns>
        internal static T GetOrAddVolumeComponent<T>(GameObject target, bool isGlobalVolume = true) where T : VolumeComponent
        {
            var volume = target.GetComponent<Volume>();
            if (volume == null)
            {
                volume = target.AddComponent<Volume>();
                volume.priority = 1;

                volume.isGlobal = isGlobalVolume;
                if (!isGlobalVolume)
                {
                    var col = target.AddComponent<SphereCollider>();
                    col.radius = 0.01f;
                    col.isTrigger = true;
                }
            }

            if (volume.profile == null)
            {
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.hideFlags = HideFlags.DontSave;
                volume.profile = profile;
            }

            if (volume.profile.Has<T>())
            {
                var success = volume.profile.TryGet(out T result);
                Assert.IsTrue(success);
                Assert.IsNotNull(result);
                return result;
            }

            return volume.profile.Add<T>();
        }

#if VP_CINEMACHINE_2_4_0
        /// <summary>
        /// Returns a reference to a Volume Component, fetched from Cinemachine Volume Settings, and added if not already present.
        /// </summary>
        /// <param name="virtualCamera">Cinemachine Virtual Camera holding the Volume Settings.</param>
        /// <typeparam name="T">Type of the volume component.</typeparam>
        /// <returns>The volume component.</returns>
        internal static T GetOrAddVolumeComponent<T>(CinemachineVirtualCamera virtualCamera) where T : VolumeComponent
        {
            var volumeSettings = virtualCamera.GetComponent<CinemachineVolumeSettings>();
            if (volumeSettings == null)
            {
                volumeSettings = virtualCamera.gameObject.AddComponent<CinemachineVolumeSettings>();
                virtualCamera.AddExtension(volumeSettings);
            }

            if (volumeSettings.m_Profile == null)
            {
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.hideFlags = HideFlags.DontSave;
                volumeSettings.m_Profile = profile;
            }

            if (volumeSettings.m_Profile.Has<T>())
            {
                var success = volumeSettings.m_Profile.TryGet(out T result);
                Assert.IsTrue(success);
                Assert.IsNotNull(result);
                return result;
            }

            return volumeSettings.m_Profile.Add<T>();
        }

#endif
    }
}
#endif
