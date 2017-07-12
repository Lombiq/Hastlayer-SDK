namespace Hast.Communication.Models
{
    public class Device : IDevice
    {
        public string Identifier { get; set; }
        public dynamic Metadata { get; set; }


        public Device()
        {
        }


        public Device(IDevice previousDevice)
        {
            Identifier = previousDevice.Identifier;
            Metadata = previousDevice.Metadata;
        }
    }
}
