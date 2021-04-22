using UnityEngine;

namespace Unity.LiveCapture
{
    [SettingFilePath("ProjectSettings/LiveCaptureSettings.asset", SettingFilePathAttribute.Location.ProjectFolder)]
    class LiveCaptureSettings : SettingAsset<LiveCaptureSettings>
    {
        static readonly string k_DefaultTakeNameFormat = $"[{TakeBuilder.Wildcards.kScene}] {TakeBuilder.Wildcards.kShot} [{TakeBuilder.Wildcards.kTake}]";
        static readonly string k_DefaultAssetNameFormat = $"{TakeBuilder.Wildcards.kName} [{TakeBuilder.Wildcards.kShot}] [{TakeBuilder.Wildcards.kTake}]";

        [SerializeField]
        string m_TakeNameFormat = k_DefaultTakeNameFormat;
        [SerializeField]
        string m_AssetNameFormat = k_DefaultAssetNameFormat;

        public string takeNameFormat => m_TakeNameFormat;
        public string assetNameFormat => m_AssetNameFormat;

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
