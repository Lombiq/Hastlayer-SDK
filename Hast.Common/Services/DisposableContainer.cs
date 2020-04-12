using System;

namespace Hast.Common.Services
{
    public sealed class DisposableContainer<T> : IDisposable
    {
        private IDisposable _context;

        public T Value { get; }


        public DisposableContainer(IDisposable context, T value)
        {
            _context = context;
            Value = value;
        }

        public void Dispose()
        {
            if (_context is null) return;

            _context.Dispose();
            _context = null;
            if (Value is IDisposable disposableValue) disposableValue?.Dispose();
        }
    }
}
