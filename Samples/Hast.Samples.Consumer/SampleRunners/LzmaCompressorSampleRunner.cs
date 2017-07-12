using System.Threading.Tasks;
using Hast.Layer;
using Hast.Samples.SampleAssembly;
using System.IO;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal static class LzmaCompressorSampleRunner
    {
        private const string UncompressedTextFilePath = "Uncompressed.txt";
        private const string CompressedTextFilePath = "Compressed.txt.lzma";


        public static void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<LzmaCompressor>();
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
        {
            var lzmaCompressor = await hastlayer.GenerateProxy(hardwareRepresentation, new LzmaCompressor());

            if (File.Exists(CompressedTextFilePath)) File.Delete(CompressedTextFilePath);

            var inputFileBytes = File.ReadAllBytes(UncompressedTextFilePath);

            var outputFileBytes = lzmaCompressor.CompressBytes(inputFileBytes);

            File.WriteAllBytes(CompressedTextFilePath, outputFileBytes);
        }
    }
}
