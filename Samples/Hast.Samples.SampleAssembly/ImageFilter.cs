using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace Hast.Samples.SampleAssembly
{
    /// <summary>
    /// Algorithm for running convolution image processing on images. Also see <c>ImageFilterSampleRunner</c> on
    /// what to configure to make this work.
    ///
    /// NOTE: this sample is not parallelized and thus not really suitable for Hastlayer. We'll rework it in the future.
    /// </summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Transformed code.")]
    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Transformed code.")]
    public class ImageFilter
    {
        private const int FilterImageImageHeightIndex = 0;
        private const int FilterImageImageWidthIndex = 1;
        private const int FilterImageTopLeftIndex = 2;
        private const int FilterImageTopMiddleIndex = 3;
        private const int FilterImageTopRightIndex = 4;
        private const int FilterImageMiddleLeftIndex = 5;
        private const int FilterImagePixelIndex = 6;
        private const int FilterImageMiddleRightIndex = 7;
        private const int FilterImageBottomLeftIndex = 8;
        private const int FilterImageBottomMiddleIndex = 9;
        private const int FilterImageBottomRightIndex = 10;
        private const int FilterImageFactorIndex = 11;
        private const int FilterImageOffsetIndex = 12;
        private const int FilterImageImageStartIndex = 13;

        /// <summary>
        /// Makes the changes according to the matrix on the image.
        /// </summary>
        /// <param name="memory">The <see cref="SimpleMemory"/> object representing the accessible memory space.</param>
        public virtual void FilterImage(SimpleMemory memory)
        {
            ushort imageWidth = (ushort)memory.ReadUInt32(FilterImageImageWidthIndex);
            ushort imageHeight = (ushort)memory.ReadUInt32(FilterImageImageHeightIndex);

            int factor = memory.ReadInt32(FilterImageFactorIndex);
            int offset = memory.ReadInt32(FilterImageOffsetIndex);
            int topLeftValue = memory.ReadInt32(FilterImageTopLeftIndex);
            int topMiddleValue = memory.ReadInt32(FilterImageTopMiddleIndex);
            int topRightValue = memory.ReadInt32(FilterImageTopRightIndex);
            int middleLeftValue = memory.ReadInt32(FilterImageMiddleLeftIndex);
            int pixelValue = memory.ReadInt32(FilterImagePixelIndex);
            int middleRightValue = memory.ReadInt32(FilterImageMiddleRightIndex);
            int bottomLeftValue = memory.ReadInt32(FilterImageBottomLeftIndex);
            int bottomMiddleValue = memory.ReadInt32(FilterImageBottomMiddleIndex);
            int bottomRightValue = memory.ReadInt32(FilterImageBottomRightIndex);

            ushort topLeft = 0;
            ushort topMiddle = 0;
            ushort topRight = 0;
            ushort middleLeft = 0;
            ushort pixel = 0;
            ushort middleRight = 0;
            ushort bottomLeft = 0;
            ushort bottomMiddle = 0;
            ushort bottomRight = 0;

            int pixelCountHelper = imageHeight * imageWidth * 3;
            ushort imageWidthHelper = (ushort)(imageWidth * 3);

            for (int x = 1; x < imageHeight - 1; x++)
            {
                for (int y = 3; y < imageWidthHelper - 3; y++)
                {
                    // Warpping all these wouldn't be an improvement.
#pragma warning disable S103 // Lines should not be too long
                    topLeft = (ushort)memory.ReadUInt32((x * imageWidthHelper) + y + pixelCountHelper - imageWidthHelper - 3 + FilterImageImageStartIndex);
                    topMiddle = (ushort)memory.ReadUInt32((x * imageWidthHelper) + y + pixelCountHelper - imageWidthHelper + FilterImageImageStartIndex);
                    topRight = (ushort)memory.ReadUInt32((x * imageWidthHelper) + y + pixelCountHelper - imageWidthHelper + 3 + FilterImageImageStartIndex);
                    middleLeft = (ushort)memory.ReadUInt32((x * imageWidthHelper) + y + pixelCountHelper - 3 + FilterImageImageStartIndex);
                    pixel = (ushort)memory.ReadUInt32((x * imageWidthHelper) + y + pixelCountHelper + FilterImageImageStartIndex);
                    middleRight = (ushort)memory.ReadUInt32((x * imageWidthHelper) + y + pixelCountHelper + 3 + FilterImageImageStartIndex);
                    bottomLeft = (ushort)memory.ReadUInt32((x * imageWidthHelper) + y + pixelCountHelper + imageWidthHelper - 3 + FilterImageImageStartIndex);
                    bottomMiddle = (ushort)memory.ReadUInt32((x * imageWidthHelper) + y + pixelCountHelper + imageWidthHelper + FilterImageImageStartIndex);
                    bottomRight = (ushort)memory.ReadUInt32((x * imageWidthHelper) + y + pixelCountHelper + imageWidthHelper + 3 + FilterImageImageStartIndex);

                    // We are trying to represent the value as a 3x3 matrix.
#pragma warning disable SA1117 // Parameters should be on same line or separate lines
                    memory.WriteUInt32((x * imageWidthHelper) + y + FilterImageImageStartIndex, CalculatePixelValue(
                        topLeft, topMiddle, topRight,
                        middleLeft, pixel, middleRight,
                        bottomLeft, bottomMiddle, bottomRight,
                        topLeftValue, topMiddleValue, topRightValue,
                        middleLeftValue, pixelValue, middleRightValue,
                        bottomLeftValue, bottomMiddleValue, bottomRightValue,
                        factor, offset));
#pragma warning restore SA1117 // Parameters should be on same line or separate lines
#pragma warning restore S103 // Lines should not be too long
                }
            }
        }

        /// <summary>
        /// Makes the required changes on the selected pixel.
        /// </summary>
        /// <param name="topLeft">Top left value.</param>
        /// <param name="topMiddle">Top middle value.</param>
        /// <param name="topRight">Top right value.</param>
        /// <param name="middleLeft">Middle left value.</param>
        /// <param name="pixel">The current pixel value.</param>
        /// <param name="middleRight">Middle right value.</param>
        /// <param name="bottomLeft">Bottom left value.</param>
        /// <param name="bottomMiddle">Bottom middle value.</param>
        /// <param name="bottomRight">Bottom right value.</param>
        /// <param name="topLeftValue">Top left value in matrix.</param>
        /// <param name="topMiddleValue">Top middle value in matrix.</param>
        /// <param name="topRightValue">Top right value in matrix.</param>
        /// <param name="middleLeftValue">Middle left value in matrix.</param>
        /// <param name="pixelValue">The current pixel value in matrix.</param>
        /// <param name="middleRightValue">Middle right value in matrix.</param>
        /// <param name="bottomLeftValue">Bottom left value in matrix.</param>
        /// <param name="bottomMiddleValue">Bottom middle value in matrix.</param>
        /// <param name="bottomRightValue">Bottom right value in matrix.</param>
        /// <param name="factor">The value to divide the summed matrix values with.</param>
        /// <param name="offset">Offset value added to the result.</param>
        /// <returns>Returns the value of the filtered pixel in matrix.</returns>
        private ushort CalculatePixelValue(
            ushort topLeft,
            ushort topMiddle,
            ushort topRight,
            ushort middleLeft,
            ushort pixel,
            ushort middleRight,
            ushort bottomLeft,
            ushort bottomMiddle,
            ushort bottomRight,
            int topLeftValue,
            int topMiddleValue,
            int topRightValue,
            int middleLeftValue,
            int pixelValue,
            int middleRightValue,
            int bottomLeftValue,
            int bottomMiddleValue,
            int bottomRightValue,
            int factor,
            int offset)
        {
            if (factor == 0)
                return pixel;

            var newPixel = (((topLeft * topLeftValue) +
                            (topMiddle * topMiddleValue) +
                            (topRight * topRightValue) +
                            (middleLeft * middleLeftValue) +
                            (pixel * pixelValue) +
                            (middleRight * middleRightValue) +
                            (bottomRight * bottomLeftValue) +
                            (bottomMiddle * bottomMiddleValue) +
                            (bottomRight * bottomRightValue))
                            / factor) + offset;

            if (newPixel < 0) newPixel = 0;
            if (newPixel > 255) newPixel = 255;

            return (ushort)newPixel;
        }

        /// <summary>
        /// Applies Gauss filter to an image.
        /// </summary>
        /// <param name="image">The image to modify.</param>
        /// <returns>Returns the smoothed image.</returns>
        public Bitmap ApplyGaussFilter(Bitmap image, IHastlayer hastlayer = null, IHardwareGenerationConfiguration configuration = null)
        {
            var memory = CreateSimpleMemory(
                image,
                hastlayer,
                configuration,
                (1, 2, 1),
                (2, 4, 2),
                (1, 2, 1),
                16);
            FilterImage(memory);
            return CreateImage(memory, image);
        }

        /// <summary>
        /// Applies Sobel filter to the image.
        /// </summary>
        /// <param name="image">The image to modify.</param>
        /// <returns>Returns the edge map of the image.</returns>
        public Bitmap ApplySobelFilter(Bitmap image, IHastlayer hastlayer = null, IHardwareGenerationConfiguration configuration = null)
        {
            var memory = CreateSimpleMemory(
                image,
                hastlayer,
                configuration,
                (1, 2, 1),
                (0, 0, 0),
                (-1, -2, -1));
            FilterImage(memory);
            return CreateImage(memory, image);
        }

        /// <summary>
        /// Applies a horizontal edge detection filter to the image.
        /// </summary>
        /// <param name="image">The image to modify.</param>
        /// <returns>Returns the edge map of the image containing only horizontal edges.</returns>
        public Bitmap DetectHorizontalEdges(Bitmap image, IHastlayer hastlayer = null, IHardwareGenerationConfiguration configuration = null)
        {
            var memory = CreateSimpleMemory(
                image,
                hastlayer,
                configuration,
                (1, 1, 1),
                (0, 0, 0),
                (-1, -1, -1));
            FilterImage(memory);
            return CreateImage(memory, image);
        }

        /// <summary>
        /// Applies a vertical edge detection filter to the image.
        /// </summary>
        /// <param name="image">The image to modify.</param>
        /// <returns>Returns the edge map of the image containing only vertical edges.</returns>
        public Bitmap DetectVerticalEdges(Bitmap image, IHastlayer hastlayer = null, IHardwareGenerationConfiguration configuration = null)
        {
            var memory = CreateSimpleMemory(
                image,
                hastlayer,
                configuration,
                (1, 0, -1),
                (1, 0, -1),
                (1, 0, -1));
            FilterImage(memory);
            return CreateImage(memory, image);
        }

        /// <summary>
        /// Creates a <see cref="SimpleMemory"/> instance that stores the image.
        /// </summary>
        /// <param name="image">The image to process.</param>
        /// <param name="top">Top values.</param>
        /// <param name="middle">Middle values.</param>
        /// <param name="bottom">Bottom values.</param>
        /// <param name="factor">The value to divide the summed matrix values with.</param>
        /// <param name="offset">Offset value added to the result.</param>
        /// <returns>The instance of the created <see cref="SimpleMemory"/>.</returns>
        private SimpleMemory CreateSimpleMemory(
            Bitmap image,
            IHastlayer hastlayer,
            IHardwareGenerationConfiguration configuration,
            (int Left, int Middle, int Right) top,
            (int Left, int Middle, int Right) middle,
            (int Left, int Middle, int Right) bottom,
            int factor = 1,
            int offset = 0)
        {
            var cellCount = (image.Width * image.Height * 6) + 13;
            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(cellCount)
                : hastlayer.CreateMemory(configuration, cellCount);

            memory.WriteUInt32(FilterImageImageWidthIndex, (uint)image.Width);
            memory.WriteUInt32(FilterImageImageHeightIndex, (uint)image.Height);
            memory.WriteInt32(FilterImageTopLeftIndex, top.Left);
            memory.WriteInt32(FilterImageTopMiddleIndex, top.Middle);
            memory.WriteInt32(FilterImageTopRightIndex, top.Right);
            memory.WriteInt32(FilterImageMiddleLeftIndex, middle.Left);
            memory.WriteInt32(FilterImagePixelIndex, middle.Middle);
            memory.WriteInt32(FilterImageMiddleRightIndex, middle.Right);
            memory.WriteInt32(FilterImageBottomLeftIndex, bottom.Left);
            memory.WriteInt32(FilterImageBottomMiddleIndex, bottom.Middle);
            memory.WriteInt32(FilterImageBottomRightIndex, bottom.Right);
            memory.WriteInt32(FilterImageFactorIndex, factor);
            memory.WriteInt32(FilterImageOffsetIndex, offset);

            int size = image.Width * image.Height;

            for (int x = 0; x < image.Height; x++)
            {
                for (int y = 0; y < image.Width; y++)
                {
                    var pixelValue = image.GetPixel(y, x);

                    memory.WriteUInt32((((x * image.Width) + y) * 3) + FilterImageImageStartIndex, pixelValue.R);
                    memory.WriteUInt32((((x * image.Width) + y) * 3) + 1 + FilterImageImageStartIndex, pixelValue.G);
                    memory.WriteUInt32((((x * image.Width) + y) * 3) + 2 + FilterImageImageStartIndex, pixelValue.B);

                    memory.WriteUInt32((((x * image.Width) + y) * 3) + (size * 3) + FilterImageImageStartIndex, pixelValue.R);
                    memory.WriteUInt32((((x * image.Width) + y) * 3) + 1 + (size * 3) + FilterImageImageStartIndex, pixelValue.G);
                    memory.WriteUInt32((((x * image.Width) + y) * 3) + 2 + (size * 3) + FilterImageImageStartIndex, pixelValue.B);
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
        private Bitmap CreateImage(SimpleMemory memory, Bitmap image)
        {
            var newImage = new Bitmap(image);

            int r, g, b;

            for (int x = 0; x < newImage.Height; x++)
            {
                for (int y = 0; y < newImage.Width; y++)
                {
                    r = memory.ReadInt32((((x * newImage.Width) + y) * 3) + FilterImageImageStartIndex);
                    g = memory.ReadInt32((((x * newImage.Width) + y) * 3) + 1 + FilterImageImageStartIndex);
                    b = memory.ReadInt32((((x * newImage.Width) + y) * 3) + 2 + FilterImageImageStartIndex);

                    newImage.SetPixel(y, x, Color.FromArgb(r, g, b));
                }
            }

            return newImage;
        }
    }
}
