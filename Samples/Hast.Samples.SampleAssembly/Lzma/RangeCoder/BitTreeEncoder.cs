namespace Hast.Samples.SampleAssembly.Lzma.RangeCoder
{
    internal class BitTreeEncoder
    {
        private const int MaxLevelsBits = 8;
        

        private int _levelsBits;

        private BitEncoder[] _models = new BitEncoder[1 << MaxLevelsBits];


        public BitTreeEncoder(int levelsBits)
        {
            _levelsBits = levelsBits;
        }


        public void Init(uint[] probabilityPrices)
        {
            for (var i = 1; i < (1 << _levelsBits); i++)
            {
                _models[i] = new BitEncoder();
                _models[i].Init(probabilityPrices);
            }
        }

        public void Encode(RangeEncoder rangeEncoder, uint symbol)
        {
            uint m = 1;
            var bitIndex = _levelsBits;
            while (bitIndex > 0)
            {
                bitIndex--;
                uint bit = (symbol >> bitIndex) & 1;
                _models[m].Encode(rangeEncoder, bit);
                m = (m << 1) | bit;
            }
        }

        public void ReverseEncode(RangeEncoder rangeEncoder, uint symbol)
        {
            uint m = 1;
            for (var i = 0; i < _levelsBits; i++)
            {
                var bit = symbol & 1;
                _models[m].Encode(rangeEncoder, bit);
                m = (m << 1) | bit;
                symbol >>= 1;
            }
        }

        public uint GetPrice(uint symbol)
        {
            uint price = 0;
            uint m = 1;
            var bitIndex = _levelsBits;
            while (bitIndex > 0)
            {
                bitIndex--;
                var bit = (symbol >> bitIndex) & 1;
                price += _models[m].GetPrice(bit);
                m = (m << 1) + bit;
            }

            return price;
        }

        public uint ReverseGetPrice(uint symbol)
        {
            uint price = 0;
            uint m = 1;
            for (var i = _levelsBits; i > 0; i--)
            {
                uint bit = symbol & 1;
                symbol >>= 1;
                price += _models[m].GetPrice(bit);
                m = (m << 1) | bit;
            }

            return price;
        }


        public static uint ReverseGetPrice(
            BitEncoder[] models, 
            uint startIndex, 
            int levelsBits, 
            uint symbol)
        {
            uint price = 0;
            uint m = 1;
            for (var i = levelsBits; i > 0; i--)
            {
                uint bit = symbol & 1;
                symbol >>= 1;
                price += models[startIndex + m].GetPrice(bit);
                m = (m << 1) | bit;
            }

            return price;
        }

        public static void ReverseEncode(
            BitEncoder[] models,
            uint startIndex,
            RangeEncoder rangeEncoder,
            int levelsBits,
            uint symbol)
        {
            uint m = 1;
            for (var i = 0; i < levelsBits; i++)
            {
                var bit = symbol & 1;
                models[startIndex + m].Encode(rangeEncoder, bit);
                m = (m << 1) | bit;
                symbol >>= 1;
            }
        }
    }
}
