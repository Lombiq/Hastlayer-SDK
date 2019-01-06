using System;

namespace Hast.Communication.Models
{
    public class Device : IDevice, IDisposable
    {
        private bool _isDisposed;
        public event EventHandler Disposing;

        public string Identifier { get; set; }
        public dynamic Metadata { get; set; }


        public Device()
        {
        }

        public Device(string identifier, dynamic metadata, EventHandler disposingEventHandler)
        {
            Identifier = identifier;
            Metadata = metadata;
            Disposing += disposingEventHandler;
        }


        public Device(IDevice previousDevice)
        {
            Identifier = previousDevice.Identifier;
            Metadata = previousDevice.Metadata;
            Disposing += (sender, arguments) => previousDevice.Dispose();
        }

        public void Dispose()
        {
            if (!_isDisposed) Disposing?.Invoke(this, new EventArgs());
            _isDisposed = true;
        }
    }
}
