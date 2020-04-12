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

        /// <summary>
        /// Creates a new instance with the specified value and removes the context from this instance.
        /// </summary>
        /// <typeparam name="T2">The value type of the result.</typeparam>
        /// <param name="value">The value exposed value of the result.</param>
        /// <returns>The result which will own this instance's context.</returns>
        /// <remarks>
        /// This instance is still responsible for the lifecylce of the <see cref="Value"/> and disposes of it, if
        /// <see cref="IDisposable"/>
        /// </remarks>
        public DisposableContainer<T2> TransferContext<T2>(T2 value)
        {
            var newContainer = new DisposableContainer<T2>(_context, value);
            _context = null;
            return newContainer;
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
