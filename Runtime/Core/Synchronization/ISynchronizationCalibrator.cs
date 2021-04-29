using System.Collections.Generic;
using UnityEngine;

namespace Unity.LiveCapture
{
    /// <summary>
    /// An enum defining the statuses of a calibration attempt.
    /// </summary>
    public enum CalibrationStatus
    {
        /// <summary>
        /// The calibrator is currently running.
        /// </summary>
        [InspectorName("In Progress")]
        InProgress,

        /// <summary>
        /// The calibrator completed successfully.
        /// </summary>
        [InspectorName("Complete")]
        Complete,

        /// <summary>
        /// The calibrator was cancelled before completion.
        /// </summary>
        [InspectorName("Incomplete")]
        Incomplete,
    }

    /// <summary>
    /// A struct containing the calibration parameters to apply.
    /// </summary>
    public readonly struct CalibrationResult
    {
        /// <summary>
        /// The status the calibration finished with.
        /// </summary>
        public readonly CalibrationStatus Status;

        /// <summary>
        /// The delay in frames to apply to the synchronizer timecode to compensate for data latency.
        /// </summary>
        public readonly FrameTime GlobalTimeOffset;

        /// <summary>
        /// The number of frames of data each source should be able to buffer to avoid dropping frames.
        /// </summary>
        public readonly int[] OptimalBufferSizes;

        /// <summary>
        /// Creates a new <see cref="CalibrationResult"/> instance.
        /// </summary>
        /// <param name="status">The status the calibration finished with.</param>
        /// <param name="globalTimeOffset">The delay in frames to apply to the synchronizer timecode to compensate for data latency.</param>
        /// <param name="optimalBufferSizes">The number of frames of data each source should be able to buffer to avoid dropping frames.</param>
        public CalibrationResult(CalibrationStatus status, FrameTime globalTimeOffset = default,
                                 int[] optimalBufferSizes = null)
        {
            Status = status;
            GlobalTimeOffset = globalTimeOffset;
            OptimalBufferSizes = optimalBufferSizes;
        }
    }

    /// <summary>
    /// The interface for synchronization calibration strategies.
    /// </summary>
    public interface ISynchronizationCalibrator
    {
        /// <summary>
        /// Perform calibration such that we have a reasonable guarantee that all timed data sources can be
        /// presented in a synchronized manner by a <see cref="Synchronizer"/>.
        /// </summary>
        /// <remarks>
        /// The calibrator should 1) determine a suitable global delay such that the data source with the
        /// highest latency will have time to receive the sample for a frame before it is asked to present it,
        /// and 2) attempt to adjust the buffers of each data source such that they will not have to drop samples
        /// they have not been presented yet due to full buffers.
        /// </remarks>
        /// <param name="timecodeSource">Timecode source used by the <see cref="Synchronizer"/>.</param>
        /// <param name="dataSources">The set of <see cref="ITimedDataSource"/> instances to be synchronized.</param>
        /// <returns>Calibration results are returned iteratively.</returns>
        IEnumerable<CalibrationResult> Execute(
            ITimecodeSource timecodeSource,
            IReadOnlyCollection<ITimedDataSource> dataSources);
    }
}
