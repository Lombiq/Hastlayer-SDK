using Hast.Samples.SampleAssembly.Lzma.Constants;

namespace Hast.Samples.SampleAssembly.Lzma.RangeCoder
{
    internal class BitEncoder
    {
        private const int MoveBits = 5;


        private uint[] _probabilityPrices;
        private uint _probability;


        public void Init(uint[] probabilityPrices)
        {
            _probabilityPrices = probabilityPrices;
            _probability = RangeEncoderConstants.BitModelTotal >> 1;
        }

        public void UpdateModel(uint symbol)
        {
            if (symbol == 0) _probability += (RangeEncoderConstants.BitModelTotal - _probability) >> MoveBits;
            else _probability -= (_probability) >> MoveBits;
        }

        public void Encode(RangeEncoder encoder, uint symbol)
        {
            var newBound = (encoder.Range >> RangeEncoderConstants.BitModelTotalBits) * _probability;

            UpdateModel(symbol);

            if (symbol == 0) encoder.Range = newBound;
            else
            {
                encoder.Low += newBound;
                encoder.Range -= newBound;
            }

            if (encoder.Range < RangeEncoderConstants.TopValue)
            {
                encoder.Range <<= 8;
                encoder.ShiftLow();
            }
        }

        public uint GetPrice(uint symbol) =>
            _probabilityPrices[(int)((((_probability - symbol) ^ (-1 * ((int)symbol))) &
                (RangeEncoderConstants.BitModelTotal - 1)) >>
                RangeEncoderConstants.MoveReducingBits)];

        public uint GetPrice0() =>
            _probabilityPrices[_probability >> RangeEncoderConstants.MoveReducingBits];

        public uint GetPrice1() =>
            _probabilityPrices[(int)(RangeEncoderConstants.BitModelTotal - _probability) >> 
                RangeEncoderConstants.MoveReducingBits];
    }
}
