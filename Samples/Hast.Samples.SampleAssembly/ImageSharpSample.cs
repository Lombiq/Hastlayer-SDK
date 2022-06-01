using Hast.Synthesis.Abstractions;
using Hast.Transformer.Abstractions.SimpleMemory;
using System.Threading.Tasks;

namespace Hast.Samples.SampleAssembly;

/// <summary>
/// Algorithm for resizing images with a modified ImageSharp library.
/// </summary>
public class ImageSharpSample
{
    private const int ResizeDestinationImageWidthIndex = 0;
    private const int ResizeDestinationImageHeightIndex = 1;
    private const int ResizeImageWidthIndex = 2;
    private const int ResizeImageHeightIndex = 3;
    private const int ResizeHeightStartIndex = 4;

    [Replaceable(nameof(ImageSharpSample) + "." + nameof(MaxDegreeOfParallelism))]
    private static readonly int MaxDegreeOfParallelism = 25;

    public virtual void CreateMatrix(SimpleMemory memory)
    {
        var destinationWidth = (ushort)memory.ReadUInt32(ResizeDestinationImageWidthIndex);
        var destinationHeight = (ushort)memory.ReadUInt32(ResizeDestinationImageHeightIndex);
        var width = (ushort)memory.ReadUInt32(ResizeImageWidthIndex);
        var height = (ushort)memory.ReadUInt32(ResizeImageHeightIndex);

        var widthFactor = width / destinationWidth;
        var heightFactor = height / destinationHeight;

        var resizeWidthStartIndex = ResizeHeightStartIndex + destinationHeight;

        var verticalSteps = 1 + ((height - 1) / MaxDegreeOfParallelism);
        var horizontalSteps = 1 + ((width - 1) / MaxDegreeOfParallelism);

        var tasks = new Task<IndexOutput>[MaxDegreeOfParallelism];

        var rowIndeces = new int[destinationHeight];
        var pixelIndeces = new int[destinationWidth];

        for (int x = 0; x < horizontalSteps; x++)
        {
            for (int t = 0; t < MaxDegreeOfParallelism; t++)
            {
                tasks[t] = Task.Factory.StartNew(() => new IndexOutput
                {
                    Index = t + (x * widthFactor * MaxDegreeOfParallelism),
                });
            }

            Task.WhenAll(tasks).Wait();

            for (int t = 0; t < MaxDegreeOfParallelism; t++)
            {
                if ((x * MaxDegreeOfParallelism) + t > destinationWidth) break;

                memory.WriteInt32(
                    ResizeHeightStartIndex + (x * MaxDegreeOfParallelism) + t,
                    tasks[t].Result.Index);
            }
        }

        for (int y = 0; y < verticalSteps; y++)
        {
            for (int t = 0; t < MaxDegreeOfParallelism; t++)
            {
                tasks[t] = Task.Factory.StartNew(() => new IndexOutput
                {
                    Index = t + (y * widthFactor * MaxDegreeOfParallelism),
                });
            }

            Task.WhenAll(tasks).Wait();

            for (int t = 0; t < MaxDegreeOfParallelism; t++)
            {
                if ((y * MaxDegreeOfParallelism) + t > destinationHeight) break;

                memory.WriteInt32(
                    ResizeHeightStartIndex + destinationHeight + (y * MaxDegreeOfParallelism) + t,
                    tasks[t].Result.Index);
            }
        }
    }

#pragma warning disable S125 // Sections of code should not be commented out
    // Serialized code left here for example.
    // for (int y = 0; y < destinationHeight; y++)
    // {
    //     int rowStartIndex = y * heightFactor; // Add value to an array
    //     memory.WriteInt32(Resize_HeightStartIndex + y, rowStartIndex);
    // }

    // for (int x = 0; x < destinationWidth; x++)
    // {
    //     int pixelIndex = x * widthFactor; // Add value to an array
    //     memory.WriteInt32(Resize_WidthStartIndex + x, pixelIndex);
    // }
#pragma warning restore S125 // Sections of code should not be commented out
    private sealed class IndexOutput
    {
        public int Index { get; set; }
    }
}
