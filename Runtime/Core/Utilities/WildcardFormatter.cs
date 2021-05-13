using System.Collections.Generic;

namespace Unity.LiveCapture
{
    class WildcardFormatter
    {
        Dictionary<string, string> m_Replacements = new Dictionary<string, string>();

        public void AddReplacement(string pattern, string replacement)
        {
            m_Replacements[pattern] = replacement;
        }

        public string Format(string name)
        {
            foreach (var pair in m_Replacements)
            {
                name = name.Replace(pair.Key, pair.Value);
            }

            return name;
        }
    }
}
