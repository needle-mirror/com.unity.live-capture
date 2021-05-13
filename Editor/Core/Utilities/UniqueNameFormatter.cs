using System.Collections.Generic;

namespace Unity.LiveCapture.Editor
{
    /// <summary>
    /// A formatter that ensures redundant string entries are given a unique name.
    /// </summary>
    class UniqueNameFormatter
    {
        Dictionary<int, int> m_UniqueItemCount = new Dictionary<int, int>();

        public string Format(string text)
        {
            var key = text.GetHashCode();
            var count = 0;

            if (m_UniqueItemCount.ContainsKey(key))
            {
                count = m_UniqueItemCount[key];
                count++;
                m_UniqueItemCount[key] = count;
                return $"{text} ({count})";
            }

            m_UniqueItemCount.Add(key, count);
            return text;
        }
    }
}
