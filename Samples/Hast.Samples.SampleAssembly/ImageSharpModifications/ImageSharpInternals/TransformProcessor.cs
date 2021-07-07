// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Modified file, original is found at:
// https://github.com/SixLabors/ImageSharp/blob/master/src/ImageSharp/Processing/Processors/Transforms/TransformProcessor.cs

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors;

namespace Hast.Samples.SampleAssembly.ImageSharpModifications
{
    /// <summary>
    /// The base class for all transform processors. Any processor that changes the dimensions of the image should inherit from this.
    /// </summary>
    /// <typeparam name="TPixel">The pixel format.</typeparam>
    internal abstract class TransformProcessor<TPixel> : CloningImageProcessor<TPixel>
         where TPixel : unmanaged, IPixel<TPixel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransformProcessor{TPixel}"/> class.
        /// </summary>
        /// <param name="configuration">The configuration which allows altering default behaviour or extending the library.</param>
        /// <param name="source">The source <see cref="Image{TPixel}"/> for the current processor instance.</param>
        /// <param name="sourceRectangle">The source area to process for the current processor instance.</param>
        protected TransformProcessor(Configuration configuration, Image<TPixel> source, Rectangle sourceRectangle)
            : base(configuration, source, sourceRectangle)
        {
        }

        /// <inheritdoc/>
        protected override void AfterImageApply(Image<TPixel> destination)
        {
            TransformProcessorHelpers.UpdateDimensionalMetadata(destination);
            base.AfterImageApply(destination);
        }
    }
}
