using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Transformer.Abstractions.SimpleMemory;
using Hast.Samples.SampleAssembly.ImageSharpModifications.Resize;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Threading.Tasks;
using Hast.Samples.SampleAssembly.ImageSharpModifications.Extensions;
using Bitmap = System.Drawing.Bitmap;
using Color = System.Drawing.Color;
using SixLabors.ImageSharp.PixelFormats;

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
        private const int Resize_ImageStartIndex = 4;

        [Replaceable(nameof(ImageSharpSample) + "." + nameof(MaxDegreeOfParallelism))]
        private static readonly int MaxDegreeOfParallelism = 25;
        private static readonly int Size = 42588;

        public virtual void ApplyTransform(SimpleMemory memory)
        {
            var width = (ushort)memory.ReadUInt32(Resize_ImageWidthIndex);
            var height = (ushort)memory.ReadUInt32(Resize_ImageHeightIndex);
            var destWidth = (ushort)memory.ReadUInt32(Resize_DestinationImageWidthIndex);
            var destHeight = (ushort)memory.ReadUInt32(Resize_DestinationImageHeightIndex);
            var destinationStartIndex = width * height + 4;

            var widthFactor = width / destWidth;
            var heightFactor = height / destHeight;

            var tasks = new Task<PixelProcessingTaskOutput>[MaxDegreeOfParallelism];

            var stepCount = destWidth / MaxDegreeOfParallelism;
            var wastedSteps = destWidth % MaxDegreeOfParallelism;

            if (wastedSteps != 0)
            {
                stepCount += 1;
            }

            for (int y = 0; y < destHeight; y++)
            {
                for (int x = 0; x < stepCount; x++)
                {
                    for (int t = 0; t < MaxDegreeOfParallelism; t++)
                    {
                        var pixelBytes = memory.Read4Bytes(
                            y * heightFactor * destWidth * heightFactor + x * widthFactor + t + Resize_ImageStartIndex);

                        tasks[t] = Task.Factory.StartNew(inputObject =>
                        {
                            var input = (PixelProcessingTaskInput)inputObject;

                            return new PixelProcessingTaskOutput
                            {
                                R = input.PixelBytes[0],
                                G = input.PixelBytes[1],
                                B = input.PixelBytes[2]
                            };

                        },
                        new PixelProcessingTaskInput { PixelBytes = pixelBytes });
                    }

                    Task.WhenAll(tasks).Wait();

                    for (int t = 0; t < MaxDegreeOfParallelism; t++)
                    {
                        // Don't write unnecessary stuff leftover from the process
                        if (x + t >= destWidth)
                        {
                            break;
                        }

                        memory.Write4Bytes(
                           destinationStartIndex + x + y * destWidth + t,
                           new[] { tasks[t].Result.R, tasks[t].Result.G, tasks[t].Result.B });
                    }
                }
            }


        }

        internal virtual void Run(SimpleMemory memory) => ApplyTransform(memory);

        public Image Resize(Image image, IHastlayer hastlayer, IHardwareGenerationConfiguration hardwareGenerationConfiguration)
        {
            image.Mutate(x => x.HastResize(image.Width / 2, image.Height / 2, MaxDegreeOfParallelism, hastlayer, hardwareGenerationConfiguration));

            return image;
        }

        public SimpleMemory CreateSimpleMemory(
            Image image,
            IHastlayer hastlayer,
            IHardwareGenerationConfiguration hardwareGenerationConfiguration)
        {
            var pixelCount = image.Width * image.Height + (image.Width / 2) * (image.Height / 2); // TODO: get the value
            var cellCount = pixelCount
                + (pixelCount % MaxDegreeOfParallelism != 0 ? MaxDegreeOfParallelism : 0)
                + 4;
            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(cellCount)
                : hastlayer.CreateMemory(hardwareGenerationConfiguration, cellCount);

            memory.WriteUInt32(Resize_ImageWidthIndex, (uint)image.Width);
            memory.WriteUInt32(Resize_ImageHeightIndex, (uint)image.Height);
            memory.WriteUInt32(Resize_DestinationImageWidthIndex, (uint)image.Width / 2);   // TODO: get the value
            memory.WriteUInt32(Resize_DestinationImageHeightIndex, (uint)image.Height / 2); // TODO: get the value

            var bitmapImage = ImageSharpExtensions.ToBitmap(image);
            for (int y = 0; y < bitmapImage.Height; y++)
            {
                for (int x = 0; x < bitmapImage.Width; x++)
                {
                    var pixel = bitmapImage.GetPixel(x, y);

                    memory.Write4Bytes(
                       x + y * bitmapImage.Width + Resize_ImageStartIndex,
                       new[] { pixel.R, pixel.G, pixel.B });
                }
            }

            return memory;
        }

        public Image ConvertToImage(
            SimpleMemory memory,
            IHastlayer hastlayer,
            IHardwareGenerationConfiguration hardwareGenerationConfiguration)
        {
            var width = (ushort)memory.ReadUInt32(Resize_ImageWidthIndex);
            var height = (ushort)memory.ReadUInt32(Resize_ImageHeightIndex);
            var destWidth = (ushort)memory.ReadUInt32(Resize_DestinationImageWidthIndex);
            var destHeight = (ushort)memory.ReadUInt32(Resize_DestinationImageHeightIndex);
            int destinationStartIndex = width * height + 4;

            var bmp = new Bitmap(destWidth, destHeight);

            for (int y = 0; y < destHeight; y++)
            {
                for (int x = 0; x < destWidth; x++)
                {
                    var pixel = memory.Read4Bytes(x + destWidth * y + destinationStartIndex);
                    var color = Color.FromArgb(pixel[0], pixel[1], pixel[2]);
                    bmp.SetPixel(x, y, color);
                }
            }

            var image = ImageSharpExtensions.ToImageSharpImage(bmp);

            return image;
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
        }
    }
}
