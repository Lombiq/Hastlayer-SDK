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

            for (int x = 0; x < newImage.Height; x++)
            {
                for (int y = 0; y < newImage.Width; y++)
                {
                    var bytes = memory.Read4Bytes(x * newImage.Width + y + prependCellCount);
                    newImage.SetPixel(y, x, Color.FromArgb(bytes[0], bytes[1], bytes[2]));
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

            for (int x = 0; x < image.Height; x++)
            {
                for (int y = 0; y < image.Width; y++)
                {
                    var pixel = image.GetPixel(y, x);

                    memory.Write4Bytes(
                        x * image.Width + y + prependCells.Length,
                        new[] { pixel.R, pixel.G, pixel.B });
                }
            }

            return memory;
        }
    }
}
