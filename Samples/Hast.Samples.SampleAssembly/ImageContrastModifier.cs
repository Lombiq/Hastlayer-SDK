using System.Drawing;
using System.Threading.Tasks;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Samples.SampleAssembly
{
    /// <summary>
    /// Algorithm for changing the contrast of an image.
    /// </summary>
    public class ImageContrastModifier
    {
        private const ushort Multiplier = 1000;

        public const int ChangeContrast_ImageWidthIndex = 0;
        public const int ChangeContrast_ImageHeightIndex = 1;
        public const int ChangeContrast_ContrastValueIndex = 2;
        public const int ChangeContrast_ImageStartIndex = 3;

        public const int MaxDegreeOfParallelism = 25;


        /// <summary>
        /// Changes the contrast of an image.
        /// </summary>
        /// <param name="memory">The <see cref="SimpleMemory"/> object representing the accessible memory space.</param>
        public virtual void ChangeContrast(SimpleMemory memory)
        {
            var imageWidth = (ushort)memory.ReadUInt32(ChangeContrast_ImageWidthIndex);
            var imageHeight = (ushort)memory.ReadUInt32(ChangeContrast_ImageHeightIndex);
            int contrastValue = memory.ReadInt32(ChangeContrast_ContrastValueIndex);

            if (contrastValue > 100) contrastValue = 100;
            else if (contrastValue < -100) contrastValue = -100;

            contrastValue = (100 + contrastValue * Multiplier) / 100;

            var tasks = new Task<PixelProcessingTaskOutput>[MaxDegreeOfParallelism];

            // Since we only need to compute the loop condition's right side once, not on each loop execution, it's an 
            // optimization to put it in a separate variable. This way it's indeed computed only once.
            var pixelCount = imageHeight * imageWidth;
            var stepCount = pixelCount / MaxDegreeOfParallelism;

            if (pixelCount % MaxDegreeOfParallelism != 0)
            {
                // This will take care of the rest of the pixels. This is wasteful as on the last step not all Tasks 
                // will work on something but it's a way to keep the number of Tasks constant.
                stepCount += 1;
            }

            for (int i = 0; i < stepCount; i++)
            {
                for (int t = 0; t < MaxDegreeOfParallelism; t++)
                {
                    var pixelBytes = memory.Read4Bytes(i * MaxDegreeOfParallelism + t + ChangeContrast_ImageStartIndex);

                    // Using an input class to also pass contrastValue because it's currently not supported to access
                    // variables from the parent scope from inside Tasks (you need to explicitly pass in all inputs).
                    // Using an output class to pass the pixel values back because returning an array from Tasks is not
                    // supported at the time either..
                    tasks[t] = Task.Factory.StartNew(
                        inputObject =>
                        {
                            var input = (PixelProcessingTaskInput)inputObject;

                            return new PixelProcessingTaskOutput
                            {
                                R = ChangePixelValue(input.PixelBytes[0], input.ContrastValue),
                                G = ChangePixelValue(input.PixelBytes[1], input.ContrastValue),
                                B = ChangePixelValue(input.PixelBytes[2], input.ContrastValue)
                            };
                        },
                    new PixelProcessingTaskInput { PixelBytes = pixelBytes, ContrastValue = contrastValue });
                }

                Task.WhenAll(tasks).Wait();

                for (int t = 0; t < MaxDegreeOfParallelism; t++)
                {
                    // It's no problem that we write just 3 bytes to a 4-byte slot.
                    memory.Write4Bytes(
                        i * MaxDegreeOfParallelism + t + ChangeContrast_ImageStartIndex,
                        new[] { tasks[t].Result.R, tasks[t].Result.G, tasks[t].Result.B });
                }
            }
        }


        /// <summary>
        /// Makes the required changes on the selected pixel.
        /// </summary>
        /// <param name="pixel">The current pixel value.</param>
        /// <param name="contrastValue">The contrast difference value.</param>
        /// <returns>The pixel value after changing the contrast.</returns>
        private byte ChangePixelValue(byte pixel, int contrastValue)
        {
            var correctedPixel = pixel * Multiplier / 255;
            correctedPixel -= (int)(0.5 * Multiplier);
            correctedPixel *= contrastValue;
            correctedPixel /= Multiplier;
            correctedPixel += (int)(0.5 * Multiplier);
            correctedPixel *= 255;
            correctedPixel /= Multiplier;

            if (correctedPixel < 0) correctedPixel = 0;
            else if (correctedPixel > 255) correctedPixel = 255;

            return (byte)correctedPixel;
        }


        private class PixelProcessingTaskInput
        {
            public byte[] PixelBytes { get; set; }
            public int ContrastValue { get; set; }
        }

        private class PixelProcessingTaskOutput
        {
            public byte R { get; set; }
            public byte G { get; set; }
            public byte B { get; set; }
        }
    }


    public static class ImageContrastModifierExtensions
    {
        /// <summary>
        /// Changes the contrast of an image.
        /// </summary>
        /// <param name="image">The image that we modify.</param>
        /// <param name="contrast">The value of the intensity to calculate the new pixel values.</param>
        /// <returns>Returns an image with changed contrast values.</returns>
        public static Bitmap ChangeImageContrast(this ImageContrastModifier imageContrast, Bitmap image, int contrast)
        {
            var memory = CreateSimpleMemory(
                image,
                contrast);
            imageContrast.ChangeContrast(memory);
            return CreateImage(memory, image);
        }


        /// <summary>
        /// Creates a <see cref="SimpleMemory"/> instance that stores the image.
        /// </summary>
        /// <param name="image">The image to process.</param>
        /// <param name="contrastValue">The contrast difference value.</param>
        /// <returns>The instance of the created <see cref="SimpleMemory"/>.</returns>
        private static SimpleMemory CreateSimpleMemory(Bitmap image, int contrastValue)
        {
            var pixelCount = image.Width * image.Height;
            var cellCount = 
                pixelCount + 
                (pixelCount % ImageContrastModifier.MaxDegreeOfParallelism != 0 ? ImageContrastModifier.MaxDegreeOfParallelism : 0) + 
                3;
            var memory = new SimpleMemory(cellCount);

            memory.WriteUInt32(ImageContrastModifier.ChangeContrast_ImageWidthIndex, (uint)image.Width);
            memory.WriteUInt32(ImageContrastModifier.ChangeContrast_ImageHeightIndex, (uint)image.Height);
            memory.WriteInt32(ImageContrastModifier.ChangeContrast_ContrastValueIndex, contrastValue);

            for (int x = 0; x < image.Height; x++)
            {
                for (int y = 0; y < image.Width; y++)
                {
                    var pixel = image.GetPixel(y, x);

                    // This leaves 1 byte unused in each memory cell, but that would make the whole logic a lot more
                    // complicated, so good enough for a sample; if we'd want to optimize memory usage, that would be
                    // needed.
                    memory.Write4Bytes(
                        x * image.Width + y + ImageContrastModifier.ChangeContrast_ImageStartIndex,
                        new[] { pixel.R, pixel.G, pixel.B });
                }
            }

            return memory;
        }

        /// <summary>
        /// Creates an image from a <see cref="SimpleMemory"/> instance.
        /// </summary>
        /// <param name="memory">The <see cref="SimpleMemory"/> instance.</param>
        /// <param name="image">The original image.</param>
        /// <returns>Returns the processed image.</returns>
        private static Bitmap CreateImage(SimpleMemory memory, Bitmap image)
        {
            var newImage = new Bitmap(image);

            for (int x = 0; x < newImage.Height; x++)
            {
                for (int y = 0; y < newImage.Width; y++)
                {
                    var bytes = memory.Read4Bytes(x * newImage.Width + y + ImageContrastModifier.ChangeContrast_ImageStartIndex);
                    newImage.SetPixel(y, x, Color.FromArgb(bytes[0], bytes[1], bytes[2]));
                }
            }

            return newImage;
        }
    }
}
