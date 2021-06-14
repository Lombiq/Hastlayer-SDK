// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Modified file, original is found at:  
// https://github.com/SixLabors/ImageSharp/blob/master/src/ImageSharp/Processing/Processors/Transforms/Resize/ResizeProcessor%7BTPixel%7D.cs
// https://github.com/SixLabors/ImageSharp/blob/master/src/ImageSharp/Advanced/ParallelRowIterator.cs

using Hast.Layer;
using Hast.Samples.SampleAssembly.ImageSharpModifications.Extensions;
using Hast.Transformer.Abstractions.SimpleMemory;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using System.Runtime.InteropServices;
using Bitmap = System.Drawing.Bitmap;
using Color = System.Drawing.Color;

namespace Hast.Samples.SampleAssembly.ImageSharpModifications.Resize
{
    class HastlayerResizeProcessor<TPixel> : TransformProcessor<TPixel>, IResamplingTransformImageProcessor<TPixel>
         where TPixel : unmanaged, IPixel<TPixel>
    {
        private readonly int _destinationWidth;
        private readonly int _destinationHeight;
        private readonly IResampler _resampler;
        private readonly Rectangle _destinationRectangle;
        private Image<TPixel> _destination;
        private IHastlayer _hastlayer;
        private IHardwareGenerationConfiguration _hardwareConfiguration;
        private readonly int MaxDegreeOfParallelism;

        public HastlayerResizeProcessor(
            Configuration configuration,
            HastlayerResizeProcessor definition,
            Image<TPixel> source,
            Rectangle sourceRectangle,
            IHastlayer hastlayer,
            IHardwareGenerationConfiguration hardwareGenerationConfiguration)
            : base(configuration, source, sourceRectangle)
        {
            _destinationWidth = definition.DestinationWidth;
            _destinationHeight = definition.DestinationHeight;
            _destinationRectangle = definition.DestinationRectangle;
            _resampler = definition.Sampler;
            _hastlayer = hastlayer;
            _hardwareConfiguration = hardwareGenerationConfiguration;
            MaxDegreeOfParallelism = configuration.MaxDegreeOfParallelism;
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
            if (!(sampler is NearestNeighborResampler)) return;

            var memory = CreateSimpleMemory(Source, _hastlayer, _hardwareConfiguration);

            for (int i = 0; i < Source.Frames.Count; i++)
            {
                var sourceFrame = Source.Frames[i];
                var destinationFrame = _destination.Frames[i];

                ApplyTransformFromMemory(sourceFrame, destinationFrame, memory);
            }
        }

        public SimpleMemory CreateSimpleMemory(
            Image<TPixel> image,
            IHastlayer hastlayer,
            IHardwareGenerationConfiguration hardwareGenerationConfiguration)
        {
            var pixelCount = image.Width * image.Height + _destinationWidth * _destinationHeight;
            var frameCount = image.Frames.Count;

            var cellCount = pixelCount * frameCount + 5;

            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(cellCount)
                : hastlayer.CreateMemory(hardwareGenerationConfiguration, cellCount);

            var accessor = new SimpleMemoryAccessor(memory);

            for (int i = 0; i < frameCount; i++)
            {
                var span = accessor.Get().Span.Slice(20 + i * pixelCount, image.Width * image.Height * 4);

                image.Frames[i].TryGetSinglePixelSpan(out var imageSpan);

                MemoryMarshal.Cast<TPixel, byte>(imageSpan).CopyTo(span);
            }

            memory.WriteUInt32(0, (uint)image.Width);
            memory.WriteUInt32(1, (uint)image.Height);
            memory.WriteUInt32(2, (uint)_destinationWidth);
            memory.WriteUInt32(3, (uint)_destinationHeight);
            memory.WriteUInt32(4, (uint)frameCount);

            return memory;
        }

        public void ApplyTransformFromMemory(
            ImageFrame<TPixel> source,
            ImageFrame<TPixel> destination,
            SimpleMemory memory)
        {
            var destinationStartIndex = source.Width * source.Height + 5;
            var accessor = new SimpleMemoryAccessor(memory);

            for (int y = 0; y < destination.Height; y++)
            {
                var destinationRow = destination.GetPixelRowSpan(y);
                var span = accessor.Get().Span
                    .Slice(destinationStartIndex + y * destination.Width * 4, destination.Width * 4);

                var sourceRow = MemoryMarshal.Cast<byte, TPixel>(span);

                for (int x = 0; x < destination.Width; x++)
                {
                    destinationRow[x] = sourceRow[x];
                }
            }
        }

        public void ApplyTestTransform(ImageFrame<TPixel> source, ImageFrame<TPixel> destination)
        {
            for (int y = 0; y < destination.Height; y++)
            {
                var sourceRow = source.GetPixelRowSpan(y);
                var row = destination.GetPixelRowSpan(y);

                for (int x = 0; x < destination.Width; x++)
                {
                    row[x] = sourceRow[sourceRow.Length / 4];
                }
            }
        }

        public Image<TPixel> ConvertToImage(SimpleMemory memory)
        {
            var width = (ushort)memory.ReadUInt32(0);
            var height = (ushort)memory.ReadUInt32(1);
            var destWidth = (ushort)memory.ReadUInt32(2);
            var destHeight = (ushort)memory.ReadUInt32(3);
            var frameCount = (ushort)memory.ReadUInt32(4);
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

            bmp.Save($"../../../../../../OutputImages/bmp_before_conversion_frame.bmp");


            var image = ImageSharpExtensions.ToImageSharpImage<TPixel>(bmp);

            return image;
        }
    }
}
