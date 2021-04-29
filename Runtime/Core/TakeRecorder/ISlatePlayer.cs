using System;
using UnityEngine;

namespace Unity.LiveCapture
{
    interface ITakeRecorderContext : IEquatable<ITakeRecorderContext>
    {
        IExposedPropertyTable GetResolver();
        ISlate GetSlate();
        double GetTimeOffset();
        double GetTime();
        void SetTime(double value);
        void Prepare(bool isRecording);
        double GetDuration();
        bool IsValid();
    }
}
