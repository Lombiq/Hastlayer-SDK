// Modified file, original is found at:  
// https://gist.github.com/vurdalakov/00d9471356da94454b372843067af24e

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Png;
using System.IO;
using Bitmap = System.Drawing.Bitmap;

namespace Hast.Samples.SampleAssembly.ImageSharpModifications.Extensions
{
    public static class ImageSharpExtensions
    {
        public static Bitmap ToBitmap(Image image)
        {
            using (var memoryStream = new MemoryStream())
            {
                var imageEncoder = image.GetConfiguration().ImageFormatsManager.FindEncoder(PngFormat.Instance);
                image.Save(memoryStream, imageEncoder);

                memoryStream.Seek(0, SeekOrigin.Begin);

                return new Bitmap(memoryStream);
            }
        }

        public static Image ToImageSharpImage(Bitmap bitmap)
        {
            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);

                memoryStream.Seek(0, SeekOrigin.Begin);

                return Image.Load(memoryStream);
            }
        }
    }
}
