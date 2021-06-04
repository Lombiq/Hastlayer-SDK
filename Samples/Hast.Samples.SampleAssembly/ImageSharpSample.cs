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

        public virtual void ApplyTransform(SimpleMemory memory)
        {
            var width = (ushort)memory.ReadUInt32(Resize_ImageWidthIndex);
            var height = (ushort)memory.ReadUInt32(Resize_ImageHeightIndex);
            var destWidth = (ushort)memory.ReadUInt32(Resize_DestinationImageWidthIndex);
            var destHeight = (ushort)memory.ReadUInt32(Resize_DestinationImageHeightIndex);
            var destinationStartIndex = width * height + 4;

            var widthFactor = width / destWidth;
            var heightFactor = height / destHeight;

            var pixelCount = destHeight * destWidth;
            var stepCount = pixelCount / MaxDegreeOfParallelism;

            if (pixelCount % MaxDegreeOfParallelism != 0)
            {
                // This will take care of the rest of the pixels. This is wasteful as on the last step not all Tasks
                // will work on something but it's a way to keep the number of Tasks constant.
                stepCount += 1;
            }

            // u sure? TODO
            var verticalStep = 1 + ((destHeight - 1) / stepCount);

            var tasks = new Task[MaxDegreeOfParallelism];

            for (int t = 0; t < MaxDegreeOfParallelism; t++)
            {
                tasks[t] = Task.Factory.StartNew(
                    inputObject =>
                    {
                        var yMin = 0 + (int)inputObject * verticalStep;
                        if (yMin > destHeight) return;

                        var yMax = System.Math.Min(yMin + verticalStep, destHeight);

                        for (int y = yMin; y < yMax; y++)
                        {
                            for (int x = 0; x < destWidth; x++)
                            {
                                var sourcePixel = memory.Read4Bytes(y * destWidth * heightFactor + x * widthFactor + Resize_ImageStartIndex);
                                memory.Write4Bytes(y * destWidth + x + destinationStartIndex, sourcePixel);
                            }
                        }

                    }, t);
            }

            Task.WhenAll(tasks).Wait();
        }

        internal virtual void Run(SimpleMemory memory) => ApplyTransform(memory);

        public Image Resize(Image image, IHastlayer hastlayer, IHardwareGenerationConfiguration hardwareGenerationConfiguration)
        {
            //image.Mutate(x => x.HastResize(image.Width / 2, image.Height / 2, MaxDegreeOfParallelism, parameters));
            image.Mutate(x => x.HastResize(image.Width / 2, image.Height / 2, MaxDegreeOfParallelism, hastlayer, hardwareGenerationConfiguration));

            return image;
        }

        // NEW METHODS
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
            memory.WriteUInt32(Resize_DestinationImageWidthIndex, (uint)image.Width/2);   // TODO: get the value
            memory.WriteUInt32(Resize_DestinationImageHeightIndex, (uint)image.Height/2); // TODO: get the value

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
    }
}
