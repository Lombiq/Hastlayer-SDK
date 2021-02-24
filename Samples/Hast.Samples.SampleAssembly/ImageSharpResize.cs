using Hast.Transformer.Abstractions.SimpleMemory;
using System.Drawing;
using System.Threading.Tasks;
using Hast.Layer;
using Hast.Synthesis.Abstractions;

namespace Hast.Samples.SampleAssembly
{
    /// <summary>
    /// Algorithm for resizing image with a modified Image Sharp library
    /// </summary>
    public class ImageSharpResize
    {
        private const ushort Divisor = 2;

        // some values here probably

        [Replaceable(nameof(ImageSharpResize) + "." + nameof(MaxDegreeOfParallelism))]
        private static readonly int MaxDegreeOfParallelism = 25;
    }
}
