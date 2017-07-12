using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Samples.SampleAssembly.Models
{
    public class SimpleMemoryStream
    {
        private readonly SimpleMemory _simpleMemory;
        private readonly int _startCellIndex;
        private readonly int _endCellIndex;
        private readonly int _cellCount;
        private readonly long _byteCount;
        private byte[] _4bytesBuffer;
        private bool _overflow;
        private int _cellIndex;
        private byte _byteIndexInCell;
        private byte _lastByteIndexInCell;


        public long Position => (_cellIndex - _startCellIndex) * 4 + _byteIndexInCell;
        public long Length => _byteCount;
        public bool Overflow => _overflow;
        public int CellCount => _cellCount;


        public SimpleMemoryStream(SimpleMemory simpleMemory, int startCellIndex, long byteCount)
        {
            _simpleMemory = simpleMemory;
            _startCellIndex = startCellIndex;
            _byteCount = byteCount;
            var byteCountModulo4 = (byte)(byteCount % 4);
            _lastByteIndexInCell = (byte)(byteCountModulo4 < 0 ? 3 : (byteCountModulo4 - 1));
            _cellCount = (int)(byteCount / 4) + (byteCountModulo4 != 0 ? 1 : 0);

            _cellIndex = startCellIndex;
            _endCellIndex = _cellIndex + _cellCount - 1;

            _4bytesBuffer = new byte[4];
        }



        public void Write(byte[] buffer, int offset, int count)
        {
            if (!_overflow)
            {
                _4bytesBuffer = _simpleMemory.Read4Bytes(_cellIndex);
                for (var i = offset; i < offset + count && !_overflow; i++)
                {
                    _4bytesBuffer[_byteIndexInCell] = buffer[i];

                    if (_byteIndexInCell == 3) _simpleMemory.Write4Bytes(_cellIndex, _4bytesBuffer);

                    IncreasePosition();

                    if (_byteIndexInCell == 0 && !_overflow) _4bytesBuffer = _simpleMemory.Read4Bytes(_cellIndex);
                }

                if (_byteIndexInCell != 0) _simpleMemory.Write4Bytes(_cellIndex, _4bytesBuffer);
            }
        }

        public void WriteByte(byte byteToWrite)
        {
            if (!_overflow)
            {
                _4bytesBuffer = _simpleMemory.Read4Bytes(_cellIndex);
                _4bytesBuffer[_byteIndexInCell] = byteToWrite;
                _simpleMemory.Write4Bytes(_cellIndex, _4bytesBuffer);

                IncreasePosition();
            }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            var bytesRead = 0;
            if (_overflow) return bytesRead;

            _4bytesBuffer = _simpleMemory.Read4Bytes(_cellIndex);
            for (var i = offset; i < offset + count && !_overflow; i++)
            {
                buffer[i] = _4bytesBuffer[_byteIndexInCell];

                bytesRead++;

                IncreasePosition();

                if (_byteIndexInCell == 0 && !_overflow) _4bytesBuffer = _simpleMemory.Read4Bytes(_cellIndex);
            }

            return bytesRead;
        }

        public void Close() { }

        public void Flush() { }

        public void ResetPosition()
        {
            _cellIndex = _startCellIndex;
            _byteIndexInCell = 0;
            _overflow = false;
        }


        private void IncreasePosition()
        {
            if (_byteIndexInCell == 3)
            {
                _byteIndexInCell = 0;
                _cellIndex++;
            }
            else
            {
                _byteIndexInCell++;
            }

            if (_cellIndex > _endCellIndex ||
                _cellIndex == _endCellIndex &&
                _byteIndexInCell > _lastByteIndexInCell)
            {
                _overflow = true;
            }
        }
    }
}
