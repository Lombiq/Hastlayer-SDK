namespace Hast.Samples.SampleAssembly.Lzma.Constants
{
	internal static class BaseConstants
	{
		public const uint RepeatDistances = 4;
		public const uint States = 12;
        public const int PositionSlotBits = 6;
        public const int LengthToPositionStatesBits = 2;
        public const uint LengthToPositionStates = 1 << LengthToPositionStatesBits;
        public const uint MinMatchLength = 2;
        public const int AlignBits = 4;
        public const uint AlignTableSize = 1 << AlignBits;
        public const uint AlignMask = (AlignTableSize - 1);
        public const uint StartPositionModelIndex = 4;
        public const uint EndPositionModelIndex = 14;
        public const uint PositionModels = EndPositionModelIndex - StartPositionModelIndex;
        public const uint FullDistances = 1 << ((int)EndPositionModelIndex / 2);
        public const uint MaxLiteralPositionStatesBitsEncoding = 4;
        public const uint MaxLiteralContextBits = 8;
        public const int MaxPositionStatesBits = 4;
        public const uint MaxPositionStates = (1 << MaxPositionStatesBits);
        public const int MaxEncodingPositionStatesBits = 4;
        public const uint MaxEncodingPositionStates = (1 << MaxEncodingPositionStatesBits);
        public const int LowLengthBits = 3;
        public const int MidLengthBits = 3;
        public const int HighLengthBits = 8;
        public const uint LowLength = 1 << LowLengthBits;
        public const uint MidLength = 1 << MidLengthBits;
        public const uint SymbolLength = LowLength + MidLength + (1 << HighLengthBits);
        public const uint MaxMatchLength = MinMatchLength + SymbolLength - 1;
        public const uint OptimumNumber = 1 << 12;
        public const uint MaxDictionarySize = 1 << 7;
        public const uint MaxHashSize = (1 << 17) + (1 << 10);
        public const uint MaxNumberOfFastBytes = 1 << 7;
        private const uint MaxBlockLengthHelper = MaxDictionarySize + OptimumNumber + MaxNumberOfFastBytes + MaxMatchLength + 1;
        public const uint MaxBlockLength = MaxBlockLengthHelper + (MaxBlockLengthHelper / 2 + 256);


        public static uint GetLengthToPositionState(uint length)
		{
			length -= MinMatchLength;

			return length < LengthToPositionStates ? length : LengthToPositionStates - 1;
        }
    }
}
