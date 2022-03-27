using System;

namespace Hast.Common.Services;

// There is intentionally no service ancestor because this must be used as an EventArgs descendant.
public class ServiceEventArgs<T> : EventArgs
{
    public T Arguments { get; set; }

    public ServiceEventArgs(T arguments) => Arguments = arguments;
}
