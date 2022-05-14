using Hast.Communication.Services;
using System.Collections.Generic;

namespace Hast.Communication.Models;

public class MemoryResourceProblem
{
    public IMemoryResourceChecker Sender { get; set; }

    public string Message { get; set; } = string.Empty;

    public ulong MemoryByteCount { get; set; }

    public ulong AvailableByteCount { get; set; }

    public IDictionary<string, object> AdditionalData { get; } = new Dictionary<string, object>();
}
