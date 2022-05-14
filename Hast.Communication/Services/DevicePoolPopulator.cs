using Hast.Communication.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hast.Communication.Services;

public class DevicePoolPopulator : IDevicePoolPopulator
{
    private readonly object _lock = new();
    private readonly IDevicePoolManager _devicePoolManager;
    private readonly ILogger _logger;
    private bool _poolIsPopulated;

    public DevicePoolPopulator(IDevicePoolManager devicePoolManager, ILogger<DevicePoolPopulator> logger)
    {
        _devicePoolManager = devicePoolManager;
        _logger = logger;
    }

    public void PopulateDevicePoolIfNew(Func<Task<IEnumerable<IDevice>>> devicesFactory)
    {
        lock (_lock)
        {
            if (_poolIsPopulated) return;

            try
            {
                // We can't use await here due to locking. To minimize the danger of a deadlock the awaiter is
                // configured not to continue on the captured context.
                // See: http://blog.stephencleary.com/2012/07/dont-block-on-async-code.html
                _devicePoolManager.SetDevicePool(devicesFactory().ConfigureAwait(false).GetAwaiter().GetResult());
                _poolIsPopulated = true;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "An exception has occurred during device pool population.");
                throw;
            }
        }
    }
}
