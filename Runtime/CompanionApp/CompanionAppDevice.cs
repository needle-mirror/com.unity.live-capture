using System;
using UnityEngine;

namespace Unity.LiveCapture.CompanionApp
{
    /// <summary>
    /// A type of <see cref="LiveCaptureDevice"/> that uses a <see cref="ICompanionAppClient"/> for communication.
    /// </summary>
    interface ICompanionAppDevice
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
        bool m_ClientRegistered;
        bool m_Recording;
        TClient m_Client;
        readonly SlateChangeTracker m_SlateChangeTracker = new SlateChangeTracker();
        readonly TakeNameFormatter m_TakeNameFormatter = new TakeNameFormatter();
        string m_LastAssetName;
        PropertyPreviewer m_Previewer;

        bool TryGetInternalClient(out ICompanionAppClientInternal client)
        {
            client = m_Client as ICompanionAppClientInternal;

            return client != null;
        }

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        protected virtual void OnEnable()
        {
            CompanionAppServer.ClientDisconnected += OnClientDisconnected;

            RegisterClient();
        }

        /// <summary>
        /// This function is called when the behaviour becomes disabled.
        /// </summary>
        /// <remaks>
        /// This is also called when the object is destroyed and can be used for any cleanup code.
        ///  When scripts are reloaded after compilation has finished, OnDisable will be called, followed by an OnEnable after the script has been loaded.
        /// </remaks>
        protected virtual void OnDisable()
        {
            CompanionAppServer.ClientDisconnected -= OnClientDisconnected;
            CompanionAppServer.DeregisterClientConnectHandler(OnClientConnected);

            StopRecording();
            UnregisterClient();
        }

        /// <summary>
        /// This function is called when the behaviour gets destroyed.
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();

            ClearClient();
        }

        /// <inheritdoc/>
        public override bool IsReady()
        {
            return m_Client != null;
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
                UnregisterClient();

                if (m_Client != null)
                {
                    ClientMappingDatabase.DeregisterClientAssociation(this, m_Client, rememberAssignment);
                }

                m_Client = client;

                if (m_Client != null)
                {
                    // if any device is also using this client, we must clear the client from the previous device.
                    if (ClientMappingDatabase.TryGetDevice(client, out var previousDevice))
                    {
                        previousDevice.ClearClient();
                    }

                    ClientMappingDatabase.RegisterClientAssociation(this, m_Client, rememberAssignment);
                }

                RegisterClient();
            }
        }

        void RegisterClient()
        {
            if (!isActiveAndEnabled || m_ClientRegistered)
            {
                return;
            }

            CompanionAppServer.DeregisterClientConnectHandler(OnClientConnected);

            m_SlateChangeTracker.Reset();

            if (TryGetInternalClient(out var client))
            {
                client.SetDeviceMode += ClientSetDeviceMode;
                client.StartRecording += ClientStartRecording;
                client.StopRecording += ClientStopRecording;
                client.StartPlayer += ClientStartPlayer;
                client.StopPlayer += ClientStopPlayer;
                client.PausePlayer += ClientPausePlayer;
                client.SetPlayerTime += ClientSetPlayerTime;
                client.SetSelectedTake += ClientSetSelectedTake;
                client.SetTakeData += ClientSetTakeData;
                client.DeleteTake += ClientDeleteTake;
                client.SetIterationBase += ClientSetIterationBase;
                client.ClearIterationBase += ClientClearIterationBase;
                client.TexturePreviewRequested += OnTexturePreviewRequested;

                RegisterLiveProperties();
                OnClientAssigned();

                client.SendInitialize();

                UpdateClient();

                m_ClientRegistered = true;
            }
            else
            {
                ClientMappingDatabase.TryGetClientAssignment(this, out var clientName, out var time);
                CompanionAppServer.RegisterClientConnectHandler(OnClientConnected, clientName, time);
            }
        }

        void UnregisterClient()
        {
            if (!m_ClientRegistered)
            {
                return;
            }

            if (TryGetInternalClient(out var client))
            {
                OnClientUnassigned();
                RestoreLiveProperties();

                client.SendEndSession();
                client.SetDeviceMode -= ClientSetDeviceMode;
                client.StartRecording -= ClientStartRecording;
                client.StopRecording -= ClientStopRecording;
                client.StartPlayer -= ClientStartPlayer;
                client.StopPlayer -= ClientStopPlayer;
                client.PausePlayer -= ClientPausePlayer;
                client.SetPlayerTime -= ClientSetPlayerTime;
                client.SetSelectedTake -= ClientSetSelectedTake;
                client.SetTakeData -= ClientSetTakeData;
                client.DeleteTake -= ClientDeleteTake;
                client.SetIterationBase -= ClientSetIterationBase;
                client.ClearIterationBase -= ClientClearIterationBase;
                client.TexturePreviewRequested -= OnTexturePreviewRequested;

                m_ClientRegistered = false;
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
                SendDeviceState(takeRecorder.IsLive());

                var slate = takeRecorder.GetActiveSlate();
                var hasSlate = slate != null;
                var slateChanged = m_SlateChangeTracker.Changed(slate);
                var take = hasSlate ? slate.Take : null;

                var assetName = GetAssetName();
                var assetNameChanged = assetName != m_LastAssetName;
                m_LastAssetName = assetName;

                if (TryGetInternalClient(out var client))
                {
                    client.SendFrameRate(takeRecorder.IsLive() || take == null ? takeRecorder.FrameRate : take.FrameRate);
                    client.SendHasSlate(hasSlate);
                    client.SendSlateDuration(takeRecorder.GetPreviewDuration());
                    client.SendSlateIsPreviewing(takeRecorder.IsPreviewPlaying());
                    client.SendSlatePreviewTime(takeRecorder.GetPreviewTime());

                    if (slateChanged || assetNameChanged)
                    {
                        if (hasSlate)
                            m_TakeNameFormatter.ConfigureTake(slate.SceneNumber, slate.ShotName, slate.TakeNumber);
                        else
                            m_TakeNameFormatter.ConfigureTake(0, "Shot", 0);

                        client.SendNextTakeName(m_TakeNameFormatter.GetTakeName());
                        client.SendNextAssetName(m_TakeNameFormatter.GetAssetName());
                    }
                }

                if (slateChanged)
                {
                    SendSlateDescriptor(slate);
                }
            }

            SendRecordingState();
        }

        /// <summary>
        /// Gets the name used for the take asset name.
        /// </summary>
        /// <returns>The name of the asset.</returns>
        protected virtual string GetAssetName() { return name; }

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
        /// The device calls this method when the slate has changed.
        /// </summary>
        /// <param name="slate">The <see cref="ISlate"/> that changed.</param>
        protected virtual void OnSlateChanged(ISlate slate) {}

        /// <summary>
        /// The device calls this method when a live performance starts and properties are about to change.
        /// </summary>
        /// <param name="previewer">The <see cref="IPropertyPreviewer"/> to use to register live properties.</param>
        protected virtual void OnRegisterLiveProperties(IPropertyPreviewer previewer) {}

        /// <summary>
        /// Registers properties before a live performance to allow to restore their original values later.
        /// </summary>
        protected void RegisterLiveProperties()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }
            
            RestoreLiveProperties();

            if (IsReady())
            {
                if (m_Previewer == null)
                    m_Previewer = new PropertyPreviewer(this);

                RegisterLiveProperties(m_Previewer);
            }
        }

        internal void RegisterLiveProperties(IPropertyPreviewer previewer)
        {
            OnRegisterLiveProperties(previewer);
        }

        /// <summary>
        /// Restores properties that changed during a live performance.
        /// </summary>
        protected void RestoreLiveProperties()
        {
            if (m_Previewer != null)
            {
                m_Previewer.Restore();
            }
        }

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
            if (TryGetInternalClient(out var client))
            {
                client.SendDeviceMode(isLive ? DeviceMode.LiveStream : DeviceMode.Playback);
            }
        }

        void SendRecordingState()
        {
            if (TryGetInternalClient(out var client))
            {
                client.SendRecordingState(IsRecording());
            }
        }

        void SendSlateDescriptor()
        {
            var takeRecorder = GetTakeRecorder();

            if (takeRecorder != null)
            {
                SendSlateDescriptor(takeRecorder.GetActiveSlate());
            }
        }

        void SendSlateDescriptor(ISlate slate)
        {
            if (TryGetInternalClient(out var client))
            {
                client.SendSlateDescriptor(SlateDescriptor.Create(slate));
            }

            OnSlateChanged(slate);
        }

        void ClientSetSelectedTake(Guid guid)
        {
            var takeRecorder = GetTakeRecorder() as ITakeRecorderInternal;

            if (takeRecorder != null)
            {
                TakeManager.Default.SelectTake(takeRecorder.GetActiveSlate(), guid);

                takeRecorder.Prepare();

                SendSlateDescriptor();
                Refresh();
            }
        }

        void ClientSetTakeData(TakeDescriptor descriptor)
        {
            TakeManager.Default.SetTakeData(descriptor);

            SendSlateDescriptor();
            Refresh();
        }

        void ClientDeleteTake(Guid guid)
        {
            TakeManager.Default.DeleteTake(guid);

            SendSlateDescriptor();
            Refresh();
        }

        void ClientSetIterationBase(Guid guid)
        {
            var takeRecorder = GetTakeRecorder() as ITakeRecorderInternal;

            if (takeRecorder != null)
            {
                var slate = takeRecorder.GetActiveSlate();

                TakeManager.Default.SetIterationBase(slate, guid);

                takeRecorder.Prepare();

                SendSlateDescriptor(slate);
                Refresh();
            }
        }

        void ClientClearIterationBase()
        {
            var takeRecorder = GetTakeRecorder() as ITakeRecorderInternal;

            if (takeRecorder != null)
            {
                var slate = takeRecorder.GetActiveSlate();

                TakeManager.Default.ClearIterationBase(slate);

                takeRecorder.Prepare();

                SendSlateDescriptor(slate);
                Refresh();
            }
        }

        void OnTexturePreviewRequested(Guid guid)
        {
            var texture = TakeManager.Default.GetAssetPreview<Texture2D>(guid);

            if (texture != null && TryGetInternalClient(out var client))
            {
                client.SendTexturePreview(guid, texture);
            }
        }

        bool OnClientConnected(ICompanionAppClient client)
        {
            if (m_Client == null && client is TClient c && (!ClientMappingDatabase.TryGetClientAssignment(this, out var clientName, out _) || c.Name == clientName))
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
