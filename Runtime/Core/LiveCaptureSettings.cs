using UnityEngine;

namespace Unity.LiveCapture
{
    [SettingFilePath("ProjectSettings/LiveCaptureSettings.asset", SettingFilePathAttribute.Location.ProjectFolder)]
    class LiveCaptureSettings : SettingAsset<LiveCaptureSettings>
    {
        static readonly string k_DefaultTakeNameFormat = $"[{TakeBuilder.Wildcards.Scene}] {TakeBuilder.Wildcards.Shot} [{TakeBuilder.Wildcards.Take}]";
        static readonly string k_DefaultAssetNameFormat = $"{TakeBuilder.Wildcards.Name} [{TakeBuilder.Wildcards.Shot}] [{TakeBuilder.Wildcards.Take}]";

        [SerializeField]
        string m_TakeNameFormat = k_DefaultTakeNameFormat;
        [SerializeField]
        string m_AssetNameFormat = k_DefaultAssetNameFormat;

        public string TakeNameFormat => m_TakeNameFormat;
        public string AssetNameFormat => m_AssetNameFormat;

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
