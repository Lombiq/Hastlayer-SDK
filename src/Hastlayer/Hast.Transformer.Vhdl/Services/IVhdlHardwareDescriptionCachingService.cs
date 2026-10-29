using Hast.Common.Interfaces;
using Hast.Common.Models;
using Hast.Common.Services;
using System.Threading.Tasks;

namespace Hast.Transformer.Vhdl.Services;

/// <summary>
/// Service for caching the <see cref="VhdlHardwareDescription"/> in the file system in <see cref="IAppDataFolder"/>.
/// </summary>
public interface IVhdlHardwareDescriptionCachingService : IDependency
{
    /// <summary>
    /// Retrieves the cached <see cref="VhdlHardwareDescription"/> if it exists.
    /// </summary>
    Task<VhdlHardwareDescription> GetHardwareDescriptionAsync(string cacheKey);

    /// <summary>
    /// Stores the <paramref name="hardwareDescription"/> into a file named after the <paramref name="cacheKey"/>.
    /// </summary>
    Task SetHardwareDescriptionAsync(string cacheKey, VhdlHardwareDescription hardwareDescription);
}
