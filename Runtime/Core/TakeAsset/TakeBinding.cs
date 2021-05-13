using System;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Unity.LiveCapture
{
    /// <summary>
    /// A serializable implementation of <see cref="ITakeBinding"/> that contains values of
    /// type UnityEngine.Object.
    /// </summary>
    /// <typeparam name="T">The type of the TakeBinding. It must be a UnityObject.</typeparam>
    [Serializable]
    public class TakeBinding<T> : ITakeBinding where T : UnityObject
    {
        [SerializeField]
        ExposedReference<UnityObject> m_ExposedReference;

        /// <inheritdoc/>
        public Type Type => typeof(T);

        /// <inheritdoc/>
        public void SetName(string name)
        {
            m_ExposedReference.exposedName = new PropertyName(name);
        }

        /// <inheritdoc/>
        public UnityObject GetValue(IExposedPropertyTable resolver)
        {
            return m_ExposedReference.Resolve(resolver);
        }

        /// <inheritdoc/>
        public void SetValue(UnityObject value, IExposedPropertyTable resolver)
        {
            resolver.SetReferenceValue(m_ExposedReference.exposedName, value);
        }

        /// <inheritdoc/>
        public void ClearValue(IExposedPropertyTable resolver)
        {
            resolver.ClearReferenceValue(m_ExposedReference.exposedName);
        }

        /// <inheritdoc/>
        public bool Equals(ITakeBinding other)
        {
            if (other is TakeBinding<T> binding)
            {
                return m_ExposedReference.exposedName == binding.m_ExposedReference.exposedName &&
                    m_ExposedReference.defaultValue == binding.m_ExposedReference.defaultValue;
            }

            return false;
        }
    }
}
