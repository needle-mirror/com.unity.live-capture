using System;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEditor;
using UnityEditor.Timeline;

using UnityObject = UnityEngine.Object;

namespace Unity.LiveCapture.Editor
{
    [CustomTimelineEditor(typeof(TakeRecorderTrack))]
    class TakeRecorderTrackEditor : TrackEditor
    {
        static readonly string ClipText = L10n.Tr("New Shot");

        public override void OnCreate(TrackAsset track, TrackAsset copiedFrom)
        {
            if (copiedFrom == null)
            {
                var clip = track.CreateDefaultClip();
                clip.displayName = ClipText;
            }
        }
    }
}
