using System;
using UnityEngine;

namespace Unity.LiveCapture
{
    /// <summary>
    /// A circular buffer of timed samples (data with an associated <see cref="Timecode"/>).
    /// </summary>
    /// <typeparam name="T">The datatype of the samples.</typeparam>
    /// <seealso cref="CircularBuffer{T}"/>
    public class TimedDataBuffer<T> : CircularBuffer<(FrameTime frameTime, T value)>
    {
        /// <summary>
        /// Gets the nominal frame rate of the buffered samples.
        /// </summary>
        public FrameRate FrameRate { get; }

        /// <summary>
        /// Creates a new <see cref="TimedDataBuffer{T}"/> instance.
        /// </summary>
        /// <param name="frameRate">The nominal frame rate of the samples.</param>
        /// <param name="capacity">The capacity of the buffer.</param>
        public TimedDataBuffer(FrameRate frameRate, int capacity = 5) : base(capacity)
        {
            FrameRate = frameRate;
        }

        /// <summary>
        /// Gets the frame time of the oldest sample in the buffer.
        /// </summary>
        /// <param name="frameRate">The frame rate to use for the returned frame time.</param>
        /// <returns>The frame time of the oldest sample or <see langword="null"/> if there are no samples in the buffer.</returns>
        public FrameTime? GetOldestSampleTime(FrameRate frameRate)
        {
            if (Count == 0)
            {
                return default;
            }

            return FrameTime.Remap(Front().frameTime, FrameRate, frameRate);
        }

        /// <summary>
        /// Gets the frame time of the newest sample in the buffer.
        /// </summary>
        /// <param name="frameRate">The frame rate to use for the returned frame time.</param>
        /// <returns>The frame time of the newest sample or <see langword="null"/> if there are no samples in the buffer.</returns>
        public FrameTime? GetNewestSampleTime(FrameRate frameRate)
        {
            if (Count == 0)
            {
                return default;
            }

            return FrameTime.Remap(Back().frameTime, FrameRate, frameRate);
        }

        /// <summary>
        /// Gets the frame time of the newest and oldest samples in the buffer.
        /// </summary>
        /// <param name="oldestSample">The frame time of the oldest sample in the buffer, or <see langword="default"/> if there are no samples in the buffer.</param>
        /// <param name="newestSample">The frame time of the newest sample in the buffer, or <see langword="default"/> if there are no samples in the buffer.</param>
        /// <returns><see langword="true"/> if there are any samples in the buffer; otherwise, <see langword="false"/>.</returns>
        public bool TryGetBufferRange(out FrameTime oldestSample, out FrameTime newestSample)
        {
            if (Count == 0)
            {
                oldestSample = default;
                newestSample = default;
                return false;
            }

            oldestSample = Front().frameTime;
            newestSample = Back().frameTime;
            return true;
        }

        /// <summary>
        /// Retrieves a sample from the buffer at the specified time, if available.
        /// </summary>
        /// <param name="timecode">The time to retrieve the sample data for, expressed relative to the given <paramref name="frameRate"/>.</param>
        /// <param name="frameRate">The framerate of the timecode source. It might be different from the framerate of the data in the buffer.</param>
        /// <param name="sampleValue">The retrieved sample, if successful.</param>
        /// <remarks>
        /// If there isn't a sample at the specified time, the following occurs:
        /// <para>
        /// a) if the time is bounded by the oldest and newest samples contained in the buffer,
        /// <paramref name="sampleValue"/> is set to the nearest sample and returns <see cref="TimedSampleStatus.Ok"/>.<br/>
        /// b) if the time is outside of the buffer but there is at least one buffered sample,
        /// <paramref name="sampleValue"/> is set to the newest or oldest buffered sample (whichever is closer)
        /// and returns <see cref="TimedSampleStatus.Behind"/> or <see cref="TimedSampleStatus.Ahead"/> respectively.<br/>
        /// c) otherwise, returns <see cref="TimedSampleStatus.DataMissing"/> and sets <paramref name="sampleValue"/> to <see langword="default"/>.
        /// </para>
        /// </remarks>
        /// <returns>The status of the retrieved sample. See remarks.</returns>
        public TimedSampleStatus TryGetSample(Timecode timecode, FrameRate frameRate, out T sampleValue)
        {
            var requestedFrameTime = FrameTime.Remap(timecode.ToFrameTime(frameRate), frameRate, FrameRate);
            return TryGetSample(requestedFrameTime, out sampleValue);
        }

        /// <summary>
        /// Retrieves a sample from the buffer at the specified time, if available.
        /// </summary>
        /// <param name="frame">The time to retrieve the sample data for, expressed relative to the <see cref="FrameRate"/>.</param>
        /// <param name="sampleValue">The retrieved sample, if successful.</param>
        /// <remarks>
        /// If there isn't a sample at the specified time, the following occurs:
        /// <para>
        /// a) if the time is bounded by the oldest and newest samples contained in the buffer,
        /// <paramref name="sampleValue"/> is set to the nearest sample and returns <see cref="TimedSampleStatus.Ok"/>.<br/>
        /// b) if the time is outside of the buffer but there is at least one buffered sample,
        /// <paramref name="sampleValue"/> is set to the newest or oldest buffered sample (whichever is closer)
        /// and returns <see cref="TimedSampleStatus.Behind"/> or <see cref="TimedSampleStatus.Ahead"/> respectively.<br/>
        /// c) otherwise, returns <see cref="TimedSampleStatus.DataMissing"/> and sets <paramref name="sampleValue"/> to <see langword="default"/>.
        /// </para>
        /// </remarks>
        /// <returns>The status of the retrieved sample. See remarks.</returns>
        public TimedSampleStatus TryGetSample(FrameTime frame, out T sampleValue)
        {
            sampleValue = default;

            if (Count == 0)
            {
                return TimedSampleStatus.DataMissing;
            }

            var(oldestFrameTime, oldestSample) = Front();
            var(newestFrameTime, newestSample) = Back();

            if (frame < oldestFrameTime)
            {
                sampleValue = oldestSample;
                return TimedSampleStatus.Ahead;
            }

            if (frame > newestFrameTime)
            {
                sampleValue = newestSample;
                return TimedSampleStatus.Behind;
            }

            var bestDiff = double.PositiveInfinity;

            foreach (var(frameTime, value) in this)
            {
                var diff = Distance(frame, frameTime);
                if (diff < bestDiff)
                {
                    (bestDiff, sampleValue) = (diff, value);
                }

                // Assuming that samples are monotonically increasing in time, we can stop searching once
                // we've gone past the requested time.
                if (frameTime > frame)
                    break;
            }

            return TimedSampleStatus.Ok;
        }

        /// <summary>
        /// Add a new sample to the back of the buffer.
        /// </summary>
        /// <param name="timecode">The timecode of the sample.</param>
        /// <param name="frameRate">The frame rate of the timecode.</param>
        /// <param name="value">The sample value.</param>
        /// <remarks>
        /// If the back of the buffer (the newest sample) is older than or has the sample time value as
        /// the new sample, the new sample will not be added. That is, out-of-order additions will
        /// be dropped.
        /// </remarks>
        public void Add(Timecode timecode, FrameRate frameRate, T value)
        {
            Add(timecode.ToFrameTime(frameRate), frameRate, value);
        }

        /// <summary>
        /// Add a new sample to the back of the buffer.
        /// </summary>
        /// <param name="frameTime">The frame time of the sample.</param>
        /// <param name="frameRate">The frame rate of the frame time.</param>
        /// <param name="value">The sample value.</param>
        /// <remarks>
        /// If the back of the buffer (the newest sample) is older than or has the sample time value as
        /// the new sample, the new sample will not be added. That is, out-of-order additions will
        /// be dropped.
        /// </remarks>
        internal void Add(FrameTime frameTime, FrameRate frameRate, T value)
        {
            frameTime = FrameTime.Remap(frameTime, frameRate, FrameRate);
            if (Count == 0 || Back().frameTime < frameTime)
            {
                Add((frameTime, value));
            }
        }

        /// <summary>
        /// Add a new sample to the back of the buffer.
        /// </summary>
        /// <param name="time">The time of the sample in seconds.</param>
        /// <param name="value">The sample value.</param>
        /// <remarks>
        /// If the back of the buffer (the newest sample) is older than or has the sample time value as
        /// the new sample, the new sample will not be added. That is, out-of-order additions will
        /// be dropped.
        /// </remarks>
        public void Add(double time, T value)
        {
            var frameTime = FrameTime.FromSeconds(FrameRate, time);
            if (Count == 0 || Back().frameTime < frameTime)
            {
                Add((frameTime, value));
            }
        }

        static double Distance(FrameTime a, FrameTime b)
        {
            return Math.Abs((double)(a - b));
        }
    }
}
