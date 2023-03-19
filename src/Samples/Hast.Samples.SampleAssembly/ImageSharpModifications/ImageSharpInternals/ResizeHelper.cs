// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Modified file, original is found at:
// https://github.com/SixLabors/ImageSharp/blob/master/src/ImageSharp/Processing/Processors/Transforms/Resize/ResizeHelper.cs

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;

namespace Hast.Samples.SampleAssembly.ImageSharpModifications;

public static class ResizeHelper
{
    public static (Size Size, Rectangle Rectangle) CalculateTargetLocationAndBounds(Size sourceSize, ResizeOptions options)
    {
        int width = options.Size.Width;
        int height = options.Size.Height;

        if (width <= 0 && height <= 0)
        {
            ThrowInvalid(
                FormattableString.Invariant($"Target width {width} and height {height} must be greater than zero."));
        }

        // Ensure target size is populated across both dimensions.
        // These dimensions are used to calculate the final dimensions determined by the mode algorithm.
        // If only one of the incoming dimensions is 0, it will be modified here to maintain aspect ratio.
        // If it is not possible to keep aspect ratio, make sure at least the minimum is is kept.
        const int Min = 1;
        if (width == 0 && height > 0)
        {
            width = (int)MathF.Max(Min, MathF.Round(sourceSize.Width * height / (float)sourceSize.Height));
        }

        if (height == 0 && width > 0)
        {
            height = (int)MathF.Max(Min, MathF.Round(sourceSize.Height * width / (float)sourceSize.Width));
        }

        return options.Mode switch
        {
            ResizeMode.Crop => CalculateCropRectangle(sourceSize, options, width, height),
            ResizeMode.Pad => CalculatePadRectangle(sourceSize, options, width, height),
            ResizeMode.BoxPad => CalculateBoxPadRectangle(sourceSize, options, width, height),
            ResizeMode.Max => CalculateMaxRectangle(sourceSize, width, height),
            ResizeMode.Min => CalculateMinRectangle(sourceSize, width, height),
            ResizeMode.Manual => CalculateManualRectangle(options, width, height),
            _ => (new Size(Sanitize(width), Sanitize(height)), new Rectangle(0, 0, Sanitize(width), Sanitize(height))),
        };
    }

    private static (Size Size, Rectangle Rectangle) CalculateBoxPadRectangle(
        Size source,
        ResizeOptions options,
        int width,
        int height)
    {
        int sourceWidth = source.Width;
        int sourceHeight = source.Height;

        // Fractional variants for preserving aspect ratio.
        float percentHeight = MathF.Abs(height / (float)sourceHeight);
        float percentWidth = MathF.Abs(width / (float)sourceWidth);

        int boxPadHeight = height > 0 ? height : (int)MathF.Round(sourceHeight * percentWidth);
        int boxPadWidth = width > 0 ? width : (int)MathF.Round(sourceWidth * percentHeight);

        // Only calculate if upscaling.
        if (sourceWidth < boxPadWidth && sourceHeight < boxPadHeight)
        {
            int targetX;
            int targetY;
            int targetWidth = sourceWidth;
            int targetHeight = sourceHeight;
            width = boxPadWidth;
            height = boxPadHeight;

            switch (options.Position)
            {
                case AnchorPositionMode.Left:
                    targetY = (height - sourceHeight) / 2;
                    targetX = 0;
                    break;
                case AnchorPositionMode.Right:
                    targetY = (height - sourceHeight) / 2;
                    targetX = width - sourceWidth;
                    break;
                case AnchorPositionMode.TopRight:
                    targetY = 0;
                    targetX = width - sourceWidth;
                    break;
                case AnchorPositionMode.Top:
                    targetY = 0;
                    targetX = (width - sourceWidth) / 2;
                    break;
                case AnchorPositionMode.TopLeft:
                    targetY = 0;
                    targetX = 0;
                    break;
                case AnchorPositionMode.BottomRight:
                    targetY = height - sourceHeight;
                    targetX = width - sourceWidth;
                    break;
                case AnchorPositionMode.Bottom:
                    targetY = height - sourceHeight;
                    targetX = (width - sourceWidth) / 2;
                    break;
                case AnchorPositionMode.BottomLeft:
                    targetY = height - sourceHeight;
                    targetX = 0;
                    break;
#pragma warning disable S3458 // Empty "case" clauses that fall through to the "default" should be omitted
                // This should fall through to the "default"
                case AnchorPositionMode.Center:
#pragma warning restore S3458 // Empty "case" clauses that fall through to the "default" should be omitted
                default:
                    targetY = (height - sourceHeight) / 2;
                    targetX = (width - sourceWidth) / 2;
                    break;
            }

            // Target image width and height can be different to the rectangle width and height.
            return (new Size(Sanitize(width), Sanitize(height)), new Rectangle(targetX, targetY, Sanitize(targetWidth), Sanitize(targetHeight)));
        }

        // Switch to pad mode to downscale and calculate from there.
        return CalculatePadRectangle(source, options, width, height);
    }

    private static (Size Size, Rectangle Rectangle) CalculateCropRectangle(
        Size source,
        ResizeOptions options,
        int width,
        int height)
    {
        float ratio;
        int sourceWidth = source.Width;
        int sourceHeight = source.Height;

        int targetX = 0;
        int targetY = 0;
        int targetWidth = width;
        int targetHeight = height;

        // Fractional variants for preserving aspect ratio.
        float percentHeight = MathF.Abs(height / (float)sourceHeight);
        float percentWidth = MathF.Abs(width / (float)sourceWidth);

        if (percentHeight < percentWidth)
        {
            ratio = percentWidth;

            if (options.CenterCoordinates.HasValue)
            {
                float center = -(ratio * sourceHeight) * options.CenterCoordinates.Value.Y;
                targetY = (int)MathF.Round(center + (height / 2F));

                if (targetY > 0)
                {
                    targetY = 0;
                }

                if (targetY < (int)MathF.Round(height - (sourceHeight * ratio)))
                {
                    targetY = (int)MathF.Round(height - (sourceHeight * ratio));
                }
            }
            else
            {
                targetY = options.Position switch
                {
                    AnchorPositionMode.Top => 0,
                    AnchorPositionMode.TopLeft => 0,
                    AnchorPositionMode.TopRight => 0,
                    AnchorPositionMode.Bottom => (int)MathF.Round(height - (sourceHeight * ratio)),
                    AnchorPositionMode.BottomLeft => (int)MathF.Round(height - (sourceHeight * ratio)),
                    AnchorPositionMode.BottomRight => (int)MathF.Round(height - (sourceHeight * ratio)),
                    AnchorPositionMode.Center => (int)MathF.Round((height - (sourceHeight * ratio)) / 2F),
                    AnchorPositionMode.Left => (int)MathF.Round((height - (sourceHeight * ratio)) / 2F),
                    AnchorPositionMode.Right => (int)MathF.Round((height - (sourceHeight * ratio)) / 2F),
                    _ => (int)MathF.Round((height - (sourceHeight * ratio)) / 2F),
                };
            }

            targetHeight = (int)MathF.Ceiling(sourceHeight * percentWidth);
        }
        else
        {
            ratio = percentHeight;

            if (options.CenterCoordinates.HasValue)
            {
                float center = -(ratio * sourceWidth) * options.CenterCoordinates.Value.X;
                targetX = (int)MathF.Round(center + (width / 2F));

                if (targetX > 0)
                {
                    targetX = 0;
                }

                if (targetX < (int)MathF.Round(width - (sourceWidth * ratio)))
                {
                    targetX = (int)MathF.Round(width - (sourceWidth * ratio));
                }
            }
            else
            {
                targetX = options.Position switch
                {
                    AnchorPositionMode.Left => 0,
                    AnchorPositionMode.TopLeft => 0,
                    AnchorPositionMode.BottomLeft => 0,
                    AnchorPositionMode.Right => (int)MathF.Round(width - (sourceWidth * ratio)),
                    AnchorPositionMode.TopRight => (int)MathF.Round(width - (sourceWidth * ratio)),
                    AnchorPositionMode.BottomRight => (int)MathF.Round(width - (sourceWidth * ratio)),
                    AnchorPositionMode.Center => (int)MathF.Round((width - (sourceWidth * ratio)) / 2F),
                    AnchorPositionMode.Top => (int)MathF.Round((width - (sourceWidth * ratio)) / 2F),
                    AnchorPositionMode.Bottom => (int)MathF.Round((width - (sourceWidth * ratio)) / 2F),
                    _ => (int)MathF.Round((width - (sourceWidth * ratio)) / 2F),
                };
            }

            targetWidth = (int)MathF.Ceiling(sourceWidth * percentHeight);
        }

        // Target image width and height can be different to the rectangle width and height.
        return (new Size(Sanitize(width), Sanitize(height)), new Rectangle(targetX, targetY, Sanitize(targetWidth), Sanitize(targetHeight)));
    }

    private static (Size Size, Rectangle Rectangle) CalculateMaxRectangle(
        Size source,
        int width,
        int height)
    {
        int targetWidth = width;
        int targetHeight = height;

        // Fractional variants for preserving aspect ratio.
        float percentHeight = MathF.Abs(height / (float)source.Height);
        float percentWidth = MathF.Abs(width / (float)source.Width);

        // Integers must be cast to floats to get needed precision
        float ratio = height / (float)width;
        float sourceRatio = source.Height / (float)source.Width;

        if (sourceRatio < ratio)
        {
            targetHeight = (int)MathF.Round(source.Height * percentWidth);
        }
        else
        {
            targetWidth = (int)MathF.Round(source.Width * percentHeight);
        }

        // Replace the size to match the rectangle.
        return (new Size(Sanitize(targetWidth), Sanitize(targetHeight)), new Rectangle(0, 0, Sanitize(targetWidth), Sanitize(targetHeight)));
    }

    private static (Size Size, Rectangle Rectangle) CalculateMinRectangle(
        Size source,
        int width,
        int height)
    {
        int sourceWidth = source.Width;
        int sourceHeight = source.Height;
        int targetWidth = width;
        int targetHeight = height;

        // Don't upscale
        if (width > sourceWidth || height > sourceHeight)
        {
            return (new Size(sourceWidth, sourceHeight), new Rectangle(0, 0, sourceWidth, sourceHeight));
        }

        // Find the shortest distance to go.
        int widthDiff = sourceWidth - width;
        int heightDiff = sourceHeight - height;

        if (widthDiff < heightDiff)
        {
            float sourceRatio = (float)sourceHeight / sourceWidth;
            targetHeight = (int)MathF.Round(width * sourceRatio);
        }
        else if (widthDiff > heightDiff)
        {
            float sourceRatioInverse = (float)sourceWidth / sourceHeight;
            targetWidth = (int)MathF.Round(height * sourceRatioInverse);
        }
        else
        {
            if (height > width)
            {
                float percentWidth = MathF.Abs(width / (float)sourceWidth);
                targetHeight = (int)MathF.Round(sourceHeight * percentWidth);
            }
            else
            {
                float percentHeight = MathF.Abs(height / (float)sourceHeight);
                targetWidth = (int)MathF.Round(sourceWidth * percentHeight);
            }
        }

        // Replace the size to match the rectangle.
        return (new Size(Sanitize(targetWidth), Sanitize(targetHeight)), new Rectangle(0, 0, Sanitize(targetWidth), Sanitize(targetHeight)));
    }

    private static (Size Size, Rectangle Rectangle) CalculatePadRectangle(
        Size sourceSize,
        ResizeOptions options,
        int width,
        int height)
    {
        float ratio;
        int sourceWidth = sourceSize.Width;
        int sourceHeight = sourceSize.Height;

        int targetX = 0;
        int targetY = 0;
        int targetWidth = width;
        int targetHeight = height;

        // Fractional variants for preserving aspect ratio.
        float percentHeight = MathF.Abs(height / (float)sourceHeight);
        float percentWidth = MathF.Abs(width / (float)sourceWidth);

        if (percentHeight < percentWidth)
        {
            ratio = percentHeight;
            targetWidth = (int)MathF.Round(sourceWidth * percentHeight);

            targetX = options.Position switch
            {
                AnchorPositionMode.Left => 0,
                AnchorPositionMode.TopLeft => 0,
                AnchorPositionMode.BottomLeft => 0,
                AnchorPositionMode.Right => (int)MathF.Round(width - (sourceWidth * ratio)),
                AnchorPositionMode.TopRight => (int)MathF.Round(width - (sourceWidth * ratio)),
                AnchorPositionMode.BottomRight => (int)MathF.Round(width - (sourceWidth * ratio)),
                AnchorPositionMode.Center => (int)MathF.Round((width - (sourceWidth * ratio)) / 2F),
                AnchorPositionMode.Top => (int)MathF.Round((width - (sourceWidth * ratio)) / 2F),
                AnchorPositionMode.Bottom => (int)MathF.Round((width - (sourceWidth * ratio)) / 2F),
                _ => (int)MathF.Round((width - (sourceWidth * ratio)) / 2F),
            };
        }
        else
        {
            ratio = percentWidth;
            targetHeight = (int)MathF.Round(sourceHeight * percentWidth);

            targetY = options.Position switch
            {
                AnchorPositionMode.Top => 0,
                AnchorPositionMode.TopLeft => 0,
                AnchorPositionMode.TopRight => 0,
                AnchorPositionMode.Bottom => (int)MathF.Round(height - (sourceHeight * ratio)),
                AnchorPositionMode.BottomLeft => (int)MathF.Round(height - (sourceHeight * ratio)),
                AnchorPositionMode.BottomRight => (int)MathF.Round(height - (sourceHeight * ratio)),
                AnchorPositionMode.Center => (int)MathF.Round((height - (sourceHeight * ratio)) / 2F),
                AnchorPositionMode.Left => (int)MathF.Round((height - (sourceHeight * ratio)) / 2F),
                AnchorPositionMode.Right => (int)MathF.Round((height - (sourceHeight * ratio)) / 2F),
                _ => (int)MathF.Round((height - (sourceHeight * ratio)) / 2F),
            };
        }

        // Target image width and height can be different to the rectangle width and height.
        return (new Size(Sanitize(width), Sanitize(height)), new Rectangle(targetX, targetY, Sanitize(targetWidth), Sanitize(targetHeight)));
    }

    private static (Size Size, Rectangle Rectangle) CalculateManualRectangle(
        ResizeOptions options,
        int width,
        int height)
    {
        if (!options.TargetRectangle.HasValue)
        {
            ThrowInvalid("Manual resizing requires a target location and size.");
        }

        var targetRectangle = options.TargetRectangle.Value;

        int targetX = targetRectangle.X;
        int targetY = targetRectangle.Y;
        int targetWidth = targetRectangle.Width > 0 ? targetRectangle.Width : width;
        int targetHeight = targetRectangle.Height > 0 ? targetRectangle.Height : height;

        // Target image width and height can be different to the rectangle width and height.
        return (new Size(Sanitize(width), Sanitize(height)), new Rectangle(targetX, targetY, Sanitize(targetWidth), Sanitize(targetHeight)));
    }

    private static void ThrowInvalid(string message) => throw new InvalidOperationException(message);

    private static int Sanitize(int input) => Math.Max(1, input);
}
