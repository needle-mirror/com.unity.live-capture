using UnityEngine;

namespace Unity.LiveCapture
{
    [SettingFilePath("ProjectSettings/LiveCaptureSettings.asset", SettingFilePathAttribute.Location.ProjectFolder)]
    class LiveCaptureSettings : SettingAsset<LiveCaptureSettings>
    {
        static readonly string k_DefaultTakeNameFormat = $"[{TakeNameFormatter.Wildcards.Scene}] {TakeNameFormatter.Wildcards.Shot} [{TakeNameFormatter.Wildcards.Take}]";
        static readonly string k_DefaultAssetNameFormat = $"{TakeNameFormatter.Wildcards.Name} [{TakeNameFormatter.Wildcards.Shot}] [{TakeNameFormatter.Wildcards.Take}]";

        [SerializeField]
        string m_TakeNameFormat = k_DefaultTakeNameFormat;
        [SerializeField]
        string m_AssetNameFormat = k_DefaultAssetNameFormat;

        public string TakeNameFormat => string.IsNullOrWhiteSpace(m_TakeNameFormat) ? k_DefaultTakeNameFormat : m_TakeNameFormat;
        public string AssetNameFormat => string.IsNullOrWhiteSpace(m_AssetNameFormat) ? k_DefaultAssetNameFormat : m_AssetNameFormat;

        /// <summary>
        /// Resets the settings to the default values.
        /// </summary>
        public void Reset()
        {
            m_TakeNameFormat = k_DefaultTakeNameFormat;
            m_AssetNameFormat = k_DefaultAssetNameFormat;
        }
    }
}
