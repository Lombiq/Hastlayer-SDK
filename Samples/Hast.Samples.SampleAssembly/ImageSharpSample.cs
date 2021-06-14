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
    /// Algorithm for resizing image with a modified Image Sharp library
    /// </summary>
    public class ImageSharpSample
    {
        private const int Resize_ImageWidthIndex = 0;
        private const int Resize_ImageHeightIndex = 1;
        private const int Resize_DestinationImageWidthIndex = 2;
        private const int Resize_DestinationImageHeightIndex = 3;
        private const int Resize_FrameCount = 4;
        private const int Resize_ImageStartIndex = 5;

        [Replaceable(nameof(ImageSharpSample) + "." + nameof(MaxDegreeOfParallelism))]
        private static readonly int MaxDegreeOfParallelism = 25;

        public virtual void ApplyTransform(SimpleMemory memory)
        {
            var width = (ushort)memory.ReadUInt32(Resize_ImageWidthIndex);
            var height = (ushort)memory.ReadUInt32(Resize_ImageHeightIndex);
            var destWidth = (ushort)memory.ReadUInt32(Resize_DestinationImageWidthIndex);
            var destHeight = (ushort)memory.ReadUInt32(Resize_DestinationImageHeightIndex);
            var frameCount = (ushort)memory.ReadUInt32(Resize_FrameCount);
            var destinationStartIndex = width * height + 5;

            var widthFactor = width / destWidth;
            var heightFactor = height / destHeight;

            var tasks = new Task<PixelProcessingTaskOutput>[MaxDegreeOfParallelism];

            var stepCount = destWidth / MaxDegreeOfParallelism;
            var wastedSteps = destWidth % MaxDegreeOfParallelism;

            if (wastedSteps != 0)
            {
                stepCount += 1;
            }

            for (int f = 0; f < frameCount; f++)
            {
                for (int y = 0; y < destHeight; y++)
                {
                    for (int x = 0; x < stepCount; x++)
                    {
                        for (int t = 0; t < MaxDegreeOfParallelism; t++)
                        {
                            if (x * widthFactor * MaxDegreeOfParallelism + t * widthFactor >= width)
                            {
                                break;
                            }

                            var pixelBytes = memory.Read4Bytes(
                                (1 + f) * (y * heightFactor * destWidth * heightFactor
                                    + x * widthFactor * MaxDegreeOfParallelism
                                    + t * widthFactor) + Resize_ImageStartIndex);

                            tasks[t] = Task.Factory.StartNew(inputObject =>
                            {
                                var input = (PixelProcessingTaskInput)inputObject;

                                return new PixelProcessingTaskOutput
                                {
                                    R = input.PixelBytes[0],
                                    G = input.PixelBytes[1],
                                    B = input.PixelBytes[2],
                                    A = input.PixelBytes[3]
                                };

                            },
                            new PixelProcessingTaskInput { PixelBytes = pixelBytes });
                        }

                        Task.WhenAll(tasks).Wait();

                        for (int t = 0; t < MaxDegreeOfParallelism; t++)
                        {
                            // Don't write unnecessary stuff leftover from the process
                            if (x * widthFactor * MaxDegreeOfParallelism + t * widthFactor >= width)
                            {
                                break;
                            }

                            memory.Write4Bytes(
                               destinationStartIndex + (1 + f) * (x * MaxDegreeOfParallelism + y * destWidth + t),
                               new[] { tasks[t].Result.R, tasks[t].Result.G, tasks[t].Result.B, tasks[t].Result.A });
                        }
                    }
                }
            }
        }

        internal virtual void Run(SimpleMemory memory) => ApplyTransform(memory);

        public Image Resize(Image image, IHastlayer hastlayer, IHardwareGenerationConfiguration hardwareGenerationConfiguration)
        {
            var newImage = image.Clone(x => x.HastResize(image.Width / 2, image.Height / 2, MaxDegreeOfParallelism, hastlayer, hardwareGenerationConfiguration));

            return newImage;
        }

        private class PixelProcessingTaskInput
        {
            public byte[] PixelBytes { get; set; }
        }

        private class PixelProcessingTaskOutput
        {
            public byte R { get; set; }
            public byte G { get; set; }
            public byte B { get; set; }
            public byte A { get; set; }
        }
    }
}
