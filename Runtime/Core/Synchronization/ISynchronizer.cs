namespace Unity.LiveCapture
{
    /// <summary>
    /// Manages a collection of <see cref="ITimedDataSource"/> instances for the purpose of
    /// coordinating their respective data to be presented in a temporally
    /// coherent manner.
    /// </summary>
    public interface ISynchronizer
    {
        /// <summary>
        /// The source of the timecode to synchronize to.
        /// </summary>
        ITimecodeSource TimecodeSource { get; set; }

        /// <summary>
        /// The frequency of synchronized updates.
        /// </summary>
        /// <remarks>
        /// This is controlled by the assigned <see cref="TimecodeSource"/>.
        /// If no <see cref="TimecodeSource"/> is set, the value is <see langword="null"/>.
        /// </remarks>
        FrameRate? FrameRate { get; }

        /// <summary>
        /// The timecode for the current frame to synchronize to.
        /// </summary>
        /// <remarks>
        /// This is controlled by the assigned <see cref="TimecodeSource"/>.
        /// If no <see cref="TimecodeSource"/> is set, the value is <see langword="null"/>.
        /// </remarks>
        Timecode? CurrentTimecode { get; }

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
        /// Perform synchronized update on synchronized group.
        /// </summary>
        void Update();
    }
}
