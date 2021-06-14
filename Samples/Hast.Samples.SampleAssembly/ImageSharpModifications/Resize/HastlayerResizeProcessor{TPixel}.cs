// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Modified file, original is found at:  
// https://github.com/SixLabors/ImageSharp/blob/master/src/ImageSharp/Processing/Processors/Transforms/Resize/ResizeProcessor%7BTPixel%7D.cs
// https://github.com/SixLabors/ImageSharp/blob/master/src/ImageSharp/Advanced/ParallelRowIterator.cs

using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using System.Runtime.InteropServices;

namespace Hast.Samples.SampleAssembly.ImageSharpModifications.Resize
{
    class HastlayerResizeProcessor<TPixel> : TransformProcessor<TPixel>, IResamplingTransformImageProcessor<TPixel>
         where TPixel : unmanaged, IPixel<TPixel>
    {
        private readonly int _destinationWidth;
        private readonly int _destinationHeight;
        private readonly IResampler _resampler;
        private Image<TPixel> _destination;
        private IHastlayer _hastlayer;
        private IHardwareGenerationConfiguration _hardwareConfiguration;

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
            _resampler = definition.Sampler;
            _hastlayer = hastlayer;
            _hardwareConfiguration = hardwareGenerationConfiguration;
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

            var hastResizer = new ImageSharpSample();

            var memory = CreateSimpleMemory(Source, _hastlayer, _hardwareConfiguration);
             
            hastResizer.ApplyTransform(memory);

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
                    .Slice(destinationStartIndex * 4 + y * destination.Width * 4, destination.Width * 4);

                var sourceRow = MemoryMarshal.Cast<byte, TPixel>(span);

                for (int x = 0; x < destination.Width; x++)
                {
                    destinationRow[x] = sourceRow[x];
                }
            }
        }
    }
}
