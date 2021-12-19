using Hast.Communication.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hast.Communication.Services
{
    public sealed class DevicePoolManager : IDevicePoolManager
    {
        private readonly ILogger<DevicePoolManager> _logger;
        private readonly object _lock = new();
        private readonly Queue<Action<IReservedDevice>> _waitQueue = new();

        private bool _isDisposed;

        private Dictionary<string, PooledDevice> _devicePool = new();

        public DevicePoolManager(ILogger<DevicePoolManager> logger) => _logger = logger;

        public void SetDevicePool(IEnumerable<IDevice> devices)
        {
            lock (_lock)
            {
                _devicePool = devices
                    .Select(device => new PooledDevice(device))
                    .ToDictionary(device => device.Identifier);

                _waitQueue.Clear();
            }
        }

        public IEnumerable<IPooledDevice> GetDevicesInPool()
        {
            lock (_lock)
            {
                // Copying the collection so no issue can arise in multi-thread access.
                return _devicePool.Values.ToArray();
            }
        }

        public Task<IReservedDevice> ReserveDeviceAsync()
        {
            lock (_lock)
            {
                if (!_devicePool.Any())
                {
                    throw new InvalidOperationException("There are no devices in the device pool (i.e. no connected devices could be detected).");
                }

                // If there is an available device, return a handle to it. If not, then we put the request into a
                // queue. The method then returns a TaskCompletionSource that will complete once the a reserved device
                // is freed up and this request is the next in the queue.

                var firstAvailableDevice = _devicePool.Values.FirstOrDefault(device => !device.IsBusy);

                if (firstAvailableDevice != null)
                {
                    _logger.LogDebug("Found an available device with the identifier {0}.", (object)firstAvailableDevice.Identifier);

                    firstAvailableDevice.IsBusy = true;

                    void Disposer(ReservedDevice thisReservedDevice)
                    {
                        lock (_lock)
                        {
                            if (_waitQueue.Any())
                            {
                                _logger.LogDebug(
                                    "Dequeuing a device reservation request. Will re-use the device with the ID {0}. {1} items are in the queue.",
                                    thisReservedDevice.Identifier,
                                    _waitQueue.Count);

                                // In this case we re-use the current ReservedDevice and the device remains IsBusy.
                                _waitQueue.Dequeue()(thisReservedDevice);
                            }
                            else
                            {
                                _logger.LogDebug(
                                    "No device reservation requests are in the queue so freeing up the device with the ID {0}.",
                                    (object)thisReservedDevice.Identifier);

                                _devicePool[thisReservedDevice.Identifier].IsBusy = false;
                            }
                        }
                    }

                    return Task.FromResult<IReservedDevice>(new ReservedDevice(firstAvailableDevice, Disposer));
                }
                else
                {
                    _logger.LogDebug("Enqueuing a device reservation request.");

                    var reservationCompletionSource = new TaskCompletionSource<IReservedDevice>();

                    _waitQueue.Enqueue(freedUpDevice => reservationCompletionSource.SetResult(freedUpDevice));

                    return reservationCompletionSource.Task;
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            foreach (var device in GetDevicesInPool()) device.Dispose();

            _isDisposed = true;
        }

        private class ReservedDevice : Device, IReservedDevice
        {
            private readonly Action<ReservedDevice> _disposer;

            public ReservedDevice(IDevice baseDevice, Action<ReservedDevice> disposer)
                : base(baseDevice) => _disposer = disposer;

            protected override void Dispose(bool disposing)
            {
                _disposer(this);
                base.Dispose(disposing);
            }
        }
    }
}
