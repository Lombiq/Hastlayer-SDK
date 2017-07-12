namespace Hast.Samples.SampleAssembly.Lzma.Constants
{
    internal static class RangeEncoderConstants
    {
        public const uint TopValue = (1 << 24);
        public const int BitModelTotalBits = 11;
        public const uint BitModelTotal = (1 << BitModelTotalBits);
        public const int BitPriceShiftBits = 6;
        public const int MoveReducingBits = 2;
    }
}
