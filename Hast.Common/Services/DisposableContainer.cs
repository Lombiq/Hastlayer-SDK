using System;
using System.Collections.Generic;
using System.Text;

namespace Hast.Common.Services
{
    public class DisposableContainer<T> : IDisposable
    {
        private IDisposable context;

        public T Value { get; }

        public DisposableContainer(IDisposable context, T value)
        {
            this.context = context;
            Value = value;
        }

        public void Dispose()
        {
            if (context is null) return;
            
            context.Dispose();
            context = null;
            if (Value is IDisposable disposableValue) disposableValue?.Dispose();
        }
    }
}
