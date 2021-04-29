using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.LiveCapture
{
    [Serializable]
    class Synchronizer : ISynchronizer
    {
        [SerializeField]
        List<SourceAndStatusBundle> m_SourcesAndStatuses = new List<SourceAndStatusBundle>();

        [SerializeField]
        [FrameNumber]
        FrameTime m_GlobalTimeOffset;

        [SerializeField]
        TimecodeSourceRef m_TimecodeSourceRef;

        /// <inheritdoc/>
        public ITimecodeSource TimecodeSource
        {
            get => m_TimecodeSourceRef.Resolve();
            set => m_TimecodeSourceRef = new TimecodeSourceRef(value);
        }

        /// <summary>
        /// The offset, in frames, applied to the timecode used for synchronization updates.
        /// </summary>
        /// <remarks>
        /// Use a negative value (i.e. a delay) to compensate for high-latency sources.
        /// </remarks>
        public FrameTime GlobalTimeOffset
        {
            get => m_GlobalTimeOffset;
            set => m_GlobalTimeOffset = value;
        }

        public CalibrationStatus CalibrationStatus { get; private set; } = CalibrationStatus.Complete;

        /// <inheritdoc />
        public FrameRate? FrameRate
        {
            get
            {
                if (TimecodeSource == null)
                    return null;

                return TimecodeSource.FrameRate;
            }
        }

        /// <inheritdoc />
        public Timecode? CurrentTimecode
        {
            get
            {
                if (TimecodeSource == null)
                    return null;

                return TimecodeSource.Now.AddFrames(TimecodeSource.FrameRate, GlobalTimeOffset);
            }
        }

        /// <inheritdoc />
        public int DataSourceCount => m_SourcesAndStatuses.Count;

        /// <summary>
        /// Checks if the given timed data source is in the synchronization group.
        /// </summary>
        /// <param name="source">The timed data source to check.</param>
        /// <returns>
        /// <c>true</c> if the data source is already in the group; <c>false</c> otherwise.
        /// </returns>
        public bool ContainsDataSource(ITimedDataSource source)
        {
            for (var i = 0; i < m_SourcesAndStatuses.Count; i++)
            {
                if (m_SourcesAndStatuses[i].Source == source)
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public bool AddDataSource(ITimedDataSource source)
        {
            if (source.Synchronizer != null && source.Synchronizer != this)
            {
                return false;
            }

            source.Synchronizer = this;
            source.IsSynchronized = true;
            return m_SourcesAndStatuses.AddUnique(new SourceAndStatusBundle(source));
        }

        /// <inheritdoc />
        public void RemoveDataSource(ITimedDataSource source)
        {
            source.IsSynchronized = false;
            source.Synchronizer = null;
            m_SourcesAndStatuses.Remove(new SourceAndStatusBundle(source));
        }

        public void RemoveDataSource(int index)
        {
            if (GetDataSource(index) is { } source)
            {
                RemoveDataSource(source);
            }
            else
            {
                m_SourcesAndStatuses.RemoveAt(index);
            }
        }

        /// <inheritdoc />
        public ITimedDataSource GetDataSource(int index) =>
            index < DataSourceCount ? m_SourcesAndStatuses[index].Source : null;

        /// <summary>
        /// Get the sample status of the data source as of the last synchronization update.
        /// </summary>
        /// <param name="dataSourceIndex">The index of the timed data source.</param>
        /// <returns>The sample status; <c>null</c> if no status is available or no source exists at the index.</returns>
        public TimedSampleStatus? GetCurrentDataStatus(int dataSourceIndex) =>
            dataSourceIndex < DataSourceCount
            ? m_SourcesAndStatuses[dataSourceIndex].Status
            : default;

        internal SourceAndStatusBundle GetSourceAndStatus(int index) =>
            index < DataSourceCount ? m_SourcesAndStatuses[index] : default;

        /// <inheritdoc />
        public void Update()
        {
            RepairReferences();

            if (CalibrationStatus == CalibrationStatus.InProgress)
            {
                return;
            }

            var frameRate = FrameRate;
            var timecode = CurrentTimecode;

            if (frameRate == null || timecode == null)
            {
                return;
            }

            foreach (var item in m_SourcesAndStatuses)
            {
                item.PresentAt(timecode.Value, frameRate.Value);
            }
        }

        /// <summary>
        /// Temporarily halt performing synchronization updates.
        /// </summary>
        /// <remarks>
        /// Call this method to signal to each <see cref="ITimedDataSource"/> in the synchronization
        /// group to halt synchronization (and enable its own update mechanism if necessary).
        /// To resume, call <see cref="Update"/>.
        /// </remarks>
        public void Pause()
        {
            foreach (var item in m_SourcesAndStatuses)
            {
                item.Pause();
            }
        }

        /// <summary>
        /// Finds the best synchronization parameters for the current data sources.
        /// </summary>
        /// <param name="calibrator">The calibration method used for finding the optimal settings.</param>
        /// <returns>The current status of the calibration.</returns>
        public IEnumerator CalibrationWith(ISynchronizationCalibrator calibrator)
        {
            if (calibrator == null)
            {
                Debug.LogError("calibrator is null");
                yield break;
            }

            CalibrationStatus = CalibrationStatus.InProgress;
            var dataSources = Enumerable.Range(0, DataSourceCount)
                .Select(GetDataSource)
                .Where(source => source != null && source.IsSynchronized)
                .ToList();

            foreach (var result in calibrator.Execute(TimecodeSource, dataSources))
            {
                CalibrationStatus = result.Status;

                // The calibrator will make per-datasource adjustments
                // The Synchronizer just needs to care about the GlobalOffset
                GlobalTimeOffset = result.GlobalTimeOffset;
                yield return null;
            }
        }

        void RepairReferences()
        {
            // Hacky workaround for the fact that we can't reliably (de)serialize the
            // ITimedDataSource.Synchronizer property
            foreach (var item in m_SourcesAndStatuses)
            {
                var source = item.Source;
                if (source != null)
                {
                    source.Synchronizer = this;
                }
            }
        }

        [Serializable]
        internal class SourceAndStatusBundle : IEquatable<SourceAndStatusBundle>
        {
            [SerializeField]
            TimedDataSourceRef m_Source;
            [SerializeField]
            TimedSampleStatus m_Status;
            [SerializeField]
            bool m_SynchronizationRequested;

            public ITimedDataSource Source => m_Source.Resolve();

            public TimedSampleStatus Status
            {
                get => m_Status;
                private set => m_Status = value;
            }

            /// <summary>
            /// Tracks whether we want this source to be part of the synchronization group.
            /// </summary>
            /// <remarks>
            /// Call <see cref="PresentAt"/> to propagate this value to
            /// the underlying source to actually enable/disable synchronization for the source.
            /// For example, the <see cref="Synchronizer"/> may want to disable all synchronization
            /// temporarily, but we want to "remember" the user's selection.
            /// </remarks>
            public bool SynchronizationRequested
            {
                get => m_SynchronizationRequested;
                set
                {
                    if (m_SynchronizationRequested != value)
                    {
                        m_SynchronizationRequested = value;
                        m_Status = TimedSampleStatus.DataMissing;
                    }
                }
            }

            public SourceAndStatusBundle(ITimedDataSource source)
            {
                m_Source = new TimedDataSourceRef(source);
                m_Status = TimedSampleStatus.DataMissing;
                m_SynchronizationRequested = true;
            }

            public void Pause()
            {
                if (Source == null)
                    return;

                Source.IsSynchronized = false;
            }

            public void PresentAt(Timecode timecode, FrameRate frameRate)
            {
                if (Source == null)
                    return;

                Source.IsSynchronized = SynchronizationRequested;

                if (Source.IsSynchronized)
                {
                    Status = Source.PresentAt(timecode, frameRate);
                }
            }

            public bool Equals(SourceAndStatusBundle other)
            {
                if (other == null)
                    return false;

                return m_Source == other.m_Source;
            }
        }
    }
}
