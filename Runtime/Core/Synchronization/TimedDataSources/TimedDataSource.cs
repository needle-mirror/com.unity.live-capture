using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.LiveCapture
{
    /// <summary>
    /// A base class that implements the default behaviour for a <see cref="ITimedDataSource"/>.
    /// </summary>
    /// <typeparam name="T">The type used to store the data for a frame.</typeparam>
    [Serializable]
    public abstract class TimedDataSource<T> : ITimedDataSource
    {
        [SerializeField, HideInInspector]
        Object m_Object;
        [SerializeField, HideInInspector]
        string m_Guid;
        [SerializeField, HideInInspector]
        int m_BufferSize = 5;
        [SerializeField, HideInInspector]
        FrameTime m_Offset;
        [SerializeField]
        bool m_IsSynchronized;

        FrameRate m_FrameRate = StandardFrameRate.FPS_60_00;
        TimedDataBuffer<T> m_Buffer;
        IInterpolator<T> m_Interpolator;
        ISynchronizer m_Synchronizer;

        /// <inheritdoc/>
        string IRegistrable.Id => m_Guid;

        /// <inheritdoc/>
        string IRegistrable.FriendlyName => m_Object != null ? m_Object.name : GetType().Name;

        /// <inheritdoc/>
        public FrameRate FrameRate
        {
            get => m_FrameRate;
            set
            {
                if (m_FrameRate != value)
                {
                    m_FrameRate = value;
                    m_Buffer?.SetFrameRate(value);
                }
            }
        }

        /// <inheritdoc/>
        public int BufferSize
        {
            get => m_BufferSize;
            set
            {
                if (m_BufferSize != value)
                {
                    m_BufferSize = value;
                    m_Buffer?.SetCapacity(value);
                }
            }
        }

        /// <inheritdoc/>
        public virtual int? MaxBufferSize => null;

        /// <inheritdoc/>
        public virtual int? MinBufferSize => null;

        /// <inheritdoc/>
        public FrameTime Offset
        {
            get => m_Offset;
            set => m_Offset = value;
        }

        /// <inheritdoc/>
        public bool IsSynchronized
        {
            get => m_IsSynchronized;
            set => m_IsSynchronized = value;
        }

        /// <inheritdoc/>
        public ISynchronizer Synchronizer
        {
            get => m_Synchronizer;
            set => m_Synchronizer = value;
        }

        /// <inheritdoc />
        Object ITimedDataSource.UndoTarget => m_Object;

        /// <summary>
        /// The object that owns this data source.
        /// </summary>
        public Object SourceObject
        {
            get => m_Object;
            set => m_Object = value;
        }

        /// <summary>
        /// Is the data source ready to buffer samples.
        /// </summary>
        public bool Enabled => m_Buffer != null;

        /// <summary>
        /// An event invoked when this source presents a synchronized frame.
        /// </summary>
        /// <remarks>
        /// This will only invoked when <see cref="IsSynchronized"/> is <see langword="true"/>.
        /// * The first parameter is the sample value for the presented frame.
        /// * The second parameter is the sample time for the presented frame.
        /// </remarks>
        public event Action<T, FrameTimeWithRate> FramePresented;

        /// <summary>
        /// The interpolator to use when presenting values between frame samples.
        /// </summary>
        public IInterpolator<T> Interpolator
        {
            get => m_Interpolator;
            set
            {
                if (m_Interpolator != value)
                {
                    m_Interpolator = value;

                    if (m_Buffer != null)
                    {
                        m_Buffer.Interpolator = value;
                    }
                }
            }
        }

        /// <summary>
        /// Activates the data source.
        /// </summary>
        public virtual void Enable()
        {
            if (Enabled)
            {
                return;
            }

            TimedDataSourceManager.Instance.EnsureIdIsValid(ref m_Guid);
            TimedDataSourceManager.Instance.Register(this);

            m_Buffer = new TimedDataBuffer<T>(m_FrameRate, m_BufferSize)
            {
                Interpolator = Interpolator,
            };
        }

        /// <summary>
        /// Deactivates the data source.
        /// </summary>
        public virtual void Disable()
        {
            if (!Enabled)
            {
                return;
            }

            TimedDataSourceManager.Instance.Unregister(this);

            // clear so samples are discarded first and may be disposed if needed
            m_Buffer.Clear();
            m_Buffer = null;
        }

        /// <inheritdoc/>
        public bool TryGetBufferRange(out FrameTime oldestSample, out FrameTime newestSample)
        {
            if (m_Buffer != null && m_Buffer.TryGetBufferRange(out oldestSample, out newestSample))
            {
                oldestSample += m_Offset;
                newestSample += m_Offset;
                return true;
            }

            oldestSample = default;
            newestSample = default;
            return false;
        }

        /// <inheritdoc cref="TimedDataBuffer{T}.TryGetSample"/>
        public TimedSampleStatus TryGetSample(FrameTime frame, out T value)
        {
            if (m_Buffer != null)
            {
                return m_Buffer.TryGetSample(frame - m_Offset, out value);
            }

            value = default;
            return TimedSampleStatus.DataMissing;
        }

        /// <inheritdoc cref="TimedDataBuffer{T}.GetSamplesInRange"/>
        public IEnumerable<(FrameTime time, T value)> GetSamplesInRange(FrameTime from, FrameTime to)
        {
            if (m_Buffer != null)
            {
                foreach (var sample in m_Buffer.GetSamplesInRange(from - m_Offset, to - m_Offset))
                {
                    yield return (sample.time + m_Offset, sample.value);
                }
            }
        }

        /// <inheritdoc/>
        TimedSampleStatus ITimedDataSource.PresentAt(FrameTimeWithRate presentTime)
        {
            Debug.Assert(IsSynchronized, "Attempted to call PresentAt() when data source is not being synchronized.");

            var localPresentTime = presentTime.Remap(m_FrameRate);
            var status = TryGetSample(localPresentTime.Time, out var frame);

            if (status != TimedSampleStatus.DataMissing)
            {
                try
                {
                    FramePresented?.Invoke(frame, localPresentTime);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            return status;
        }

        /// <summary>
        /// Removes all frames from the data buffer.
        /// </summary>
        public void ClearBuffer()
        {
            m_Buffer?.Clear();
        }

        /// <summary>
        /// Adds a new data sample to the buffer.
        /// </summary>
        /// <param name="value">The sample value to add.</param>
        /// <param name="time">The time of the sample.</param>
        /// <seealso cref="AddSampleWithGeneratedTime"/>
        public void AddSample(in T value, in FrameTimeWithRate time)
        {
            m_Buffer?.Add(value, time);
        }

        /// <summary>
        /// Adds a new data sample to the buffer.
        /// </summary>
        /// <remarks>
        /// Use this when a device does not provide timecode information with the samples, and instead relies on genlock to
        /// facilitate synchronization. When this data source is assigned to a synchronizer, a timecode will be generated using
        /// the synchronizer's presentation time. This should usually be called once per frame. When called multiple times
        /// in a single frame, the new value will overwrite any previous values submitted for the current frame.
        /// </remarks>
        /// <param name="value">The sample value to add.</param>
        /// <returns>The generated sample time.</returns>
        /// <seealso cref="AddSample"/>
        public FrameTimeWithRate AddSampleWithGeneratedTime(in T value)
        {
            if (m_Buffer == null)
            {
                return default;
            }

            if (m_Synchronizer != null)
            {
                var presentTime = m_Synchronizer.PresentTime;

                if (presentTime != null && presentTime.Value.Rate == FrameRate)
                {
                    m_Buffer.Add(value, presentTime.Value);
                    return presentTime.Value;
                }
            }

            var generatedTime = GenerateFrameTime(m_FrameRate);
            m_Buffer.Add(value, generatedTime);
            return generatedTime;
        }

        /// <summary>
        /// Gets a time based on the current engine time.
        /// </summary>
        /// <param name="frameRate">The frame rate of the frame sequence.</param>
        /// <returns>The generated time.</returns>
        public static FrameTimeWithRate GenerateFrameTime(in FrameRate frameRate)
        {
            return FrameTimeWithRate.FromSeconds(frameRate, Time.unscaledTimeAsDouble);
        }
    }
}
