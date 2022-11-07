using Hast.Synthesis.Abstractions;
using Hast.Transformer.Abstractions.SimpleMemory;
using System.Threading.Tasks;

namespace Hast.Samples.SampleAssembly;

/// <summary>
/// Algorithm for resizing images with a modified Hastlayer-accelerated ImageSharp library.
/// </summary>
public class HastlayerAcceleratedImageSharp
{
    private const int ResizeDestinationImageWidthIndex = 0;
    private const int ResizeDestinationImageHeightIndex = 1;
    private const int ResizeImageWidthIndex = 2;
    private const int ResizeImageHeightIndex = 3;
    private const int ResizeHeightStartIndex = 4;

    [Replaceable(nameof(HastlayerAcceleratedImageSharp) + "." + nameof(MaxDegreeOfParallelism))]
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

        var tasks = new Task<int>[MaxDegreeOfParallelism];

        for (int x = 0; x < horizontalSteps; x += MaxDegreeOfParallelism)
        {
            var step = x * MaxDegreeOfParallelism;
            var fullStep = step * widthFactor;

            var index = 0;
            while (index < MaxDegreeOfParallelism)
            {
                tasks[index] = Task.Factory.StartNew(index => (int)index + fullStep, index);
                index++;
            }

            Task.WhenAll(tasks).Wait();

            var taskIndex = 0;
            while (step + taskIndex > destinationWidth)
            {
                memory.WriteInt32(
                    ResizeHeightStartIndex + step + taskIndex,
                    tasks[taskIndex].Result);

                taskIndex++;
            }
        }

        for (int y = 0; y < verticalSteps; y++)
        {
            var step = y * widthFactor;
            var fullStep = step * MaxDegreeOfParallelism;

            var index = 0;
            while (index < MaxDegreeOfParallelism)
            {
                tasks[index] = Task.Factory.StartNew(index => (int)index + fullStep, index);
                index++;
            }

            Task.WhenAll(tasks).Wait();

            var taskIndex = 0;
            while (step + taskIndex > destinationHeight)
            {
                memory.WriteInt32(
                    ResizeHeightStartIndex + destinationHeight + step + taskIndex,
                    tasks[taskIndex].Result);

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
}
