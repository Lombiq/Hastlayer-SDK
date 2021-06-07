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
using Bitmap = System.Drawing.Bitmap;

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

            // Hastlayerization
            var hastlayerSample = new ImageSharpSample();
            //var memory = hastlayerSample.CreateSimpleMemory(source, _hastlayer, _hardwareConfiguration);
            var memory = CreateSimpleMemory(Source, _hastlayer, _hardwareConfiguration);
            hastlayerSample.ApplyTransform(memory);
            var newImage = (Image<TPixel>)hastlayerSample.ConvertToImage(memory, _hastlayer, _hardwareConfiguration);

            _destination = newImage;
        }

        public SimpleMemory CreateSimpleMemory(
            Image<TPixel> image,
            IHastlayer hastlayer,
            IHardwareGenerationConfiguration hardwareGenerationConfiguration)
        {
            var pixelCount = image.Width * image.Height + (image.Width / 2) * (image.Height / 2); // TODO: get the value
            var cellCount = pixelCount
                + (pixelCount % MaxDegreeOfParallelism != 0 ? MaxDegreeOfParallelism : 0)
                + 4;
            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(cellCount)
                : hastlayer.CreateMemory(hardwareGenerationConfiguration, cellCount);

            // TODO: get constants???
            memory.WriteUInt32(0, (uint)image.Width);
            memory.WriteUInt32(1, (uint)image.Height);
            memory.WriteUInt32(2, (uint)image.Width / 2);  // TODO: get the value
            memory.WriteUInt32(3, (uint)image.Height / 2); // TODO: get the value

            for (int y = 0; y < image.Height; y++)
            {
                var row = image.GetPixelRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    var pixel = row[x];
                    var rgba = new Rgba32();
                    pixel.ToRgba32(ref rgba);

                    memory.Write4Bytes(x + y * _destinationWidth + 4, new[] { rgba.R, rgba.G, rgba.B });
                }
            }

            return memory;
        }
    }
}
