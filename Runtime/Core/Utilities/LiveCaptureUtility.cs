using System.Collections.Generic;

namespace Unity.LiveCapture
{
    /// <summary>
    /// Contains useful extension methods.
    /// </summary>
    static class LiveCaptureUtility
    {
        /// <summary>
        /// Appends an item to a list if the item is not already contained by the list.
        /// </summary>
        /// <param name="list">The list to append to.</param>
        /// <param name="item">The item to add.</param>
        /// <typeparam name="T">The list element type.</typeparam>
        /// <returns>True if a new item was added; false otherwise.</returns>
        public static bool AddUnique<T>(this List<T> list, T item)
        {
            if (!list.Contains(item))
            {
                list.Add(item);
                return true;
            }

            return false;
        }
    }
}
