using System.Collections;

namespace Unity.LiveCapture
{
    /// <summary>
    /// Manages a collection of <see cref="ITimedDataSource"/>s for the purpose of
    /// coordinating their respective data to be presented in a temporally
    /// coherent manner.
    /// </summary>
    public interface ISynchronizer
    {
        /// <summary>
        /// The offset in frames applied to the timecode used for synchronization updates.
        /// </summary>
        /// <remarks>
        /// Set this to be negative (i.e. a delay) to compensate for high-latency sources.
        /// </remarks>
        FrameTime GlobalTimeOffset { get; set; }

        /// <summary>
        /// Get the frequency of synchronized frames.
        /// </summary>
        FrameRate FrameRate { get; }

        /// <summary>
        /// The timecode used for the most recent synchronized update.
        /// </summary>
        Timecode CurrentTimecode { get; }

        /// <summary>
        /// Get the number of timed data sources in the synchronization group.
        /// </summary>
        int DataSourceCount { get; }

        /// <summary>
        /// Add a timed data source to the synchronization group.
        /// </summary>
        /// <param name="source">The timed data source to add.</param>
        /// <returns>
        /// <c>true</c> if added successfully; <c>false</c> if data source is already in the group.
        /// </returns>
        /// <remarks>
        /// <c>source</c> must derive from <see cref="UnityEngine.Object"/>.
        /// </remarks>
        bool AddDataSource(ITimedDataSource source);

        /// <summary>
        /// Remove a timed data source from the synchronization group.
        /// </summary>
        /// <param name="source">The timed data source to remove.</param>
        void RemoveDataSource(ITimedDataSource source);

        /// <summary>
        /// Get the timed data source at the specified index.
        /// </summary>
        /// <param name="index">The index of the timed data source.</param>
        /// <returns>The timed data source at the index; <c>null</c> if no source exists at the index.</returns>
        ITimedDataSource GetDataSource(int index);

        /// <summary>
        /// Get the sample status of the data source as of the last synchronization update.
        /// </summary>
        /// <param name="dataSourceIndex">The index of the timed data source.</param>
        /// <returns>The sample status; <c>null</c> if no status is available or no source exists at the index.</returns>
        TimedSampleStatus? GetCurrentDataStatus(int dataSourceIndex);

        /// <summary>
        /// Perform synchronized update on synchronized group.
        /// </summary>
        void Update();

        /// <summary>
        /// Find the best synchronization parameters for the current source latencies.
        /// </summary>
        /// <param name="calibrator">The calibration method used for finding the optimal settings.</param>
        /// <returns>The current status of the calibration.</returns>
        IEnumerator CalibrationWith(ISynchronizationCalibrator calibrator);
    }
}
