namespace Hast.Communication.Models
{
    /// <summary>
    /// A connected hardware-executing device that is reserved for a session and thus can't handle anything else.
    /// </summary>
    public interface IReservedDevice : IDevice
    {
    }
}
