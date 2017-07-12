namespace Hast.Samples.SampleAssembly.Lzma
{
	public class CRC
	{
        private readonly uint[] _table;
        private uint _value = 0xFFFFFFFF;


        public uint[] Table => _table;
        

        public CRC()
        {
            _table = new uint[256];

            const uint kPoly = 0xEDB88320;
            for (uint i = 0; i < 256; i++)
            {
                var r = i;
                for (var j = 0; j < 8; j++)
                    if ((r & 1) != 0) r = (r >> 1) ^ kPoly;
                    else r >>= 1;
                _table[i] = r;
            }
        }


		public void Init() => 
            _value = 0xFFFFFFFF;

		public void Updatebyte(byte b) =>
            _value = _table[(((byte)(_value)) ^ b)] ^ (_value >> 8);

		public void Update(byte[] data, uint offset, uint size)
		{
            for (uint i = 0; i < size; i++)
            {
                _value = _table[(((byte)(_value)) ^ data[offset + i])] ^ (_value >> 8);
            }
		}

		public uint GetDigest() => 
            _value ^ 0xFFFFFFFF;


        private static uint CalculateDigest(byte[] data, uint offset, uint size)
		{
			var crc = new CRC();

			crc.Update(data, offset, size);

			return crc.GetDigest();
		}

		private static bool VerifyDigest(uint digest, byte[] data, uint offset, uint size) =>
            CalculateDigest(data, offset, size) == digest;
	}
}
