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
    public class ImageSharpSample
    {
        private const ushort Divisor = 2;

        // some values here probably

        [Replaceable(nameof(ImageSharpSample) + "." + nameof(MaxDegreeOfParallelism))]
        private static readonly int MaxDegreeOfParallelism = 25;
    }
}
