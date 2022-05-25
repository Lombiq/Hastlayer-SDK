using System;
using System.Diagnostics.CodeAnalysis;

namespace Hast.Communication.Models;

[SuppressMessage("Minor Code Smell", "S4041:Type names should not match namespaces", Justification = "We won't confuse them.")]
public class Device : IDevice
{
    private bool _isDisposed;

    protected bool _runDisposingEventHandler = true;

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
        Disposing += (_, _) => previousDevice.Dispose();
    }

    // It is implemented like the pattern, just with an extra parameter on the protected Dispose().
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed && disposing && _runDisposingEventHandler) Disposing?.Invoke(this, EventArgs.Empty);
        _isDisposed = true;
    }
}
