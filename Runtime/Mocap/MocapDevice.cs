using System;
using UnityEngine;

namespace Unity.LiveCapture.Mocap
{
    interface IMocapDevice
    {
        Animator Animator { get; set; }
        bool TryGetMocapGroup(out MocapGroup mocapGroup);
        double GetCurrentFrameTime();
        void InvokeRecordingChanged();
        void RegisterLiveProperties();
        void RestoreLiveProperties();
    }

    /// <summary>
    /// A type of <see cref="LiveCaptureDevice"/> that provides the common functionality required to
    /// implement support for third-party motion capture devices.
    /// </summary>
    /// <typeparam name="T">The type of data the device uses each frame to pose the actor.</typeparam>
    public abstract class MocapDevice<T> : LiveCaptureDevice, IMocapDevice
    {
        [Serializable]
        class TimedDataSource : TimedDataSource<T>
        {
        }

        [SerializeField]
        Animator m_Animator;
        [SerializeField]
        MocapRecorder m_Recorder = new MocapRecorder();
        [SerializeField, HideInInspector]
        TimedDataSource m_SyncBuffer = new TimedDataSource();

        MocapGroup m_MocapGroup;
        double? m_FirstFrameTime;
        double m_CurrentFrameTime;
        bool m_IsRecording;
        bool m_AddingFrame;

        // Avoiding "The same field name is serialized multiple times in the class or its parent class."
        Transform m_CachedParent2;

        /// <summary>
        /// The Animator component this device operates.
        /// </summary>
        public Animator Animator
        {
            get => m_Animator;
            set
            {
                m_Animator = value;

                ValidateRecorder();
            }
        }

        /// <summary>
        /// The number of data samples per second.
        /// </summary>
        protected FrameRate FrameRate
        {
            get => m_SyncBuffer.FrameRate;
            set => m_SyncBuffer.FrameRate = value;
        }

        /// <summary>
        /// The interpolator to use when presenting values between frame samples.
        /// </summary>
        protected IInterpolator<T> Interpolator
        {
            get => m_SyncBuffer.Interpolator;
            set => m_SyncBuffer.Interpolator = value;
        }

        /// <summary>
        /// The synchronized data buffer.
        /// </summary>
        public ITimedDataSource SyncBuffer => m_SyncBuffer;

        /// <summary>
        /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
        /// </summary>
        /// <remarks>
        /// You would usually use this to perform an action after a value changes in the Inspector;
        /// for example, making sure that data stays within a certain range.
        /// </remarks>
        public virtual void OnValidate()
        {
            m_SyncBuffer.SourceObject = this;

            ValidateRecorder();
        }

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        protected virtual void OnEnable()
        {
            m_SyncBuffer.FramePresented += PresentAt;
            m_SyncBuffer.Enable();

            RegisterLiveProperties();
        }

        /// <summary>
        /// This function is called when the behaviour becomes disabled.
        /// </summary>
        /// <remaks>
        /// This is also called when the object is destroyed and can be used for any cleanup code.
        /// When scripts are reloaded after compilation has finished, OnDisable will be called, followed by an OnEnable after the script has been loaded.
        /// </remaks>
        protected virtual void OnDisable()
        {
            m_SyncBuffer.Disable();
            m_SyncBuffer.FramePresented -= PresentAt;

            RestoreLiveProperties();
        }

        /// <inheritdoc/>
        public sealed override bool IsRecording()
        {
            if (m_MocapGroup != null)
            {
                return m_MocapGroup.IsRecording();
            }

            return m_IsRecording;
        }

        /// <inheritdoc/>
        public sealed override void StartRecording()
        {
            if (m_MocapGroup != null)
            {
                return;
            }

            if (!IsRecording())
            {
                m_IsRecording = true;
                m_FirstFrameTime = null;
                m_Recorder.FrameRate = GetTakeRecorder().FrameRate;
                m_Recorder.Clear();

                OnRecordingChanged();
            }
        }

        /// <inheritdoc/>
        public sealed override void StopRecording()
        {
            if (m_MocapGroup != null)
            {
                return;
            }

            if (IsRecording())
            {
                m_IsRecording = false;

                OnRecordingChanged();
            }
        }

        /// <inheritdoc/>
        public override void LiveUpdate()
        {
            if (m_MocapGroup == null)
            {
                m_Recorder.ApplyFrame();
            }
        }

        /// <summary>
        /// Process a new frame of data.
        /// </summary>
        /// <param name="frame">The frame to add.</param>
        /// <param name="frameTime">The timecode of the frame. When <see langword="null"/>, a timecode will be generated.</param>
        protected void AddFrame(T frame, FrameTimeWithRate? frameTime)
        {
            // Guard re-entry. We are calling ProcessFrame, from which the user could attempt
            // to call AddFrame again, entering in an infinite loop.
            if (m_AddingFrame)
            {
                return;
            }

            if (m_SyncBuffer.IsSynchronized)
            {
                if (frameTime == null)
                {
                    m_SyncBuffer.AddSampleWithGeneratedTime(frame);
                }
                else
                {
                    m_SyncBuffer.AddSample(frame, frameTime.Value);
                }
            }
            else
            {
                PresentAt(frame, frameTime ?? TimedDataSource.GenerateFrameTime(m_SyncBuffer.FrameRate));
            }
        }

        /// <summary>
        /// Override this method to process the specified frame.
        /// </summary>
        /// <param name="frame">The frame to add.</param>
        protected abstract void ProcessFrame(T frame);

        /// <summary>
        /// Sets the position, rotation and scale of a specified transform.
        /// </summary>
        /// <remarks>
        /// The values are not immediately applied and might change due to other devices operating the same
        /// transform.
        /// </remarks>
        /// <param name="transform">The transform to present the values to.</param>
        /// <param name="position">The position to set.</param>
        /// <param name="rotation">The rotation to set.</param>
        /// <param name="scale">The scale to set.</param>
        protected void Present(Transform transform, Vector3? position, Quaternion? rotation, Vector3? scale)
        {
            ValidateRecorder();

            m_Recorder.Present(transform, position, rotation, scale);

            if (m_MocapGroup != null)
            {
                var mocapGroupRecorder = m_MocapGroup.GetRecorder();
                var channels = m_Recorder.GetChannels(transform);

                mocapGroupRecorder.SetChannels(transform, channels);
                mocapGroupRecorder.Present(transform, position, rotation, scale);
            }
        }

        /// <summary>
        /// Clears all frames in the synchronization buffer.
        /// </summary>
        protected void ResetSyncBuffer()
        {
            m_SyncBuffer.ClearBuffer();
        }

        /// <inheritdoc/>
        public override void Write(ITakeBuilder takeBuilder)
        {
            if (m_MocapGroup != null)
            {
                return;
            }

            if (Animator == null || m_Recorder.IsEmpty())
                return;

            takeBuilder.CreateAnimationTrack(
                Animator.name,
                Animator,
                m_Recorder.Bake(),
                alignTime: m_FirstFrameTime);
        }

        void PresentAt(T frame, FrameTimeWithRate time)
        {
            if (m_AddingFrame)
            {
                return;
            }

            m_AddingFrame = true;

            try
            {
                ProcessFrame(frame);
                UpdateRecorder(time);

                m_CurrentFrameTime = time.ToSeconds();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                m_AddingFrame = false;
            }
        }

        void UpdateRecorder(in FrameTimeWithRate frameTime)
        {
            if (IsRecording() && m_MocapGroup == null)
            {
                var time = frameTime.ToSeconds();

                m_FirstFrameTime ??= time;
                m_Recorder.Record(time - m_FirstFrameTime.Value);
            }
        }

        /// <summary>
        /// Tries to retrieve the mocap group this device belongs to.
        /// </summary>
        /// <param name="mocapGroup">Returns the mocap group this device belongs to.</param>
        /// <returns>True if the mocap group exists.</returns>
        bool IMocapDevice.TryGetMocapGroup(out MocapGroup mocapGroup)
        {
            mocapGroup = m_MocapGroup;

            return m_MocapGroup != null;
        }

        /// <summary>
        /// Gets the current frame time.
        /// </summary>
        /// <returns>The current frame time</returns>
        double IMocapDevice.GetCurrentFrameTime()
        {
            return m_CurrentFrameTime;
        }

        /// <summary>
        /// Invokes the delegate that handles a recording state change.
        /// </summary>
        void IMocapDevice.InvokeRecordingChanged()
        {
            OnRecordingChanged();
        }

        /// <summary>
        /// Registers the animated transforms to prevent Unity from marking Prefabs or the Scene
        /// as modified when you preview animations.
        /// </summary>
        public void RegisterLiveProperties()
        {
            m_Recorder.RegisterLiveProperties(this);
        }

        /// <summary>
        /// Restores the transforms previously registered.
        /// </summary>
        public void RestoreLiveProperties()
        {
            m_Recorder.RestoreLiveProperties();
        }

        /// <summary>
        /// The device calls this method when the recording state changes.
        /// </summary>
        protected virtual void OnRecordingChanged() {}

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            base.OnDestroy();

            Unregister();
        }

        void ValidateRecorder()
        {
            m_Recorder.Animator = m_Animator;
        }

        internal MocapRecorder GetRecorder()
        {
            return m_Recorder;
        }

        internal override void Update()
        {
            base.Update();

            Validate();
        }

        internal void Validate()
        {
            var parent = transform.parent;

            if (m_CachedParent2 != parent)
            {
                Unregister();

                if (parent != null)
                    m_MocapGroup = parent.GetComponent<MocapGroup>();
                else
                    m_MocapGroup = null;

                Register();

                m_CachedParent2 = parent;
            }
        }

        void Register()
        {
            if (m_MocapGroup != null)
            {
                m_MocapGroup.AddDevice(this);
            }
        }

        void Unregister()
        {
            if (m_MocapGroup != null)
            {
                m_MocapGroup.RemoveDevice(this);
                m_MocapGroup = null;
            }
        }
    }
}
