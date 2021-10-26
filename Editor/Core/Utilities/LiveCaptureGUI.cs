using UnityEngine;
using UnityEditor;

namespace Unity.LiveCapture.Editor
{
    static class LiveCaptureGUI
    {
        static class Contents
        {
            public static readonly GUIContent InfoIcon = EditorGUIUtility.TrIconContent("console.infoicon");
            public static readonly GUIContent WarningIcon = EditorGUIUtility.TrIconContent("console.warnicon");
            public static readonly GUIContent ErrorIcon = EditorGUIUtility.TrIconContent("console.erroricon");
            public static readonly GUIStyle HelpBoxStyleNoIcon = new GUIStyle(EditorStyles.helpBox);
            public static readonly GUIStyle HelpBoxStyleWithIcon = new GUIStyle(EditorStyles.helpBox);
            public static readonly GUIStyle HelpBoxLinkStyle = new GUIStyle();

            static Contents()
            {
                HelpBoxStyleNoIcon.alignment = TextAnchor.UpperLeft;
                HelpBoxStyleNoIcon.padding = new RectOffset(5, 5, 5, 20);
                HelpBoxStyleWithIcon.alignment = TextAnchor.UpperLeft;
                HelpBoxStyleWithIcon.padding = new RectOffset(37, 5, 5, 20);
                HelpBoxLinkStyle.font = HelpBoxStyleWithIcon.font;
                HelpBoxLinkStyle.padding = new RectOffset(5, 5, 2, 2);
                HelpBoxLinkStyle.alignment = TextAnchor.LowerRight;
                HelpBoxLinkStyle.normal.textColor = new Color(0.34f, 0.68f, 1f, 1f);
            }
        }

        internal static void HelpBoxWithURL(string message, string linkText, string url, MessageType messageType)
        {
            var icon = GUIContent.none;

            switch (messageType)
            {
                case MessageType.Info: icon = Contents.InfoIcon; break;
                case MessageType.Warning: icon = Contents.WarningIcon; break;
                case MessageType.Error: icon = Contents.ErrorIcon; break;
            }

            var style = Contents.HelpBoxStyleWithIcon;

            if (messageType == MessageType.None)
            {
                style = Contents.HelpBoxStyleNoIcon;
            }

            var messageContents = new GUIContent(message);
            var linkContents = new GUIContent(linkText);
            var buttonSize = Contents.HelpBoxLinkStyle.CalcSize(linkContents);

            EditorGUILayout.BeginHorizontal();

            var rect = GUILayoutUtility.GetRect(messageContents, style);

            EditorGUI.LabelField(rect, messageContents, style);

            if (messageType != MessageType.None)
            {
                var iconWidth = style.padding.left;
                var iconHeight = icon.image.height * iconWidth / icon.image.width;
                var iconY = rect.y + rect.height * 0.5f - iconHeight * 0.5f;
                var iconRect = new Rect(rect.x + 2, iconY, iconWidth, rect.height);

                GUI.Label(iconRect, icon);
            }

            var buttonRect = new Rect(rect.x + rect.width - buttonSize.x, rect.y + rect.height - buttonSize.y, buttonSize.x, buttonSize.y);

            if (GUI.Button(buttonRect, linkContents, Contents.HelpBoxLinkStyle))
            {
                Application.OpenURL(url);
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
