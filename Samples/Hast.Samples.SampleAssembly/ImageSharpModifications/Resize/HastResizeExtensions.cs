﻿// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Modified file, original is found at:
// https://github.com/SixLabors/ImageSharp/blob/master/src/ImageSharp/Processing/Extensions/Transforms/ResizeExtensions.cs

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using System;

namespace Hastlayer_ImageSharp_PracticeDemo.Resize
{
    public static class HastResizeExtensions
    {
        /// <summary>
        /// Resizes an image to the given width and height.
        /// </summary>
        /// <param name="source">The image to resize.</param>
        /// <param name="width">The target image width.</param>
        /// <param name="height">The target image height.</param>
        /// <returns>The <see cref="IImageProcessingContext"/> to allow chaining of operations.</returns>
        /// <remarks>Passing zero for one of height or width will automatically preserve
        /// the aspect ratio of the original image or the nearest possible ratio.</remarks>
        public static IImageProcessingContext HastResize(this IImageProcessingContext source, int width, int height)
            => HastResize(
                source,
                width,
                height,
                Environment.ProcessorCount);

        /// <summary>
        /// Resizes an image to the given width and height.
        /// </summary>
        /// <param name="source">The image to resize.</param>
        /// <param name="width">The target image width.</param>
        /// <param name="height">The target image height.</param>
        /// <returns>The <see cref="IImageProcessingContext"/> to allow chaining of operations.</returns>
        /// <remarks>Passing zero for one of height or width will automatically preserve
        /// the aspect ratio of the original image or the nearest possible ratio.</remarks>
        public static IImageProcessingContext HastResize(
            this IImageProcessingContext source,
            int width,
            int height,
            int maxDegreeOfParallelism)
            => HastResize(
                source,
                width,
                height,
                maxDegreeOfParallelism,
                KnownResamplers.NearestNeighbor,
                new Rectangle(0, 0, width, height),
                false);

        /// <summary>
        /// Resizes an image to the given width and height with the given sampler and source rectangle.
        /// </summary>
        /// <param name="source">The image to resize.</param>
        /// <param name="width">The target image width.</param>
        /// <param name="height">The target image height.</param>
        /// <param name="sampler">The <see cref="IResampler"/> to perform the resampling.</param>
        /// <param name="targetRectangle">
        /// The <see cref="Rectangle"/> structure that specifies the portion of the target image object to draw to.
        /// </param>
        /// <param name="compand">Whether to compress and expand the image color-space to gamma correct the image 
        /// during processing.</param>
        /// <returns>The <see cref="IImageProcessingContext"/> to allow chaining of operations.</returns>
        /// <remarks>Passing zero for one of height or width will automatically preserve the aspect ratio of the 
        /// original image or the nearest possible ratio.</remarks>
        public static IImageProcessingContext HastResize(
            this IImageProcessingContext source,
            int width,
            int height,
            int maxDegreeOfParallelism,
            IResampler sampler,
            Rectangle targetRectangle,
            bool compand)
        {
            var options = new ResizeOptions
            {
                Size = new Size(width, height),
                Mode = ResizeMode.Manual,
                Sampler = sampler,
                TargetRectangle = targetRectangle,
                Compand = compand
            };

            return HastResize(source, options, maxDegreeOfParallelism);
        }

        /// <summary>
        /// Resizes an image in accordance with the given <see cref="ResizeOptions"/>.
        /// </summary>
        /// <param name="source">The image to resize.</param>
        /// <param name="options">The resize options.</param>
        /// <returns>The <see cref="IImageProcessingContext"/> to allow chaining of operations.</returns>
        /// <remarks>Passing zero for one of height or width within the resize options will automatically preserve 
        /// the aspect ratio of the original image or the nearest possible ratio.</remarks>
        public static IImageProcessingContext HastResize(
            this IImageProcessingContext source,
            ResizeOptions options,
            int maxDegreeOfParallelism)
            => source.ApplyProcessor(new HastResizeProcessor(options, source.GetCurrentSize(), maxDegreeOfParallelism));
    }
}
