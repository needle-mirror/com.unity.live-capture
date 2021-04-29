using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Unity.LiveCapture.Ntp
{
    /// <summary>
    /// A component that gets timecode from an network time protocol (NTP) server.
    /// </summary>
    [ExecuteAlways]
    [CreateTimecodeSourceMenuItemAttribute("NTP Timecode Source")]
    [AddComponentMenu("Live Capture/Timecode/NTP Timecode Source")]
    [HelpURL(Documentation.baseURL + "ref-component-ntp-timecode-source" + Documentation.endURL)]
    public class NtpTimecodeSource : MonoBehaviour, ITimecodeSource
    {
        struct NtpUpdate
        {
        }

        [SerializeField, HideInInspector]
        string m_Guid;

        [SerializeField, Tooltip("The frame rate of the timecodes.")]
        [OnlyStandardFrameRates]
        FrameRate m_FrameRate = StandardFrameRate.FPS_24_00;

        [SerializeField, Tooltip("The hostname or IP address of NTP server to get the time from.")]
        string m_ServerAddress = "pool.ntp.org";

        readonly NtpClient m_Client = new NtpClient();
        readonly object m_Lock = new object();
        DateTime? m_ReferenceTime;
        DateTime? m_LastUpdateLocalTime;
        Task m_UpdateReferenceTimeTask;

        /// <inheritdoc />
        public string Id => m_Guid;

        /// <inheritdoc/>
        public string FriendlyName => $"NTP ({name})";

        /// <inheritdoc />
        public Timecode Now { get; private set; }

        /// <inheritdoc />
        public FrameRate FrameRate => m_FrameRate;

        void OnEnable()
        {
            TimecodeSourceManager.Instance.EnsureIdIsValid(ref m_Guid);
            TimecodeSourceManager.Instance.Register(this);
            PlayerLoopExtensions.RegisterUpdate<PreUpdate, NtpUpdate>(UpdateTimecode);

            ReInitialize();
        }

        void OnDisable()
        {
            TimecodeSourceManager.Instance.Unregister(this);
            PlayerLoopExtensions.DeregisterUpdate<NtpUpdate>(UpdateTimecode);

            ReInitialize();
        }

        void OnValidate()
        {
            ReInitialize();
        }

        void ReInitialize()
        {
            lock (m_Lock)
            {
                m_ReferenceTime = null;
                m_LastUpdateLocalTime = null;
            }

            m_UpdateReferenceTimeTask = null;

            m_Client.Disconnect();
        }

        /// <summary>
        /// Queries the NTP server to resynchronize the time with the server clock.
        /// </summary>
        public void ForceUpdate()
        {
            UpdateReferenceTime(DateTime.Now);
        }

        void UpdateTimecode()
        {
            var localTime = DateTime.Now;

            // poll the server for the current time if required
            var needsUpdate = false;

            lock (m_Lock)
            {
                if (!m_LastUpdateLocalTime.HasValue)
                {
                    needsUpdate = true;
                }
            }

            if (needsUpdate)
            {
                UpdateReferenceTime(localTime);
            }

            // calculate the timecode from the current NTP time
            var time = GetCurrentTime(localTime);

            if (time == null)
            {
                Now = Timecode.FromSeconds(m_FrameRate, 0.0);
                return;
            }

            Now = Timecode.FromSeconds(m_FrameRate, time.Value.TimeOfDay.TotalSeconds);
        }

        DateTime? GetCurrentTime(DateTime localTime)
        {
            lock (m_Lock)
            {
                if (!m_ReferenceTime.HasValue || !m_LastUpdateLocalTime.HasValue)
                {
                    return null;
                }

                // The last known ntp time combined with the local time since we received that ntp time
                return m_ReferenceTime + (localTime - m_LastUpdateLocalTime);
            }
        }

        void UpdateReferenceTime(DateTime localTime)
        {
            m_UpdateReferenceTimeTask ??= UpdateReferenceTimeAsync(localTime);
        }

        async Task UpdateReferenceTimeAsync(DateTime localTime)
        {
            var clientTime = GetCurrentTime(localTime) ?? localTime;
            var newClientTime = await m_Client.PollTimeAsync(m_ServerAddress, clientTime).ConfigureAwait(false);

            if (newClientTime.HasValue)
            {
                lock (m_Lock)
                {
                    m_ReferenceTime = newClientTime;
                    m_LastUpdateLocalTime = DateTime.Now;
                }
            }

            m_UpdateReferenceTimeTask = null;
        }
    }
}
