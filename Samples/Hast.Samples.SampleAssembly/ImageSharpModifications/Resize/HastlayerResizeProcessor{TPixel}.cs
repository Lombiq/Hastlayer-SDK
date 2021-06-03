// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Modified file, original is found at:  
// https://github.com/SixLabors/ImageSharp/blob/master/src/ImageSharp/Processing/Processors/Transforms/Resize/ResizeProcessor%7BTPixel%7D.cs
// https://github.com/SixLabors/ImageSharp/blob/master/src/ImageSharp/Advanced/ParallelRowIterator.cs

using Hast.Samples.SampleAssembly;
using Hast.Samples.SampleAssembly.ImageSharpModifications.Extensions;
using Hast.Transformer.Abstractions.SimpleMemory;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static Hast.Samples.SampleAssembly.ImageSharpSample;
using Bitmap = System.Drawing.Bitmap;
using Color = System.Drawing.Color;

namespace ImageSharpHastlayerExtension.Resize
{
    class HastlayerResizeProcessor<TPixel> : TransformProcessor<TPixel>, IResamplingTransformImageProcessor<TPixel>
         where TPixel : unmanaged, IPixel<TPixel>
    {
        private readonly int _destinationWidth;
        private readonly int _destinationHeight;
        private readonly IResampler _resampler;
        private readonly Rectangle _destinationRectangle;
        private Image<TPixel> _destination;
        private HastlayerResizeParameters _parameters;

        public HastlayerResizeProcessor(
            Configuration configuration,
            HastlayerResizeProcessor definition,
            Image<TPixel> source,
            Rectangle sourceRectangle,
            HastlayerResizeParameters hastlayerResizeParameters)
            : base(configuration, source, sourceRectangle)
        {
            _destinationWidth = definition.DestinationWidth;
            _destinationHeight = definition.DestinationHeight;
            _destinationRectangle = definition.DestinationRectangle;
            _resampler = definition.Sampler;
            _parameters = hastlayerResizeParameters;
        }

        /// <inheritdoc/>
        protected override Size GetDestinationSize() => new Size(_destinationWidth, _destinationHeight);

        /// <inheritdoc/>
        protected override void BeforeImageApply(Image<TPixel> destination)
        {
            _destination = destination;
            _resampler.ApplyTransform(this);

            base.BeforeImageApply(destination);
        }

        /// <inheritdoc/>
        protected override void OnFrameApply(ImageFrame<TPixel> source, ImageFrame<TPixel> destination)
        {
            // Everything happens in BeforeImageApply.
        }

        public void ApplyTransform<TResampler>(in TResampler sampler)
            where TResampler : struct, IResampler
        {
            var configuration = Configuration;
            var source = Source;
            var destination = _destination;
            var sourceRectangle = SourceRectangle;
            var destinationRectangle = _destinationRectangle;

            var interest = Rectangle.Intersect(destinationRectangle, destination.Bounds());

            if (!(sampler is NearestNeighborResampler)) return;

            // Hastlayerization
            var hastlayerSample = new ImageSharpSample();
            var memory = CreateSimpleMemory(source, _parameters);
            hastlayerSample.ApplyTransform(memory);
            // After the memory transform convert back to IS.Image
            destination = ConvertToImage(memory, _parameters); // Maybe to frame???



            // Remove this once completed above
            for (int i = 0; i < source.Frames.Count; i++)
            {
                var sourceFrame = source.Frames[i];
                var destinationFrame = destination.Frames[i];

                ApplyNNResizeFrameTransform(
                    configuration,
                    sourceFrame,
                    destinationFrame,
                    sourceRectangle,
                    destinationRectangle,
                    interest);
            }
        }

        // NEW METHODS
        public SimpleMemory CreateSimpleMemory(Image<TPixel> image, HastlayerResizeParameters parameters)
        {
            var pixelCount = image.Width * image.Height + (image.Width / 2) * (image.Height / 2); // TODO: get the value
            var cellCount = pixelCount
                + (pixelCount % parameters.MaxDegreeOfParallelism != 0 ? parameters.MaxDegreeOfParallelism : 0)
                + 4;
            var memory = parameters.Hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(cellCount)
                : parameters.Hastlayer.CreateMemory(parameters.HardwareGenerationConfiguration, cellCount);

            memory.WriteUInt32(parameters.ImageWidthIndex, (uint)image.Width);
            memory.WriteUInt32(parameters.ImageHeightIndex, (uint)image.Height);
            memory.WriteUInt32(parameters.DestinationImageWidthIndex, (uint)_destinationWidth);   // TODO: get the value
            memory.WriteUInt32(parameters.DestinationImageHeightIndex, (uint)_destinationHeight); // TODO: get the value

            var bitmapImage = ImageSharpExtensions.ToBitmap(image);
            for (int y = 0; y < bitmapImage.Height; y++)
            {
                for (int x = 0; x < bitmapImage.Width; x++)
                {
                    var pixel = bitmapImage.GetPixel(x, y);

                    memory.Write4Bytes(
                       x + y * bitmapImage.Width + parameters.ImageStartIndex,
                       new[] { pixel.R, pixel.G, pixel.B });
                }
            }

            return memory;
        }

        public Image<TPixel> ConvertToImage(SimpleMemory memory, HastlayerResizeParameters parameters)
        {
            var width = (ushort)memory.ReadUInt32(parameters.ImageWidthIndex);
            var height = (ushort)memory.ReadUInt32(parameters.ImageHeightIndex);
            var destWidth = (ushort)memory.ReadUInt32(parameters.DestinationImageWidthIndex);
            var destHeight = (ushort)memory.ReadUInt32(parameters.DestinationImageHeightIndex);
            int destinationStartIndex = width * height + 4;

            var bmp = new Bitmap(destWidth, destHeight);

            for (int x = 0; x < destWidth; x++)
            {
                for (int y = 0; y < destHeight; y++)
                {
                    var pixel = memory.Read4Bytes(x + y * destWidth + destinationStartIndex);
                    var color = Color.FromArgb(pixel[0], pixel[1], pixel[2]);
                    bmp.SetPixel(x, y, color);
                }
            }

            var image = ImageSharpExtensions.ToImageSharpImage<TPixel>(bmp);

            return image;
        }
    }
}
