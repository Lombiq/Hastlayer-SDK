using Hast.Synthesis.Models;
using System.Diagnostics.CodeAnalysis;

namespace Hast.Catapult.Models;

[SuppressMessage(
    "Minor Code Smell",
    "S2094:Classes should not be empty",
    Justification = "The type is used to indetify the device.")]
public class CatapultDeviceManifest : DeviceManifest
{
}
