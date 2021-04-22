using System;
using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    [CustomPropertyDrawer(typeof(ReticleAttribute))]
    class ReticlePropertyDrawer : PropertyDrawer
    {
        enum FocusReticleControl { Device = 0, DeviceAndGameView = 1 }

        static string GetDescription(FocusReticleControl value)
        {
            switch (value)
            {
                case FocusReticleControl.Device:
                    return "Device";
                case FocusReticleControl.DeviceAndGameView:
                    return "Device And GameView";
            }
            return string.Empty;
        }

        static class Contents
        {
            public static readonly GUIContent reticlePosition = new GUIContent("Reticle Position", "Normalized position of the reticle used for Depth Of Field.");
            public static readonly GUIContent reticleControl = new GUIContent("Reticle Control", "Whether the Focus Reticle position is controlled from the remote device only or also by clicking in the Game View.");
            public static readonly GUIContent[] reticleOptions =
            {
                new GUIContent(GetDescription((FocusReticleControl)0)),
                new GUIContent(GetDescription((FocusReticleControl)1))
            };
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.PropertyField(position, property, Contents.reticlePosition);

            position.y += EditorGUIUtility.singleLineHeight;

            var index = (int)(GameViewController.instance.isActive ? FocusReticleControl.DeviceAndGameView : FocusReticleControl.Device);
            var newIndex = EditorGUI.Popup(position, Contents.reticleControl, index, Contents.reticleOptions);

            if (newIndex != index)
            {
                if (newIndex == (int)FocusReticleControl.DeviceAndGameView)
                    GameViewController.instance.Enable();
                else
                    GameViewController.instance.Disable();
            }
        }
    }
}
