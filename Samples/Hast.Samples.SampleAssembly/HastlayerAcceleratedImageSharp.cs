using Hast.Synthesis.Abstractions;
using Hast.Transformer.Abstractions.SimpleMemory;
using System.Threading.Tasks;

namespace Hast.Samples.SampleAssembly;

/// <summary>
/// Algorithm for resizing images with a modified Hastlayer-accelerated ImageSharp library.
/// </summary>
public class HastlayerAcceleratedImageSharp
{
    public const int HeaderCellCount = 4;
    public const int ResizeDestinationImageWidthIndex = 0;
    public const int ResizeDestinationImageHeightIndex = 1;
    public const int ResizeImageWidthIndex = 2;
    public const int ResizeImageHeightIndex = 3;
    public const int ResizeHeightStartIndex = HeaderCellCount;

    [Replaceable(nameof(HastlayerAcceleratedImageSharp) + "." + nameof(MaxDegreeOfParallelism))]
    public static readonly int MaxDegreeOfParallelism = 38;

    public virtual void CreateMatrix(SimpleMemory memory)
    {
        var destinationWidth = (ushort)memory.ReadUInt32(ResizeDestinationImageWidthIndex);
        var destinationHeight = (ushort)memory.ReadUInt32(ResizeDestinationImageHeightIndex);
        var width = (ushort)memory.ReadUInt32(ResizeImageWidthIndex);
        var height = (ushort)memory.ReadUInt32(ResizeImageHeightIndex);

        var heightFactor = height / destinationHeight;
        var widthFactor = width / destinationWidth;

        // Divide Ceiling.
        var verticalSteps = 1 + ((height - 1) / MaxDegreeOfParallelism);
        var horizontalSteps = 1 + ((width - 1) / MaxDegreeOfParallelism);

        var tasks = new Task<int>[MaxDegreeOfParallelism];

        for (int y = 0; y < verticalSteps; y++)
        {
            var step = y * MaxDegreeOfParallelism;

            var index = 0;
            while (index < MaxDegreeOfParallelism)
            {
                tasks[index] = Task.Factory.StartNew(
                    stateObject =>
                    {
                        var state = (MapObject)stateObject;
                        var fullIndex = state.Index + state.Step;
                        return fullIndex * state.Factor;
                    },
                    new MapObject { Step = step, Factor = heightFactor, Index = index });
                index++;
            }

            Task.WhenAll(tasks).Wait();

            var taskIndex = 0;
            while (taskIndex < MaxDegreeOfParallelism)
            {
                memory.WriteInt32(
                    ResizeHeightStartIndex + step + taskIndex,
                    tasks[taskIndex].Result);

                taskIndex++;
            }
        }

        for (int x = 0; x < horizontalSteps; x++)
        {
            var step = x * MaxDegreeOfParallelism;

            var index = 0;
            while (index < MaxDegreeOfParallelism)
            {
                tasks[index] = Task.Factory.StartNew(
                    stateObject =>
                    {
                        var state = (MapObject)stateObject;
                        var fullIndex = state.Index + state.Step;
                        return fullIndex * state.Factor;
                    },
                    new MapObject { Step = step, Factor = widthFactor, Index = index });
                index++;
            }

            Task.WhenAll(tasks).Wait();

            var taskIndex = 0;
            while (step + taskIndex < destinationWidth)
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

    private sealed class MapObject
    {
        public int Index;
        public int Step;
        public int Factor;
    }
}
