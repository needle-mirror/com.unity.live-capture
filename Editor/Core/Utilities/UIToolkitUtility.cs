using System.Linq;
using UnityEngine.UIElements;

namespace Unity.LiveCapture.Editor
{
    static class UIToolkitUtility
    {
        public static void SetDisplay(this VisualElement element, bool visible)
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static void MoveChildrenTo(this VisualElement from, VisualElement to)
        {
            while (from.childCount > 0)
            {
                var child = from.Children().First();
                child.RemoveFromHierarchy();
                to.Add(child);
            }
        }
    }
}
