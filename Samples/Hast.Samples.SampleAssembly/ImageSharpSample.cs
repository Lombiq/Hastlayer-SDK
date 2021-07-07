using Hast.Synthesis.Abstractions;
using Hast.Transformer.Abstractions.SimpleMemory;
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

            var verticalSteps = 1 + ((height - 1) / MaxDegreeOfParallelism);
            var horizontalSteps = 1 + ((width - 1) / MaxDegreeOfParallelism);

            var tasks = new Task<IndexOutput>[MaxDegreeOfParallelism];

            var rowIndeces = new int[destinationHeight];
            var pixelIndeces = new int[destinationWidth];

            for (int x = 0; x < horizontalSteps; x++)
            {
                for (int t = 0; t < MaxDegreeOfParallelism; t++)
                {
                    tasks[t] = Task.Factory.StartNew(() =>
                    {
                        return new IndexOutput
                        {
                            Index = t + x * widthFactor * MaxDegreeOfParallelism
                        };
                    });
                }

                Task.WhenAll(tasks).Wait();

                for (int t = 0; t < MaxDegreeOfParallelism; t++)
                {
                    if (x * MaxDegreeOfParallelism + t > destinationWidth) break;

                    memory.WriteInt32(
                        Resize_HeightStartIndex + x * MaxDegreeOfParallelism + t,
                        tasks[t].Result.Index);
                }
            }

            for (int y = 0; y < verticalSteps; y++)
            {
                for (int t = 0; t < MaxDegreeOfParallelism; t++)
                {
                    tasks[t] = Task.Factory.StartNew(() =>
                    {
                        return new IndexOutput
                        {
                            Index = t + y * widthFactor * MaxDegreeOfParallelism
                        };
                    });
                }

                Task.WhenAll(tasks).Wait();

                for (int t = 0; t < MaxDegreeOfParallelism; t++)
                {
                    if (y * MaxDegreeOfParallelism + t > destinationHeight) break;

                    memory.WriteInt32(
                        Resize_HeightStartIndex + destinationHeight + y * MaxDegreeOfParallelism + t,
                        tasks[t].Result.Index);
                }
            }
        }

        // Serialized 

        //for (int y = 0; y < destinationHeight; y++)
        //{
        //    int rowStartIndex = y * heightFactor; // Add value to an array
        //    memory.WriteInt32(Resize_HeightStartIndex + y, rowStartIndex);
        //}

        //for (int x = 0; x < destinationWidth; x++)
        //{
        //    int pixelIndex = x * widthFactor; // Add value to an array
        //    memory.WriteInt32(Resize_WidthStartIndex + x, pixelIndex);
        //}

        private class IndexOutput
        {
            public int Index { get; set; }
        }
    }
}
