using UnityEngine;
using UnityEngine.Timeline;
using UnityEditor;

namespace Unity.LiveCapture
{
    /// <summary>
    /// The TakePostProcessor ensures that <see cref="Take"> assets reference the TimelineAsset stored
    /// as a sub-asset. It also renames the TimelineAsset to match the Take asset name.
    /// </summary>
    class TakePostProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets)
            {
                PostProcess(path);
            }

            foreach (var path in movedAssets)
            {
                PostProcess(path);
            }
        }

        static void PostProcess(string path)
        {
            if (TryLoad<Take>(path, out var take))
            {
                take.timeline = null;

                if (TryLoad<TimelineAsset>(path, out var timeline))
                {
                    take.timeline = timeline;

                    if (timeline.name != take.name)
                    {
                        timeline.name = take.name;
                        EditorUtility.SetDirty(timeline);
                        AssetDatabase.SaveAssets();
                    }
                }
            }
        }

        static bool TryLoad<T>(string path, out T asset) where T : Object
        {
            asset = AssetDatabase.LoadAssetAtPath<T>(path);

            return asset != null;
        }
    }
}
