using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    class FormatPresetsAssetChangeDetector : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            FormatPresetsCache.SetDirty();
        }
    }

    [InitializeOnLoad]
    static class FormatPresetsCache
    {
        static class Contents
        {
            public static string customStr = "Custom";
        }

        static Vector2[] s_SensorSizes;
        static string[] s_SensorNames;
        static string[] s_SensorNamesWithCustom;
        static GUIContent[] s_SensorNameContents;
        static GUIContent[] s_SensorNameContentsWithCustom;
        static float[] s_AspectRatios;
        static string[] s_AspectRatioNames;
        static string[] s_AspectRatioNamesWithCustom;
        static GUIContent[] s_AspectRatioNameContents;
        static GUIContent[] s_AspectRatioNameContentsWithCustom;
        static bool s_Dirty;

        static FormatPresetsCache()
        {
            Undo.postprocessModifications += PostprocessModifications;

            UpdateCache();
        }

        static UndoPropertyModification[] PostprocessModifications(UndoPropertyModification[] modifications)
        {
            foreach (var modification in modifications)
            {
                if (modification.currentValue.target is FormatPresets)
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
            var presets = LoadAllAssetsOfType<FormatPresets>();
            var sensorPresets = presets
                .SelectMany(f => f.sensorPresets)
                .ToLookup(s => s.sensorSize, s => s.name);

            s_SensorSizes = sensorPresets.Select(s => s.Key).ToArray();
            s_SensorNames = sensorPresets
                .Select(s => $"[{Format(s.Key.x)} x {Format(s.Key.y)}] {string.Join(", ", s)}")
                .ToArray();
            s_SensorNamesWithCustom = s_SensorNames.Append(Contents.customStr).ToArray();
            s_SensorNameContents = s_SensorNames.Select(n => new GUIContent(n)).ToArray();
            s_SensorNameContentsWithCustom = s_SensorNamesWithCustom.Select(n => new GUIContent(n)).ToArray();

            var aspectPresets = presets
                .SelectMany(f => f.aspectRatioPresets)
                .ToLookup(a => a.aspectRatio, a => a.name);

            s_AspectRatios = aspectPresets.Select(a => a.Key).ToArray();
            s_AspectRatioNames = aspectPresets
                .Select(a => $"{a.Key} : 1 - {string.Join(", ", a)}")
                .ToArray();
            s_AspectRatioNamesWithCustom = s_AspectRatioNames.Append(Contents.customStr).ToArray();
            s_AspectRatioNameContents = s_AspectRatioNames.Select(n => new GUIContent(n)).ToArray();
            s_AspectRatioNameContentsWithCustom = s_AspectRatioNamesWithCustom.Select(n => new GUIContent(n)).ToArray();
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

        public static Vector2[] GetSensorSizes()
        {
            UpdateIfNeeded();

            return s_SensorSizes;
        }

        public static string[] GetSensorNames()
        {
            UpdateIfNeeded();

            return s_SensorNames;
        }

        public static GUIContent[] GetSensorNameContents()
        {
            UpdateIfNeeded();

            return s_SensorNameContents;
        }

        public static string[] GetSensorNamesWithCustom()
        {
            UpdateIfNeeded();

            return s_SensorNamesWithCustom;
        }

        public static GUIContent[] GetSensorNameContentsWithCustom()
        {
            UpdateIfNeeded();

            return s_SensorNameContentsWithCustom;
        }

        public static float[] GetAspectRatios()
        {
            UpdateIfNeeded();

            return s_AspectRatios;
        }

        public static string[] GetAspectRatioNames()
        {
            UpdateIfNeeded();

            return s_AspectRatioNames;
        }

        public static GUIContent[] GetAspectRatioNameContents()
        {
            UpdateIfNeeded();

            return s_AspectRatioNameContents;
        }

        public static string[] GetAspectRatioNamesWithCustom()
        {
            UpdateIfNeeded();

            return s_AspectRatioNamesWithCustom;
        }

        public static GUIContent[] GetAspectRatioNameContentsWithCustom()
        {
            UpdateIfNeeded();

            return s_AspectRatioNameContentsWithCustom;
        }
    }
}
