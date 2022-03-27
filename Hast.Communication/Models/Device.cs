using System;
using System.Diagnostics.CodeAnalysis;

namespace Hast.Communication.Models;

[SuppressMessage("Minor Code Smell", "S4041:Type names should not match namespaces", Justification = "We won't confuse them.")]
public class Device : IDevice
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
        if (this is not IReservedDevice) Disposing += (_, _) => previousDevice.Dispose();
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed && disposing) Disposing?.Invoke(this, EventArgs.Empty);
        _isDisposed = true;
    }
}
