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
        /// Defines the size of a "valid" readable boundary around the beginning and end of the buffer,
        /// in frames.
        /// </summary>
        /// <remarks>
        /// When attempting to retrieve a sample outside of this boundary, <see cref="TryGetSample"/>
        /// will return <see cref="TimedSampleStatus.DataMissing"/>.
        /// </remarks>
        public int Boundary { get; set; } = 30;

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
        /// Retrieve a sample from the buffer at the time, if available.
        /// </summary>
        /// <param name="timecode">The time to retrieve the sample data for, expressed relative to the given <paramref name="frameRate"/>.</param>
        /// <param name="frameRate">The framerate of the timecode source. Could be different from the framerate of the data in the buffer.</param>
        /// <param name="sampleValue">The retrieved sample, if successful.</param>
        /// <remarks>
        /// If a sample with the requested time could not be found, this method will perform the following:
        /// a) if the time is bounded by the oldest and newest samples contained in the buffer,
        /// set <paramref name="sampleValue"/> to the nearest sample and return <see cref="TimedSampleStatus.Ok"/>.
        /// b) if the time is outside of the buffer and is within the valid region as defined by <see cref="Boundary"/>,
        /// set <paramref name="sampleValue"/> to the newest or oldest (whichever is closer) sample in the buffer,
        /// and return <see cref="TimedSampleStatus.Behind"/> or <see cref="TimedSampleStatus.Ahead"/> respectively.
        /// c) else the return <see cref="TimedSampleStatus.DataMissing"/> and <paramref name="sampleValue"/> is set to its default.
        /// </remarks>
        /// <returns>The status of the retrieved sample. See remarks.</returns>
        public TimedSampleStatus TryGetSample(Timecode timecode, FrameRate frameRate, out T sampleValue)
        {
            var requestedFrameTime = FrameTime.Remap(timecode.ToFrameTime(frameRate), frameRate, FrameRate);
            return TryGetSample(requestedFrameTime, out sampleValue);
        }

        /// <summary>
        /// Retrieve a sample from the buffer at the requested time, if available.
        /// </summary>
        /// <param name="frame">The time to retrieve the sample data for, expressed relative to the nominal <see cref="FrameRate"/>.</param>
        /// <param name="sampleValue">The retrieved sample, if successful.</param>
        /// <remarks>
        /// If a sample with the requested time could not be found, this method will perform the following:
        /// a) if the time is bounded by the oldest and newest samples contained in the buffer,
        /// set <paramref name="sampleValue"/> to the nearest sample and return <see cref="TimedSampleStatus.Ok"/>.
        /// b) if the time is outside of the buffer and is within the valid region as defined by <see cref="Boundary"/>,
        /// set <paramref name="sampleValue"/> to the newest or oldest (whichever is closer) sample in the buffer,
        /// and return <see cref="TimedSampleStatus.Behind"/> or <see cref="TimedSampleStatus.Ahead"/> respectively.
        /// c) else the return <see cref="TimedSampleStatus.DataMissing"/> and <paramref name="sampleValue"/> is set to its default.
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

            var boundaryDelta = new FrameTime(Boundary);
            var frontBoundary = oldestFrameTime - boundaryDelta;
            var backBoundary = newestFrameTime + boundaryDelta;

            if (frame < frontBoundary || frame > backBoundary)
            {
                return TimedSampleStatus.DataMissing;
            }

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
            var frameTime = FrameTime.Remap(timecode.ToFrameTime(frameRate), frameRate, FrameRate);
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
