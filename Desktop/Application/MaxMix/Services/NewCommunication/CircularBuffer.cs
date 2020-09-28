using System;
using System.Collections.Generic;

namespace MaxMix.Services.NewCommunication
{
    public class CircularBuffer<T>
    {
        private T[] m_Data;
        private int m_First;
        private int m_Last;

        public CircularBuffer(int capacity)
        {
            m_Data = new T[capacity];
            m_First = 0;
            m_Last = 0;
            Count = 0;
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException("index");
                return m_Data[(m_First + index) % Capacity];
            }
            set
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException("index");
                m_Data[(m_First + index) % Capacity] = value;
            }
        }

        public int Count { get; private set; }
        public int Capacity => m_Data.Length;

        public void Enqueue(T item)
        {
            m_Data[m_Last] = item;
            m_Last = (m_Last + 1) % m_Data.Length;
            if (m_Last == m_First)
                m_First = (m_First + 1) % m_Data.Length;
            else
                Count++;
        }

        public T Dequeue()
        {
            if (Count == 0)
                throw new InvalidOperationException("Empty CircularBuffer.");
            T result = m_Data[m_First];
            m_Data[m_First] = default;
            m_First = (m_First + 1) % m_Data.Length;
            Count--;
            return result;
        }

        public int FindIndex(Predicate<T> match)
        {
            for (int i = 0; i < Count; ++i)
            {
                if (match(this[i]))
                    return i;
            }
            return -1;
        }

        public void Insert(int index, T item)
        {
            if (index < 0 || index > Count || index == Capacity)
                throw new ArgumentOutOfRangeException("index");

            if (Count == index)
            {
                Enqueue(item);
                return;
            }

            var last = this[Count - 1];
            for (int i = index; i < Count - 2; ++i)
                this[i + 1] = this[i];
            this[index] = item;
            Enqueue(last);
        }

        public int IndexOf(T item)
        {
            for (int i = 0; i < Count; ++i)
            {
                if (EqualityComparer<T>.Default.Equals(item, this[i]))
                    return i;
            }
            return -1;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException("index");
            for (int i = index; i > 0; --i)
                this[i] = this[i - 1];
            Dequeue();
        }
    }
}
