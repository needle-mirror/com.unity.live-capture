using System;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Unity.LiveCapture
{
    /// <summary>
    /// A component that generates timecodes using the system clock.
    /// </summary>
    [ExecuteAlways]
    [CreateTimecodeSourceMenuItemAttribute("System Clock Timecode Source")]
    [AddComponentMenu("Live Capture/Timecode/System Clock Timecode Source")]
    [HelpURL(Documentation.baseURL + "ref-component-system-clock-timecode-source" + Documentation.endURL)]
    public class SystemClockTimecodeSource : MonoBehaviour, ITimecodeSource
    {
        struct SystemClockUpdate
        {
        }

        /// <summary>
        /// An enum defining the source clocks that may be used to provide the time.
        /// </summary>
        public enum ClockType
        {
            /// <summary>
            /// The timecode is derived from the engine time.
            /// </summary>
            GameTime,
            /// <summary>
            /// The timecode is derived from OS system time.
            /// </summary>
            SystemClock,
        }

        [SerializeField, HideInInspector]
        string m_Guid;

        [SerializeField, EnumButtonGroup(35), Tooltip(
            "The source clocks that may be used to provide the time.\n\n" +
            "GameTime - The timecode is derived from the engine time.\n" +
            "SystemClock - The timecode is derived from OS system time.")]
        ClockType m_ClockType;

        [SerializeField, OnlyStandardFrameRates, Tooltip("The frame rate of the timecodes.")]
        FrameRate m_FrameRate = StandardFrameRate.FPS_24_00;

        /// <inheritdoc/>
        public string Id => m_Guid;

        /// <inheritdoc/>
        public string FriendlyName => $"System Clock ({name})";

        /// <inheritdoc/>
        public Timecode Now { get; private set; }

        /// <inheritdoc/>
        public FrameRate FrameRate
        {
            get => m_FrameRate;
            set => m_FrameRate = value;
        }

        /// <summary>
        /// The source clock used to generate timecodes.
        /// </summary>
        public ClockType Type
        {
            get => m_ClockType;
            set => m_ClockType = value;
        }

        void OnEnable()
        {
            TimecodeSourceManager.Instance.EnsureIdIsValid(ref m_Guid);
            TimecodeSourceManager.Instance.Register(this);
            PlayerLoopExtensions.RegisterUpdate<PreUpdate, SystemClockUpdate>(UpdateTimecode);
        }

        void OnDisable()
        {
            TimecodeSourceManager.Instance.Unregister(this);
            PlayerLoopExtensions.DeregisterUpdate<SystemClockUpdate>(UpdateTimecode);
        }

        void UpdateTimecode()
        {
            switch (Type)
            {
                case ClockType.GameTime:
                    Now = Timecode.FromSeconds(FrameRate, Time.timeAsDouble);
                    break;
                case ClockType.SystemClock:
                    Now = Timecode.FromSeconds(FrameRate, DateTime.Now.TimeOfDay.TotalSeconds);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Type));
            }
        }

        void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                UnityEditor.SceneView.RepaintAll();
            }
#endif
        }
    }
}
