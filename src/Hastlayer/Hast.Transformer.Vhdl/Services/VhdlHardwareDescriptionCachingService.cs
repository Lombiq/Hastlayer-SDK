using Hast.Common.Models;
using Hast.Common.Services;
using System.Threading.Tasks;

namespace Hast.Transformer.Vhdl.Services;

public class VhdlHardwareDescriptionCachingService : IVhdlHardwareDescriptionCachingService
{
    private readonly IAppDataFolder _appDataFolder;

    public VhdlHardwareDescriptionCachingService(IAppDataFolder appDataFolder) => _appDataFolder = appDataFolder;

    public async Task<VhdlHardwareDescription> GetHardwareDescriptionAsync(string cacheKey)
    {
        var filePath = GetCacheFilePath(cacheKey);

        if (!_appDataFolder.FileExists(filePath)) return null;

        await using var fileStream = _appDataFolder.OpenFile(filePath);
        return await VhdlHardwareDescription.DeserializeAsync(fileStream);
    }

    public async Task SetHardwareDescriptionAsync(string cacheKey, VhdlHardwareDescription hardwareDescription)
    {
        await using var fileStream = _appDataFolder.CreateFile(GetCacheFilePath(cacheKey));
        await hardwareDescription.SerializeAsync(fileStream);
    }

    private string GetCacheFilePath(string cacheKey)
        => _appDataFolder.Combine("Hastlayer", "VhdlHardwareDescriptionCacheFiles", cacheKey);
}
