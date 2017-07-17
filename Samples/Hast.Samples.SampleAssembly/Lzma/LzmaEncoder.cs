using Hast.Samples.SampleAssembly.Lzma.Constants;
using Hast.Samples.SampleAssembly.Lzma.Helpers;
using Hast.Samples.SampleAssembly.Lzma.Models;
using Hast.Samples.SampleAssembly.Lzma.RangeCoder;
using Hast.Samples.SampleAssembly.Models;

namespace Hast.Samples.SampleAssembly.Lzma
{
    public class LzmaEncoder
    {
        private const uint InfinityPrice = 0xFFFFFFF;
        private const int DefaultDictionaryLogSize = 22;
        private const uint DefaultFastBytesCount = 0x20;
        private const uint SpecialSymbolLengthCount = BaseConstants.LowLength + BaseConstants.MidLength;
        private const int PropertySize = 5;


        private CoderState _coderState = new CoderState();
        private byte _previousByte;
        private uint[] _repeatDistances = new uint[BaseConstants.RepeatDistances];
        private Optimal[] _optimum = new Optimal[BaseConstants.OptimumNumber];
        private BinTree _matchFinder = null;
        private RangeEncoder _rangeEncoder = new RangeEncoder();
        private BitEncoder[] _isMatch = new BitEncoder[BaseConstants.States << BaseConstants.MaxPositionStatesBits];
        private BitEncoder[] _isRepeat = new BitEncoder[BaseConstants.States];
        private BitEncoder[] _isRepeatG0 = new BitEncoder[BaseConstants.States];
        private BitEncoder[] _isRepeatG1 = new BitEncoder[BaseConstants.States];
        private BitEncoder[] _isRepeatG2 = new BitEncoder[BaseConstants.States];
        private BitEncoder[] _isRepeat0Long = new BitEncoder[BaseConstants.States << BaseConstants.MaxPositionStatesBits];
        private BitTreeEncoder[] _slotEncoderPosition = new BitTreeEncoder[BaseConstants.LengthToPositionStates];
        private BitEncoder[] _encodersPosition = new BitEncoder[BaseConstants.FullDistances - BaseConstants.EndPositionModelIndex];
        private BitTreeEncoder _alignEncoderPosition = new BitTreeEncoder(BaseConstants.AlignBits);
        private LengthPriceTableEncoder _lengthEncoder = new LengthPriceTableEncoder();
        private LengthPriceTableEncoder _repeatedMatchLengthEncoder = new LengthPriceTableEncoder();
        private LiteralEncoder _literalEncoder = new LiteralEncoder();
        private uint[] _matchDistances = new uint[BaseConstants.MaxMatchLength * 2 + 2];
        private uint _fastBytesCount = DefaultFastBytesCount;
        private uint _longestMatchLength;
        private uint _distancePairsCount;
        private uint _additionalOffset;
        private uint _optimumEndIndex;
        private uint _optimumCurrentIndex;
        private bool _longestMatchWasFound;
        private uint[] _slotPricesPosition = new uint[1 << (BaseConstants.PositionSlotBits + BaseConstants.LengthToPositionStatesBits)];
        private uint[] _distancesPrices = new uint[BaseConstants.FullDistances << BaseConstants.LengthToPositionStatesBits];
        private uint[] _alignPrices = new uint[BaseConstants.AlignTableSize];
        private uint _alignPriceCount;
        private uint _distanceTableSize = (DefaultDictionaryLogSize * 2);
        private int _positionStateBits = 2;
        private uint _positionStateMask = (4 - 1);
        private int _literalPositionBits = 0;
        private int _literalContextBits = 3;
        private uint _dictionarySize = (1 << DefaultDictionaryLogSize);
        private uint _dictionarySizePrevious = 0xFFFFFFFF;
        private uint _fastBytesCountPrevious = 0xFFFFFFFF;
        private long _currentPosition;
        private bool _finished;
        private SimpleMemoryStream _inputStream;
        private uint _matchFinderHashBytesCount = 4;
        private bool _writeEndMarker = false;
        private bool _needReleaseMatchFinderStream;
        private byte[] _properties = new byte[PropertySize];
        private uint[] _tempPrices = new uint[BaseConstants.FullDistances];
        private uint _matchPriceCount;
        private uint _trainSize = 0;
        private uint[] _repeats = new uint[BaseConstants.RepeatDistances];
        private uint[] _repeatLengths = new uint[BaseConstants.RepeatDistances];
        private byte[] _fastPosition = new byte[1 << 11];


        public LzmaEncoder()
        {
            const byte FastSlots = 22;

            var c = 2;
            _fastPosition[0] = 0;
            _fastPosition[1] = 1;

            for (byte slotFast = 2; slotFast < FastSlots; slotFast++)
            {
                var k = ((uint)1 << ((slotFast >> 1) - 1));

                for (uint j = 0; j < k; j++, c++)
                    _fastPosition[c] = slotFast;
            }

            for (int i = 0; i < BaseConstants.OptimumNumber; i++)
                _optimum[i] = new Optimal();

            for (int i = 0; i < BaseConstants.LengthToPositionStates; i++)
                _slotEncoderPosition[i] = new BitTreeEncoder(BaseConstants.PositionSlotBits);
        }


        public bool CodeOneBlock()
        {
            long inputSize = 0;
            long outputSize = 0;
            var finished = true;

            if (_inputStream != null)
            {
                _matchFinder.SetStream(_inputStream);
                _matchFinder.Init();

                _needReleaseMatchFinderStream = true;
                _inputStream = null;

                if (_trainSize > 0) _matchFinder.Skip(_trainSize);
            }

            var runCoding = true;
            if (!_finished)
            {
                _finished = true;

                var previousPosition = _currentPosition;
                if (_currentPosition == 0)
                {
                    if (_matchFinder.GetAvailableBytesCount() == 0)
                    {
                        Flush((uint)_currentPosition);

                        runCoding = false;
                    }
                    else
                    {
                        ReadMatchDistances();

                        var posState = (uint)(_currentPosition) & _positionStateMask;
                        _isMatch[(_coderState.Index << BaseConstants.MaxPositionStatesBits) + posState].Encode(_rangeEncoder, 0);
                        _coderState.UpdateChar();
                        byte curbyte = _matchFinder.GetIndexByte((int)(0 - _additionalOffset));
                        _literalEncoder.GetSubCoder((uint)(_currentPosition), _previousByte).Encode(_rangeEncoder, curbyte);
                        _previousByte = curbyte;
                        _additionalOffset--;
                        _currentPosition++;
                    }
                }
                if (runCoding)
                {
                    if (_matchFinder.GetAvailableBytesCount() == 0) Flush((uint)_currentPosition);
                    else
                    {
                        while (runCoding)
                        {
                            var returnValues = GetOptimum((uint)_currentPosition);

                            var position = returnValues.OutValue;
                            var length = returnValues.ReturnValue;

                            var positionState = ((uint)_currentPosition) & _positionStateMask;
                            var complexState = (_coderState.Index << BaseConstants.MaxPositionStatesBits) + positionState;

                            if (length == 1 && position == 0xFFFFFFFF)
                            {
                                _isMatch[complexState].Encode(_rangeEncoder, 0);
                                byte currentByte = _matchFinder.GetIndexByte((int)(0 - _additionalOffset));

                                var subCoder = _literalEncoder.GetSubCoder((uint)_currentPosition, _previousByte);
                                if (!_coderState.IsCharState())
                                {
                                    var matchByte = _matchFinder.GetIndexByte((int)(0 - _repeatDistances[0] - 1 - _additionalOffset));
                                    subCoder.EncodeMatched(_rangeEncoder, matchByte, currentByte);
                                }
                                else subCoder.Encode(_rangeEncoder, currentByte);

                                _previousByte = currentByte;
                                _coderState.UpdateChar();
                            }
                            else
                            {
                                _isMatch[complexState].Encode(_rangeEncoder, 1);
                                if (position < BaseConstants.RepeatDistances)
                                {
                                    _isRepeat[_coderState.Index].Encode(_rangeEncoder, 1);

                                    if (position == 0)
                                    {
                                        _isRepeatG0[_coderState.Index].Encode(_rangeEncoder, 0);

                                        if (length == 1) _isRepeat0Long[complexState].Encode(_rangeEncoder, 0);
                                        else _isRepeat0Long[complexState].Encode(_rangeEncoder, 1);
                                    }
                                    else
                                    {
                                        _isRepeatG0[_coderState.Index].Encode(_rangeEncoder, 1);

                                        if (position == 1) _isRepeatG1[_coderState.Index].Encode(_rangeEncoder, 0);
                                        else
                                        {
                                            _isRepeatG1[_coderState.Index].Encode(_rangeEncoder, 1);
                                            _isRepeatG2[_coderState.Index].Encode(_rangeEncoder, position - 2);
                                        }
                                    }

                                    if (length == 1) _coderState.UpdateShortRepeat();
                                    else
                                    {
                                        _repeatedMatchLengthEncoder.Encode(_rangeEncoder, length - BaseConstants.MinMatchLength, positionState);
                                        _coderState.UpdateRepeat();
                                    }

                                    var distance = _repeatDistances[position];

                                    if (position != 0)
                                    {
                                        for (var i = position; i >= 1; i--)
                                            _repeatDistances[i] = _repeatDistances[i - 1];

                                        _repeatDistances[0] = distance;
                                    }
                                }
                                else
                                {
                                    _isRepeat[_coderState.Index].Encode(_rangeEncoder, 0);
                                    _coderState.UpdateMatch();
                                    _lengthEncoder.Encode(_rangeEncoder, length - BaseConstants.MinMatchLength, positionState);
                                    position -= BaseConstants.RepeatDistances;

                                    var positionSlot = GetPositionSlot(position);
                                    var lengthToPositionState = BaseConstants.GetLengthToPositionState(length);

                                    _slotEncoderPosition[lengthToPositionState].Encode(_rangeEncoder, positionSlot);

                                    if (positionSlot >= BaseConstants.StartPositionModelIndex)
                                    {
                                        var footerBits = (int)((positionSlot >> 1) - 1);
                                        var baseValue = ((2 | (positionSlot & 1)) << footerBits);
                                        var reducedPosition = position - baseValue;

                                        if (positionSlot < BaseConstants.EndPositionModelIndex)
                                        {
                                            BitTreeEncoder.ReverseEncode(_encodersPosition,
                                                    baseValue - positionSlot - 1, _rangeEncoder, footerBits, reducedPosition);
                                        }
                                        else
                                        {
                                            _rangeEncoder.EncodeDirectBits(reducedPosition >> BaseConstants.AlignBits, footerBits - BaseConstants.AlignBits);
                                            _alignEncoderPosition.ReverseEncode(_rangeEncoder, reducedPosition & BaseConstants.AlignMask);
                                            _alignPriceCount++;
                                        }
                                    }

                                    var distance = position;

                                    for (var i = BaseConstants.RepeatDistances - 1; i >= 1; i--)
                                        _repeatDistances[i] = _repeatDistances[i - 1];

                                    _repeatDistances[0] = distance;
                                    _matchPriceCount++;
                                }

                                _previousByte = _matchFinder.GetIndexByte((int)(length - 1 - _additionalOffset));
                            }

                            _additionalOffset -= length;
                            _currentPosition += length;

                            if (_additionalOffset == 0)
                            {
                                // Following if statement should not be executed in case of fast mode, however, 
                                // the if statement was commented out in the SDK.
                                if (_matchPriceCount >= (1 << 7)) FillDistancePrices();

                                if (_alignPriceCount >= BaseConstants.AlignTableSize) FillAlignPrices();

                                inputSize = _currentPosition;
                                outputSize = _rangeEncoder.GetProcessedSizeAdd();
                                if (_matchFinder.GetAvailableBytesCount() == 0)
                                {
                                    Flush((uint)_currentPosition);
                                    runCoding = false;
                                }
                                else if (_currentPosition - previousPosition >= (1 << 12))
                                {
                                    _finished = false;
                                    finished = false;
                                    runCoding = false;
                                }
                            }
                        }
                    }
                }
            }

            return finished;
        }

        public void Encode(SimpleMemoryStream inputStream, SimpleMemoryStream outputStream)
        {
            _needReleaseMatchFinderStream = false;

            SetStreams(inputStream, outputStream);

            var finished = false;
            while (!finished)
            {
                finished = CodeOneBlock();
            }
        }

        public void SetCoderProperties(EncoderProperties properties)
        {
            const int DictionaryLogSizeMaxCompress = 30;

            _fastBytesCount = properties.NumberOfFastBytes;

            _matchFinderHashBytesCount = properties.NumberOfMatchFinderHashBytes;
            _dictionarySizePrevious = 0xFFFFFFFF;
            _matchFinder = null;

            _dictionarySize = properties.DictionarySize;
            int dictionaryLogSize;
            for (dictionaryLogSize = 0; dictionaryLogSize < (uint)DictionaryLogSizeMaxCompress; dictionaryLogSize++)
            {
                if (_dictionarySize <= ((uint)(1) << dictionaryLogSize)) break;
            }
            _distanceTableSize = (uint)dictionaryLogSize * 2;

            _positionStateBits = properties.PositionStateBits;
            _positionStateMask = (((uint)1) << _positionStateBits) - 1;

            _literalPositionBits = properties.LiteralPositionBits;

            _literalContextBits = properties.LiteralContextBits;

            SetWriteEndMarkerMode(properties.WriteEndMarker);

            // Algorithm parameter is not yet processed. The related code lines are commented out
            // in the original LZMA SDK too:
            /*
            int maximize = (int)prop;
            _fastMode = (maximize == 0);
            _maxMode = (maximize >= 2);
            */
        }

        public void SetTrainSize(uint trainSize) =>
            _trainSize = trainSize;

        public void WriteCoderProperties(SimpleMemoryStream outputStream)
        {
            _properties[0] = (byte)((_positionStateBits * 5 + _literalPositionBits) * 9 + _literalContextBits);

            for (int i = 0; i < 4; i++)
                _properties[1 + i] = (byte)((_dictionarySize >> (8 * i)) & 0xFF);

            outputStream.Write(_properties, 0, PropertySize);
        }


        private uint GetPositionSlot(uint position)
        {
            uint positionSlot;

            if (position < (1 << 11)) positionSlot = _fastPosition[position];
            else if (position < (1 << 21)) positionSlot = (uint)(_fastPosition[position >> 10] + 20);
            else positionSlot = (uint)(_fastPosition[position >> 20] + 40);

            return positionSlot;
        }

        private uint GetPositionSlot2(uint pos)
        {
            uint positionSlot;

            if (pos < (1 << 17)) positionSlot = (uint)(_fastPosition[pos >> 6] + 12);
            else if (pos < (1 << 27)) positionSlot = (uint)(_fastPosition[pos >> 16] + 32);
            else positionSlot = (uint)(_fastPosition[pos >> 26] + 52);

            return positionSlot;
        }

        private void BaseInit()
        {
            _coderState.Init();
            _previousByte = 0;

            for (uint i = 0; i < BaseConstants.RepeatDistances; i++)
                _repeatDistances[i] = 0;
        }

        private void Create()
        {
            var binTree = new BinTree();
            binTree.SetType(_matchFinderHashBytesCount);

            _matchFinder = binTree;

            _literalEncoder.Create(_literalPositionBits, _literalContextBits);

            if (_dictionarySize != _dictionarySizePrevious || _fastBytesCountPrevious != _fastBytesCount)
            {
                _matchFinder.Create(_dictionarySize, BaseConstants.OptimumNumber, _fastBytesCount, BaseConstants.MaxMatchLength + 1);
                _dictionarySizePrevious = _dictionarySize;
                _fastBytesCountPrevious = _fastBytesCount;
            }
        }

        private void SetWriteEndMarkerMode(bool writeEndMarker) =>
            _writeEndMarker = writeEndMarker;

        private void Init()
        {
            const int BaseBitCount = (RangeEncoderConstants.BitModelTotalBits - RangeEncoderConstants.MoveReducingBits);

            BaseInit();
            _rangeEncoder.Init();

            var probabilityPrices = new uint[RangeEncoderConstants.BitModelTotal >> RangeEncoderConstants.MoveReducingBits];
            for (int k = BaseBitCount - 1; k >= 0; k--)
            {
                var start = (uint)1 << (BaseBitCount - k - 1);
                var end = (uint)1 << (BaseBitCount - k);
                for (var j = start; j < end; j++)
                {
                    var a = (uint)k << RangeEncoderConstants.BitPriceShiftBits;
                    var b = end - j;
                    var c = b << RangeEncoderConstants.BitPriceShiftBits;
                    var d = BaseBitCount - k - 1;
                    var e = c >> d;
                    var f = a + e;

                    probabilityPrices[j] = f;
                }
            }

            uint i;
            for (i = 0; i < BaseConstants.States; i++)
            {
                for (uint j = 0; j <= _positionStateMask; j++)
                {
                    var complexState = (i << BaseConstants.MaxPositionStatesBits) + j;

                    _isMatch[complexState] = new BitEncoder();
                    _isMatch[complexState].Init(probabilityPrices);
                    _isRepeat0Long[complexState] = new BitEncoder();
                    _isRepeat0Long[complexState].Init(probabilityPrices);
                }

                _isRepeat[i] = new BitEncoder();
                _isRepeat[i].Init(probabilityPrices);
                _isRepeatG0[i] = new BitEncoder();
                _isRepeatG0[i].Init(probabilityPrices);
                _isRepeatG1[i] = new BitEncoder();
                _isRepeatG1[i].Init(probabilityPrices);
                _isRepeatG2[i] = new BitEncoder();
                _isRepeatG2[i].Init(probabilityPrices);
            }

            _literalEncoder.Init(probabilityPrices);

            for (i = 0; i < BaseConstants.LengthToPositionStates; i++)
                _slotEncoderPosition[i].Init(probabilityPrices);

            for (i = 0; i < BaseConstants.FullDistances - BaseConstants.EndPositionModelIndex; i++)
            {
                _encodersPosition[i] = new BitEncoder();
                _encodersPosition[i].Init(probabilityPrices);
            }

            _lengthEncoder.InitLengthEncoder((uint)1 << _positionStateBits, probabilityPrices);
            _repeatedMatchLengthEncoder.InitLengthEncoder((uint)1 << _positionStateBits, probabilityPrices);

            _alignEncoderPosition.Init(probabilityPrices);

            _longestMatchWasFound = false;
            _optimumEndIndex = 0;
            _optimumCurrentIndex = 0;
            _additionalOffset = 0;
        }

        private OutResult ReadMatchDistances()
        {
            uint resLength = 0;
            var distancePairsCount = _matchFinder.GetMatches(_matchDistances);

            if (distancePairsCount > 0)
            {
                resLength = _matchDistances[distancePairsCount - 2];
                if (resLength == _fastBytesCount)
                {
                    resLength += _matchFinder.GetMatchLength((int)resLength - 1, _matchDistances[distancePairsCount - 1],
                        BaseConstants.MaxMatchLength - resLength);
                }
            }

            _additionalOffset++;

            return new OutResult { ReturnValue = resLength, OutValue = distancePairsCount };
        }

        private void MovePosition(uint count)
        {
            if (count > 0)
            {
                _matchFinder.Skip(count);
                _additionalOffset += count;
            }
        }

        private uint GetRepeatLength1Price(CoderState state, uint posState) =>
            _isRepeatG0[state.Index].GetPrice0() +
                _isRepeat0Long[(state.Index << BaseConstants.MaxPositionStatesBits) +
                    posState].GetPrice0();

        private uint GetPureRepeatPrice(uint repeatIndex, CoderState state, uint posState)
        {
            uint price;
            if (repeatIndex == 0)
            {
                price = _isRepeatG0[state.Index].GetPrice0();
                price += _isRepeat0Long[(state.Index << BaseConstants.MaxPositionStatesBits) + posState].GetPrice1();
            }
            else
            {
                price = _isRepeatG0[state.Index].GetPrice1();
                if (repeatIndex == 1)
                    price += _isRepeatG1[state.Index].GetPrice0();
                else
                {
                    price += _isRepeatG1[state.Index].GetPrice1();
                    price += _isRepeatG2[state.Index].GetPrice(repeatIndex - 2);
                }
            }

            return price;
        }

        private uint GetRepeatPrice(uint repeatIndex, uint length, CoderState state, uint positionState)
        {
            var price = _repeatedMatchLengthEncoder.GetPrice(length - BaseConstants.MinMatchLength, positionState);

            return price + GetPureRepeatPrice(repeatIndex, state, positionState);
        }

        private uint GetLengthPricePosition(uint position, uint length, uint positionState)
        {
            uint price;
            var lengthToPositionState = BaseConstants.GetLengthToPositionState(length);

            if (position < BaseConstants.FullDistances)
            {
                price = _distancesPrices[(lengthToPositionState * BaseConstants.FullDistances) + position];
            }
            else
            {
                price = _slotPricesPosition[
                    (lengthToPositionState << BaseConstants.PositionSlotBits) +
                    GetPositionSlot2(position)] +
                    _alignPrices[position & BaseConstants.AlignMask];
            }

            return price + _lengthEncoder.GetPrice(length - BaseConstants.MinMatchLength, positionState);
        }

        private OutResult Backward(uint current)
        {
            var returnValues = new OutResult { ReturnValue = 0, OutValue = 0 };

            _optimumEndIndex = current;
            var position = _optimum[current].PreviousPosition;
            var back = _optimum[current].PreviousBack;
            do
            {
                if (_optimum[current].Previous1IsChar)
                {
                    _optimum[position].MakeAsChar();
                    _optimum[position].PreviousPosition = position - 1;

                    if (_optimum[current].Previous2)
                    {
                        _optimum[position - 1].Previous1IsChar = false;
                        _optimum[position - 1].PreviousPosition = _optimum[current].PreviousPosition2;
                        _optimum[position - 1].PreviousBack = _optimum[current].PreviousBack2;
                    }
                }
                var previousPosition = position;
                var currentBack = back;

                back = _optimum[previousPosition].PreviousBack;
                position = _optimum[previousPosition].PreviousPosition;

                _optimum[previousPosition].PreviousBack = currentBack;
                _optimum[previousPosition].PreviousPosition = current;
                current = previousPosition;
            }
            while (current > 0);

            returnValues.OutValue = _optimum[0].PreviousBack;
            _optimumCurrentIndex = _optimum[0].PreviousPosition;

            returnValues.ReturnValue = _optimumCurrentIndex;

            return returnValues;
        }

        private OutResult GetOptimum(uint position)
        {
            var returnValues = new OutResult { ReturnValue = 0, OutValue = 0 };

            if (_optimumEndIndex != _optimumCurrentIndex)
            {
                returnValues.ReturnValue = _optimum[_optimumCurrentIndex].PreviousPosition - _optimumCurrentIndex;
                returnValues.OutValue = _optimum[_optimumCurrentIndex].PreviousBack;
                _optimumCurrentIndex = _optimum[_optimumCurrentIndex].PreviousPosition;
                // Return.
            }
            else
            {
                _optimumCurrentIndex = _optimumEndIndex = 0;

                uint mainLength, distancePairsLength;
                if (!_longestMatchWasFound)
                {
                    var result = ReadMatchDistances();

                    mainLength = result.ReturnValue;
                    distancePairsLength = result.OutValue;
                }
                else
                {
                    mainLength = _longestMatchLength;
                    distancePairsLength = _distancePairsCount;
                    _longestMatchWasFound = false;
                }

                var availableBytesCount = _matchFinder.GetAvailableBytesCount() + 1;
                if (availableBytesCount < 2)
                {
                    returnValues.OutValue = 0xFFFFFFFF;
                    // Return.
                    returnValues.ReturnValue = 1;
                }
                else
                {
                    if (availableBytesCount > BaseConstants.MaxMatchLength)
                    {
                        availableBytesCount = BaseConstants.MaxMatchLength;
                    }

                    uint maxRepeatIndex = 0;
                    uint i;
                    for (i = 0; i < BaseConstants.RepeatDistances; i++)
                    {
                        _repeats[i] = _repeatDistances[i];
                        _repeatLengths[i] = _matchFinder.GetMatchLength(0 - 1, _repeats[i], BaseConstants.MaxMatchLength);

                        if (_repeatLengths[i] > _repeatLengths[maxRepeatIndex]) maxRepeatIndex = i;
                    }
                    if (_repeatLengths[maxRepeatIndex] >= _fastBytesCount)
                    {
                        returnValues.OutValue = maxRepeatIndex;
                        returnValues.ReturnValue = _repeatLengths[maxRepeatIndex];
                        MovePosition(returnValues.ReturnValue - 1);

                        // Return;
                    }
                    else
                    {
                        if (mainLength >= _fastBytesCount)
                        {
                            returnValues.OutValue = _matchDistances[distancePairsLength - 1] + BaseConstants.RepeatDistances;
                            MovePosition(mainLength - 1);

                            // Return.
                            returnValues.ReturnValue = mainLength;
                        }
                        else
                        {
                            var currentByte = _matchFinder.GetIndexByte(0 - 1);
                            var matchByte = _matchFinder.GetIndexByte((int)(0 - _repeatDistances[0] - 1 - 1));

                            if (mainLength < 2 && currentByte != matchByte && _repeatLengths[maxRepeatIndex] < 2)
                            {
                                returnValues.OutValue = 0xFFFFFFFF;

                                returnValues.ReturnValue = 1;
                                // Return.
                            }
                            else
                            {
                                _optimum[0].State = _coderState;

                                var positionState = (position & _positionStateMask);

                                _optimum[1].Price = _isMatch[
                                    (_coderState.Index << BaseConstants.MaxPositionStatesBits) +
                                    positionState].GetPrice0() +
                                    _literalEncoder.GetSubCoder(position, _previousByte).GetPrice(!_coderState.IsCharState(), matchByte, currentByte);

                                _optimum[1].MakeAsChar();

                                var matchPrice = _isMatch[(_coderState.Index << BaseConstants.MaxPositionStatesBits) + positionState].GetPrice1();
                                var repeatMatchPrice = matchPrice + _isRepeat[_coderState.Index].GetPrice1();

                                if (matchByte == currentByte)
                                {
                                    var shortRepPrice = repeatMatchPrice + GetRepeatLength1Price(_coderState, positionState);
                                    if (shortRepPrice < _optimum[1].Price)
                                    {
                                        _optimum[1].Price = shortRepPrice;
                                        _optimum[1].MakeAsShortRepeat();
                                    }
                                }

                                var endLength = ((mainLength >= _repeatLengths[maxRepeatIndex]) ? mainLength : _repeatLengths[maxRepeatIndex]);

                                if (endLength < 2)
                                {
                                    returnValues.OutValue = _optimum[1].PreviousBack;
                                    returnValues.ReturnValue = 1;

                                    // Return.
                                }
                                else
                                {

                                    _optimum[1].PreviousPosition = 0;

                                    _optimum[0].Backs0 = _repeats[0];
                                    _optimum[0].Backs1 = _repeats[1];
                                    _optimum[0].Backs2 = _repeats[2];
                                    _optimum[0].Backs3 = _repeats[3];

                                    var length = endLength;
                                    do
                                    {
                                        _optimum[length].Price = InfinityPrice;
                                        length--;
                                    }
                                    while (length >= 2);

                                    for (i = 0; i < BaseConstants.RepeatDistances; i++)
                                    {
                                        var repeatLength = _repeatLengths[i];
                                        if (repeatLength >= 2)
                                        {
                                            var price = repeatMatchPrice + GetPureRepeatPrice(i, _coderState, positionState);
                                            do
                                            {
                                                var currentAndLengthPrice = price + _repeatedMatchLengthEncoder.GetPrice(repeatLength - 2, positionState);
                                                var optimum = _optimum[repeatLength];

                                                if (currentAndLengthPrice < optimum.Price)
                                                {
                                                    optimum.Price = currentAndLengthPrice;
                                                    optimum.PreviousPosition = 0;
                                                    optimum.PreviousBack = i;
                                                    optimum.Previous1IsChar = false;
                                                }
                                            }
                                            while (--repeatLength >= 2);
                                        }
                                    }

                                    var normalMatchPrice = matchPrice + _isRepeat[_coderState.Index].GetPrice0();

                                    length = ((_repeatLengths[0] >= 2) ? _repeatLengths[0] + 1 : 2);
                                    if (length <= mainLength)
                                    {
                                        uint offs = 0;
                                        while (length > _matchDistances[offs])
                                            offs += 2;
                                        
                                        while (true)
                                        {
                                            var distance = _matchDistances[offs + 1];
                                            var currentAndLengthPrice = normalMatchPrice + GetLengthPricePosition(distance, length, positionState);
                                            var optimum = _optimum[length];
                                            if (currentAndLengthPrice < optimum.Price)
                                            {
                                                optimum.Price = currentAndLengthPrice;
                                                optimum.PreviousPosition = 0;
                                                optimum.PreviousBack = distance + BaseConstants.RepeatDistances;
                                                optimum.Previous1IsChar = false;
                                            }

                                            if (length == _matchDistances[offs])
                                            {
                                                offs += 2;
                                                if (offs == distancePairsLength) break;
                                            }

                                            length++;
                                        }
                                    }

                                    uint current = 0;

                                    while (true)
                                    {
                                        current++;
                                        if (current == endLength)
                                        {
                                            var backwardReturnValues = Backward(current);
                                            returnValues.ReturnValue = backwardReturnValues.ReturnValue;
                                            returnValues.OutValue = backwardReturnValues.OutValue;

                                            break;
                                        }
                                        uint newLength;
                                        var result = ReadMatchDistances();
                                        newLength = result.ReturnValue;
                                        distancePairsLength = result.OutValue;
                                        if (newLength >= _fastBytesCount)
                                        {
                                            _distancePairsCount = distancePairsLength;
                                            _longestMatchLength = newLength;
                                            _longestMatchWasFound = true;
                                            var backwardReturnValues = Backward(current);
                                            returnValues.ReturnValue = backwardReturnValues.ReturnValue;
                                            returnValues.OutValue = backwardReturnValues.OutValue;

                                            break;
                                        }

                                        position++;
                                        var previousPosition = _optimum[current].PreviousPosition;
                                        CoderState state;
                                        if (_optimum[current].Previous1IsChar)
                                        {
                                            previousPosition--;
                                            if (_optimum[current].Previous2)
                                            {
                                                state = _optimum[_optimum[current].PreviousPosition2].State;

                                                if (_optimum[current].PreviousBack2 < BaseConstants.RepeatDistances)
                                                {
                                                    state.UpdateRepeat();
                                                }
                                                else
                                                {
                                                    state.UpdateMatch();
                                                }
                                            }
                                            else
                                                state = _optimum[previousPosition].State;
                                            state.UpdateChar();
                                        }
                                        else state = _optimum[previousPosition].State;

                                        if (previousPosition == current - 1)
                                        {
                                            if (_optimum[current].IsShortRepeat()) state.UpdateShortRepeat();
                                            else state.UpdateChar();
                                        }
                                        else
                                        {
                                            uint pos;
                                            if (_optimum[current].Previous1IsChar && _optimum[current].Previous2)
                                            {
                                                previousPosition = _optimum[current].PreviousPosition2;
                                                pos = _optimum[current].PreviousBack2;
                                                state.UpdateRepeat();
                                            }
                                            else
                                            {
                                                pos = _optimum[current].PreviousBack;
                                                if (pos < BaseConstants.RepeatDistances) state.UpdateRepeat();
                                                else state.UpdateMatch();
                                            }

                                            var optimum = _optimum[previousPosition];
                                            if (pos < BaseConstants.RepeatDistances)
                                            {
                                                if (pos == 0)
                                                {
                                                    _repeats[0] = optimum.Backs0;
                                                    _repeats[1] = optimum.Backs1;
                                                    _repeats[2] = optimum.Backs2;
                                                    _repeats[3] = optimum.Backs3;
                                                }
                                                else if (pos == 1)
                                                {
                                                    _repeats[0] = optimum.Backs1;
                                                    _repeats[1] = optimum.Backs0;
                                                    _repeats[2] = optimum.Backs2;
                                                    _repeats[3] = optimum.Backs3;
                                                }
                                                else if (pos == 2)
                                                {
                                                    _repeats[0] = optimum.Backs2;
                                                    _repeats[1] = optimum.Backs0;
                                                    _repeats[2] = optimum.Backs1;
                                                    _repeats[3] = optimum.Backs3;
                                                }
                                                else
                                                {
                                                    _repeats[0] = optimum.Backs3;
                                                    _repeats[1] = optimum.Backs0;
                                                    _repeats[2] = optimum.Backs1;
                                                    _repeats[3] = optimum.Backs2;
                                                }
                                            }
                                            else
                                            {
                                                _repeats[0] = (pos - BaseConstants.RepeatDistances);
                                                _repeats[1] = optimum.Backs0;
                                                _repeats[2] = optimum.Backs1;
                                                _repeats[3] = optimum.Backs2;
                                            }
                                        }

                                        _optimum[current].State = state;
                                        _optimum[current].Backs0 = _repeats[0];
                                        _optimum[current].Backs1 = _repeats[1];
                                        _optimum[current].Backs2 = _repeats[2];
                                        _optimum[current].Backs3 = _repeats[3];

                                        var currentPrice = _optimum[current].Price;

                                        currentByte = _matchFinder.GetIndexByte(0 - 1);
                                        matchByte = _matchFinder.GetIndexByte((int)(0 - _repeats[0] - 1 - 1));

                                        positionState = (position & _positionStateMask);

                                        var currentAndPrice1 = currentPrice +
                                            _isMatch[(state.Index << BaseConstants.MaxPositionStatesBits) + positionState].GetPrice0() +
                                            _literalEncoder.GetSubCoder(position, _matchFinder.GetIndexByte(0 - 2)).
                                            GetPrice(!state.IsCharState(), matchByte, currentByte);

                                        var nextOptimum = _optimum[current + 1];

                                        bool nextIsChar = false;
                                        if (currentAndPrice1 < nextOptimum.Price)
                                        {
                                            nextOptimum.Price = currentAndPrice1;
                                            nextOptimum.PreviousPosition = current;
                                            nextOptimum.MakeAsChar();
                                            nextIsChar = true;
                                        }

                                        matchPrice = currentPrice + _isMatch[(state.Index << BaseConstants.MaxPositionStatesBits) + positionState].GetPrice1();
                                        repeatMatchPrice = matchPrice + _isRepeat[state.Index].GetPrice1();

                                        if (matchByte == currentByte &&
                                            !(nextOptimum.PreviousPosition < current && nextOptimum.PreviousBack == 0))
                                        {
                                            var shortRepPrice = repeatMatchPrice + GetRepeatLength1Price(state, positionState);
                                            if (shortRepPrice <= nextOptimum.Price)
                                            {
                                                nextOptimum.Price = shortRepPrice;
                                                nextOptimum.PreviousPosition = current;
                                                nextOptimum.MakeAsShortRepeat();
                                                nextIsChar = true;
                                            }
                                        }

                                        var fullAvailableBytesCount = _matchFinder.GetAvailableBytesCount() + 1;
                                        fullAvailableBytesCount = LzmaHelpers.GetMinValue(BaseConstants.OptimumNumber - 1 - current, fullAvailableBytesCount);
                                        availableBytesCount = fullAvailableBytesCount;

                                        var continueLoop = availableBytesCount < 2;

                                        if (!continueLoop)
                                        {
                                            if (availableBytesCount > _fastBytesCount)
                                                availableBytesCount = _fastBytesCount;
                                            if (!nextIsChar && matchByte != currentByte)
                                            {
                                                var t = LzmaHelpers.GetMinValue(fullAvailableBytesCount - 1, _fastBytesCount);
                                                var lengthTest2 = _matchFinder.GetMatchLength(0, _repeats[0], t);
                                                if (lengthTest2 >= 2)
                                                {
                                                    var state2 = state;
                                                    state2.UpdateChar();
                                                    var posStateNext = (position + 1) & _positionStateMask;
                                                    var nextRepMatchPrice = currentAndPrice1 +
                                                        _isMatch[(state2.Index << BaseConstants.MaxPositionStatesBits) + posStateNext].GetPrice1() +
                                                        _isRepeat[state2.Index].GetPrice1();
                                                    {
                                                        var offset = current + 1 + lengthTest2;

                                                        while (endLength < offset)
                                                            _optimum[++endLength].Price = InfinityPrice;

                                                        var curAndLenPrice = nextRepMatchPrice +
                                                            GetRepeatPrice(0, lengthTest2, state2, posStateNext);
                                                        var optimum = _optimum[offset];

                                                        if (curAndLenPrice < optimum.Price)
                                                        {
                                                            optimum.Price = curAndLenPrice;
                                                            optimum.PreviousPosition = current + 1;
                                                            optimum.PreviousBack = 0;
                                                            optimum.Previous1IsChar = true;
                                                            optimum.Previous2 = false;
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        if (!continueLoop)
                                        {
                                            uint startLength = 2; // speed optimization 

                                            for (uint repeatIndex = 0; !continueLoop && repeatIndex < BaseConstants.RepeatDistances; repeatIndex++)
                                            {
                                                var lengthTest = _matchFinder.GetMatchLength(0 - 1, _repeats[repeatIndex], availableBytesCount);

                                                continueLoop = lengthTest < 2;

                                                if (!continueLoop)
                                                {
                                                    var lengthTestTemp = lengthTest;
                                                    do
                                                    {
                                                        while (endLength < current + lengthTest)
                                                            _optimum[++endLength].Price = InfinityPrice;

                                                        var curAndLenPrice = repeatMatchPrice +
                                                            GetRepeatPrice(repeatIndex, lengthTest, state, positionState);

                                                        var optimum = _optimum[current + lengthTest];

                                                        if (curAndLenPrice < optimum.Price)
                                                        {
                                                            optimum.Price = curAndLenPrice;
                                                            optimum.PreviousPosition = current;
                                                            optimum.PreviousBack = repeatIndex;
                                                            optimum.Previous1IsChar = false;
                                                        }
                                                    }
                                                    while (--lengthTest >= 2);

                                                    lengthTest = lengthTestTemp;

                                                    if (repeatIndex == 0) startLength = lengthTest + 1;

                                                    // The following statement possibly should only be executed only if _maxMode is true,
                                                    // however, the if statement was commented out in the SDK.
                                                    if (lengthTest < fullAvailableBytesCount)
                                                    {
                                                        var t = LzmaHelpers.GetMinValue(fullAvailableBytesCount - 1 - lengthTest, _fastBytesCount);
                                                        var lengthTest2 = _matchFinder.GetMatchLength((int)lengthTest, _repeats[repeatIndex], t);

                                                        if (lengthTest2 >= 2)
                                                        {
                                                            var state2 = state;
                                                            state2.UpdateRepeat();
                                                            var nextPositionState = (position + lengthTest) & _positionStateMask;
                                                            var currentAndLengthCharPrice =
                                                                repeatMatchPrice + GetRepeatPrice(repeatIndex, lengthTest, state, positionState) +
                                                                _isMatch[(state2.Index << BaseConstants.MaxPositionStatesBits) + nextPositionState].GetPrice0() +
                                                                _literalEncoder.GetSubCoder(position + lengthTest,
                                                                _matchFinder.GetIndexByte((int)lengthTest - 1 - 1)).GetPrice(true,
                                                                _matchFinder.GetIndexByte(((int)lengthTest - 1 - (int)(_repeats[repeatIndex] + 1))),
                                                                _matchFinder.GetIndexByte((int)lengthTest - 1));
                                                            state2.UpdateChar();
                                                            nextPositionState = (position + lengthTest + 1) & _positionStateMask;
                                                            var nextMatchPrice = currentAndLengthCharPrice + _isMatch[(state2.Index << BaseConstants.MaxPositionStatesBits) + nextPositionState].GetPrice1();
                                                            var nextRepeatMatchPrice = nextMatchPrice + _isRepeat[state2.Index].GetPrice1();

                                                            var offset = lengthTest + 1 + lengthTest2;

                                                            while (endLength < current + offset)
                                                                _optimum[++endLength].Price = InfinityPrice;

                                                            var currentAndLengthPrice = nextRepeatMatchPrice + GetRepeatPrice(0, lengthTest2, state2, nextPositionState);
                                                            var optimum = _optimum[current + offset];
                                                            if (currentAndLengthPrice < optimum.Price)
                                                            {
                                                                optimum.Price = currentAndLengthPrice;
                                                                optimum.PreviousPosition = current + lengthTest + 1;
                                                                optimum.PreviousBack = 0;
                                                                optimum.Previous1IsChar = true;
                                                                optimum.Previous2 = true;
                                                                optimum.PreviousPosition2 = current;
                                                                optimum.PreviousBack2 = repeatIndex;
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                            if (!continueLoop)
                                            {
                                                if (newLength > availableBytesCount)
                                                {
                                                    newLength = availableBytesCount;

                                                    for (distancePairsLength = 0; newLength > _matchDistances[distancePairsLength]; distancePairsLength += 2) ;

                                                    _matchDistances[distancePairsLength] = newLength;
                                                    distancePairsLength += 2;
                                                }

                                                if (newLength >= startLength)
                                                {
                                                    normalMatchPrice = matchPrice + _isRepeat[state.Index].GetPrice0();
                                                    while (endLength < current + newLength)
                                                        _optimum[++endLength].Price = InfinityPrice;

                                                    uint offs = 0;
                                                    while (startLength > _matchDistances[offs])
                                                        offs += 2;
                                                    
                                                    var lengthTest = startLength;
                                                    while (true)
                                                    {
                                                        var currentBack = _matchDistances[offs + 1];
                                                        var currentAndLengthPrice = normalMatchPrice + GetLengthPricePosition(currentBack, lengthTest, positionState);
                                                        var optimum = _optimum[current + lengthTest];

                                                        if (currentAndLengthPrice < optimum.Price)
                                                        {
                                                            optimum.Price = currentAndLengthPrice;
                                                            optimum.PreviousPosition = current;
                                                            optimum.PreviousBack = currentBack + BaseConstants.RepeatDistances;
                                                            optimum.Previous1IsChar = false;
                                                        }

                                                        if (lengthTest == _matchDistances[offs])
                                                        {
                                                            if (lengthTest < fullAvailableBytesCount)
                                                            {
                                                                var t = LzmaHelpers.GetMinValue(fullAvailableBytesCount - 1 - lengthTest, _fastBytesCount);
                                                                var lengthTest2 = _matchFinder.GetMatchLength((int)lengthTest, currentBack, t);
                                                                if (lengthTest2 >= 2)
                                                                {
                                                                    var state2 = state;
                                                                    state2.UpdateMatch();
                                                                    var nextPositionState = (position + lengthTest) & _positionStateMask;
                                                                    var currentAndLengthCharPrice = currentAndLengthPrice +
                                                                        _isMatch[(state2.Index << BaseConstants.MaxPositionStatesBits) + nextPositionState].GetPrice0() +
                                                                        _literalEncoder.GetSubCoder(position + lengthTest,
                                                                        _matchFinder.GetIndexByte((int)lengthTest - 1 - 1)).
                                                                        GetPrice(true,
                                                                        _matchFinder.GetIndexByte((int)lengthTest - (int)(currentBack + 1) - 1),
                                                                        _matchFinder.GetIndexByte((int)lengthTest - 1));
                                                                    state2.UpdateChar();
                                                                    nextPositionState = (position + lengthTest + 1) & _positionStateMask;
                                                                    var nextMatchPrice = currentAndLengthCharPrice + _isMatch[(state2.Index << BaseConstants.MaxPositionStatesBits) + nextPositionState].GetPrice1();
                                                                    var nextRepMatchPrice = nextMatchPrice + _isRepeat[state2.Index].GetPrice1();

                                                                    var offset = lengthTest + 1 + lengthTest2;

                                                                    while (endLength < current + offset)
                                                                        _optimum[++endLength].Price = InfinityPrice;

                                                                    currentAndLengthPrice = nextRepMatchPrice + GetRepeatPrice(0, lengthTest2, state2, nextPositionState);
                                                                    optimum = _optimum[current + offset];
                                                                    if (currentAndLengthPrice < optimum.Price)
                                                                    {
                                                                        optimum.Price = currentAndLengthPrice;
                                                                        optimum.PreviousPosition = current + lengthTest + 1;
                                                                        optimum.PreviousBack = 0;
                                                                        optimum.Previous1IsChar = true;
                                                                        optimum.Previous2 = true;
                                                                        optimum.PreviousPosition2 = current;
                                                                        optimum.PreviousBack2 = currentBack + BaseConstants.RepeatDistances;
                                                                    }
                                                                }
                                                            }
                                                            offs += 2;

                                                            if (offs == distancePairsLength) break;
                                                        }
                                                        
                                                        lengthTest++;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return returnValues;
        }

        private bool ChangePair(uint smallDistance, uint bigDistance)
        {
            const int Difference = 7;

            return (smallDistance < ((uint)(1) << (32 - Difference)) && bigDistance >= (smallDistance << Difference));
        }

        private void WriteEndMarker(uint posState)
        {
            if (_writeEndMarker)
            {

                _isMatch[(_coderState.Index << BaseConstants.MaxPositionStatesBits) + posState].Encode(_rangeEncoder, 1);
                _isRepeat[_coderState.Index].Encode(_rangeEncoder, 0);

                _coderState.UpdateMatch();

                var length = BaseConstants.MinMatchLength;

                _lengthEncoder.Encode(_rangeEncoder, length - BaseConstants.MinMatchLength, posState);

                uint positionSlot = (1 << BaseConstants.PositionSlotBits) - 1;
                var lengthToPositionState = BaseConstants.GetLengthToPositionState(length);

                _slotEncoderPosition[lengthToPositionState].Encode(_rangeEncoder, positionSlot);

                var footerBits = 30;
                var reducedPosition = (((uint)1) << footerBits) - 1;

                _rangeEncoder.EncodeDirectBits(reducedPosition >> BaseConstants.AlignBits, footerBits - BaseConstants.AlignBits);
                _alignEncoderPosition.ReverseEncode(_rangeEncoder, reducedPosition & BaseConstants.AlignMask);
            }
        }

        private void Flush(uint nowPos)
        {
            ReleaseMatchFinderStream();

            WriteEndMarker(nowPos & _positionStateMask);

            _rangeEncoder.FlushData();
        }

        private void ReleaseMatchFinderStream()
        {
            if (_needReleaseMatchFinderStream)
            {
                _matchFinder.ReleaseStream();

                _needReleaseMatchFinderStream = false;
            }
        }

        private void SetOutStream(SimpleMemoryStream outStream) =>
            _rangeEncoder.SetStream(outStream);

        private void ReleaseOutputStream() =>
            _rangeEncoder.ReleaseStream();

        private void ReleaseStreams()
        {
            ReleaseMatchFinderStream();

            ReleaseOutputStream();
        }

        private void SetStreams(SimpleMemoryStream inputStream, SimpleMemoryStream outputStream)
        {
            _inputStream = inputStream;
            _finished = false;

            Create();

            SetOutStream(outputStream);

            Init();

            // Following two method calls should not be run in case of fast mode, however, the if statement was
            // commented out in the SDK.
            FillDistancePrices();
            FillAlignPrices();

            _lengthEncoder.SetTableSize(_fastBytesCount + 1 - BaseConstants.MinMatchLength);
            _lengthEncoder.UpdateTables((uint)1 << _positionStateBits);
            _repeatedMatchLengthEncoder.SetTableSize(_fastBytesCount + 1 - BaseConstants.MinMatchLength);
            _repeatedMatchLengthEncoder.UpdateTables((uint)1 << _positionStateBits);

            _currentPosition = 0;
        }

        private void FillDistancePrices()
        {
            for (var i = BaseConstants.StartPositionModelIndex; i < BaseConstants.FullDistances; i++)
            {
                var posSlot = GetPositionSlot(i);
                var footerBits = (int)((posSlot >> 1) - 1);
                var baseValue = ((2 | (posSlot & 1)) << footerBits);

                _tempPrices[i] = BitTreeEncoder.ReverseGetPrice(
                    _encodersPosition,
                    baseValue - posSlot - 1,
                    footerBits, i - baseValue);
            }

            for (uint lengthToPositionState = 0; lengthToPositionState < BaseConstants.LengthToPositionStates; lengthToPositionState++)
            {
                uint positionSlot;
                var bitTreeEncoder = _slotEncoderPosition[lengthToPositionState];

                uint st = (lengthToPositionState << BaseConstants.PositionSlotBits);

                for (positionSlot = 0; positionSlot < _distanceTableSize; positionSlot++)
                    _slotPricesPosition[st + positionSlot] = bitTreeEncoder.GetPrice(positionSlot);

                for (positionSlot = BaseConstants.EndPositionModelIndex; positionSlot < _distanceTableSize; positionSlot++)
                    _slotPricesPosition[st + positionSlot] +=
                        ((((positionSlot >> 1) - 1) - BaseConstants.AlignBits) << RangeEncoderConstants.BitPriceShiftBits);

                var st2 = lengthToPositionState * BaseConstants.FullDistances;
                uint i;

                for (i = 0; i < BaseConstants.StartPositionModelIndex; i++)
                    _distancesPrices[st2 + i] = _slotPricesPosition[st + i];

                for (; i < BaseConstants.FullDistances; i++)
                    _distancesPrices[st2 + i] = _slotPricesPosition[st + GetPositionSlot(i)] + _tempPrices[i];
            }

            _matchPriceCount = 0;
        }

        private void FillAlignPrices()
        {
            for (uint i = 0; i < BaseConstants.AlignTableSize; i++)
                _alignPrices[i] = _alignEncoderPosition.ReverseGetPrice(i);

            _alignPriceCount = 0;
        }


        private class LiteralEncoder
        {
            // Literal position bits can be 2 for 32-bit data and 0 for other cases.
            // Literal context bits can be 0 for 32-bit data and 3 for other cases.
            // Either case the sum of these bits are maximum 3 so we need a static array for 1 << 3 coders.
            private const byte MaxNumberOfCoders = 1 << 3;


            private Encoder2[] _coders;
            private int _previousBits;
            private int _positionBits;
            private uint _positionMask;


            public void Create(int positionBits, int previousBits)
            {
                _coders = new Encoder2[MaxNumberOfCoders];

                if (_previousBits != previousBits || _positionBits != positionBits)
                {
                    _positionBits = positionBits;
                    _positionMask = ((uint)1 << positionBits) - 1;
                    _previousBits = previousBits;
                    var states = (uint)1 << (_previousBits + _positionBits);

                    for (uint i = 0; i < states; i++)
                    {
                        _coders[i] = new Encoder2();
                        _coders[i].Create();
                    }
                }
            }

            public void Init(uint[] probabilityPrices)
            {
                var states = (uint)1 << (_previousBits + _positionBits);

                for (uint i = 0; i < states; i++)
                    _coders[i].Init(probabilityPrices);
            }

            public Encoder2 GetSubCoder(uint position, byte previousByte) =>
                _coders[((position & _positionMask) << _previousBits) + (uint)(previousByte >> (8 - _previousBits))];


            public class Encoder2
            {
                private BitEncoder[] _encoders;


                public void Create() =>
                    _encoders = new BitEncoder[0x300];

                public void Init(uint[] probabilityPrices)
                {
                    for (var i = 0; i < 0x300; i++)
                    {
                        _encoders[i] = new BitEncoder();
                        _encoders[i].Init(probabilityPrices);
                    }
                }

                public void Encode(RangeEncoder rangeEncoder, byte symbol)
                {
                    uint context = 1;
                    for (var i = 7; i >= 0; i--)
                    {
                        var bit = (uint)((symbol >> i) & 1);

                        _encoders[context].Encode(rangeEncoder, bit);

                        context = (context << 1) | bit;
                    }
                }

                public void EncodeMatched(RangeEncoder rangeEncoder, byte matchByte, byte symbol)
                {
                    uint context = 1;
                    var same = true;

                    for (int i = 7; i >= 0; i--)
                    {
                        var bit = (uint)((symbol >> i) & 1);
                        var state = context;

                        if (same)
                        {
                            var matchBit = (uint)((matchByte >> i) & 1);
                            state += ((1 + matchBit) << 8);
                            same = (matchBit == bit);
                        }

                        _encoders[state].Encode(rangeEncoder, bit);
                        context = (context << 1) | bit;
                    }
                }

                public uint GetPrice(bool matchMode, byte matchbyte, byte symbol)
                {
                    uint price = 0;
                    uint context = 1;
                    var i = 7;

                    if (matchMode)
                    {
                        while (i >= 0)
                        {
                            uint matchBit = (uint)(matchbyte >> i) & 1;
                            uint bit = (uint)(symbol >> i) & 1;
                            price += _encoders[((1 + matchBit) << 8) + context].GetPrice(bit);
                            context = (context << 1) | bit;

                            i--;

                            if (matchBit != bit) break;
                        }
                    }

                    for (; i >= 0; i--)
                    {
                        var bit = (uint)(symbol >> i) & 1;

                        price += _encoders[context].GetPrice(bit);
                        context = (context << 1) | bit;
                    }

                    return price;
                }
            }
        }


        private class LengthPriceTableEncoder
        {
            private uint[] _prices = new uint[BaseConstants.SymbolLength << BaseConstants.MaxEncodingPositionStatesBits];
            private uint _tableSize;
            private uint[] _counters = new uint[BaseConstants.MaxEncodingPositionStates];

            #region Length Encoder fields

            private BitEncoder _choice;
            private BitEncoder _choice2;
            private BitTreeEncoder[] _lowCoder;
            private BitTreeEncoder[] _midCoder;
            private BitTreeEncoder _highCoder;

            #endregion


            public LengthPriceTableEncoder()
            {
                _choice = new BitEncoder();
                _choice2 = new BitEncoder();
            }


            public void SetTableSize(uint tableSize) =>
                _tableSize = tableSize;

            public uint GetPrice(uint symbol, uint positionState) =>
                _prices[positionState * BaseConstants.SymbolLength + symbol];

            public void UpdateTables(uint positionStateCount)
            {
                for (uint positionState = 0; positionState < positionStateCount; positionState++)
                    UpdateTable(positionState);
            }

            public void Encode(RangeEncoder rangeEncoder, uint symbol, uint posState)
            {
                EncodeLengthEncoder(rangeEncoder, symbol, posState);

                if (--_counters[posState] == 0) UpdateTable(posState);
            }


            private void UpdateTable(uint posState)
            {
                SetPricesLenEncoder(posState, _tableSize, _prices, posState * BaseConstants.SymbolLength);

                _counters[posState] = _tableSize;
            }


            #region Length Encoder methods

            public void InitLengthEncoder(uint positionStatesCount, uint[] probabilityPrices)
            {
                _lowCoder = new BitTreeEncoder[BaseConstants.MaxEncodingPositionStates];
                _midCoder = new BitTreeEncoder[BaseConstants.MaxEncodingPositionStates];
                _highCoder = new BitTreeEncoder(BaseConstants.HighLengthBits);

                for (uint positionState = 0; positionState < BaseConstants.MaxEncodingPositionStates; positionState++)
                {
                    _lowCoder[positionState] = new BitTreeEncoder(BaseConstants.LowLengthBits);
                    _midCoder[positionState] = new BitTreeEncoder(BaseConstants.MidLengthBits);
                }

                _choice.Init(probabilityPrices);
                _choice2.Init(probabilityPrices);

                for (uint posState = 0; posState < positionStatesCount; posState++)
                {
                    _lowCoder[posState].Init(probabilityPrices);
                    _midCoder[posState].Init(probabilityPrices);
                }

                _highCoder.Init(probabilityPrices);
            }

            private void EncodeLengthEncoder(RangeEncoder rangeEncoder, uint symbol, uint posState)
            {
                if (symbol < BaseConstants.LowLength)
                {
                    _choice.Encode(rangeEncoder, 0);
                    _lowCoder[posState].Encode(rangeEncoder, symbol);
                }
                else
                {
                    symbol -= BaseConstants.LowLength;
                    _choice.Encode(rangeEncoder, 1);

                    if (symbol < BaseConstants.MidLength)
                    {
                        _choice2.Encode(rangeEncoder, 0);
                        _midCoder[posState].Encode(rangeEncoder, symbol);
                    }
                    else
                    {
                        _choice2.Encode(rangeEncoder, 1);
                        _highCoder.Encode(rangeEncoder, symbol - BaseConstants.MidLength);
                    }
                }
            }

            private void SetPricesLenEncoder(uint posState, uint numSymbols, uint[] prices, uint st)
            {
                var a0 = _choice.GetPrice0();
                var a1 = _choice.GetPrice1();
                var b0 = a1 + _choice2.GetPrice0();
                var b1 = a1 + _choice2.GetPrice1();
                uint i = 0;

                for (i = 0; i < BaseConstants.LowLength; i++)
                {
                    if (i < numSymbols) prices[st + i] = a0 + _lowCoder[posState].GetPrice(i);
                }

                for (; i < BaseConstants.LowLength + BaseConstants.MidLength; i++)
                {
                    if (i < numSymbols) prices[st + i] = b0 + _midCoder[posState].GetPrice(i - BaseConstants.LowLength);
                }

                for (; i < numSymbols; i++)
                    prices[st + i] = b1 + _highCoder.GetPrice(i - BaseConstants.LowLength - BaseConstants.MidLength);
            }

            #endregion
        }

        private class Optimal
        {
            public CoderState State;
            public bool Previous1IsChar;
            public bool Previous2;
            public uint PreviousPosition2;
            public uint PreviousBack2;
            public uint Price;
            public uint PreviousPosition;
            public uint PreviousBack;
            public uint Backs0;
            public uint Backs1;
            public uint Backs2;
            public uint Backs3;


            public void MakeAsChar()
            {
                PreviousBack = 0xFFFFFFFF;
                Previous1IsChar = false;
            }

            public void MakeAsShortRepeat()
            {
                PreviousBack = 0;
                Previous1IsChar = false;
            }

            public bool IsShortRepeat() =>
                PreviousBack == 0;
        };

        public class OutResult
        {
            public uint ReturnValue { get; set; }
            public uint OutValue { get; set; }
        }
    }
}