using System;
using UnityEngine;

namespace Unity.LiveCapture
{
    interface ITakeRecorderContext : IShot, IEquatable<ITakeRecorderContext>
    {
        IExposedPropertyTable GetResolver();
        double GetTimeOffset();
        void Play();
        bool IsPlaying();
        void Pause();
        double GetTime();
        void SetTime(double value);
        void Rebuild();
        double GetDuration();
        bool IsValid();
        void ClearSceneBindings();
        void SetSceneBindings();
    }
}
