using System.IO;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEditor;
using UnityEditor.Timeline;

namespace Unity.LiveCapture.Editor
{
    [CustomTimelineEditor(typeof(SlatePlayableAsset))]
    class SlatePlayableAssetClipEditor : ClipEditor
    {
        static readonly Color s_SelectedColor = new Color(0.23f, 0.33f, 0.43f, 1f);
        static readonly Color s_RecordColor = new Color(0.75f, 0.1f, 0.1f, 0.5f);

        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            var options = base.GetClipOptions(clip);
            var slateAsset = clip.asset as SlatePlayableAsset;
            var take = slateAsset.Take;
            var director = TimelineEditor.inspectedDirector;

            if (take != null
                && director != null
                && TakeBindingsEditor.ContainsNullBindings(take, director))
            {
                options.errorText = TakeBindingsEditor.Contents.NullBindingsMsg;
            }

            return options;
        }

        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            var takeRecorder = TakeRecorder.Main;

            if (takeRecorder == null)
            {
                return;
            }

            var context = takeRecorder.GetContext() as PlayableAssetContext;

            if (takeRecorder.IsLocked()
                && context != null
                && clip.asset == context.GetClip().asset
                && context.GetHierarchyContext().MatchesTimelineNavigation())
            {
                var color = takeRecorder.IsRecording() ? s_RecordColor : s_SelectedColor;

                EditorGUI.DrawRect(region.position, color);
            }
        }

        public override void OnCreate(TimelineClip clip, TrackAsset track, TimelineClip clonedFrom)
        {
            var slateAsset = clip.asset as SlatePlayableAsset;
            var take = slateAsset.Take;

            if (take != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(take);

                slateAsset.Directory = Path.GetDirectoryName(assetPath);
                slateAsset.SceneNumber = take.SceneNumber;
                slateAsset.ShotName = take.ShotName;
                slateAsset.TakeNumber = take.TakeNumber + 1;

                clip.displayName = take.ShotName;
            }
        }
    }
}
