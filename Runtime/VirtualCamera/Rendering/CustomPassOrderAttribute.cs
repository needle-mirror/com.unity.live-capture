using System;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// An attribute that allows to specify the rendering order of custom passes, used for HDRP.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    class CustomPassOrderAttribute : Attribute
    {
        int m_OrderHint;

        public int orderHint => m_OrderHint;

        public CustomPassOrderAttribute(int orderHint)
        {
            m_OrderHint = orderHint;
        }
    }
}
