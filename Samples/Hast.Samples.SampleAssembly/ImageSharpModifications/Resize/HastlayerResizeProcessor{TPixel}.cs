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
using System.Threading.Tasks;
using Hast.Samples.SampleAssembly;
using Hast.Samples.SampleAssembly.ImageSharpModifications.Resize;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;

namespace Hast.Samples.SampleAssembly.ImageSharpModifications.Resize
{
    internal class HastlayerResizeProcessor<TPixel> : TransformProcessor<TPixel>, IResamplingTransformImageProcessor<TPixel>
         where TPixel : unmanaged, IPixel<TPixel>
    {
        private readonly int _destinationWidth;
        private readonly int _destinationHeight;
        private readonly IResampler _resampler;
        private Image<TPixel> _destination;
        private IHastlayer _hastlayer;
        private IHardwareRepresentation _hardwareRepresentation;
        private IProxyGenerationConfiguration _proxyConfiguration;

        public HastlayerResizeProcessor(
            Configuration configuration,
            HastlayerResizeProcessor definition,
            Image<TPixel> source,
            Rectangle sourceRectangle,
            IHastlayer hastlayer,
            IHardwareRepresentation hardwareRepresentation,
            IProxyGenerationConfiguration proxyConfiguration)
            : base(configuration, source, sourceRectangle)
        {
            _destinationWidth = definition.DestinationWidth;
            _destinationHeight = definition.DestinationHeight;
            _resampler = definition.Sampler;
            _hastlayer = hastlayer;
            _hardwareRepresentation = hardwareRepresentation;
            _proxyConfiguration = proxyConfiguration;
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

            var memory = CreateMatrixMemory(
                Source,
                _destinationWidth,
                _destinationHeight,
                _hastlayer,
                _hardwareRepresentation.HardwareGenerationConfiguration);

            var resizeImage = _hastlayer
                .GenerateProxy(_hardwareRepresentation, new ImageSharpSample(), _proxyConfiguration).Result;
            resizeImage.CreateMatrix(memory);

            var accessor = new SimpleMemoryAccessor(memory);

            var rowIndecesSpan = accessor.Get().Span.Slice(16, _destinationHeight * 4);
            var pixelIndecesSpan = accessor.Get().Span.Slice((4 + _destinationHeight) * 4, _destinationWidth * 4);

            var rowIndeces = MemoryMarshal.Cast<byte, int>(rowIndecesSpan);
            var pixelIndeces = MemoryMarshal.Cast<byte, int>(pixelIndecesSpan);

            for (int i = 0; i < Source.Frames.Count; i++)
            {
                var sourceFrame = Source.Frames[i];
                var destinationFrame = _destination.Frames[i];

                for (int y = 0; y < destinationFrame.Height; y++)
                {
                    var destinationRow = destinationFrame.GetPixelRowSpan(y);

                    var sourceRow = sourceFrame.GetPixelRowSpan(rowIndeces[y]);

                    for (int x = 0; x < destinationFrame.Width; x++)
                    {
                        destinationRow[x] = sourceRow[pixelIndeces[x]];
                    }
                }
            }
        }

        public SimpleMemory CreateMatrixMemory(
            Image<TPixel> image,
            int destinationWidth,
            int destinationHeight,
            IHastlayer hastlayer,
            IHardwareGenerationConfiguration hardwareGenerationConfiguration)
        {
            var width = image.Width;
            var height = image.Height;

            var cellCount = destinationWidth + destinationHeight + 4;

            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(cellCount)
                : hastlayer.CreateMemory(hardwareGenerationConfiguration, cellCount);

            memory.WriteUInt32(0, (uint)destinationWidth);
            memory.WriteUInt32(1, (uint)destinationHeight);
            memory.WriteUInt32(2, (uint)width);
            memory.WriteUInt32(3, (uint)height);

            return memory;
        }
    }
}
