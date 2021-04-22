using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.LiveCapture
{
    /// <summary>
    /// Represents an animation curve.
    /// </summary>
    public interface ICurve
    {
        /// <summary>
        /// The transform path of the object that is animated.
        /// </summary>
        string relativePath { get; }

        /// <summary>
        /// The name of the property to be animated.
        /// </summary>
        string propertyName { get; }

        /// <summary>
        /// The type of the property to be animated.
        /// </summary>
        Type bindingType { get; }

        /// <summary>
        /// The sampling rate in Hz.
        /// </summary>
        FrameRate frameRate { get; set; }

        /// <summary>
        /// Checks if the animation curve contains keyframes.
        /// </summary>
        /// <returns>true if the curve contains no keyframes; otherwise, false.</returns>
        bool IsEmpty();

        /// <summary>
        /// Clears the keyframes from the curve.
        /// </summary>
        void Clear();

        /// <summary>
        /// Sets the curve to the given animation clip.
        /// </summary>
        /// <param name="clip">The animation clip to set the curve to.</param>
        void SetToAnimationClip(AnimationClip clip);
    }

    /// <summary>
    /// Represents an animation curve that stores keyframes of generic value.
    /// </summary>
    /// <typeparam name="T">The type of data to store in the curve.</typeparam>
    public interface ICurve<T> : ICurve
    {
        /// <summary>
        /// Adds a keyframe to the curve.
        /// </summary>
        /// <param name="time">The time in seconds to insert the keyframe at.</param>
        /// <param name="value">The keyframe value.</param>
        void AddKey(float time, T value);
    }
}
