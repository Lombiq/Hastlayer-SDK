using Hast.Layer;
using Hast.Samples.SampleAssembly.ImageSharpModifications.Resize;
using Hast.Synthesis.Abstractions;
using Hast.Transformer.Abstractions.SimpleMemory;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Threading.Tasks;

namespace Hast.Samples.SampleAssembly
{
    /// <summary>
    /// Algorithm for resizing images with a modified ImageSharp library.
    /// </summary>
    public class ImageSharpSample
    {
        private const int Resize_DestinationImageWidthIndex = 0;
        private const int Resize_DestinationImageHeightIndex = 1;
        private const int Resize_ImageWidthIndex = 2;
        private const int Resize_ImageHeightIndex = 3;
        private const int Resize_HeightStartIndex = 4;

        [Replaceable(nameof(ImageSharpSample) + "." + nameof(MaxDegreeOfParallelism))]
        private static readonly int MaxDegreeOfParallelism = 25;

        public virtual void CreateMatrix(SimpleMemory memory)
        {
            var destinationWidth = (ushort)memory.ReadUInt32(Resize_DestinationImageWidthIndex);
            var destinationHeight = (ushort)memory.ReadUInt32(Resize_DestinationImageHeightIndex);
            var width = (ushort)memory.ReadUInt32(Resize_ImageWidthIndex);
            var height = (ushort)memory.ReadUInt32(Resize_ImageHeightIndex);

            var widthFactor = width / destinationWidth;
            var heightFactor = height / destinationHeight;

            var Resize_WidthStartIndex = Resize_HeightStartIndex + destinationHeight;

            for (int y = 0; y < destinationHeight; y++)
            {
                int rowStartIndex = y * heightFactor; // Add value to an array
                memory.WriteInt32(Resize_HeightStartIndex + y, rowStartIndex);
            }

            for (int x = 0; x < destinationWidth; x++)
            {
                int pixelIndex = x * widthFactor; // Add value to an array
                memory.WriteInt32(Resize_WidthStartIndex + x, pixelIndex);
            }
        }
    }
}
