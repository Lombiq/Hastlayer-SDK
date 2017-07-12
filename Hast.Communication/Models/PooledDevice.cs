namespace Hast.Communication.Models
{
    public class PooledDevice : Device, IPooledDevice
    {
        public bool IsBusy { get; set; }


        public PooledDevice(IDevice baseDevice) : base(baseDevice)
        {
        }
    }
}
