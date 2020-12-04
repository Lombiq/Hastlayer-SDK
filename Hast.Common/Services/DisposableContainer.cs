using System;

namespace Hast.Common.Services
{
    /// <summary>
    /// Contains a value of T that is attached to an <see cref="IDisposable"/> context while manages their lifecylce together.
    /// </summary>
    /// <typeparam name="T">The type of the value wich is exposed.</typeparam>
    public sealed class DisposableContainer<T> : IDisposable
    {
        private bool _disposed = false;
        private IDisposable _context;

        public T Value { get; }

        public DisposableContainer(IDisposable context, T value)
        {
            _context = context;
            Value = value;
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            _context?.Dispose();
            _context = null;
            if (Value is IDisposable disposableValue) disposableValue.Dispose();
            _disposed = true;
        }
    }
}
