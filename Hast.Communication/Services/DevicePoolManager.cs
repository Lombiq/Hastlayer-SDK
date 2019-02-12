using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Hast.Communication.Models;

namespace Hast.Communication.Services
{
    public class DevicePoolManager : IDevicePoolManager
    {
        private readonly object _lock = new object();
        private readonly Queue<Action<IReservedDevice>> _waitQueue = new Queue<Action<IReservedDevice>>();

        private Dictionary<string, PooledDevice> _devicePool = new Dictionary<string, PooledDevice>();


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

        public Task<IReservedDevice> ReserveDevice()
        {
            lock (_lock)
            {
                if (!_devicePool.Any())
                {
                    throw new InvalidOperationException("There are no devices in the device pool (i.e. no connected devices could be detected).");
                }

                // If there is an available device, return a handle to it. If not, then we put the request into a queue.
                // The method then returns a TaskCompletionSource that will complete once the a reserved device is freed
                // up and this request is the next in the queue.

                var firstAvailableDevice = _devicePool.Values.FirstOrDefault(device => !device.IsBusy);

                if (firstAvailableDevice != null)
                {
                    Debug.WriteLine("Found an available device with the identifier {0}.", (object)firstAvailableDevice.Identifier);

                    firstAvailableDevice.IsBusy = true;

                    Action<ReservedDevice> disposer = thisReservedDevice =>
                        {
                            lock (_lock)
                            {
                                if (_waitQueue.Any())
                                {
                                    Debug.WriteLine(
                                        "Dequeuing a device reservation request. Will re-use the device with the ID {0}. {1} items are in the queue.", 
                                        thisReservedDevice.Identifier,
                                        _waitQueue.Count);

                                    // In this case we re-use the current ReservedDevice and the device remains IsBusy.
                                    _waitQueue.Dequeue()(thisReservedDevice);
                                }
                                else
                                {
                                    Debug.WriteLine(
                                        "No device reservation requests are in the queue so freeing up the device with the ID {0}.",
                                        (object)thisReservedDevice.Identifier);

                                    _devicePool[thisReservedDevice.Identifier].IsBusy = false;
                                }
                            }
                        };

                    return Task.FromResult<IReservedDevice>(new ReservedDevice(firstAvailableDevice, disposer));
                }
                else
                {
                    Debug.WriteLine("Enqueuing a device reservation request.");

                    var reservationCompletionSource = new TaskCompletionSource<IReservedDevice>();

                    _waitQueue.Enqueue(freedUpDevice => reservationCompletionSource.SetResult(freedUpDevice));

                    return reservationCompletionSource.Task;
                }
            }
        }


        private class ReservedDevice : Device, IReservedDevice
        {
            private readonly Action<ReservedDevice> _disposer;


            public ReservedDevice(IDevice baseDevice, Action<ReservedDevice> disposer) : base(baseDevice)
            {
                _disposer = disposer;
            }


            public void Dispose()
            {
                _disposer(this);
            }
        }
    }
}
