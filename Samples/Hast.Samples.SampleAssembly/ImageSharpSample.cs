using Hast.Transformer.Abstractions.SimpleMemory;
using System.Threading.Tasks;
using Hast.Layer;
using Hast.Synthesis.Abstractions;
using SixLabors.ImageSharp;
using ImageSharpHastlayerExtension.Resize;
using SixLabors.ImageSharp.Processing;
using Bitmap = System.Drawing.Bitmap;
using System.IO;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Png;
using Hast.Samples.SampleAssembly.ImageSharpModifications.Extensions;
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
            int destinationStartIndex = width * height + 4;

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

            var tasks = new Task[MaxDegreeOfParallelism];


            //for (int t = 0; t < MaxDegreeOfParallelism; t++)
            //{
            //    tasks[t] = Task.Factory.StartNew(
            //        inputObject =>
            //        {
            //            // írjak valamit be ide.
            //            // TODO Check SIMD
            //        }, t);
            //}

            //Task.WhenAll(tasks).Wait();

            //// Serialized
            //for (int y = 0; y < destHeight; y++)
            //{
            //    for (int x = 0; x < destWidth; x++)
            //    {
            //        var pixel = memory.Read4Bytes(x * widthFactor + y * heightFactor);
            //        memory.Write4Bytes(x + destWidth * y + destinationStartIndex, pixel);
            //    }
            //}
        }

        internal virtual void Run(SimpleMemory memory) => ApplyTransform(memory);

        public Image Resize(Image image, IHastlayer hastlayer, IHardwareGenerationConfiguration hardwareGenerationConfiguration)
        {
            var parameters = new HastlayerResizeParameters
            {
                MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                ImageWidthIndex = Resize_ImageWidthIndex,
                ImageHeightIndex = Resize_ImageHeightIndex,
                DestinationImageWidthIndex = Resize_DestinationImageWidthIndex,
                DestinationImageHeightIndex = Resize_DestinationImageHeightIndex,
                ImageStartIndex = Resize_ImageStartIndex,
                Hastlayer = hastlayer,
                HardwareGenerationConfiguration = hardwareGenerationConfiguration,
            };

            image.Mutate(x => x.HastResize(image.Width / 2, image.Height / 2, MaxDegreeOfParallelism, parameters));

            return image;
        }

        public class HastlayerResizeParameters
        {
            public int MaxDegreeOfParallelism { get; set; }
            public int ImageWidthIndex { get; set; }
            public int ImageHeightIndex { get; set; }
            public int DestinationImageWidthIndex { get; set; }
            public int DestinationImageHeightIndex { get; set; }
            public int ImageStartIndex { get; set; }
            public IHastlayer Hastlayer { get; set; }
            public IHardwareGenerationConfiguration HardwareGenerationConfiguration { get; set; }
        }




    }
}
