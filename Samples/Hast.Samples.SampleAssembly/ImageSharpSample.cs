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

        private const int Resize_SourceX = 4;
        private const int Resize_SourceY = 5;
        private const int Resize_DestOriginX = 6;
        private const int Resize_DestOriginY = 7;
        private const int Resize_DestLeft = 8;
        private const int Resize_DestRight = 9;

        private const int Resize_ImageStartIndex = 10;


        [Replaceable(nameof(ImageSharpSample) + "." + nameof(MaxDegreeOfParallelism))]
        private static readonly int MaxDegreeOfParallelism = 25;

        public virtual void ApplyTransform(SimpleMemory memory)
        {
            var widthFactor = memory.ReadUInt32(Resize_ImageWidthIndex) / memory.ReadUInt32(Resize_DestinationImageWidthIndex);
            var heightFactor = memory.ReadUInt32(Resize_ImageHeightIndex) / memory.ReadUInt32(Resize_DestinationImageHeightIndex);

            var operation = new Operation(widthFactor, heightFactor);
        }

        private readonly struct Operation
        {
            private readonly uint _widthFactor;
            private readonly uint _heightFactor;

            public Operation(
                uint widthFactor,
                uint heightFactor)
            {
                _widthFactor = widthFactor;
                _heightFactor = heightFactor;
            }

            public void Invoke(int y)
            {
                var sourceX = _sourceBounds.X;
                var sourceY = _sourceBounds.Y;
                var destOriginX = _destinationBounds.X;
                var destOriginY = _destinationBounds.Y;
                var destLeft = _interest.Left;
                var destRight = _interest.Right;

                // Span<Rgba32> types, RGBA 4 byte data. 
                // Y coordinates of source points
                var sourceRow = _source.GetPixelRowSpan((int)(((y - destOriginY) * _heightFactor) + sourceY));
                var targetRow = _destination.GetPixelRowSpan(y);

                for (int x = destLeft; x < destRight; x++)
                {
                    // X coordinates of source points
                    targetRow[x] = sourceRow[(int)(((x - destOriginX) * _widthFactor) + sourceX)];

                }
            }
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
                DestinationImageHeightIndex = Resize_ImageHeightIndex,
                SourceX = Resize_SourceX,
                SourceY = Resize_SourceY,
                DestOriginX = Resize_DestOriginX,
                DestOriginY = Resize_DestOriginY,
                DestLeft = Resize_DestLeft,
                DestRight = Resize_DestRight,
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
            public int SourceX { get; set; }
            public int SourceY { get; set; }
            public int DestOriginX { get; set; }
            public int DestOriginY { get; set; }
            public int DestLeft { get; set; }
            public int DestRight { get; set; }
            public int ImageStartIndex { get; set; }
            public IHastlayer Hastlayer { get; set; }
            public IHardwareGenerationConfiguration HardwareGenerationConfiguration { get; set; }
        }




    }
}
