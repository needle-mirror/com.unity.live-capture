using UnityEngine;

namespace Unity.LiveCapture
{
    /// <summary>
    /// The base class for implementing a capture device. It provides a mechanism to record live
    /// data from a source and preview it in the scene.
    /// </summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public abstract class LiveCaptureDevice : MonoBehaviour
    {
        TakeRecorder m_TakeRecorder;
        ITakeRecorder m_TakeRecorderOverride;
        Transform m_CachedParent;

        /// <summary>
        /// Gets the <see cref="ITakeRecorder"/> that operates this device.
        /// </summary>
        /// <returns>
        /// The take recorder object reference.
        /// </returns>
        public ITakeRecorder GetTakeRecorder()
        {
            if (m_TakeRecorderOverride != null)
            {
                return m_TakeRecorderOverride;
            }

            Validate();

            return m_TakeRecorder;
        }

        // Used for testing purposes.
        internal void SetTakeRecorderOverride(ITakeRecorder takeRecorder)
        {
            m_TakeRecorderOverride = takeRecorder;
        }

        /// <summary>
        /// The device calls this method when the device is about to get destroyed.
        /// </summary>
        /// <remarks>
        /// If you override this method, call the base method in your implementation.
        /// </remarks>
        protected virtual void OnDestroy()
        {
            Unregister();
        }

        /// <summary>
        /// Updates the internal state of the device.
        /// </summary>
        public abstract void UpdateDevice();

        /// <summary>
        /// Override this method to update the device during live mode.
        /// </summary>
        /// <remarks>
        /// This method is called after the animation system execution and before the script's LateUpdate.
        /// </remarks>
        public abstract void LiveUpdate();

        /// <summary>
        /// Indicates whether a device is ready for recording.
        /// </summary>
        /// <returns>
        /// true if ready for recording; otherwise, false.
        /// </returns>
        public abstract bool IsReady();

        /// <summary>
        /// Checks if the device has started recording.
        /// </summary>
        /// <returns>
        /// true if the recording has started; otherwise, false.
        /// </returns>
        public abstract bool IsRecording();

        /// <summary>
        /// Starts a new recording.
        /// </summary>
        public abstract void StartRecording();

        /// <summary>
        /// Stops the current recording.
        /// </summary>
        public abstract void StopRecording();

        /// <summary>
        /// Stores the recording into a take using a <see cref="ITakeBuilder"/>.
        /// </summary>
        /// <param name="takeBuilder">The take builder object.</param>
        public abstract void Write(ITakeBuilder takeBuilder);

        /// <summary>
        /// Updates the scene during edit mode.
        /// </summary>
        /// <remarks>
        /// Call this method every time your device changes the scene. This method is
        /// only effective during edit mode.
        /// </remarks>
        protected void Refresh()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            }
#endif
        }

        internal virtual void Update()
        {
            Validate();
        }

        void Validate()
        {
            var parent = transform.parent;

            if (m_CachedParent != parent)
            {
                Unregister();

                if (parent != null)
                {
                    m_TakeRecorder = parent.GetComponent<TakeRecorder>();
                }
                else
                {
                    m_TakeRecorder = null;
                }

                Register();

                m_CachedParent = parent;
            }
        }

        void Register()
        {
            if (m_TakeRecorder != null)
            {
                m_TakeRecorder.AddDevice(this);
            }
        }

        void Unregister()
        {
            if (m_TakeRecorder != null)
            {
                m_TakeRecorder.RemoveDevice(this);
                m_TakeRecorder = null;
                m_CachedParent = null;
            }
        }
    }
}
