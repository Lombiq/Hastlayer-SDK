using System;
using System.Drawing;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Communication.Tester.Helpers
{
    public static class BitmapHelper
    {
        private const int MaxDegreeOfParallelism = 25;

        public static Bitmap FromSimpleMemory(SimpleMemory memory, Bitmap image, int prependCellCount = 0)
        {
            var newImage = new Bitmap(image);

            for (int y = 0; y < newImage.Height; y++)
            {
                for (int x = 0; x < newImage.Width; x++)
                {
                    var bytes = memory.Read4Bytes((y * newImage.Width) + x + prependCellCount);
                    newImage.SetPixel(x, y, Color.FromArgb(bytes[0], bytes[1], bytes[2]));
                }
            }

            return newImage;
        }

        public static SimpleMemory ToSimpleMemory(
            IHardwareGenerationConfiguration configuration,
            IHastlayer hastlayer,
            Bitmap image,
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
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image.GetPixel(x, y);

                    memory.Write4Bytes(
                        (y * image.Width) + x + prependCells.Length,
                        new[] { pixel.R, pixel.G, pixel.B });
                }
            }

            return memory;
        }
    }
}
