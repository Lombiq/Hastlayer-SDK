// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Modified file, original is found at:  
// https://github.com/SixLabors/ImageSharp/blob/master/src/ImageSharp/Processing/Processors/Transforms/Resize/ResizeProcessor%7BTPixel%7D.cs
// https://github.com/SixLabors/ImageSharp/blob/master/src/ImageSharp/Advanced/ParallelRowIterator.cs

using Hast.Layer;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

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
            var source = Source;

            if (!(sampler is NearestNeighborResampler)) return;

            // Hastlayerization
            var hastlayerSample = new ImageSharpSample();
            var memory = hastlayerSample.CreateSimpleMemory(source, _hastlayer, _hardwareConfiguration);
            hastlayerSample.ApplyTransform(memory);
            var newImage = (Image<TPixel>)hastlayerSample.ConvertToImage(memory, _hastlayer, _hardwareConfiguration);

            _destination = newImage;
        }
    }
}
