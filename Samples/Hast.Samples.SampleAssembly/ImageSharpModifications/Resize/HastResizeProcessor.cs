// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Modified file, original is found at:  
// https://github.com/SixLabors/ImageSharp/blob/master/src/ImageSharp/Processing/Processors/Transforms/Resize/ResizeProcessor.cs 

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace Hastlayer_ImageSharp_PracticeDemo.Resize
{
    public class HastResizeProcessor : CloningImageProcessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResizeProcessor"/> class.
        /// </summary>
        /// <param name="options">The resize options.</param>
        /// <param name="sourceSize">The source image size.</param>
        public HastResizeProcessor(ResizeOptions options, Size sourceSize, int maxDegreeOfParallelism)
        {
            Guard.NotNull(options, nameof(options));
            Guard.NotNull(options.Sampler, nameof(options.Sampler));
            Guard.MustBeValueType(options.Sampler, nameof(options.Sampler));

            (var size, var rectangle) = ResizeHelper.CalculateTargetLocationAndBounds(sourceSize, options);

            Sampler = options.Sampler;
            DestinationWidth = size.Width;
            DestinationHeight = size.Height;
            DestinationRectangle = rectangle;
            Compand = options.Compand;
            MaxDegreeOfParallelism = maxDegreeOfParallelism;
        }

        /// <summary>
        /// Gets the sampler to perform the resize operation.
        /// </summary>
        public IResampler Sampler { get; }

        /// <summary>
        /// Gets the destination width.
        /// </summary>
        public int DestinationWidth { get; }

        /// <summary>
        /// Gets the destination height.
        /// </summary>
        public int DestinationHeight { get; }

        /// <summary>
        /// Gets the resize rectangle.
        /// </summary>
        public Rectangle DestinationRectangle { get; }

        /// <summary>
        /// Gets a value indicating whether to compress or expand individual pixel color values on processing.
        /// </summary>
        public bool Compand { get; }

        /// <summary>
        /// Gets a value indicating whether to premultiply the alpha (if it exists) during the resize operation.
        /// </summary>
        public bool PremultiplyAlpha { get; }

        /// <summary>
        /// Gets a value indicating the max degree of parallelism.
        /// </summary>
        public int MaxDegreeOfParallelism { get; }

        /// <inheritdoc />
        public override ICloningImageProcessor<TPixel> CreatePixelSpecificCloningProcessor<TPixel>(
            Configuration configuration,
            Image<TPixel> source,
            Rectangle sourceRectangle)
        {
            configuration.MaxDegreeOfParallelism = MaxDegreeOfParallelism;
            return new HastResizeProcessor<TPixel>(configuration, this, source, sourceRectangle);
        }
    }
}
