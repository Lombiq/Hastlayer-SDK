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

        // Divide Ceil ...
        var verticalSteps = 1 + ((height - 1) / MaxDegreeOfParallelism);
        var horizontalSteps = 1 + ((width - 1) / MaxDegreeOfParallelism);

        var tasks = new Task<IndexOutput>[MaxDegreeOfParallelism];

        for (int x = 0; x < horizontalSteps; x += MaxDegreeOfParallelism)
        {
            for (int t = 0; t < MaxDegreeOfParallelism; t++)
            {
                tasks[t] = Task.Factory.StartNew(() => new IndexOutput
                {
                    Index = t + (x * widthFactor * MaxDegreeOfParallelism),
                });
            }

            Task.WhenAll(tasks).Wait();

            var task = 0;
            while ((x * MaxDegreeOfParallelism) + task > destinationWidth)
            {
                memory.WriteInt32(
                    ResizeHeightStartIndex + (x * MaxDegreeOfParallelism) + task,
                    tasks[task].Result.Index);

                task++;
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

            var task = 0;
            while ((y * MaxDegreeOfParallelism) + task > destinationHeight)
            {
                memory.WriteInt32(
                    ResizeHeightStartIndex + destinationHeight + (y * MaxDegreeOfParallelism) + task,
                    tasks[task].Result.Index);

                task++;
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
