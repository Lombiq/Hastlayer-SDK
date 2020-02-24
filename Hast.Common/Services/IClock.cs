using Hast.Common.Interfaces;
using System;

namespace Hast.Common.Services
{
    public interface IClock : ISingletonDependency
    {
        DateTime UtcNow { get; }
    }
}