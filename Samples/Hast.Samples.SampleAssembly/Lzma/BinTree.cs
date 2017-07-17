using Hast.Samples.SampleAssembly.Models;
using Hast.Samples.SampleAssembly.Lzma.Constants;
using Hast.Samples.SampleAssembly.Lzma.Helpers;

namespace Hast.Samples.SampleAssembly.Lzma
{
    public class BinTree
    {
        private const uint Hash2Size = 1 << 10;
        private const uint Hash3Size = 1 << 16;
        private const uint BT2HashSize = 1 << 16;
        private const uint MaxStartLength = 1;
        private const uint Hash3Offset = Hash2Size;
        private const uint EmptyHashValue = 0;
        private const uint MaxValueForNormalize = ((uint)1 << 31) - 1;


        private uint _cyclicBufferPosition;
        private uint _cyclicBufferSize = 0;
        private uint _maxMatchLength;
        private uint[] _son;
        private uint[] _hash;
        private uint _count = 0xFF;
        private uint _hashMask;
        private uint _hashSizeSum = 0;
        private bool _hashArray = true;
        private uint _hashDirectBytes = 0;
        private uint _minMatchCheck = 4;
        private uint _fixHashSize = Hash2Size + Hash3Size;
        private CRC _crc;

        #region LZ Input Window fields

        private SimpleMemoryStream _stream;
        private uint _positionLimit; // Offset (from _buffer) of first byte when new block reading must be done.
        private bool _streamEndWasReached; // If (true) then _streamPosition shows real end of stream.
        private uint _keepSizeBefore; // How many bytess must be kept in buffer before _position.
        private uint _keepSizeAfter; // How many bytess must be kept in buffer after _position.
        private uint _pointerToLastSafePosition;
        private byte[] _bufferBase; // Pointer to buffer with data.
        private uint _blockSize; // Size of Allocated memory block.
        private uint _position; // Offset (from _buffer) of curent byte.
        private uint _streamPosition; // Offset (from _buffer) of first not read byte from Stream.
        private uint _bufferOffset;

        #endregion

        public void SetType(uint hashBytes)
        {
            _hashArray = (hashBytes > 2);
            if (_hashArray)
            {
                _hashDirectBytes = 0;
                _minMatchCheck = 4;
                _fixHashSize = Hash2Size + Hash3Size;
            }
            else
            {
                _hashDirectBytes = 2;
                _minMatchCheck = 2 + 1;
                _fixHashSize = 0;
            }
        }

        public void SetStream(SimpleMemoryStream stream) =>
            _stream = stream;

        public void ReleaseStream() =>
            _stream = null;

        public void Init()
        {
            InitLzInputWindow();

            _hash = new uint[BaseConstants.MaxHashSize];
            _son = new uint[(BaseConstants.MaxDictionarySize + 1) * 2];

            _crc = new CRC();

            for (var i = 0; i < _hashSizeSum; i++) _hash[i] = EmptyHashValue;

            _cyclicBufferPosition = 0;

            ReduceOffsetsLzInputWindow(-1);
        }

        public void MovePosition()
        {
            if (++_cyclicBufferPosition >= _cyclicBufferSize) _cyclicBufferPosition = 0;

            MovePositionLzInputWindow();

            if (_position == MaxValueForNormalize) Normalize();
        }

        public byte GetIndexByte(int index) =>
            _bufferBase[_bufferOffset + _position + (uint)index];

        public uint GetMatchLength(int index, uint distance, uint limit)
        {
            // index + limit have not to exceed _keepSizeAfter.
            if (_streamEndWasReached)
            {
                if ((_position + index) + limit > _streamPosition) limit = _streamPosition - (uint)(_position + index);
            }

            distance++;

            var pby = _bufferOffset + _position + (uint)index;

            uint i;
            for (i = 0; i < limit && _bufferBase[pby + i] == _bufferBase[pby + i - distance]; i++) ;
            return i;
        }

        public uint GetAvailableBytesCount() =>
            _streamPosition - _position;

        public void Create(
            uint dictionarySize,
            uint keepAddBufferBefore,
            uint maxMatchLength,
            uint keepAddBufferAfter)
        {
            _count = 16 + (maxMatchLength >> 1);

            var windowReservSize = (dictionarySize + keepAddBufferBefore + maxMatchLength + keepAddBufferAfter) / 2 + 256;

            CreateLzInputWindow(dictionarySize + keepAddBufferBefore, maxMatchLength + keepAddBufferAfter, windowReservSize);

            _maxMatchLength = maxMatchLength;

            // Dictionary size can be maximum 128 bytes.
            _cyclicBufferSize = dictionarySize + 1;

            _hashSizeSum = BT2HashSize;
            if (_hashArray)
            {
                _hashSizeSum = dictionarySize - 1;
                _hashSizeSum |= (_hashSizeSum >> 1);
                _hashSizeSum |= (_hashSizeSum >> 2);
                _hashSizeSum |= (_hashSizeSum >> 4);
                _hashSizeSum |= (_hashSizeSum >> 8);
                _hashSizeSum >>= 1;
                _hashSizeSum |= 0xFFFF;

                if (_hashSizeSum > (1 << 24)) _hashSizeSum >>= 1;

                _hashMask = _hashSizeSum;
                _hashSizeSum++;
                _hashSizeSum += _fixHashSize;
            }
        }

        public uint GetMatches(uint[] distances)
        {
            uint lenLimit;
            if (_position + _maxMatchLength <= _streamPosition) lenLimit = _maxMatchLength;
            else
            {
                lenLimit = _streamPosition - _position;
                if (lenLimit < _minMatchCheck)
                {
                    MovePosition();

                    return 0;
                }
            }

            uint offset = 0;
            var minMatchPosition = (_position > _cyclicBufferSize) ? (_position - _cyclicBufferSize) : 0;
            var current = _bufferOffset + _position;
            var maxLength = MaxStartLength; // To avoid items for length < hashSize.
            uint hashValue, hash2Value = 0, hash3Value = 0;

            if (_hashArray)
            {
                uint temp = _crc.Table[_bufferBase[current]] ^ _bufferBase[current + 1];
                hash2Value = temp & (Hash2Size - 1);
                temp ^= ((uint)(_bufferBase[current + 2]) << 8);
                hash3Value = temp & (Hash3Size - 1);
                hashValue = (temp ^ (_crc.Table[_bufferBase[current + 3]] << 5)) & _hashMask;
            }
            else hashValue = _bufferBase[current] ^ ((uint)(_bufferBase[current + 1]) << 8);

            var currentMatch = _hash[_fixHashSize + hashValue];

            if (_hashArray)
            {
                var currentMatch2 = _hash[hash2Value];
                var currentMatch3 = _hash[Hash3Offset + hash3Value];
                _hash[hash2Value] = _position;
                _hash[Hash3Offset + hash3Value] = _position;

                if (currentMatch2 > minMatchPosition)
                {
                    if (_bufferBase[_bufferOffset + currentMatch2] == _bufferBase[current])
                    {
                        distances[offset++] = maxLength = 2;
                        distances[offset++] = _position - currentMatch2 - 1;
                    }
                }

                if (currentMatch3 > minMatchPosition)
                {
                    if (_bufferBase[_bufferOffset + currentMatch3] == _bufferBase[current])
                    {
                        if (currentMatch3 == currentMatch2) offset -= 2;

                        distances[offset++] = maxLength = 3;
                        distances[offset++] = _position - currentMatch3 - 1;
                        currentMatch2 = currentMatch3;
                    }
                }

                if (offset != 0 && currentMatch2 == currentMatch)
                {
                    offset -= 2;
                    maxLength = MaxStartLength;
                }
            }

            _hash[_fixHashSize + hashValue] = _position;

            var pointer0 = (_cyclicBufferPosition << 1) + 1;
            var pointer1 = (_cyclicBufferPosition << 1);
            uint length0, length1;
            length0 = length1 = _hashDirectBytes;

            if (_hashDirectBytes != 0)
            {
                if (currentMatch > minMatchPosition)
                {
                    if (_bufferBase[_bufferOffset + currentMatch + _hashDirectBytes] !=
                            _bufferBase[current + _hashDirectBytes])
                    {
                        distances[offset++] = maxLength = _hashDirectBytes;
                        distances[offset++] = _position - currentMatch - 1;
                    }
                }
            }

            var count = _count;

            while (true)
            {
                if (currentMatch <= minMatchPosition || count == 0)
                {
                    _son[pointer0] = _son[pointer1] = EmptyHashValue;

                    count--;

                    break;
                }

                var delta = _position - currentMatch;
                var cyclicPosition = ((delta <= _cyclicBufferPosition) ?
                    (_cyclicBufferPosition - delta) :
                    (_cyclicBufferPosition - delta + _cyclicBufferSize)) << 1;

                var pby1 = _bufferOffset + currentMatch;
                var len = LzmaHelpers.GetMinValue(length0, length1);
                if (_bufferBase[pby1 + len] == _bufferBase[current + len])
                {
                    while (++len != lenLimit)
                    {
                        if (_bufferBase[pby1 + len] != _bufferBase[current + len]) break;
                    }

                    if (maxLength < len)
                    {
                        distances[offset++] = maxLength = len;
                        distances[offset++] = delta - 1;
                        if (len == lenLimit)
                        {
                            _son[pointer1] = _son[cyclicPosition];
                            _son[pointer0] = _son[cyclicPosition + 1];

                            break;
                        }
                    }
                }

                if (_bufferBase[pby1 + len] < _bufferBase[current + len])
                {
                    _son[pointer1] = currentMatch;
                    pointer1 = cyclicPosition + 1;
                    currentMatch = _son[pointer1];
                    length1 = len;
                }
                else
                {
                    _son[pointer0] = currentMatch;
                    pointer0 = cyclicPosition;
                    currentMatch = _son[pointer0];
                    length0 = len;
                }

                count--;
            }

            MovePosition();

            return offset;
        }

        public void Skip(uint skipCount)
        {
            do
            {
                var continueLoop = false;
                uint lenLimit;
                if (_position + _maxMatchLength <= _streamPosition) lenLimit = _maxMatchLength;
                else
                {
                    lenLimit = _streamPosition - _position;
                    if (lenLimit < _minMatchCheck)
                    {
                        MovePosition();

                        continueLoop = true;
                    }
                }

                if (!continueLoop)
                {
                    var minMatchPosition = (_position > _cyclicBufferSize) ? (_position - _cyclicBufferSize) : 0;
                    var current = _bufferOffset + _position;

                    uint hashValue;

                    if (_hashArray)
                    {
                        var temp = _crc.Table[_bufferBase[current]] ^ _bufferBase[current + 1];
                        var hash2Value = temp & (Hash2Size - 1);
                        _hash[hash2Value] = _position;
                        temp ^= ((uint)(_bufferBase[current + 2]) << 8);
                        var hash3Value = temp & (Hash3Size - 1);
                        _hash[Hash3Offset + hash3Value] = _position;
                        hashValue = (temp ^ (_crc.Table[_bufferBase[current + 3]] << 5)) & _hashMask;
                    }
                    else hashValue = _bufferBase[current] ^ ((uint)(_bufferBase[current + 1]) << 8);

                    var currentMatch = _hash[_fixHashSize + hashValue];
                    _hash[_fixHashSize + hashValue] = _position;

                    var ptr0 = (_cyclicBufferPosition << 1) + 1;
                    var ptr1 = (_cyclicBufferPosition << 1);

                    uint len0, len1;
                    len0 = len1 = _hashDirectBytes;

                    var count = _count;
                    while (true)
                    {
                        if (currentMatch <= minMatchPosition || count == 0)
                        {
                            _son[ptr0] = _son[ptr1] = EmptyHashValue;

                            break;
                        }

                        var delta = _position - currentMatch;
                        var cyclicPosition = ((delta <= _cyclicBufferPosition) ?
                            (_cyclicBufferPosition - delta) :
                            (_cyclicBufferPosition - delta + _cyclicBufferSize)) << 1;

                        var pby1 = _bufferOffset + currentMatch;
                        var len = LzmaHelpers.GetMinValue(len0, len1);
                        if (_bufferBase[pby1 + len] == _bufferBase[current + len])
                        {
                            while (++len != lenLimit)
                            {
                                if (_bufferBase[pby1 + len] != _bufferBase[current + len]) break;
                            }

                            if (len == lenLimit)
                            {
                                _son[ptr1] = _son[cyclicPosition];
                                _son[ptr0] = _son[cyclicPosition + 1];

                                break;
                            }
                        }

                        if (_bufferBase[pby1 + len] < _bufferBase[current + len])
                        {
                            _son[ptr1] = currentMatch;
                            ptr1 = cyclicPosition + 1;
                            currentMatch = _son[ptr1];
                            len1 = len;
                        }
                        else
                        {
                            _son[ptr0] = currentMatch;
                            ptr0 = cyclicPosition;
                            currentMatch = _son[ptr0];
                            len0 = len;
                        }
                    }

                    count--;

                    MovePosition();
                }
            }
            while (--skipCount != 0);
        }

        public void SetCutValue(uint cutValue) =>
            _count = cutValue;


        private void NormalizeSon(uint subValue)
        {
            for (uint i = 0; i < _cyclicBufferSize * 2; i++)
            {
                var value = _son[i];
                if (value <= subValue) value = EmptyHashValue;
                else value -= subValue;
                _son[i] = value;
            }
        }

        private void NormalizeHash(uint subValue)
        {
            for (uint i = 0; i < _hashSizeSum; i++)
            {
                var value = _hash[i];

                if (value <= subValue) value = EmptyHashValue;
                else value -= subValue;

                _hash[i] = value;
            }
        }

        private void Normalize()
        {
            var subValue = _position - _cyclicBufferSize;

            NormalizeSon(subValue);
            NormalizeHash(subValue);

            ReduceOffsetsLzInputWindow((int)subValue);
        }

        #region LZ Input Window Methods

        private void CreateLzInputWindow(uint keepSizeBefore, uint keepSizeAfter, uint keepSizeReserv)
        {
            _keepSizeBefore = keepSizeBefore;
            _keepSizeAfter = keepSizeAfter;

            var blockSize = keepSizeBefore + keepSizeAfter + keepSizeReserv;

            // Suppose that the _bufferBase has already been initialized (not null).
            _blockSize = blockSize;

            _pointerToLastSafePosition = _blockSize - keepSizeAfter;
        }

        private void InitLzInputWindow()
        {
            _bufferBase = new byte[BaseConstants.MaxBlockLength];

            _bufferOffset = 0;
            _position = 0;
            _streamPosition = 0;
            _streamEndWasReached = false;

            ReadBlockLzInputWindow();
        }

        private void ReadBlockLzInputWindow()
        {
            if (!_streamEndWasReached)
            {
                while (true)
                {
                    var size = (int)((0 - _bufferOffset) + _blockSize - _streamPosition);
                    if (size == 0) break;
                    var bytesRead = _stream.Read(_bufferBase, (int)(_bufferOffset + _streamPosition), size);
                    if (bytesRead == 0)
                    {
                        _positionLimit = _streamPosition;
                        var pointerToPostion = _bufferOffset + _positionLimit;
                        if (pointerToPostion > _pointerToLastSafePosition)
                            _positionLimit = _pointerToLastSafePosition - _bufferOffset;

                        _streamEndWasReached = true;

                        break;
                    }

                    _streamPosition += (uint)bytesRead;

                    if (_streamPosition >= _position + _keepSizeAfter)
                        _positionLimit = _streamPosition - _keepSizeAfter;
                    
                }
            }
        }

        private void ReduceOffsetsLzInputWindow(int subValue)
        {
            _bufferOffset += (uint)subValue;
            _positionLimit -= (uint)subValue;
            _position -= (uint)subValue;
            _streamPosition -= (uint)subValue;
        }

        private void MovePositionLzInputWindow()
        {
            _position++;
            if (_position > _positionLimit)
            {
                var pointerToPostion = _bufferOffset + _position;

                if (pointerToPostion > _pointerToLastSafePosition) MoveBlockLzInputWindow();

                ReadBlockLzInputWindow();
            }
        }

        private void MoveBlockLzInputWindow()
        {
            var offset = _bufferOffset + _position - _keepSizeBefore;

            // We need one additional byte, since MovePosition moves on 1 byte.
            if (offset > 0) offset--;

            var bytesCount = _bufferOffset + _streamPosition - offset;

            for (uint i = 0; i < bytesCount; i++) _bufferBase[i] = _bufferBase[offset + i];

            _bufferOffset -= offset;
        }

        #endregion
    }
}
