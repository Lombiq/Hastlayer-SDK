using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hast.Communication.Models;

namespace Hast.Communication.Services
{
    public class DevicePoolPopulator : IDevicePoolPopulator
    {
        private readonly object _lock = new object();
        private readonly IDevicePoolManager _devicePoolManager;

        private bool _poolIsPopulated = false;


        public DevicePoolPopulator(IDevicePoolManager devicePoolManager)
        {
            _devicePoolManager = devicePoolManager;
        }


        public void PopulateDevicePoolIfNew(Func<Task<IEnumerable<IDevice>>> devicesFactory)
        {
            lock (_lock)
            {
                if (_poolIsPopulated) return;

                // We can't use await here due to locking. To minimize the danger of a deadlock the awaiter
                // is configured not to continue on the captured context.
                // See: http://blog.stephencleary.com/2012/07/dont-block-on-async-code.html
                _devicePoolManager.SetDevicePool(devicesFactory().ConfigureAwait(false).GetAwaiter().GetResult());
                _poolIsPopulated = true;
            }
        }
    }
}
