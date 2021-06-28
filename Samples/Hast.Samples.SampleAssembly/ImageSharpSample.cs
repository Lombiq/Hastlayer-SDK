using Hast.Layer;
using Hast.Samples.SampleAssembly.ImageSharpModifications.Resize;
using Hast.Synthesis.Abstractions;
using Hast.Transformer.Abstractions.SimpleMemory;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Threading.Tasks;

namespace Hast.Samples.SampleAssembly
{
    /// <summary>
    /// Algorithm for resizing images with a modified ImageSharp library.
    /// </summary>
    public class ImageSharpSample
    {
        //private const int Resize_ImageWidthIndex = 0;
        //private const int Resize_ImageHeightIndex = 1;
        //private const int Resize_DestinationImageWidthIndex = 2;
        //private const int Resize_DestinationImageHeightIndex = 3;
        //private const int Resize_FrameCount = 4;
        //private const int Resize_ImageStartIndex = 5;

        private const int Resize_DestinationImageWidthIndex = 0;
        private const int Resize_DestinationImageHeightIndex = 1;
        private const int Resize_ImageWidthIndex = 2;
        private const int Resize_ImageHeightIndex = 3;
        private const int Resize_HeightStartIndex = 4;

        [Replaceable(nameof(ImageSharpSample) + "." + nameof(MaxDegreeOfParallelism))]
        private static readonly int MaxDegreeOfParallelism = 25;

        public virtual void CreateMatrix(SimpleMemory memory)
        {
            var destinationWidth = (ushort)memory.ReadUInt32(Resize_DestinationImageWidthIndex);   //6
            var destinationHeight = (ushort)memory.ReadUInt32(Resize_DestinationImageHeightIndex); //4
            var width = (ushort)memory.ReadUInt32(Resize_ImageWidthIndex);                         //12
            var height = (ushort)memory.ReadUInt32(Resize_ImageHeightIndex);                       //8

            var widthFactor = width / destinationWidth;                                            //2
            var heightFactor = height / destinationHeight;                                         //2

            var Resize_WidthStartIndex = Resize_HeightStartIndex + destinationHeight;

            // BAD Serialized version
            //for (int y = 0; y < destinationHeight; y++)
            //{
            //    // int sourceRowStartIndex = (int)(y * heightFactor * destinationWidth * heightFactor); //0,24,48,72
            //    for (int x = 0; x < destinationWidth; x++)
            //    {
            //        var pixelIndex = y * heightFactor * destinationWidth * heightFactor + x * widthFactor; //0,2,4,6,8,10;24,26,28,30,32,34;...
            //        memory.WriteInt32(y * destinationWidth + x + Resize_StartIndex, pixelIndex); //4,5,6,7,8,9;10,11,12,13,14,15;...
            //    }
            //}

            for (int y = 0; y < destinationHeight; y++)
            {
                int rowStartIndex = y * heightFactor; // Add value to an array
                memory.WriteInt32(Resize_HeightStartIndex + y, rowStartIndex);
            }

            for (int x = 0; x < destinationWidth; x++)
            {
                int pixelIndex = x * widthFactor; // Add value to an array
                memory.WriteInt32(Resize_WidthStartIndex + x, pixelIndex);
            }
        }

        //public virtual void ApplyTransform(SimpleMemory memory)
        //{
        //    var width = (ushort)memory.ReadUInt32(Resize_ImageWidthIndex);
        //    var height = (ushort)memory.ReadUInt32(Resize_ImageHeightIndex);
        //    var destWidth = (ushort)memory.ReadUInt32(Resize_DestinationImageWidthIndex);
        //    var destHeight = (ushort)memory.ReadUInt32(Resize_DestinationImageHeightIndex);
        //    var frameCount = (ushort)memory.ReadUInt32(Resize_FrameCount);
        //    var destinationStartIndex = width * height + 5;

        //    var widthFactor = width / destWidth;
        //    var heightFactor = height / destHeight;

        //    var tasks = new Task<PixelProcessingTaskOutput>[MaxDegreeOfParallelism];

        //    var stepCount = destWidth / MaxDegreeOfParallelism;
        //    var wastedSteps = destWidth % MaxDegreeOfParallelism;

        //    if (wastedSteps != 0)
        //    {
        //        stepCount += 1;
        //    }

        //    for (int f = 0; f < frameCount; f++)
        //    {
        //        for (int y = 0; y < destHeight; y++)
        //        {
        //            for (int x = 0; x < stepCount; x++)
        //            {
        //                for (int t = 0; t < MaxDegreeOfParallelism; t++)
        //                {
        //                    if (x * widthFactor * MaxDegreeOfParallelism + t * widthFactor >= width)
        //                    {
        //                        break;
        //                    }

        //                    var pixelBytes = memory.Read4Bytes(
        //                        (1 + f) * (y * heightFactor * destWidth * heightFactor
        //                            + x * widthFactor * MaxDegreeOfParallelism
        //                            + t * widthFactor) + Resize_ImageStartIndex);

        //                    tasks[t] = Task.Factory.StartNew(inputObject =>
        //                    {
        //                        var input = (PixelProcessingTaskInput)inputObject;

        //                        return new PixelProcessingTaskOutput
        //                        {
        //                            R = input.PixelBytes[0],
        //                            G = input.PixelBytes[1],
        //                            B = input.PixelBytes[2],
        //                            A = input.PixelBytes[3]
        //                        };

        //                    },
        //                    new PixelProcessingTaskInput { PixelBytes = pixelBytes });
        //                }

        //                Task.WhenAll(tasks).Wait();

        //                for (int t = 0; t < MaxDegreeOfParallelism; t++)
        //                {
        //                    // Don't write unnecessary stuff leftover from the process
        //                    if (x * widthFactor * MaxDegreeOfParallelism + t * widthFactor >= width)
        //                    {
        //                        break;
        //                    }

        //                    memory.Write4Bytes(
        //                       destinationStartIndex + (1 + f) * (x * MaxDegreeOfParallelism + y * destWidth + t),
        //                       new[] { tasks[t].Result.R, tasks[t].Result.G, tasks[t].Result.B, tasks[t].Result.A });
        //                }
        //            }
        //        }
        //    }
        //}

        public Image Resize(
            Image image,
            IHastlayer hastlayer,
            IHardwareGenerationConfiguration hardwareGenerationConfiguration)
        {
            var newImage = image.Clone(img => img.HastResize(
                image.Width / 2, image.Height / 2, MaxDegreeOfParallelism, hastlayer, hardwareGenerationConfiguration));

            return newImage;
        }

        private class PixelProcessingTaskInput
        {
            public byte[] PixelBytes { get; set; }
        }

        private class PixelProcessingTaskOutput
        {
            public byte R { get; set; }
            public byte G { get; set; }
            public byte B { get; set; }
            public byte A { get; set; }
        }
    }
}
