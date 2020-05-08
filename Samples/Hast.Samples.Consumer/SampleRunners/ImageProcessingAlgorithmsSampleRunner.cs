﻿using Hast.Layer;
using Hast.Samples.SampleAssembly;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal static class ImageProcessingAlgorithmsSampleRunner
    {
        public static void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<ImageContrastModifier>();

            // ImageFilter is not parallelized, so not including it not to take away FPGA resources from
            // ImageContrastModifier.
            //configuration.AddHardwareEntryPointType<ImageFilter>();
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
        {
            using (var bitmap = new Bitmap("fpga.jpg"))
            {
                var imageContrastModifier = await hastlayer
                    .GenerateProxy(hardwareRepresentation, new ImageContrastModifier());
                var modifiedImage = imageContrastModifier.ChangeImageContrast(bitmap, -50);
                modifiedImage.Save("contrast.bmp", ImageFormat.Bmp);

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var cpuOutput = new ImageContrastModifier().ChangeImageContrast(bitmap, -50);
                sw.Stop();
                System.Console.WriteLine("On CPU it took " + sw.ElapsedMilliseconds + " ms.");

                // ImageFilter disabled until it's improved.
                //var imageFilter = await hastlayer.GenerateProxy(hardwareRepresentation, new ImageFilter());
                //var filteredImage = imageFilter.DetectHorizontalEdges(bitmap);
            }
        }
    }
}
