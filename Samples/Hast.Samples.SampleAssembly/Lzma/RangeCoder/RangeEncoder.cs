using Hast.Samples.SampleAssembly.Models;
using Hast.Samples.SampleAssembly.Lzma.Constants;

namespace Hast.Samples.SampleAssembly.Lzma.RangeCoder
{
    internal class RangeEncoder
    {
        private SimpleMemoryStream _stream;
        private uint _cacheSize;
        private byte _cache;
        private long _startPosition;


        public ulong Low { get; set; }
        public uint Range { get; set; }


        public void SetStream(SimpleMemoryStream stream) =>
            _stream = stream;

        public void ReleaseStream() =>
            _stream = null;

        public void Init()
        {
            _startPosition = _stream.Position;

            Low = 0;
            Range = 0xFFFFFFFF;

            _cacheSize = 1;
            _cache = 0;
        }

        public void FlushData()
        {
            for (var i = 0; i < 5; i++) ShiftLow();
        }

        public void Encode(uint start, uint size, uint total)
        {
            Low += start * (Range /= total);
            Range *= size;

            while (Range < RangeEncoderConstants.TopValue)
            {
                Range <<= 8;

                ShiftLow();
            }
        }

        public void ShiftLow()
        {
            if ((uint)Low < 0xFF000000 || (uint)(Low >> 32) == 1)
            {
                var temp = _cache;
                do
                {
                    _stream.WriteByte((byte)(temp + (Low >> 32)));
                    temp = 0xFF;
                }
                while (--_cacheSize != 0);

                _cache = (byte)(((uint)Low) >> 24);
            }

            _cacheSize++;
            Low = ((uint)Low) << 8;
        }

        public void EncodeDirectBits(uint v, int totalBits)
        {
            for (var i = totalBits - 1; i >= 0; i--)
            {
                Range >>= 1;

                if (((v >> i) & 1) == 1) Low += Range;

                if (Range < RangeEncoderConstants.TopValue)
                {
                    Range <<= 8;

                    ShiftLow();
                }
            }
        }

        public void EncodeBit(uint size0, int totalBits, uint symbol)
        {
            var newBound = (Range >> totalBits) * size0;
            if (symbol == 0) Range = newBound;
            else
            {
                Low += newBound;
                Range -= newBound;
            }

            while (Range < RangeEncoderConstants.TopValue)
            {
                Range <<= 8;

                ShiftLow();
            }
        }

        public long GetProcessedSizeAdd() =>
            _cacheSize + _stream.Position - _startPosition + 4;
    }
}
