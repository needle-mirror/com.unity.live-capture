using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Unity.LiveCapture.CompanionApp
{
    /// <summary>
    /// A type of <see cref="LiveCaptureDevice"/> that uses a <see cref="ICompanionAppClient"/> for communication.
    /// </summary>
    public interface ICompanionAppDevice
    {
        /// <summary>
        /// Clears the client assigned to this device.
        /// </summary>
        void ClearClient();
    }

    /// <summary>
    /// A type of <see cref="LiveCaptureDevice"/> that uses a <see cref="ICompanionAppClient"/> for communication.
    /// </summary>
    /// <typeparam name="TClient">The type of client this device communicates with.</typeparam>
    public abstract class CompanionAppDevice<TClient> : LiveCaptureDevice, ICompanionAppDevice
        where TClient : class, ICompanionAppClient
    {
        [SerializeField, HideInInspector]
        string m_AssignedClientName;
        [SerializeField, HideInInspector]
        DateTime m_AssignedClientTime;
        [SerializeField, HideInInspector]
        bool m_Live;
        bool m_Recording;
        TClient m_Client;
        SlateChangeTracker m_SlateChangeTracker = new SlateChangeTracker();

        /// <inheritdoc/>
        protected virtual void OnEnable()
        {
            CompanionAppServer.clientDisconnected += OnClientDisconnected;

            if (m_Client == null)
            {
                CompanionAppServer.RegisterClientConnectHandler(OnClientConnected, m_AssignedClientName, m_AssignedClientTime);
            }

            if (m_Client != null)
            {
                OnClientAssigned();
            }
        }

        /// <inheritdoc/>
        protected virtual void OnDisable()
        {
            CompanionAppServer.clientDisconnected -= OnClientDisconnected;
            CompanionAppServer.DeregisterClientConnectHandler(OnClientConnected);

            if (m_Client != null)
            {
                OnClientUnassigned();
            }
        }

        /// <inheritdoc/>
        protected virtual void OnDestroy()
        {
            if (m_Client != null)
            {
                ClientMappingDatabase.DeregisterClientAssociation(m_Client);
            }
        }

        /// <inheritdoc/>
        public override bool IsLive()
        {
            return m_Live;
        }

        /// <inheritdoc/>
        public override void SetLive(bool value)
        {
            if (m_Live != value)
            {
                m_Live = value;
                OnLiveModeChanged();
                SendDeviceState();
            }
        }

        /// <inheritdoc/>
        public override bool IsRecording()
        {
            return m_Recording;
        }

        /// <inheritdoc/>
        public override void StartRecording()
        {
            if (!m_Recording)
            {
                m_Recording = true;

                OnRecordingChanged();
                SendRecordingState();
            }
        }

        /// <inheritdoc/>
        public override void StopRecording()
        {
            if (m_Recording)
            {
                m_Recording = false;

                OnRecordingChanged();
                SendRecordingState();
            }
        }

        /// <summary>
        /// Gets the client currently assigned to this device.
        /// </summary>
        /// <returns>The assigned client, or null if none is assigned.</returns>
        public TClient GetClient()
        {
            return m_Client;
        }

        /// <summary>
        /// Assigns a client to this device.
        /// </summary>
        /// <param name="client">The client to assign, or null to clear the assigned client.</param>
        /// <param name="rememberAssignment">Try to auto-assign the client to this device when it reconnects in the future.</param>
        public void SetClient(TClient client, bool rememberAssignment)
        {
            if (m_Client != client)
            {
                if (m_Client != null)
                {
                    SetLive(false);
                    OnClientUnassigned();

                    m_Client.setDeviceMode -= ClientSetDeviceMode;
                    m_Client.startRecording -= ClientStartRecording;
                    m_Client.stopRecording -= ClientStopRecording;
                    m_Client.startPlayer -= ClientStartPlayer;
                    m_Client.stopPlayer -= ClientStopPlayer;
                    m_Client.pausePlayer -= ClientPausePlayer;
                    m_Client.setPlayerTime -= ClientSetPlayerTime;
                    m_Client.setSelectedTake -= ClientSetSelectedTake;

                    ClientMappingDatabase.DeregisterClientAssociation(m_Client);
                }

                m_Client = client;

                if (m_Client != null)
                {
                    // if any device is also using this client, we must clear the client from the previous device.
                    if (ClientMappingDatabase.TryGetDevice(client, out var previousDevice))
                    {
                        previousDevice.ClearClient();
                    }

                    ClientMappingDatabase.RegisterClientAssociation(m_Client, this);

                    m_Client.setDeviceMode += ClientSetDeviceMode;
                    m_Client.startRecording += ClientStartRecording;
                    m_Client.stopRecording += ClientStopRecording;
                    m_Client.startPlayer += ClientStartPlayer;
                    m_Client.stopPlayer += ClientStopPlayer;
                    m_Client.pausePlayer += ClientPausePlayer;
                    m_Client.setPlayerTime += ClientSetPlayerTime;
                    m_Client.setSelectedTake += ClientSetSelectedTake;

                    m_SlateChangeTracker.Reset();
                    SendRecordingState();

                    OnClientAssigned();
                    UpdateClient();
                    SetLive(true);
                }

                if (rememberAssignment)
                {
                    m_AssignedClientName = m_Client != null ? m_Client.name : string.Empty;
                    m_AssignedClientTime = DateTime.Now;

#if UNITY_EDITOR
                    EditorUtility.SetDirty(this);
#endif
                }

                if (isActiveAndEnabled && m_Client == null)
                {
                    CompanionAppServer.RegisterClientConnectHandler(OnClientConnected, m_AssignedClientName, m_AssignedClientTime);
                }
                else
                {
                    CompanionAppServer.DeregisterClientConnectHandler(OnClientConnected);
                }
            }
        }

        /// <inheritdoc />
        public void ClearClient()
        {
            SetClient(null, true);
        }

        /// <summary>
        /// Called to send the device state to the client.
        /// </summary>
        public virtual void UpdateClient()
        {
            var takeRecorder = GetTakeRecorder();

            if (takeRecorder != null)
            {
                SendPlayerState(takeRecorder.IsPreviewPlaying(), takeRecorder.slate);
                SendDeviceState(takeRecorder.IsLive());

                if (m_SlateChangeTracker.Changed(takeRecorder.slate))
                {
                    SendSlateDescriptor(takeRecorder.slate);
                }
            }
        }

        /// <summary>
        /// The device calls this method when a new client is assigned.
        /// </summary>
        protected virtual void OnClientAssigned() {}

        /// <summary>
        /// The device calls this method when the client is unassigned.
        /// </summary>
        protected virtual void OnClientUnassigned() {}

        /// <summary>
        /// The device calls this method when the recording state has changed.
        /// </summary>
        protected virtual void OnRecordingChanged() {}

        /// <summary>
        /// The device calls this method when the live state has changed.
        /// </summary>
        protected virtual void OnLiveModeChanged() {}

        void ClientStartRecording()
        {
            var takeRecorder = GetTakeRecorder();

            if (takeRecorder != null)
            {
                takeRecorder.StartRecording();
            }

            Refresh();
        }

        void ClientStopRecording()
        {
            var takeRecorder = GetTakeRecorder();

            if (takeRecorder != null)
            {
                takeRecorder.StopRecording();
            }

            Refresh();
        }

        void ClientSetDeviceMode(DeviceMode deviceMode)
        {
            var takeRecorder = GetTakeRecorder();

            if (takeRecorder != null)
            {
                takeRecorder.SetLive(deviceMode == DeviceMode.LiveStream);

                SendDeviceState(takeRecorder.IsLive());
            }
        }

        void ClientStartPlayer()
        {
            var takeRecorder = GetTakeRecorder();

            if (takeRecorder != null)
            {
                takeRecorder.PlayPreview();
            }

            Refresh();
        }

        void ClientStopPlayer()
        {
            var takeRecorder = GetTakeRecorder();

            if (takeRecorder != null)
            {
                takeRecorder.PausePreview();
                takeRecorder.SetPreviewTime(0d);
            }

            Refresh();
        }

        void ClientPausePlayer()
        {
            var takeRecorder = GetTakeRecorder();

            if (takeRecorder != null)
            {
                takeRecorder.PausePreview();
            }

            Refresh();
        }

        void ClientSetPlayerTime(double time)
        {
            var takeRecorder = GetTakeRecorder();

            if (takeRecorder != null)
            {
                takeRecorder.SetPreviewTime(time);
            }

            Refresh();
        }

        void SendDeviceState()
        {
            var takeRecorder = GetTakeRecorder();

            if (takeRecorder != null)
            {
                SendDeviceState(takeRecorder.IsLive());
            }
        }

        void SendDeviceState(bool isLive)
        {
            if (m_Client != null)
            {
                m_Client.SendDeviceMode(isLive ? DeviceMode.LiveStream : DeviceMode.Playback);
            }
        }

        void SendRecordingState()
        {
            if (m_Client != null)
            {
                m_Client.SendRecordingState(IsRecording());
            }
        }

        void SendPlayerState(bool isPlaying, ISlate slate)
        {
            Debug.Assert(slate != null);

            var time = slate.time;
            var duration = slate.duration;
            var take = slate.take;

            if (m_Client != null)
            {
                m_Client.SendPlayerState(new PlayerState
                {
                    playing = isPlaying,
                    time = time,
                    duration = duration,
                    hasTimeline = take != null,
                });
            }
        }

        void SendSlateDescriptor(ISlate slate)
        {
            Debug.Assert(slate != null);

            var client = GetClient();

            if (client != null)
            {
                client.SendSlateDescriptor(SlateDescriptor.Create(slate));
            }
        }

        void ClientSetSelectedTake(SerializableGuid guid)
        {
#if UNITY_EDITOR
            var takeRecorder = GetTakeRecorder();

            if (takeRecorder != null)
            {
                var slate = takeRecorder.slate;
                var take = AssetDatabaseUtility.LoadAssetWithGuid<Take>(guid.ToString());

                slate.take = take;

                Refresh();
            }
#endif
        }

        bool OnClientConnected(ICompanionAppClient client)
        {
            if (m_Client == null && client is TClient c && (string.IsNullOrEmpty(m_AssignedClientName) || client.name == m_AssignedClientName))
            {
                SetClient(c, false);
                return true;
            }
            return false;
        }

        void OnClientDisconnected(ICompanionAppClient client)
        {
            if (m_Client == client)
            {
                SetClient(null, false);
            }
        }
    }
}
