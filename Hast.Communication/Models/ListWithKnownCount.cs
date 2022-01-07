using System;
using System.Collections.Generic;

namespace Hast.Communication.Models
{
    /// <summary>
    /// An alternative to <see cref="List{T}"/> with explicit known capacity. It can also work with <see cref="Span"/>
    /// unlike <see cref="List{T}"/> and provides a safe mechanism for turning it into <see cref="Memory{T}"/> too.
    /// </summary>
    public class ListWithKnownCount<T>
    {
        private T[] _data;

        /// <summary>
        /// Gets the current count of set data.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Gets the known count or capacity.
        /// </summary>
        public int KnownCount { get; private set; }

        /// <summary>
        /// Gets the start index. Useful to get the relevant section from the result of <see cref="HandOver"/>.
        /// </summary>
        public int StartIndex { get; private set; }

        /// <summary>
        /// Gets the entire reserved data. Don't use unless you are certain.
        /// </summary>
        public ReadOnlySpan<T> FullSpan => _data.AsSpan();

        /// <summary>
        /// Gets the <see cref="KnownCount"/> long span.
        /// </summary>
        public ReadOnlySpan<T> Span => _data.AsSpan()[StartIndex..];

        public ListWithKnownCount(int knownCount, int startIndex = 0) => Reset(knownCount, startIndex);

        /// <summary>
        /// Appends a new entry at the end.
        /// </summary>
        /// <param name="item">The entry to be added.</param>
        /// <exception cref="OverflowException">
        /// Thrown if the <see cref="Count"/> would exceed <see cref="KnownCount"/>.
        /// </exception>
        public void Add(T item)
        {
            if (Count >= KnownCount) throw new OverflowException($"The {nameof(Count)} is already at maximum.");

            _data[StartIndex + Count] = item;
            Count++;
        }

        /// <summary>
        /// Re-initializes the object. Only reallocates the internal array if necessary.
        /// </summary>
        public void Reset(int knownCount, int startIndex = 0)
        {
            if (_data == null || _data.Length < knownCount + startIndex) _data = new T[knownCount + startIndex];
            Count = 0;
            KnownCount = knownCount;
            StartIndex = startIndex;
        }

        /// <summary>
        /// Casts the complete internal array into a <see cref="Memory{T}"/> and hands over ownership to the caller.
        /// If you only want the segment used with <see cref="Add"/>, slice the result with <see cref="StartIndex"/>
        /// which is preserved after this call for that reason. If you do that, the resulting <see cref="Memory{T}"/>
        /// will have the same length as <see cref="KnownCount"/> had.
        /// If you want to reuse this object, you must call <see cref="Reset"/>, because the internal array is removed
        /// and <see cref="KnownCount"/> is set to 0 to ensure the result's exclusivity.
        /// </summary>
        public Memory<T> HandOver()
        {
            var currentData = (Memory<T>)_data;
            _data = Array.Empty<T>();
            Count = 0;
            KnownCount = 0;

            return currentData;
        }
    }
}
