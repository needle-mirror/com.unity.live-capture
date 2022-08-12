using System;
using System.Collections;
using System.Collections.Generic;

namespace Unity.LiveCapture
{
    /// <summary>
    /// A queue-like data structure that allows read-only access to all values in the queue.
    /// </summary>
    /// <typeparam name="T">The type of data stored in the buffer.</typeparam>
    public class CircularBuffer<T> : IReadOnlyList<T>
    {
        readonly Action<T> m_OnDiscard;
        T[] m_Data;
        int m_StartIndex = 0;
        int m_EndIndex = 0;

        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        public int Count => (m_Data.Length + m_EndIndex - m_StartIndex) % m_Data.Length;

        /// <summary>
        /// Gets the maximum number of elements which can be stored in the collection.
        /// </summary>
        public int Capacity => m_Data.Length - 1;

        /// <summary>
        /// Constructs a new <see cref="CircularBuffer{T}"/> instance with an initial capacity.
        /// </summary>
        /// <param name="capacity">The maximum number of elements which can be stored in the collection.</param>
        /// <param name="onDiscard">A callback invoked for each element that is discarded from the buffer.
        /// This does not include when <see cref="PopFront"/> or <see cref="PopBack"/> are called.</param>
        public CircularBuffer(int capacity, Action<T> onDiscard = null)
        {
            m_OnDiscard = onDiscard;

            SetCapacity(capacity);
        }

        /// <inheritdoc cref="PushBack"/>
        public void Add(T value)
        {
            // The add method is an alias for PushBack to support initializer syntax
            PushBack(value);
        }

        /// <summary>
        /// Adds an element to the back of the buffer.
        /// </summary>
        /// <remarks>
        /// If the buffer is full, the element at the front of the buffer will be discarded.
        /// </remarks>
        /// <param name="value">The element to add.</param>
        public void PushBack(T value)
        {
            if (Count == Capacity)
            {
                if (m_OnDiscard != null)
                {
                    m_OnDiscard(PeekFront());
                }

                IncrementIndex(ref m_StartIndex);
            }

            m_Data[m_EndIndex] = value;
            IncrementIndex(ref m_EndIndex);
        }

        /// <summary>
        /// Adds an element to the front of the buffer.
        /// </summary>
        /// <remarks>
        /// If the buffer is full, the element at the back of the buffer will be discarded.
        /// </remarks>
        /// <param name="value">The element to add.</param>
        public void PushFront(T value)
        {
            if (Count == Capacity)
            {
                if (m_OnDiscard != null)
                {
                    m_OnDiscard(PeekBack());
                }

                DecrementIndex(ref m_EndIndex);
            }

            DecrementIndex(ref m_StartIndex);
            m_Data[m_StartIndex] = value;
        }

        /// <summary>
        /// Removes an element from the front of the buffer.
        /// </summary>
        /// <returns>The removed element.</returns>
        public T PopFront()
        {
            PreconditionNotEmpty();
            var item = PeekFront();
            IncrementIndex(ref m_StartIndex);
            return item;
        }

        /// <summary>
        /// Removes an element from the back of the buffer.
        /// </summary>
        /// <returns>The removed element.</returns>
        public T PopBack()
        {
            PreconditionNotEmpty();
            var item = PeekBack();
            DecrementIndex(ref m_EndIndex);
            return item;
        }

        /// <summary>
        /// Get the element at the front of the buffer.
        /// </summary>
        /// <returns>The element at the front of the buffer.</returns>
        public T PeekFront()
        {
            PreconditionNotEmpty();
            return m_Data[m_StartIndex];
        }

        /// <summary>
        /// Get the element at the back of the buffer.
        /// </summary>
        /// <returns>The element at the back of the buffer.</returns>
        public T PeekBack()
        {
            PreconditionNotEmpty();
            var backIndex = m_EndIndex;
            DecrementIndex(ref backIndex);
            return m_Data[backIndex];
        }

        /// <summary>
        /// Removes all items in the collection.
        /// </summary>
        public void Clear()
        {
            if (m_OnDiscard != null)
            {
                foreach (var value in this)
                {
                    m_OnDiscard(value);
                }
            }

            m_StartIndex = 0;
            m_EndIndex = 0;
        }

        /// <summary>
        /// Sets the <see cref="Capacity"/> of the circular buffer.
        /// </summary>
        /// <remarks>If the new size is smaller than the current <see cref="Count"/>, elements will be truncated from the front.</remarks>
        /// <param name="capacity">The desired capacity of the collection.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the capacity is not greater than zero.</exception>
        public void SetCapacity(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Must be greater than zero.");
            }

            if (m_Data != null && capacity == Capacity)
            {
                return;
            }

            // We determine that the buffer is full by checking that the end index
            // is one less than the start index. This means that the capacity of
            // the buffer is the array length minus one, but it allows us to differentiate
            // between the empty case and the full case using the start and
            // end indices.
            var newArray = new T[capacity + 1];
            var endIndex = 0;

            if (m_Data != null)
            {
                while (Count > capacity)
                {
                    var discardedValue = PopFront();

                    if (m_OnDiscard != null)
                    {
                        m_OnDiscard(discardedValue);
                    }
                }

                var index = m_StartIndex;
                while (index != m_EndIndex)
                {
                    newArray[endIndex] = m_Data[index];
                    IncrementIndex(ref index);
                    ++endIndex;
                }
            }

            m_Data = newArray;
            m_StartIndex = 0;
            m_EndIndex = endIndex;
        }

        /// <inheritdoc />
        public T this[int index]
        {
            get
            {
                PreconditionInBounds(index);
                return m_Data[(index + m_StartIndex) % m_Data.Length];
            }
            set
            {
                PreconditionInBounds(index);
                m_Data[(index + m_StartIndex) % m_Data.Length] = value;
            }
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            var index = m_StartIndex;
            while (index != m_EndIndex)
            {
                yield return m_Data[index];
                IncrementIndex(ref index);
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void PreconditionNotEmpty()
        {
            if (m_EndIndex == m_StartIndex)
            {
                throw new InvalidOperationException("Buffer is empty");
            }
        }

        void PreconditionInBounds(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new IndexOutOfRangeException();
            }
        }

        void IncrementIndex(ref int index)
        {
            index = (index + 1) % m_Data.Length;
        }

        void DecrementIndex(ref int index)
        {
            index = (m_Data.Length + index - 1) % m_Data.Length;
        }
    }
}
