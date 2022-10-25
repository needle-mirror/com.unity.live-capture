using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.LiveCapture
{
    /// <summary>
    /// A circular buffer of data samples ordered by the <see cref="FrameTime"/> of each sample.
    /// </summary>
    /// <typeparam name="T">The datatype of the samples.</typeparam>
    /// <seealso cref="CircularBuffer{T}"/>
    public class TimedDataBuffer<T> : CircularBuffer<(FrameTime frameTime, T value)>
    {
        FrameRate m_FrameRate;

        /// <summary>
        /// The interpolator used when <see cref="TryGetSample"/> is called for a time which
        /// lies between two samples.
        /// </summary>
        public IInterpolator<T> Interpolator { get; set; }

        /// <summary>
        /// Creates a new <see cref="TimedDataBuffer{T}"/> instance.
        /// </summary>
        /// <param name="frameRate">The frame rate of the samples.</param>
        /// <param name="capacity">The capacity of the buffer.</param>
        /// <param name="sampleDiscard">A callback invoked for each sample that is discarded from the buffer. This may be used
        /// to dispose samples if needed.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="frameRate"/> is invalid.</exception>
        public TimedDataBuffer(FrameRate frameRate, int capacity = 5, Action<T> sampleDiscard = null) : base
        (
            capacity,
            sampleDiscard != null ? new Action<(FrameTime, T)>(x => sampleDiscard(x.Item2)) : null
        )
        {
            if (!frameRate.IsValid)
                throw new ArgumentException("Cannot be invalid.", nameof(frameRate));

            m_FrameRate = frameRate;
        }

        /// <summary>
        /// Gets the frame rate of the buffered samples.
        /// </summary>
        /// <returns>The frame rate of the buffered samples.</returns>
        public FrameRate GetFrameRate()
        {
            return m_FrameRate;
        }

        /// <summary>
        /// Sets the frame rate of the buffered samples.
        /// </summary>
        /// <remarks>
        /// This will remap the frame time of all samples in the buffer to match the new frame rate.
        /// </remarks>
        /// <param name="frameRate">The new frame rate.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="frameRate"/> is invalid.</exception>
        public void SetFrameRate(FrameRate frameRate)
        {
            if (!frameRate.IsValid)
                throw new ArgumentException("Cannot be invalid.", nameof(frameRate));
            if (m_FrameRate == frameRate)
                return;

            for (var i = 0; i < Count; i++)
            {
                var value = this[i];
                value.frameTime = FrameTime.Remap(value.frameTime, m_FrameRate, frameRate);
                this[i] = value;
            }

            m_FrameRate = frameRate;
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

            ref readonly var front = ref PeekFront();
            ref readonly var back = ref PeekBack();

            oldestSample = front.frameTime;
            newestSample = back.frameTime;
            return true;
        }

        /// <summary>
        /// Retrieves a sample from the buffer at the specified time.
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
        /// <returns>The status of the retrieved sample.</returns>
        public TimedSampleStatus TryGetSample(FrameTime frame, out T sampleValue)
        {
            sampleValue = default;

            if (Count == 0)
            {
                return TimedSampleStatus.DataMissing;
            }

            ref readonly var oldestSample = ref PeekFront();

            if (frame < oldestSample.frameTime)
            {
                sampleValue = oldestSample.value;
                return TimedSampleStatus.Ahead;
            }

            ref readonly var newestSample = ref PeekBack();

            if (frame > newestSample.frameTime)
            {
                sampleValue = newestSample.value;
                return TimedSampleStatus.Behind;
            }

            for (var i = 0; i < Count; i++)
            {
                ref readonly var nextSample = ref PeekIndex(i);

                if (nextSample.frameTime < frame)
                {
                    continue;
                }
                if (nextSample.frameTime == frame)
                {
                    sampleValue = nextSample.value;
                    return TimedSampleStatus.Ok;
                }

                ref readonly var prevSample = ref PeekIndex(i - 1);

                if (Interpolator != null)
                {
                    var fac = (float)(frame - prevSample.frameTime) / (float)(nextSample.frameTime - prevSample.frameTime);
                    sampleValue = Interpolator.Interpolate(prevSample.value, nextSample.value, fac);
                }
                else
                {
                    var prevDiff = Math.Abs((double)(frame - prevSample.frameTime));
                    var nextDiff = Math.Abs((double)(frame - nextSample.frameTime));
                    sampleValue = prevDiff < nextDiff ? prevSample.value : nextSample.value;
                }

                return TimedSampleStatus.Ok;
            }

            return TimedSampleStatus.DataMissing;
        }

        /// <summary>
        /// Retrieves the buffered samples that lie in a time range.
        /// </summary>
        /// <remarks>
        /// The range bounds are inclusive.
        /// </remarks>
        /// <param name="from">The start time of the range, expressed relative to the <see cref="FrameRate"/>.</param>
        /// <param name="to">The end time of the range, expressed relative to the <see cref="FrameRate"/>.</param>
        /// <returns>An iterator that returns all samples in the specified range.</returns>
        public IEnumerable<(FrameTime time, T value)> GetSamplesInRange(FrameTime from, FrameTime to)
        {
            if (Count == 0)
            {
                yield break;
            }

            // ensure the range bounds are ascending in time
            if (to < from)
            {
                var temp = to;
                to = from;
                from = temp;
            }

            // early outs to prevent reading through all the samples when we know none are in range
            if (to < PeekFront().frameTime)
            {
                yield break;
            }
            if (from > PeekBack().frameTime)
            {
                yield break;
            }

            // iterate through the samples returning all in the range
            foreach (var sample in this)
            {
                if (from <= sample.frameTime && sample.frameTime <= to)
                {
                    yield return sample;
                }
                else if (to < sample.frameTime)
                {
                    yield break;
                }
            }
        }

        /// <summary>
        /// Adds a new sample to the buffer.
        /// </summary>
        /// <remarks>
        /// When the buffer is full, the oldest sample in the buffer is discarded.
        /// The sample is inserted into the buffer so that the samples are ordered by increasing time.
        /// If a sample with the specified time already exists in the buffer, its value is updated with the new value.
        /// </remarks>
        /// <param name="value">The sample value.</param>
        /// <param name="time">The time of the sample in frames.</param>
        public void Add(in T value, in FrameTimeWithRate time)
        {
            var frameTime = FrameTime.Remap(time.Time, time.Rate, m_FrameRate);
            var newSample = (frameTime, value);

            // find out where to insert the sample to keep them in order by time
            for (var i = Count - 1; i >= 0; i--)
            {
                ref readonly var sample = ref PeekIndex(i);

                if (sample.frameTime < frameTime)
                {
                    PushIndex(i + 1, newSample);
                    return;
                }

                // if there is already a sample with the specified time, replace the data with the new value
                if (sample.frameTime == frameTime)
                {
                    this[i] = newSample;
                    return;
                }
            }

            PushIndex(0, newSample);
        }

        /// <summary>
        /// Adds a new sample to the buffer.
        /// </summary>
        /// <remarks>
        /// When the buffer is full, the oldest sample in the buffer is discarded.
        /// The sample is inserted into the buffer so that the samples are ordered by increasing time.
        /// If a sample with the specified time already exists in the buffer, its value is updated with the new value.
        /// </remarks>
        /// <param name="value">The sample value.</param>
        /// <param name="time">The time of the sample in seconds.</param>
        public void Add(in T value, double time)
        {
            Add(value, FrameTimeWithRate.FromSeconds(m_FrameRate, time));
        }
    }
}
