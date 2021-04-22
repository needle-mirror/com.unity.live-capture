using System;
using System.Linq;
using System.Reflection;

namespace Unity.LiveCapture
{
    static class TimelineUndoUtility
    {
        static FieldInfo s_EnableUndo;

        static TimelineUndoUtility()
        {
            var disableUndoGuardType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.FullName.Equals("UnityEngine.Timeline.TimelineUndo+DisableUndoGuard"));

            if (disableUndoGuardType != null)
            {
                s_EnableUndo = disableUndoGuardType.GetField("enableUndo", BindingFlags.NonPublic | BindingFlags.Static);
            }
        }

        public static void SetUndoEnabled(bool value)
        {
            s_EnableUndo.SetValue(null, value);
        }
    }
}
