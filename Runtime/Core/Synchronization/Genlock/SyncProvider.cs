using System;
using UnityEngine;

namespace Unity.LiveCapture
{
    /// <summary>
    /// A base class that includes functionality suitable for most <see cref="ISyncProvider"/> implementations.
    /// </summary>
    [Serializable]
    public abstract class SyncProvider : ISyncProvider
    {
        float? m_LastCaptureDeltaTime;

        /// <inheritdoc />
        public abstract string Name { get; }

        /// <inheritdoc />
        public abstract FrameRate SyncRate { get; }

        /// <inheritdoc />
        [field: NonSerialized]
        public SyncStatus Status { get; protected set; } = SyncStatus.Stopped;

        /// <inheritdoc />
        [field: NonSerialized]
        public int? LastPulseCountDelta { get; private set; }

        /// <inheritdoc />
        [field: NonSerialized]
        public int DroppedFrameCount { get; private set; }

        /// <summary>
        /// Determines if this synchronization provider can synchronize while the Editor is in edit mode.
        /// </summary>
        protected virtual bool RunInEditMode => false;

        /// <inheritdoc />
        void ISyncProvider.StartSynchronizing()
        {
            if ((RunInEditMode || Application.isPlaying) && Status == SyncStatus.Stopped)
            {
                Status = SyncStatus.Synchronized;
                LastPulseCountDelta = null;
                DroppedFrameCount = 0;
                m_LastCaptureDeltaTime = null;

                OnStart();
            }
        }

        /// <inheritdoc />
        void ISyncProvider.StopSynchronizing()
        {
            if (Status != SyncStatus.Stopped)
            {
                Status = SyncStatus.Stopped;
                LastPulseCountDelta = null;
                DroppedFrameCount = 0;
                if (m_LastCaptureDeltaTime != null)
                {
                    Time.captureDeltaTime = 0;
                    m_LastCaptureDeltaTime = null;
                }

                OnStop();
            }
        }

        /// <inheritdoc />
        bool ISyncProvider.WaitForNextPulse()
        {
            if (Status == SyncStatus.Stopped)
            {
                return false;
            }

            if (!CanSync() || !OnWaitForNextPulse(out var pulseCount))
            {
                Status = SyncStatus.NotSynchronized;
                LastPulseCountDelta = null;
                if (m_LastCaptureDeltaTime != null)
                {
                    Time.captureDeltaTime = 0;
                    m_LastCaptureDeltaTime = null;
                }
                return false;
            }

            Status = SyncStatus.Synchronized;

            // if multiple pulses have occurred since the last update, then at least one pulse was skipped over, meaning a
            // frame was dropped.
            if (pulseCount > 1)
            {
                var droppedFrames = pulseCount - 1;
                DroppedFrameCount += droppedFrames;
                Debug.LogWarning($"Dropped {droppedFrames} frame{(droppedFrames != 1 ? "s" : string.Empty)}.");
            }

            LastPulseCountDelta = pulseCount;

            // The engine time should progress based on the synchronization signal rate and pulse count, not the platform time.
            // This helps make synchronized data feeds have better temporal coherence with other engine systems. To achieve
            // this, you can use Time.captureDeltaTime to control how much Time.time increments the next frame. However,
            // other scripts may also use Time.captureDeltaTime, so you must be careful to ensure it doesn't unexpectedly change.
            var deltaTime = pulseCount * (float)SyncRate.FrameInterval;

            Time.captureDeltaTime = deltaTime;
            m_LastCaptureDeltaTime = deltaTime;
            return true;
        }

        bool CanSync()
        {
            if (m_LastCaptureDeltaTime.HasValue && m_LastCaptureDeltaTime.Value != Time.captureDeltaTime)
            {
                Debug.LogWarning($"The sync provider requires exclusive control of Time.captureDeltaTime, but it is being set elsewhere.");
                return false;
            }
            if (QualitySettings.vSyncCount != 0)
            {
                Debug.LogWarning($"A sync provider is used, but Vsync is enabled. Disable Vsync in the Quality Settings to enable synchronization.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Called when the sync provider becomes active.
        /// </summary>
        protected virtual void OnStart()
        {
            // ensures the player loop runs as fast as possible.
            Application.targetFrameRate = -1;
        }

        /// <summary>
        /// Called when the sync provider becomes inactive.
        /// </summary>
        protected virtual void OnStop() {}

        /// <summary>
        /// Blocks execution on the current thread until the next synchronization signal pulse is received.
        /// </summary>
        /// <param name="pulseCount">The number of synchronization signal pulses since this method was last called.</param>
        /// <returns><see langword="true"/> when a pulse was successfully received; otherwise, <see langword="false"/>.</returns>
        protected abstract bool OnWaitForNextPulse(out int pulseCount);
    }
}
