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
using System;
using System.Runtime.InteropServices;
using static Hast.Samples.SampleAssembly.HastlayerAcceleratedImageSharp;

namespace Hast.Samples.SampleAssembly.ImageSharpModifications.Resize;

internal class HastlayerResizeProcessor<TPixel> : TransformProcessor<TPixel>, IResamplingTransformImageProcessor<TPixel>
    where TPixel : unmanaged, IPixel<TPixel>
{
    private readonly int _destinationWidth;
    private readonly int _destinationHeight;
    private readonly IResampler _resampler;
    private readonly IHastlayer _hastlayer;
    private readonly IHardwareRepresentation _hardwareRepresentation;

    private Image<TPixel> _destination;

    public HastlayerResizeProcessor(
        Configuration configuration,
        HastlayerResizeProcessor definition,
        Image<TPixel> source,
        Rectangle sourceRectangle,
        IHastlayer hastlayer,
        IHardwareRepresentation hardwareRepresentation)
        : base(configuration, source, sourceRectangle)
    {
        _destinationWidth = definition.DestinationWidth;
        _destinationHeight = definition.DestinationHeight;
        _resampler = definition.Sampler;
        _hastlayer = hastlayer;
        _hardwareRepresentation = hardwareRepresentation;
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
        if (sampler is not NearestNeighborResampler) return;

        var memory = CreateMatrixMemory(
            Source,
            _destinationWidth,
            _destinationHeight,
            _hastlayer,
            _hardwareRepresentation.HardwareGenerationConfiguration);

        HastlayerResizeProcessor.ResizeProxy.CreateMatrix(memory);

        var accessor = new SimpleMemoryAccessor(memory);
        var rowIndices = Slice(accessor, HeaderCellCount, _destinationHeight);
        var pixelIndices = Slice(accessor, HeaderCellCount + _destinationHeight, _destinationWidth);

        for (int i = 0; i < Source.Frames.Count; i++)
        {
            var sourceFrame = Source.Frames[i];
            var destinationFrame = _destination.Frames[i];

            for (int y = 0; y < destinationFrame.Height; y++)
            {
                var destinationRow = destinationFrame.PixelBuffer.DangerousGetRowSpan(y);

                var sourceRow = sourceFrame.PixelBuffer.DangerousGetRowSpan(rowIndices[y]);

                for (int x = 0; x < destinationFrame.Width; x++)
                {
                    destinationRow[x] = sourceRow[pixelIndices[x]];
                }
            }
        }
    }

    public static SimpleMemory CreateMatrixMemory(
        Image<TPixel> image,
        int destinationWidth,
        int destinationHeight,
        IHastlayer hastlayer,
        IHardwareGenerationConfiguration hardwareGenerationConfiguration)
    {
        var width = image.Width;
        var height = image.Height;
        var cellCount = HeaderCellCount + destinationWidth + destinationHeight;

        var memory = hastlayer is null
            ? SimpleMemory.CreateSoftwareMemory(cellCount)
            : hastlayer.CreateMemory(hardwareGenerationConfiguration, cellCount);

        memory.WriteUInt32(ResizeDestinationImageWidthIndex, (uint)destinationWidth);
        memory.WriteUInt32(ResizeDestinationImageHeightIndex, (uint)destinationHeight);
        memory.WriteUInt32(ResizeImageWidthIndex, (uint)width);
        memory.WriteUInt32(ResizeImageHeightIndex, (uint)height);

        return memory;
    }

    private static Span<int> Slice(SimpleMemoryAccessor accessor, int cellOffset, int cellLength) =>
        MemoryMarshal.Cast<byte, int>(
            accessor.Get().Span.Slice(
                cellOffset * SimpleMemory.MemoryCellSizeBytes,
                cellLength * SimpleMemory.MemoryCellSizeBytes));
}
