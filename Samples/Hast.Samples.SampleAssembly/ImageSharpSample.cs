using Hast.Transformer.Abstractions.SimpleMemory;
using System.Threading.Tasks;
using Hast.Layer;
using Hast.Synthesis.Abstractions;
using SixLabors.ImageSharp;
using ImageSharpHastlayerExtension.Resize;
using SixLabors.ImageSharp.Processing;
using Bitmap = System.Drawing.Bitmap;
using System.IO;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Png;
using Hast.Samples.SampleAssembly.ImageSharpModifications.Extensions;
using SixLabors.ImageSharp.PixelFormats;

namespace Hast.Samples.SampleAssembly
{
    /// <summary>
    /// Algorithm for resizing image with a modified Image Sharp library
    /// </summary>
    public class ImageSharpSample
    {
        private const int Resize_ImageWidthIndex = 0;
        private const int Resize_ImageHeightIndex = 1;

        [Replaceable(nameof(ImageSharpSample) + "." + nameof(MaxDegreeOfParallelism))]
        private static readonly int MaxDegreeOfParallelism = 25;

        public virtual void VirtualResize(SimpleMemory memory)
        {
            // TODO
        }

        /// <summary>
        /// Changes the contrast of an image. Same as <see cref="ChangeContrast"/>. Used for Hast.Communication.Tester
        /// to access this sample by a common method name just for testing. Internal so it doesn't bother otherwise.
        /// </summary>
        /// <param name="memory">The <see cref="SimpleMemory"/> object representing the accessible memory space.</param>
        internal virtual void Run(SimpleMemory memory) => VirtualResize(memory); // Re-think this??

        public Image Resize(Image image, IHastlayer hastlayer, IHardwareGenerationConfiguration hardwareGenerationConfiguration)
        {
            var parameters = new HastlayerResizeParameters
            {
                ImageWidthIndex = Resize_ImageWidthIndex,
                ImageHeightIndex = Resize_ImageHeightIndex,
                Hastlayer = hastlayer,
                HardwareGenerationConfiguration = hardwareGenerationConfiguration,
            };

            image.Mutate(x => x.HastResize(image.Width / 2, image.Height / 2, MaxDegreeOfParallelism, parameters));

            return image;
        }
        public class HastlayerResizeParameters
        {
            public int ImageWidthIndex { get; set; }
            public int ImageHeightIndex { get; set; }
            public IHastlayer Hastlayer { get; set; }
            public IHardwareGenerationConfiguration HardwareGenerationConfiguration { get; set; }
        }
    }
}
