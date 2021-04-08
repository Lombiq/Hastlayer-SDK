using System;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Hast.Communication.Tester.Helpers
{
    public static class BitmapHelper
    {
        private const int MaxDegreeOfParallelism = 25;

        public static Image<Rgba32> FromSimpleMemory(SimpleMemory memory, Image<Rgba32> image, int prependCellCount = 0)
        {
            var newImage = image.Clone();

            for (int y = 0; y < newImage.Height; y++)
            {
                var row = image.GetPixelRowSpan(y);
                for (int x = 0; x < newImage.Width; x++)
                {
                    var bytes = memory.Read4Bytes(y * newImage.Width + x + prependCellCount);
                    row[x] = new Rgba32(bytes[0], bytes[1], bytes[2], bytes[3]);
                }
            }

            return newImage;
        }

        public static SimpleMemory ToSimpleMemory(
            IHardwareGenerationConfiguration configuration,
            IHastlayer hastlayer,
            Image<Rgba32> image,
            int[] prependCells = null)
        {
            prependCells ??= Array.Empty<int>();

            var pixelCount = image.Width * image.Height;
            var cellCount =
                pixelCount +
                (pixelCount % MaxDegreeOfParallelism != 0 ? MaxDegreeOfParallelism : 0) +
                prependCells.Length;
            var memory = hastlayer.CreateMemory(configuration, cellCount);

            for (int i = 0; i < prependCells.Length; i++)
            {
                memory.WriteInt32(i, prependCells[i]);
            }

            for (int y = 0; y < image.Height; y++)
            {
                var row = image.GetPixelRowSpan(y);
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = row[x];

                    memory.Write4Bytes(
                        y * image.Width + x + prependCells.Length,
                        new[] { pixel.R, pixel.G, pixel.B, pixel.A });
                }
            }

            return memory;
        }
    }
}
