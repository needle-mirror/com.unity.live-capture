using System.Collections.Generic;
using System.Linq;
using UnityEditor.Timeline;

namespace Unity.LiveCapture.Editor
{
    static class TimelineHierarchyContextUtility
    {
        /// <summary>
        /// Creates a <see cref="TimelineHierarchyContext"/> that matches the hierarchy displayed in the Timeline window.
        /// </summary>
        public static TimelineHierarchyContext FromTimelineNavigation()
        {
            return new TimelineHierarchyContext(EnumerateFromTimelineNavigation());
        }

        internal static IEnumerable<TimelineContext> EnumerateFromTimelineNavigation()
        {
            return EnumerateFromTimelineNavigationInternal().Reverse();
        }

        static IEnumerable<TimelineContext> EnumerateFromTimelineNavigationInternal()
        {
            var window = TimelineEditor.GetWindow();

            if (window == null)
            {
                yield return default(TimelineContext);
                yield break;
            }

            var contexts = window.navigator.GetBreadcrumbs();
            var prevContext = default(SequenceContext);
            var first = true;

            foreach (var context in contexts)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    yield return new TimelineContext(prevContext.director, context.clip);
                }

                prevContext = context;
            }

            yield return new TimelineContext(Timeline.InspectedDirector);
        }
    }
}
