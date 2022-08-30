using Hast.Synthesis.Abstractions;
using Hast.Transformer.Abstractions.SimpleMemory;
using System.Threading.Tasks;

namespace Hast.Samples.SampleAssembly;

/// <summary>
/// Algorithm for resizing images with a modified Hastlayer-accelerated ImageSharp library.
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

        // Divide Ceiling.
        var verticalSteps = 1 + ((height - 1) / MaxDegreeOfParallelism);
        var horizontalSteps = 1 + ((width - 1) / MaxDegreeOfParallelism);

        var tasks = new Task<IndexOutput>[MaxDegreeOfParallelism];

        for (int x = 0; x < horizontalSteps; x += MaxDegreeOfParallelism)
        {
            var step = x * MaxDegreeOfParallelism;

            for (int i = 0; i < MaxDegreeOfParallelism; i++)
            {
                tasks[i] = Task.Factory.StartNew(() => new IndexOutput { Index = i + (step * widthFactor), });
            }

            Task.WhenAll(tasks).Wait();

            var taskIndex = 0;
            while ((x * MaxDegreeOfParallelism) + taskIndex > destinationWidth)
            {
                memory.WriteInt32(
                    ResizeHeightStartIndex + step + taskIndex,
                    tasks[taskIndex].Result.Index);

                taskIndex++;
            }
        }

        for (int y = 0; y < verticalSteps; y++)
        {
            var step = y * widthFactor;

            for (int i = 0; i < MaxDegreeOfParallelism; i++)
            {
                tasks[i] = Task.Factory.StartNew(() => new IndexOutput { Index = i + (step * MaxDegreeOfParallelism), });
            }

            Task.WhenAll(tasks).Wait();

            var taskIndex = 0;
            while ((y * MaxDegreeOfParallelism) + taskIndex > destinationHeight)
            {
                memory.WriteInt32(
                    ResizeHeightStartIndex + destinationHeight + step + taskIndex,
                    tasks[taskIndex].Result.Index);

                taskIndex++;
            }
        }
    }

    // Serialized code left here for example.
    //// for (int y = 0; y < destinationHeight; y++)
    //// {
    ////     int rowStartIndex = y * heightFactor; // Add value to an array
    ////     memory.WriteInt32(Resize_HeightStartIndex + y, rowStartIndex);
    //// }
    //// for (int x = 0; x < destinationWidth; x++)
    //// {
    ////     int pixelIndex = x * widthFactor; // Add value to an array
    ////     memory.WriteInt32(Resize_WidthStartIndex + x, pixelIndex);
    //// }

    private struct IndexOutput
    {
        public int Index { get; set; }
    }
}
