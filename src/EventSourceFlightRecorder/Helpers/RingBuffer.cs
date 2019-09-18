using System;
using System.Linq;

namespace Observito.Trace.EventSourceFlightRecorder.Helpers
{
    internal sealed class RingBuffer<T>
    {
        public RingBuffer(uint capacity)
        {
            _buffer = new T[capacity];
        }

        private readonly T[] _buffer;
        private int _count;

        public int Capacity => _buffer.Length;

        public int Count => Math.Min(_count, Capacity);

        private int NextIndex => _count % Capacity;

        public int PutCount => _count;

        public void Put(T item)
        {
            lock (_buffer)
            {
                _buffer[NextIndex] = item;
                _count += 1;
            }
        }

        public T[] ToArray()
        {
            T[] c;
            lock (_buffer)
            {
                if (Capacity <= 1 || _count <= Capacity)
                    c = _buffer.Take(_count).ToArray();
                else
                {
                    c = new T[_buffer.Length];
                    //NextIndex.Dump();
                    var i = NextIndex + Capacity;
                    for (var j = 0; j < Count; j++)
                    {
                        c[j] = _buffer[(i + j) % Capacity];
                    }
                }
            }
            return c;
        }
    }
}
