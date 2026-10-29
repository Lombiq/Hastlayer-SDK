// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Modified file, original is found at:
// https://github.com/SixLabors/ImageSharp/blob/master/src/ImageSharp/Processing/Processors/Transforms/TransformProcessorHelpers.cs

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace Hast.Samples.SampleAssembly.ImageSharpModifications;

public static class TransformProcessorHelpers
{
    // Originally in Transform ProcessorHelpers.cs
    public static void UpdateDimensionalMetadata<TPixel>(Image<TPixel> image)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        var profile = image.Metadata.ExifProfile;
        if (profile is null)
        {
            return;
        }

        // Only set the value if it already exists.
        if (profile.TryGetValue(ExifTag.PixelXDimension, out _))
        {
            profile.SetValue(ExifTag.PixelXDimension, image.Width);
        }

        if (profile.TryGetValue(ExifTag.PixelYDimension, out _))
        {
            profile.SetValue(ExifTag.PixelYDimension, image.Height);
        }
    }
}
