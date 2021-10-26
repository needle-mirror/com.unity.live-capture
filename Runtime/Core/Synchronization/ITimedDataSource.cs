using UnityEngine;

namespace Unity.LiveCapture
{
    /// <summary>
    /// Describes the ability for a <see cref="ITimedDataSource"/> to produce a data sample at a specific timecode.
    /// </summary>
    public enum TimedSampleStatus
    {
        /// <summary>
        /// Data is available at the specified timecode.
        /// </summary>
        [InspectorName("OK")]
        Ok,

        /// <summary>
        /// The data in the buffer is too old (i.e. latency is too high).
        /// </summary>
        [InspectorName("Behind")]
        Behind,

        /// <summary>
        /// The data in the buffer is too new (i.e. synchronization updates are happening too late, buffer is too small).
        /// </summary>
        [InspectorName("Ahead")]
        Ahead,

        /// <summary>
        /// There is no data available.
        /// </summary>
        [InspectorName("Data missing")]
        DataMissing
    }

    /// <summary>
    /// An interface for a collection of timecoded data samples. Represents a data source
    /// that can be synchronized.
    /// </summary>
    public interface ITimedDataSource : IRegistrable
    {
        /// <summary>
        /// Gets or sets the <see cref="ISynchronizer"/> controlling this source.
        /// </summary>
        /// <remarks>
        /// In most cases you can simply implement this
        /// as an auto-property. The default <see cref="Unity.LiveCapture.Synchronizer"/> will automatically
        /// assign this property when you call <see cref="Unity.LiveCapture.Synchronizer.AddDataSource"/>.
        /// </remarks>
        ISynchronizer Synchronizer { get; set; }

        /// <summary>
        /// Gets or sets the current buffer size.
        /// </summary>
        int BufferSize { get; set; }

        /// <summary>
        /// Get the maximum size of the sample buffer, if limited.
        /// </summary>
        int? MaxBufferSize { get; }

        /// <summary>
        /// Get the minimum size of the sample buffer, if limited.
        /// </summary>
        int? MinBufferSize { get; }

        /// <summary>
        /// Apply this constant frame offset when invoking <see cref="PresentAt"/>.
        /// </summary>
        /// <remarks>
        /// Set this to non-zero if the captured data's timecodes are "off" from the true values.
        /// For example, if you determined that the samples and timecodes as mismatched such that
        /// each sample lags its timecode by 2 frames, you would set this property to
        /// <c>new FrameTime(2)</c>.
        /// </remarks>
        FrameTime PresentationOffset { get; set; }

        /// <summary>
        /// Signal to the data source whether it is being synchronized via <see cref="PresentAt"/>.
        /// </summary>
        /// <remarks>
        /// When <c>true</c>, the data source should disable its own update mechanism.
        /// </remarks>
        bool IsSynchronized { get; set; }

        /// <summary>
        /// Set the currently active timecode for presentation.
        /// </summary>
        /// <param name="timecode">The timecode that we wish to present</param>
        /// <param name="frameRate">The underlying framerate of the timecode value</param>
        /// <returns>A status representing the ability of the source to present at the requested timecode.</returns>
        TimedSampleStatus PresentAt(Timecode timecode, FrameRate frameRate);
    }
}
