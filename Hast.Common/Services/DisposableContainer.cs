using System;
using System.Collections.Generic;

namespace Hast.Common.Services
{
    /// <summary>
    /// Contains a values that are attached to an <see cref="IDisposable"/> context while manages their lifecycle
    /// together.
    /// </summary>
    public class DisposableContainer : IDisposable
    {
        private bool _disposed;
        private IDisposable _context;

        public IReadOnlyList<object> Values { get; }

        public DisposableContainer(IDisposable context, params object[] values)
        {
            _context = context;
            Values = values;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            _context?.Dispose();
            _context = null;

            if (disposing)
            {
                foreach (var value in Values)
                {
                    if (value is IDisposable disposableValue)
                    {
                        disposableValue.Dispose();
                    }
                }
            }

            _disposed = true;
        }
    }

    /// <inheritdoc cref="DisposableContainer"/>
    public sealed class DisposableContainer<T> : DisposableContainer
    {
        public T Value => (T)Values[0];

        public DisposableContainer(IDisposable context, T value)
            : base(context, value) { }
    }

    /// <inheritdoc cref="DisposableContainer"/>
    public sealed class DisposableContainer<T1, T2> : DisposableContainer
    {
        public DisposableContainer(IDisposable context, T1 value1, T2 value2)
            : base(context, value1, value2) { }

        public void Deconstruct(out T1 first, out T2 second)
        {
            first = (T1)Values[0];
            second = (T2)Values[1];
        }
    }
}
