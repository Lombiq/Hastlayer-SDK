// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Modified file, original is found at:
// https://github.com/SixLabors/ImageSharp/blob/master/src/ImageSharp/Processing/Processors/Transforms/Resize/ResizeProcessor%7BTPixel%7D.cs
// https://github.com/SixLabors/ImageSharp/blob/master/src/ImageSharp/Advanced/ParallelRowIterator.cs

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Hast.Samples.SampleAssembly.ImageSharpModifications.Resize;

internal class ResizeProcessor<TPixel> : TransformProcessor<TPixel>, IResamplingTransformImageProcessor<TPixel>
    where TPixel : unmanaged, IPixel<TPixel>
{
    private readonly int _destinationWidth;
    private readonly int _destinationHeight;
    private readonly IResampler _resampler;
    private readonly Rectangle _destinationRectangle;
    private Image<TPixel> _destination;

    public ResizeProcessor(
        Configuration configuration,
        ResizeProcessor definition,
        Image<TPixel> source,
        Rectangle sourceRectangle)
        : base(configuration, source, sourceRectangle)
    {
        _destinationWidth = definition.DestinationWidth;
        _destinationHeight = definition.DestinationHeight;
        _destinationRectangle = definition.DestinationRectangle;
        _resampler = definition.Sampler;
    }

    /// <inheritdoc/>
    protected override Size GetDestinationSize() => new(_destinationWidth, _destinationHeight);

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

        var intersect = Rectangle.Intersect(destinationRectangle, destination.Bounds());

        if (sampler is not NearestNeighborResampler) return;

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
                intersect);
        }
    }

    private static void ApplyNNResizeFrameTransform(
        Configuration configuration,
        ImageFrame<TPixel> source,
        ImageFrame<TPixel> destination,
        Rectangle sourceRectangle,
        Rectangle destinationRectangle,
        Rectangle intersect)
    {
        // Scaling factors
        var widthFactor = sourceRectangle.Width / (float)destinationRectangle.Width;
        var heightFactor = sourceRectangle.Height / (float)destinationRectangle.Height;

        var operation = new NNRowOperation(
            sourceRectangle,
            destinationRectangle,
            intersect,
            widthFactor,
            heightFactor,
            source.PixelBuffer,
            destination.PixelBuffer);

        IterateRows(configuration, intersect, operation);
    }

#pragma warning disable S3898 // Value types should implement "IEquatable<T>"
    private readonly struct NNRowOperation : IRowOperation
#pragma warning restore S3898 // Value types should implement "IEquatable<T>"
    {
        private readonly Rectangle _sourceBounds;
        private readonly Rectangle _destinationBounds;
        private readonly Rectangle _intersect;
        private readonly float _widthFactor;
        private readonly float _heightFactor;
        private readonly Buffer2D<TPixel> _source;
        private readonly Buffer2D<TPixel> _destination;

        [MethodImpl(InliningOptions.ShortMethod)]
        public NNRowOperation(
            Rectangle sourceBounds,
            Rectangle destinationBounds,
            Rectangle intersect,
            float widthFactor,
            float heightFactor,
            Buffer2D<TPixel> source,
            Buffer2D<TPixel> destination)
        {
            _sourceBounds = sourceBounds;
            _destinationBounds = destinationBounds;
            _intersect = intersect;
            _widthFactor = widthFactor;
            _heightFactor = heightFactor;
            _source = source;
            _destination = destination;
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        public void Invoke(int y)
        {
            var sourceX = _sourceBounds.X;
            var sourceY = _sourceBounds.Y;
            var destOriginX = _destinationBounds.X;
            var destOriginY = _destinationBounds.Y;
            var destLeft = _intersect.Left;
            var destRight = _intersect.Right;

            // Y coordinates of source points
            var sourceRow = _source.DangerousGetRowSpan((int)(((y - destOriginY) * _heightFactor) + sourceY));
            var targetRow = _destination.DangerousGetRowSpan(y);

            for (int x = destLeft; x < destRight; x++)
            {
                // X coordinates of source points
                targetRow[x] = sourceRow[(int)(((x - destOriginX) * _widthFactor) + sourceX)];
            }
        }
    }

    [MethodImpl(InliningOptions.ShortMethod)]
    public static void IterateRows<T>(Configuration configuration, Rectangle rectangle, T operation)
        where T : struct, IRowOperation
    {
        var parallelSettings = ParallelExecutionSettings.FromConfiguration(configuration);
        IterateRows(rectangle, in parallelSettings, operation);
    }

    public static void IterateRows<T>(
        Rectangle rectangle,
        in ParallelExecutionSettings paralellSettings,
        T operation)
        where T : struct, IRowOperation
    {
        var top = rectangle.Top;
        var bottom = rectangle.Bottom;
        var width = rectangle.Width;
        var height = rectangle.Height;

        var maxSteps = DivideCeil(width * height, paralellSettings.MinimumPixelsProcessedPerTask);
        var numOfSteps = Math.Min(paralellSettings.MaxDegreeOfParallelism, maxSteps);

        // Avoid TPL overhead in this trivial case:
        if (numOfSteps == 1)
        {
            for (int y = top; y < bottom; y++)
            {
                Unsafe.AsRef(operation).Invoke(y);
            }

            return;
        }

        var verticalStep = DivideCeil(rectangle.Height, numOfSteps);
        var paralellOptions = new ParallelOptions { MaxDegreeOfParallelism = numOfSteps };
        var wrappingOperation = new RowOperationWrapper<T>(top, bottom, verticalStep, in operation);

        var tasks = new Task[paralellOptions.MaxDegreeOfParallelism];

        for (int t = 0; t < paralellOptions.MaxDegreeOfParallelism; t++)
        {
            tasks[t] = Task.Factory.StartNew(
                inputObject => wrappingOperation.Invoke((int)inputObject), t, paralellOptions.CancellationToken);
        }

        Task.WhenAll(tasks).Wait(paralellOptions.CancellationToken);

        // Serial version of the same code.
        ////for (int t = 0; t < paralellOptions.MaxDegreeOfParallelism; t++)
        ////{
        ////    wrappingOperation.Invoke(t);
        ////}
    }

    [MethodImpl(InliningOptions.ShortMethod)]
    private static int DivideCeil(int dividend, int divisor) => 1 + ((dividend - 1) / divisor);

    [StructLayout(LayoutKind.Auto)]
#pragma warning disable S3898 // Value types should implement "IEquatable<T>"
    private readonly struct RowOperationWrapper<T>
#pragma warning restore S3898 // Value types should implement "IEquatable<T>"
        where T : struct, IRowOperation
    {
        private readonly int _minY;
        private readonly int _maxY;
        private readonly int _stepY;
        private readonly T _action;

        [MethodImpl(InliningOptions.ShortMethod)]
        public RowOperationWrapper(
            int minY,
            int maxY,
            int stepY,
            in T action)
        {
            _minY = minY;
            _maxY = maxY;
            _stepY = stepY;
            _action = action;
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        public void Invoke(int i)
        {
            var yMin = _minY + (i * _stepY);

            if (yMin >= _maxY)
            {
                return;
            }

            var yMax = Math.Min(yMin + _stepY, _maxY);

            for (int y = yMin; y < yMax; y++)
            {
                // Skip the safety copy when invoking a potentially impure method on a readonly field
                Unsafe.AsRef(_action).Invoke(y);
            }
        }
    }
}
