namespace Hast.Samples.SampleAssembly.Lzma.Models
{
    public class EncoderProperties
    {
        public int LiteralContextBits { get; set; }
        public uint DictionarySize { get; set; }
        public int PositionStateBits { get; set; }
        public int LiteralPositionBits { get; set; }
        public int Algorithm { get; set; }
        public uint NumberOfFastBytes { get; set; }
        public bool WriteEndMarker { get; set; }
        public uint NumberOfMatchFinderHashBytes { get; set; }
    }
}
