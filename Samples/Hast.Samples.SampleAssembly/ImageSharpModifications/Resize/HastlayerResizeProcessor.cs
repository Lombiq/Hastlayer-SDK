// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Modified file, original is found at:
// https://github.com/SixLabors/ImageSharp/blob/master/src/ImageSharp/Processing/Processors/Transforms/Resize/ResizeProcessor.cs

using Hast.Layer;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

namespace Hast.Samples.SampleAssembly.ImageSharpModifications.Resize;

public class HastlayerResizeProcessor : CloningImageProcessor
{
    private static readonly object _lock = new();

    internal static HastlayerAcceleratedImageSharp ResizeProxy;

    public static TextWriter LogPixelsWriter { get; set; }

    public HastlayerResizeProcessor(
        ResizeOptions options,
        Size sourceSize,
        int maxDegreeOfParallelism,
        IHastlayer hastlayer,
        IHardwareRepresentation hardwareRepresentation,
        IProxyGenerationConfiguration configuration)
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
        Hastlayer = hastlayer;
        HardwareRepresentation = hardwareRepresentation;
        Configuration = configuration;

        if (ResizeProxy == null)
        {
            lock (_lock)
            {
                // We only want to create the proxy once, but it requires the IHastlayer instance that's not available
                // from a static member.
#pragma warning disable S3010 // S3010:Static fields should not be updated in constructors
                ResizeProxy = hastlayer
                    .GenerateProxyAsync(hardwareRepresentation, new HastlayerAcceleratedImageSharp(), configuration).Result;
#pragma warning restore S3010 // S3010:Static fields should not be updated in constructors
            }
        }
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

    /// <summary>
    /// Gets necessary values for Hastlayer.
    /// </summary>
    public IHastlayer Hastlayer { get; }

    /// <summary>
    /// Gets necessary values for Hastlayer.
    /// </summary>
    public IHardwareRepresentation HardwareRepresentation { get; }

    /// <summary>
    /// Gets necessary values for Hastlayer.
    /// </summary>
    public IProxyGenerationConfiguration Configuration { get; }

    public override ICloningImageProcessor<TPixel> CreatePixelSpecificCloningProcessor<TPixel>(
        Configuration configuration,
        Image<TPixel> source,
        Rectangle sourceRectangle)
    {
        configuration.MaxDegreeOfParallelism = MaxDegreeOfParallelism;

        if (LogPixelsWriter != null) PixelsToOutput(source, "before");
        var result = new HastlayerResizeProcessor<TPixel>(
            configuration, this, source, sourceRectangle, Hastlayer, HardwareRepresentation, Configuration);
        if (LogPixelsWriter != null) PixelsToOutput(source, "after");

        return result;
    }

    [SuppressMessage(
        "Major Code Smell",
        "S106:Standard outputs should not be used directly to log anything",
        Justification = "This is a logger method.")]
    private void PixelsToOutput<TPixel>(Image<TPixel> source, string note)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        Console.WriteLine($"Pixels in the image {note}:");

        for (int i = 0; i < source.Height; i++)
        {
            var row = MemoryMarshal.Cast<TPixel, byte>(source.DangerousGetPixelRowMemory(i).Span);
            foreach (var pixelByte in row) Console.Write($"{pixelByte:X2} ");
            Console.WriteLine();
        }

        Console.WriteLine("\n\n\n");
    }
}
