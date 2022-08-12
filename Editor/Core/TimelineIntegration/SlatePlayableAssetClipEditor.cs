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
        static readonly Color s_LockedColor = new Color(0.23f, 0.33f, 0.43f, 1f);
        static readonly Color s_MarkerColor = new Color(1f, 0.65f,0f, 1f);
        static readonly Color s_RecordColor = new Color(0.75f, 0.1f, 0.1f, 0.5f);
        static readonly Color s_PlaybackColor = new Color(0.23f, 0.33f, 0.43f, 0.5f);

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
            DoTakeRecorderBackground(clip, region);
            DoTakeContentMarkers(clip, region);
        }

        void DoTakeContentMarkers(TimelineClip clip, ClipBackgroundRegion region)
        {
            var slateAsset = clip.asset as SlatePlayableAsset;
            var take = slateAsset.Take;

            if (take.TryGetContentRange(out var start, out var end))
            {
                start -= region.startTime;
                end -= region.startTime;

                var timeSpan = region.endTime - region.startTime;
                var position = region.position;
                var toPixels = position.width / timeSpan;
                var xMin = start * toPixels;
                var width = (end - start) * toPixels;

                position.x += (float)xMin;
                position.width = (float)width;

                EditorGUI.DrawRect(
                    new Rect(position.x, position.y, 2f, position.height)
                    , s_MarkerColor);
                EditorGUI.DrawRect(
                    new Rect(position.x + 2f, position.y, 5f, 2f)
                    , s_MarkerColor);
                EditorGUI.DrawRect(
                    new Rect(position.x + 2f, position.y + position.height - 2f, 5f, 2f)
                    , s_MarkerColor);
                EditorGUI.DrawRect(
                    new Rect(position.xMax - 2f, position.y, 2f, position.height)
                    , s_MarkerColor);
                EditorGUI.DrawRect(
                    new Rect(position.xMax - 7f, position.y, 5f, 2f)
                    , s_MarkerColor);
                EditorGUI.DrawRect(
                    new Rect(position.xMax - 7f, position.y + position.height - 2f, 5f, 2f)
                    , s_MarkerColor);
            }
        }

        void DoTakeRecorderBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            var takeRecorder = TakeRecorder.Main;

            if (takeRecorder == null)
            {
                return;
            }

            var lockedContext = takeRecorder.IsLocked()
                ? takeRecorder.GetContext() as PlayableAssetContext
                : default(PlayableAssetContext);
            var recordingContext = takeRecorder.RecordContext as PlayableAssetContext;
            var playbackContext = takeRecorder.PlaybackContext as PlayableAssetContext;

            if (recordingContext != null)
            {
                DrawContext(region.position, clip, recordingContext, s_RecordColor);
            }

            if (lockedContext != null
                && !lockedContext.Equals(recordingContext))
            {
                DrawContext(region.position, clip, lockedContext, s_LockedColor);
            }

            if (playbackContext != null
                && !playbackContext.Equals(recordingContext)
                && !playbackContext.Equals(lockedContext))
            {
                DrawContext(region.position, clip, playbackContext, s_PlaybackColor);
            }
        }

        void DrawContext(Rect position, TimelineClip clip, PlayableAssetContext context, Color color)
        {
            Debug.Assert(context != null);

            if (clip.asset == context.GetClip().asset
                && context.GetHierarchyContext().MatchesTimelineNavigation())
            {
                EditorGUI.DrawRect(position, color);
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

                if (take.TryGetContentRange(out var start, out var end))
                {
                    clip.duration = end - start;
                    clip.clipIn = start;

                    // Prevent Timeline from changing clip.duration.
                    // TimleineHelpers.cs AddClipOnTrack:
                    // // get the clip editor
                    // try
                    // {
                    //     CustomTimelineEditorCache.GetClipEditor(newClip).OnCreate(newClip, parentTrack, null);
                    // }
                    // catch (Exception e)
                    // {
                    //     Debug.LogException(e);
                    // }
                    // 
                    // reset the duration as the newly assigned values may have changed the default
                    // if (playableAsset != null)
                    // {
                    //     var candidateDuration = playableAsset.duration;
                    // 
                    //     if (!double.IsInfinity(candidateDuration) && candidateDuration > 0)
                    //         newClip.duration = Math.Min(Math.Max(candidateDuration, TimelineClip.kMinDuration), TimelineClip.kMaxTimeValue);
                    // }

                    slateAsset.SetDurationOverride(float.PositiveInfinity);
                    EditorApplication.delayCall += () =>
                    {
                        slateAsset.SetDurationOverride(null);
                    };
                }
            }
        }
    }
}
