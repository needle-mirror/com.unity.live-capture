using System.Linq;
using Unity.LiveCapture.VirtualCamera;
using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    class LensPresetAssetChangeDetector : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            LensPresetsCache.SetDirty();
        }
    }

    /// <summary>
    /// Utility class that caches every LensPreset asset in the project and prepares the data needed
    /// for rendering a preset selector in the Inspector.
    /// </summary>
    [InitializeOnLoad]
    static class LensPresetsCache
    {
        static class Contents
        {
            public static string customStr = "Custom";
            public static string noPresetsAvailable = "No LensPresets available";
        }

        static Lens[] s_LensPresets;
        static string[] s_LensPresetNames;
        static string[] s_LensPresetNamesWithCustom;
        static GUIContent[] s_LensPresetNameContents;
        static GUIContent[] s_LensPresetNameContentsWithCustom;
        static bool s_Dirty;

        static LensPresetsCache()
        {
            Undo.postprocessModifications += PostprocessModifications;

            UpdateCache();
        }

        static UndoPropertyModification[] PostprocessModifications(UndoPropertyModification[] modifications)
        {
            foreach (var modification in modifications)
            {
                if (modification.currentValue.target is LensPreset)
                {
                    SetDirty();

                    break;
                }
            }

            return modifications;
        }

        public static void SetDirty()
        {
            s_Dirty = true;
        }

        static void UpdateIfNeeded()
        {
            if (s_Dirty)
            {
                UpdateCache();

                s_Dirty = false;
            }
        }

        static void UpdateCache()
        {
            var presets = LoadAllAssetsOfType<LensPreset>();
            var lensPresets = presets
                .ToLookup(p => p.lens, p => p.name, LensSettingsEqualityComparer.Default);

            s_LensPresets = lensPresets.Select(l => l.Key).ToArray();
            s_LensPresetNames = lensPresets
                .Select(l => string.Join(", ", l))
                .ToArray();

            var lastElement = presets.Length == 0 ? Contents.noPresetsAvailable : Contents.customStr;

            s_LensPresetNamesWithCustom = s_LensPresetNames.Append(lastElement).ToArray();
            s_LensPresetNameContents = s_LensPresetNames.Select(n => new GUIContent(n)).ToArray();
            s_LensPresetNameContentsWithCustom = s_LensPresetNamesWithCustom.Select(n => new GUIContent(n)).ToArray();
        }

        static string Format(float number)
        {
            return string.Format("{0:0.00}", number);
        }

        static T[] LoadAllAssetsOfType<T>() where T : Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => LoadAssetAtPath<T>(path))
                .ToArray();
        }

        static T LoadAssetAtPath<T>(string path) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path) as T;
        }

        /// <summary>
        /// Get all the unique Lens data stored in presets.
        /// </summary>
        /// <returns>The Lens array containing the presets.</returns>
        public static Lens[] GetLensPresets()
        {
            UpdateIfNeeded();

            return s_LensPresets;
        }

        /// <summary>
        /// Get the names of each preset entry.
        /// </summary>
        /// <returns>The array containing the names as string.</returns>
        public static string[] GetLensPresetNames()
        {
            UpdateIfNeeded();

            return s_LensPresetNames;
        }

        /// <summary>
        /// Get the names of each preset entry.
        /// </summary>
        /// <returns>The array containing the names as GUIContent.</returns>
        public static GUIContent[] GetLensPresetNameContents()
        {
            UpdateIfNeeded();

            return s_LensPresetNameContents;
        }

        /// <summary>
        /// Get the names of each preset entry. The last name is a label used for non-matching presets.
        /// </summary>
        /// <returns>The array containing the names as string.</returns>
        public static string[] GetLensPresetNamesWithCustom()
        {
            UpdateIfNeeded();

            return s_LensPresetNamesWithCustom;
        }

        /// <summary>
        /// Get the names of each preset entry. The last name is a label used for non-matching presets.
        /// </summary>
        /// <returns>The array containing the names as GUIContent.</returns>
        public static GUIContent[] GetLensPresetNameContentsWithCustom()
        {
            UpdateIfNeeded();

            return s_LensPresetNameContentsWithCustom;
        }
    }
}
