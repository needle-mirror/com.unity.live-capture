using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.LiveCapture
{
    /// <summary>
    /// Basic synchronization calibrator suitable for low-latency and low-jitter scenarios.
    /// </summary>
    class DefaultSyncCalibrator : ISynchronizationCalibrator
    {
        /// <summary>
        /// Required number of consecutive successful synchronization updates for calibration to be accepted.
        /// </summary>
        public int RequiredGoodSamples { get; set; } = 60;

        public IEnumerable<CalibrationResult> Execute(ITimecodeSource timecodeSource,
            IReadOnlyCollection<ITimedDataSource> dataSources)
        {
            if (timecodeSource == null || dataSources == null)
            {
                Debug.LogWarning("Timecode source or data sources are null");
                yield return new CalibrationResult(CalibrationStatus.Incomplete);
                yield break;
            }

            // Step 1: Find the global delay necessary to deal with the highest-latency sources
            var globalOffset = new FrameTime();
            foreach (var offset in CalibrateDelayIterative(timecodeSource, dataSources, RequiredGoodSamples))
            {
                globalOffset = new FrameTime(offset);
                yield return new CalibrationResult(CalibrationStatus.InProgress, globalTimeOffset: globalOffset,
                    optimalBufferSizes: null);
            }

            // Step 2: Increase buffer sizes until we can sample from each source
            foreach (var status in CalibrateBufferSizesIterative(timecodeSource, dataSources, globalOffset,
                RequiredGoodSamples))
            {
                yield return new CalibrationResult(status, globalTimeOffset: globalOffset,
                    optimalBufferSizes: dataSources.Select(s => s.BufferSize).ToArray());
            }
        }

        static IEnumerable<int> CalibrateDelayIterative(
            ITimecodeSource timecodeSource,
            IReadOnlyCollection<ITimedDataSource> dataSources,
            int requiredGoodSamples)
        {
            var frameOffset = 0;
            var goodSamples = 0;
            while (goodSamples < requiredGoodSamples)
            {
                var currentTime = timecodeSource.Now.AddFrames(timecodeSource.FrameRate, frameOffset);
                var noExtraLatency = dataSources.All(s =>
                    s.PresentAt(currentTime, timecodeSource.FrameRate) != TimedSampleStatus.Behind);

                yield return frameOffset;

                if (noExtraLatency)
                {
                    ++goodSamples;
                }
                else
                {
                    --frameOffset;
                    goodSamples = 0;
                }
            }
        }

        static IEnumerable<CalibrationStatus> CalibrateBufferSizesIterative(
            ITimecodeSource timecodeSource,
            IReadOnlyCollection<ITimedDataSource> dataSources,
            FrameTime globalOffset,
            int requiredGoodSamples)
        {
            var frameRate = timecodeSource.FrameRate;

            var goodSamples = 0;
            while (goodSamples < requiredGoodSamples)
            {
                var timecode = timecodeSource.Now.AddFrames(frameRate, globalOffset);
                var fastSources = dataSources
                    .Where(s => s.PresentAt(timecode, frameRate) == TimedSampleStatus.Ahead)
                    .ToList();

                if (fastSources.Any())
                {
                    goodSamples = 0;

                    foreach (var fastSource in fastSources)
                    {
                        var requestedBufferSize = fastSource.BufferSize + 1;
                        if (requestedBufferSize <= (fastSource.MaxBufferSize ?? int.MaxValue))
                        {
                            fastSource.BufferSize = requestedBufferSize;
                        }
                        else
                        {
                            // Proper calibration not possible
                            yield return CalibrationStatus.Incomplete;
                            yield break;
                        }
                    }
                }
                else
                {
                    ++goodSamples;
                }

                yield return CalibrationStatus.InProgress;
            }

            yield return CalibrationStatus.Complete;
        }
    }
}
