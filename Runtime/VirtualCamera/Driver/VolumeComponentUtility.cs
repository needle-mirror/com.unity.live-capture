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
        /// Returns a reference to a Volume component, fetched from a specified gameObject, and added if not already present.
        /// </summary>
        /// <param name="target">Game object to obtain the Volume from.</param>
        /// <param name="isGlobalVolume">Whether or not the volume is global.</param>
        /// <returns>The Volume component.</returns>
        internal static Volume GetOrAddVolume(GameObject target, bool isGlobalVolume = true)
        {
            if (!target.TryGetComponent<Volume>(out var volume))
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

            return volume;
        }

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
        /// Returns a reference to a VolumeComponent, fetched from a specified VolumeProfile, and added if not already present.
        /// </summary>
        /// <param name="profile">VolumeProfile to get or add the volume component from.</param>
        /// <typeparam name="T">Type of the volume component.</typeparam>
        /// <returns>The volume component.</returns>
        internal static T GetOrAddVolumeComponent<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (profile.Has<T>())
            {
                var success = profile.TryGet(out T result);
                Assert.IsTrue(success);
                Assert.IsNotNull(result);
                return result;
            }

            return profile.Add<T>();
        }

#if VP_CINEMACHINE_2_4_0
        internal static CinemachineVolumeSettings GetOrAddVolumeSettings(CinemachineVirtualCamera virtualCamera)
        {
            if (!virtualCamera.TryGetComponent<CinemachineVolumeSettings>(out var volumeSettings))
            {
                volumeSettings = virtualCamera.gameObject.AddComponent<CinemachineVolumeSettings>();
                virtualCamera.AddExtension(volumeSettings);
            }

            return volumeSettings;
        }
#endif
    }
}
#endif
