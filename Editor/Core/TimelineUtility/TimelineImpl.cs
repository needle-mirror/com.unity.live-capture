using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;
using UnityEditor.Timeline;

namespace Unity.LiveCapture.Editor
{
    [InitializeOnLoad]
    /// <summary>
    /// An implementation of <see cref="ITimelineImpl"/> that uses the TimelineEditor state.
    /// </summary>
    class TimelineImpl : ITimelineImpl
    {
        static TimelineImpl Instance { get; } = new TimelineImpl();

        static TimelineImpl()
        {
            Timeline.Instance.SetImpl(Instance);
        }

        /// <inheritdoc />
        public PlayableDirector InspectedDirector => TimelineEditor.inspectedDirector;

        /// <inheritdoc />
        public PlayableDirector MasterDirector => TimelineEditor.masterDirector;

        /// <inheritdoc />
        public TimelineAsset InspectedAsset => TimelineEditor.inspectedAsset;

        /// <inheritdoc />
        public TimelineAsset MasterAsset => TimelineEditor.masterAsset;

        public void SetAsMasterDirector(PlayableDirector director)
        {
            if (TimelineEditor.masterDirector == director)
                return;

            var window = TimelineEditor.GetWindow();

            if (window == null)
                return;

            if (window.locked)
                return;

            window.SetTimeline(director);
        }

        public void Repaint()
        {
            TimelineEditor.Refresh(RefreshReason.WindowNeedsRedraw);
        }
    }
}
