using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// Asset that stores Lens parameters.
    /// </summary>
    [CreateAssetMenu(fileName = "Lens Preset", menuName = "Live Capture/Virtual Camera/Lens Preset", order = 2)]
    public class LensPreset : ScriptableObject
    {
        [SerializeField]
        Lens m_Lens;

        /// <summary>
        /// The stored Lens parameters.
        /// </summary>
        public Lens lens => m_Lens;

        void Reset()
        {
            m_Lens = Lens.defaultParams;
        }

        void OnValidate()
        {
            m_Lens.Validate();
        }
    }
}
