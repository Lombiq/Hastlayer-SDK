using Hast.Samples.SampleAssembly.Lzma.Constants;
using Hast.Samples.SampleAssembly.Lzma.Helpers;
using Hast.Samples.SampleAssembly.Lzma.Models;
using Hast.Samples.SampleAssembly.Lzma.RangeCoder;
using Hast.Samples.SampleAssembly.Models;
//using System;

namespace Hast.Samples.SampleAssembly.Lzma
{


    public class LzmaEncoder
    {
        private const uint InfinityPrice = 0xFFFFFFF;
        private const int DefaultDictionaryLogSize = 22;
        private const uint DefaultFastBytesCount = 0x20;
        private const uint SpecialSymbolLengthCount = BaseConstants.LowLengthSymbols + BaseConstants.MidLengthSymbols;
        private const int PropertySize = 5;


        private CoderState _state = new CoderState();
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
        private BitEncoder[] _isRepeated0Long = new BitEncoder[BaseConstants.States << BaseConstants.MaxPositionStatesBits];
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
        private uint[] _slotPricesPosition = new uint[1 << (BaseConstants.SlotPositionBits + BaseConstants.LengthToStatesPositionBits)];
        private uint[] _distancesPrices = new uint[BaseConstants.FullDistances << BaseConstants.LengthToStatesPositionBits];
        private uint[] _alignPrices = new uint[BaseConstants.AlignTableSize];
        private uint _alignPriceCount;
        private uint _distanceTableSize = (DefaultDictionaryLogSize * 2);
        private int _statePositionBits = 2;
        private uint _statePositionMask = (4 - 1);
        private int _literalPositionBits = 0;
        private int _literalContextBits = 3;
        private uint _dictionarySize = (1 << DefaultDictionaryLogSize);
        private uint _dictionarySizePrevious = 0xFFFFFFFF;
        private uint _fastBytesCountPrevious = 0xFFFFFFFF;
        private long _currentPosition;
        private bool _finished;
        private SimpleMemoryStream _inputStream;
        private uint _matchFinderHashBytesCount = 4;
        private bool _writeEndMark = false;
        private bool _needReleaseStream;
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
                _slotEncoderPosition[i] = new BitTreeEncoder(BaseConstants.SlotPositionBits);
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

                _needReleaseStream = true;
                _inputStream = null;

                if (_trainSize > 0) _matchFinder.Skip(_trainSize);
            }

            var run = true;
            if (!_finished)
            {
                _finished = true;

                var previousPosition = _currentPosition;
                if (_currentPosition == 0)
                {
                    if (_matchFinder.GetAvailableBytesCount() == 0)
                    {
                        Flush((uint)_currentPosition);

                        run = false;
                    }
                    else
                    {
                        ReadMatchDistances();

                        uint posState = (uint)(_currentPosition) & _statePositionMask;
                        _isMatch[(_state.Index << BaseConstants.MaxPositionStatesBits) + posState].Encode(_rangeEncoder, 0);
                        _state.UpdateChar();
                        byte curbyte = _matchFinder.GetIndexByte((int)(0 - _additionalOffset));
                        _literalEncoder.GetSubCoder((uint)(_currentPosition), _previousByte).Encode(_rangeEncoder, curbyte);
                        _previousByte = curbyte;
                        _additionalOffset--;
                        _currentPosition++;
                    }
                }
                if (run)
                {
                    if (_matchFinder.GetAvailableBytesCount() == 0) Flush((uint)_currentPosition);
                    else
                    {
                        while (run)
                        {
                            var returnValues = GetOptimum((uint)_currentPosition);

                            var position = returnValues.OutValue;
                            var length = returnValues.ReturnValue;

                            uint statePosition = ((uint)_currentPosition) & _statePositionMask;
                            uint complexState = (_state.Index << BaseConstants.MaxPositionStatesBits) + statePosition;

                            if (length == 1 && position == 0xFFFFFFFF)
                            {
                                _isMatch[complexState].Encode(_rangeEncoder, 0);
                                byte currentByte = _matchFinder.GetIndexByte((int)(0 - _additionalOffset));

                                var subCoder = _literalEncoder.GetSubCoder((uint)_currentPosition, _previousByte);
                                if (!_state.IsCharState())
                                {
                                    var matchByte = _matchFinder.GetIndexByte((int)(0 - _repeatDistances[0] - 1 - _additionalOffset));
                                    subCoder.EncodeMatched(_rangeEncoder, matchByte, currentByte);
                                }
                                else subCoder.Encode(_rangeEncoder, currentByte);

                                _previousByte = currentByte;
                                _state.UpdateChar();
                            }
                            else
                            {
                                _isMatch[complexState].Encode(_rangeEncoder, 1);
                                if (position < BaseConstants.RepeatDistances)
                                {
                                    _isRepeat[_state.Index].Encode(_rangeEncoder, 1);

                                    if (position == 0)
                                    {
                                        _isRepeatG0[_state.Index].Encode(_rangeEncoder, 0);

                                        if (length == 1) _isRepeated0Long[complexState].Encode(_rangeEncoder, 0);
                                        else _isRepeated0Long[complexState].Encode(_rangeEncoder, 1);
                                    }
                                    else
                                    {
                                        _isRepeatG0[_state.Index].Encode(_rangeEncoder, 1);

                                        if (position == 1) _isRepeatG1[_state.Index].Encode(_rangeEncoder, 0);
                                        else
                                        {
                                            _isRepeatG1[_state.Index].Encode(_rangeEncoder, 1);
                                            _isRepeatG2[_state.Index].Encode(_rangeEncoder, position - 2);
                                        }
                                    }

                                    if (length == 1) _state.UpdateShortRep();
                                    else
                                    {
                                        _repeatedMatchLengthEncoder.Encode(_rangeEncoder, length - BaseConstants.MinMatchLength, statePosition);
                                        _state.UpdateRep();
                                    }

                                    uint distance = _repeatDistances[position];

                                    if (position != 0)
                                    {
                                        for (uint i = position; i >= 1; i--)
                                            _repeatDistances[i] = _repeatDistances[i - 1];

                                        _repeatDistances[0] = distance;
                                    }
                                }
                                else
                                {
                                    _isRepeat[_state.Index].Encode(_rangeEncoder, 0);
                                    _state.UpdateMatch();
                                    _lengthEncoder.Encode(_rangeEncoder, length - BaseConstants.MinMatchLength, statePosition);
                                    position -= BaseConstants.RepeatDistances;

                                    var slotPosition = GetSlotPosition(position);
                                    var lengthToStatePosition = BaseConstants.GetLengthToStatePosition(length);

                                    _slotEncoderPosition[lengthToStatePosition].Encode(_rangeEncoder, slotPosition);

                                    if (slotPosition >= BaseConstants.StartPositionModelIndex)
                                    {
                                        var footerBits = (int)((slotPosition >> 1) - 1);
                                        var baseValue = ((2 | (slotPosition & 1)) << footerBits);
                                        var reducedPosition = position - baseValue;

                                        if (slotPosition < BaseConstants.EndPositionModelIndex)
                                        {
                                            BitTreeEncoder.ReverseEncode(_encodersPosition,
                                                    baseValue - slotPosition - 1, _rangeEncoder, footerBits, reducedPosition);
                                        }
                                        else
                                        {
                                            _rangeEncoder.EncodeDirectBits(reducedPosition >> BaseConstants.AlignBits, footerBits - BaseConstants.AlignBits);
                                            _alignEncoderPosition.ReverseEncode(_rangeEncoder, reducedPosition & BaseConstants.AlignMask);
                                            _alignPriceCount++;
                                        }
                                    }

                                    var distance = position;

                                    for (uint i = BaseConstants.RepeatDistances - 1; i >= 1; i--)
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
                                // if (!_fastMode)
                                if (_matchPriceCount >= (1 << 7)) FillDistancesPrices();

                                if (_alignPriceCount >= BaseConstants.AlignTableSize) FillAlignPrices();

                                inputSize = _currentPosition;
                                outputSize = _rangeEncoder.GetProcessedSizeAdd();
                                if (_matchFinder.GetAvailableBytesCount() == 0)
                                {
                                    Flush((uint)_currentPosition);
                                    run = false;
                                }
                                else if (_currentPosition - previousPosition >= (1 << 12))
                                {
                                    _finished = false;
                                    finished = false;
                                    run = false;
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
            _needReleaseStream = false;

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

            // Should throw an Exception when it becomes supported.
            // if (dictionarySize < (uint)(1 << BaseConstants.DicLogSizeMin) ||
            //     dictionarySize > (uint)(1 << kDicLogSizeMaxCompress))

            _dictionarySize = properties.DictionarySize;
            int dictionaryLogSize;
            for (dictionaryLogSize = 0; dictionaryLogSize < (uint)DictionaryLogSizeMaxCompress; dictionaryLogSize++)
            {
                if (_dictionarySize <= ((uint)(1) << dictionaryLogSize))
                    break;
            }
            _distanceTableSize = (uint)dictionaryLogSize * 2;

            // Should throw an Exception when it becomes supported.
            // if (params.PositionStateBits < 0 || params.PositionStateBits > (uint)BaseConstants.NumPosStatesBitsEncodingMax)
            _statePositionBits = properties.PositionStateBits;
            _statePositionMask = (((uint)1) << _statePositionBits) - 1;

            // Should throw an Exception when it becomes supported.
            // if (parameters.LiteralPositionBits < 0 || 
            //     parameters.LiteralPositionBits > (uint)BaseConstants.NumLitPosStatesBitsEncodingMax)
            _literalPositionBits = properties.LiteralPositionBits;

            // Should throw an Exception when it becomes supported.
            // if (parameters.LiteralContextBits < 0 || parameters.LiteralContextBits > (uint)BaseConstants.NumLitContextBitsMax)
            //     throw new LzmaInvalidParamException(); ;
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
            _properties[0] = (byte)((_statePositionBits * 5 + _literalPositionBits) * 9 + _literalContextBits);

            for (int i = 0; i < 4; i++)
                _properties[1 + i] = (byte)((_dictionarySize >> (8 * i)) & 0xFF);

            outputStream.Write(_properties, 0, PropertySize);
        }


        private uint GetSlotPosition(uint position)
        {
            uint slotPosition;

            if (position < (1 << 11)) slotPosition = _fastPosition[position];
            else if (position < (1 << 21)) slotPosition = (uint)(_fastPosition[position >> 10] + 20);
            else slotPosition = (uint)(_fastPosition[position >> 20] + 40);

            return slotPosition;
        }

        private uint GetSlotPosition2(uint pos)
        {
            uint slotPosition;

            if (pos < (1 << 17)) slotPosition = (uint)(_fastPosition[pos >> 6] + 12);
            else if (pos < (1 << 27)) slotPosition = (uint)(_fastPosition[pos >> 16] + 32);
            else slotPosition = (uint)(_fastPosition[pos >> 26] + 52);

            return slotPosition;
        }

        private void BaseInit()
        {
            _state.Init();
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
            _writeEndMark = writeEndMarker;

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
                for (uint j = 0; j <= _statePositionMask; j++)
                {
                    var complexState = (i << BaseConstants.MaxPositionStatesBits) + j;

                    _isMatch[complexState] = new BitEncoder();
                    _isMatch[complexState].Init(probabilityPrices);
                    _isRepeated0Long[complexState] = new BitEncoder();
                    _isRepeated0Long[complexState].Init(probabilityPrices);
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

            _lengthEncoder.InitLenEncoder((uint)1 << _statePositionBits, probabilityPrices);
            _repeatedMatchLengthEncoder.InitLenEncoder((uint)1 << _statePositionBits, probabilityPrices);

            _alignEncoderPosition.Init(probabilityPrices);

            _longestMatchWasFound = false;
            _optimumEndIndex = 0;
            _optimumCurrentIndex = 0;
            _additionalOffset = 0;
        }

        private OutResult ReadMatchDistances()
        {
            uint resLength = 0;
            uint distancePairsCount = _matchFinder.GetMatches(_matchDistances);

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
                _isRepeated0Long[(state.Index << BaseConstants.MaxPositionStatesBits) +
                    posState].GetPrice0();

        private uint GetPureRepeatPrice(uint repeatIndex, CoderState state, uint posState)
        {
            uint price;
            if (repeatIndex == 0)
            {
                price = _isRepeatG0[state.Index].GetPrice0();
                price += _isRepeated0Long[(state.Index << BaseConstants.MaxPositionStatesBits) + posState].GetPrice1();
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

        private uint GetRepeatPrice(uint repeatIndex, uint length, CoderState state, uint statePosition)
        {
            var price = _repeatedMatchLengthEncoder.GetPrice(length - BaseConstants.MinMatchLength, statePosition);

            return price + GetPureRepeatPrice(repeatIndex, state, statePosition);
        }

        private uint GetLengthPricePosition(uint position, uint length, uint statePosition)
        {
            uint price;
            uint lenToPosState = BaseConstants.GetLengthToStatePosition(length);

            if (position < BaseConstants.FullDistances)
            {
                price = _distancesPrices[(lenToPosState * BaseConstants.FullDistances) + position];
            }
            else
            {
                price = _slotPricesPosition[
                    (lenToPosState << BaseConstants.SlotPositionBits) +
                    GetSlotPosition2(position)] +
                    _alignPrices[position & BaseConstants.AlignMask];
            }

            return price + _lengthEncoder.GetPrice(length - BaseConstants.MinMatchLength, statePosition);
        }

        private OutResult Backward(uint current)
        {
            // ReturnValue: _optimumCurrentIndex, OutValue: backRes
            var returnValues = new OutResult { ReturnValue = 0, OutValue = 0 };

            _optimumEndIndex = current;
            uint position = _optimum[current].PreviousPosition;
            uint back = _optimum[current].PreviousBack;
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
                uint previousPosition = position;
                uint currentBack = back;

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
            // ReturnValue: lenRes, OutValue: backRes
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

                uint availableBytesCount = _matchFinder.GetAvailableBytesCount() + 1;
                if (availableBytesCount < 2)
                {
                    returnValues.OutValue = 0xFFFFFFFF;
                    // Return.
                    returnValues.ReturnValue = 1;
                }
                else
                {
                    if (availableBytesCount > BaseConstants.MaxMatchLength)
                        availableBytesCount = BaseConstants.MaxMatchLength;

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
                                _optimum[0].State = _state;

                                var statePosition = (position & _statePositionMask);

                                _optimum[1].Price = _isMatch[
                                    (_state.Index << BaseConstants.MaxPositionStatesBits) +
                                    statePosition].GetPrice0() +
                                    _literalEncoder.GetSubCoder(position, _previousByte).GetPrice(!_state.IsCharState(), matchByte, currentByte);

                                _optimum[1].MakeAsChar();

                                uint matchPrice = _isMatch[(_state.Index << BaseConstants.MaxPositionStatesBits) + statePosition].GetPrice1();
                                uint repeatMatchPrice = matchPrice + _isRepeat[_state.Index].GetPrice1();

                                if (matchByte == currentByte)
                                {
                                    uint shortRepPrice = repeatMatchPrice + GetRepeatLength1Price(_state, statePosition);
                                    if (shortRepPrice < _optimum[1].Price)
                                    {
                                        _optimum[1].Price = shortRepPrice;
                                        _optimum[1].MakeAsShortRep();
                                    }
                                }

                                uint endLength = ((mainLength >= _repeatLengths[maxRepeatIndex]) ? mainLength : _repeatLengths[maxRepeatIndex]);

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
                                            var price = repeatMatchPrice + GetPureRepeatPrice(i, _state, statePosition);
                                            do
                                            {
                                                var currentAndLengthPrice = price + _repeatedMatchLengthEncoder.GetPrice(repeatLength - 2, statePosition);
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

                                    uint normalMatchPrice = matchPrice + _isRepeat[_state.Index].GetPrice0();

                                    length = ((_repeatLengths[0] >= 2) ? _repeatLengths[0] + 1 : 2);
                                    if (length <= mainLength)
                                    {
                                        uint offs = 0;
                                        while (length > _matchDistances[offs])
                                            offs += 2;

                                        var run2 = true;
                                        while (run2)
                                        {
                                            uint distance = _matchDistances[offs + 1];
                                            uint currentAndLengthPrice = normalMatchPrice + GetLengthPricePosition(distance, length, statePosition);
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
                                                if (offs == distancePairsLength)
                                                    // Break.
                                                    run2 = false;
                                            }
                                            // It's because we are possibly in the break.
                                            if (run2) length++;
                                        }
                                    }

                                    uint current = 0;

                                    var run = true;
                                    while (run)
                                    {
                                        current++;
                                        if (current == endLength)
                                        {
                                            var backwardReturnValues = Backward(current);
                                            returnValues.ReturnValue = backwardReturnValues.ReturnValue;
                                            returnValues.OutValue = backwardReturnValues.OutValue;
                                            run = false;
                                            // Return.
                                        }
                                        else
                                        {
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
                                                run = false;

                                                // Return.
                                            }
                                            else
                                            {
                                                position++;
                                                uint previousPosition = _optimum[current].PreviousPosition;
                                                CoderState state;
                                                if (_optimum[current].Previous1IsChar)
                                                {
                                                    previousPosition--;
                                                    if (_optimum[current].Previous2)
                                                    {
                                                        state = _optimum[_optimum[current].PreviousPosition2].State;

                                                        if (_optimum[current].PreviousBack2 < BaseConstants.RepeatDistances)
                                                        {
                                                            state.UpdateRep();
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
                                                    if (_optimum[current].IsShortRep()) state.UpdateShortRep();
                                                    else state.UpdateChar();
                                                }
                                                else
                                                {
                                                    uint pos;
                                                    if (_optimum[current].Previous1IsChar && _optimum[current].Previous2)
                                                    {
                                                        previousPosition = _optimum[current].PreviousPosition2;
                                                        pos = _optimum[current].PreviousBack2;
                                                        state.UpdateRep();
                                                    }
                                                    else
                                                    {
                                                        pos = _optimum[current].PreviousBack;
                                                        if (pos < BaseConstants.RepeatDistances) state.UpdateRep();
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

                                                uint currentPrice = _optimum[current].Price;

                                                currentByte = _matchFinder.GetIndexByte(0 - 1);
                                                matchByte = _matchFinder.GetIndexByte((int)(0 - _repeats[0] - 1 - 1));

                                                statePosition = (position & _statePositionMask);

                                                uint currentAndPrice1 = currentPrice +
                                                    _isMatch[(state.Index << BaseConstants.MaxPositionStatesBits) + statePosition].GetPrice0() +
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

                                                matchPrice = currentPrice + _isMatch[(state.Index << BaseConstants.MaxPositionStatesBits) + statePosition].GetPrice1();
                                                repeatMatchPrice = matchPrice + _isRepeat[state.Index].GetPrice1();

                                                if (matchByte == currentByte &&
                                                    !(nextOptimum.PreviousPosition < current && nextOptimum.PreviousBack == 0))
                                                {
                                                    uint shortRepPrice = repeatMatchPrice + GetRepeatLength1Price(state, statePosition);
                                                    if (shortRepPrice <= nextOptimum.Price)
                                                    {
                                                        nextOptimum.Price = shortRepPrice;
                                                        nextOptimum.PreviousPosition = current;
                                                        nextOptimum.MakeAsShortRep();
                                                        nextIsChar = true;
                                                    }
                                                }

                                                uint fullAvailableBytesCount = _matchFinder.GetAvailableBytesCount() + 1;
                                                fullAvailableBytesCount = LzmaHelpers.GetMinValue(BaseConstants.OptimumNumber - 1 - current, fullAvailableBytesCount);
                                                availableBytesCount = fullAvailableBytesCount;

                                                var continueLoop = availableBytesCount < 2;

                                                if (!continueLoop)
                                                {
                                                    if (availableBytesCount > _fastBytesCount)
                                                        availableBytesCount = _fastBytesCount;
                                                    if (!nextIsChar && matchByte != currentByte)
                                                    {
                                                        // try Literal + rep0
                                                        uint t = LzmaHelpers.GetMinValue(fullAvailableBytesCount - 1, _fastBytesCount);
                                                        uint lengthTest2 = _matchFinder.GetMatchLength(0, _repeats[0], t);
                                                        if (lengthTest2 >= 2)
                                                        {
                                                            var state2 = state;
                                                            state2.UpdateChar();
                                                            uint posStateNext = (position + 1) & _statePositionMask;
                                                            uint nextRepMatchPrice = currentAndPrice1 +
                                                                _isMatch[(state2.Index << BaseConstants.MaxPositionStatesBits) + posStateNext].GetPrice1() +
                                                                _isRepeat[state2.Index].GetPrice1();
                                                            {
                                                                uint offset = current + 1 + lengthTest2;

                                                                while (endLength < offset)
                                                                    _optimum[++endLength].Price = InfinityPrice;

                                                                uint curAndLenPrice = nextRepMatchPrice +
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
                                                        uint lengthTest = _matchFinder.GetMatchLength(0 - 1, _repeats[repeatIndex], availableBytesCount);

                                                        continueLoop = lengthTest < 2;

                                                        if (!continueLoop)
                                                        {
                                                            uint lengthTestTemp = lengthTest;
                                                            do
                                                            {
                                                                while (endLength < current + lengthTest)
                                                                    _optimum[++endLength].Price = InfinityPrice;

                                                                uint curAndLenPrice = repeatMatchPrice +
                                                                    GetRepeatPrice(repeatIndex, lengthTest, state, statePosition);

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

                                                            // if (_maxMode)
                                                            if (lengthTest < fullAvailableBytesCount)
                                                            {
                                                                var t = LzmaHelpers.GetMinValue(fullAvailableBytesCount - 1 - lengthTest, _fastBytesCount);
                                                                var lenTest2 = _matchFinder.GetMatchLength((int)lengthTest, _repeats[repeatIndex], t);

                                                                if (lenTest2 >= 2)
                                                                {
                                                                    var state2 = state;
                                                                    state2.UpdateRep();
                                                                    var nextStatePosition = (position + lengthTest) & _statePositionMask;
                                                                    var currentAndLengthCharPrice =
                                                                        repeatMatchPrice + GetRepeatPrice(repeatIndex, lengthTest, state, statePosition) +
                                                                        _isMatch[(state2.Index << BaseConstants.MaxPositionStatesBits) + nextStatePosition].GetPrice0() +
                                                                        _literalEncoder.GetSubCoder(position + lengthTest,
                                                                        _matchFinder.GetIndexByte((int)lengthTest - 1 - 1)).GetPrice(true,
                                                                        _matchFinder.GetIndexByte(((int)lengthTest - 1 - (int)(_repeats[repeatIndex] + 1))),
                                                                        _matchFinder.GetIndexByte((int)lengthTest - 1));
                                                                    state2.UpdateChar();
                                                                    nextStatePosition = (position + lengthTest + 1) & _statePositionMask;
                                                                    var nextMatchPrice = currentAndLengthCharPrice + _isMatch[(state2.Index << BaseConstants.MaxPositionStatesBits) + nextStatePosition].GetPrice1();
                                                                    var nextRepeatMatchPrice = nextMatchPrice + _isRepeat[state2.Index].GetPrice1();

                                                                    var offset = lengthTest + 1 + lenTest2;

                                                                    while (endLength < current + offset)
                                                                        _optimum[++endLength].Price = InfinityPrice;

                                                                    var currentAndLengthPrice = nextRepeatMatchPrice + GetRepeatPrice(0, lenTest2, state2, nextStatePosition);
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

                                                            var run2 = true;
                                                            var lengthTest = startLength;
                                                            while (run2)
                                                            {
                                                                var currentBack = _matchDistances[offs + 1];
                                                                var currentAndLengthPrice = normalMatchPrice + GetLengthPricePosition(currentBack, lengthTest, statePosition);
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
                                                                        uint t = LzmaHelpers.GetMinValue(fullAvailableBytesCount - 1 - lengthTest, _fastBytesCount);
                                                                        uint lengthTest2 = _matchFinder.GetMatchLength((int)lengthTest, currentBack, t);
                                                                        if (lengthTest2 >= 2)
                                                                        {
                                                                            var state2 = state;
                                                                            state2.UpdateMatch();
                                                                            var nextStatePosition = (position + lengthTest) & _statePositionMask;
                                                                            var currentAndLengthCharPrice = currentAndLengthPrice +
                                                                                _isMatch[(state2.Index << BaseConstants.MaxPositionStatesBits) + nextStatePosition].GetPrice0() +
                                                                                _literalEncoder.GetSubCoder(position + lengthTest,
                                                                                _matchFinder.GetIndexByte((int)lengthTest - 1 - 1)).
                                                                                GetPrice(true,
                                                                                _matchFinder.GetIndexByte((int)lengthTest - (int)(currentBack + 1) - 1),
                                                                                _matchFinder.GetIndexByte((int)lengthTest - 1));
                                                                            state2.UpdateChar();
                                                                            nextStatePosition = (position + lengthTest + 1) & _statePositionMask;
                                                                            uint nextMatchPrice = currentAndLengthCharPrice + _isMatch[(state2.Index << BaseConstants.MaxPositionStatesBits) + nextStatePosition].GetPrice1();
                                                                            uint nextRepMatchPrice = nextMatchPrice + _isRepeat[state2.Index].GetPrice1();

                                                                            uint offset = lengthTest + 1 + lengthTest2;

                                                                            while (endLength < current + offset)
                                                                                _optimum[++endLength].Price = InfinityPrice;

                                                                            currentAndLengthPrice = nextRepMatchPrice + GetRepeatPrice(0, lengthTest2, state2, nextStatePosition);
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
                                                                    if (offs == distancePairsLength)
                                                                        // Break.
                                                                        run2 = false;
                                                                }

                                                                // It's because we are possibly in break.
                                                                if (run2) lengthTest++;
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
                }
            }

            return returnValues;
        }

        private bool ChangePair(uint smallDist, uint bigDist)
        {
            const int kDif = 7;
            return (smallDist < ((uint)(1) << (32 - kDif)) && bigDist >= (smallDist << kDif));
        }

        private void WriteEndMarker(uint posState)
        {
            if (_writeEndMark)
            {

                _isMatch[(_state.Index << BaseConstants.MaxPositionStatesBits) + posState].Encode(_rangeEncoder, 1);
                _isRepeat[_state.Index].Encode(_rangeEncoder, 0);
                _state.UpdateMatch();
                uint len = BaseConstants.MinMatchLength;
                _lengthEncoder.Encode(_rangeEncoder, len - BaseConstants.MinMatchLength, posState);
                uint posSlot = (1 << BaseConstants.SlotPositionBits) - 1;
                uint lenToPosState = BaseConstants.GetLengthToStatePosition(len);
                _slotEncoderPosition[lenToPosState].Encode(_rangeEncoder, posSlot);
                int footerBits = 30;
                uint posReduced = (((uint)1) << footerBits) - 1;
                _rangeEncoder.EncodeDirectBits(posReduced >> BaseConstants.AlignBits, footerBits - BaseConstants.AlignBits);
                _alignEncoderPosition.ReverseEncode(_rangeEncoder, posReduced & BaseConstants.AlignMask);
            }
        }

        private void Flush(uint nowPos)
        {
            ReleaseMFStream();
            WriteEndMarker(nowPos & _statePositionMask);
            _rangeEncoder.FlushData();
        }

        private void ReleaseMFStream()
        {
            if (_needReleaseStream)
            {
                _matchFinder.ReleaseStream();
                _needReleaseStream = false;
            }
        }

        private void SetOutStream(SimpleMemoryStream outStream) { _rangeEncoder.SetStream(outStream); }
        private void ReleaseOutStream() { _rangeEncoder.ReleaseStream(); }

        private void ReleaseStreams()
        {
            ReleaseMFStream();
            ReleaseOutStream();
        }

        private void SetStreams(SimpleMemoryStream inStream, SimpleMemoryStream outStream)
        {
            _inputStream = inStream;
            _finished = false;
            Create();
            SetOutStream(outStream);
            Init();

            // if (!_fastMode)
            {
                FillDistancesPrices();
                FillAlignPrices();
            }

            _lengthEncoder.SetTableSize(_fastBytesCount + 1 - BaseConstants.MinMatchLength);
            _lengthEncoder.UpdateTables((uint)1 << _statePositionBits);
            _repeatedMatchLengthEncoder.SetTableSize(_fastBytesCount + 1 - BaseConstants.MinMatchLength);
            _repeatedMatchLengthEncoder.UpdateTables((uint)1 << _statePositionBits);

            _currentPosition = 0;
        }

        private void FillDistancesPrices()
        {
            for (uint i = BaseConstants.StartPositionModelIndex; i < BaseConstants.FullDistances; i++)
            {
                uint posSlot = GetSlotPosition(i);
                int footerBits = (int)((posSlot >> 1) - 1);
                uint baseVal = ((2 | (posSlot & 1)) << footerBits);
                _tempPrices[i] = BitTreeEncoder.ReverseGetPrice(_encodersPosition,
                    baseVal - posSlot - 1, footerBits, i - baseVal);
            }

            for (uint lenToPosState = 0; lenToPosState < BaseConstants.LengthToPositionStates; lenToPosState++)
            {
                uint posSlot;
                BitTreeEncoder encoder = _slotEncoderPosition[lenToPosState];

                uint st = (lenToPosState << BaseConstants.SlotPositionBits);
                for (posSlot = 0; posSlot < _distanceTableSize; posSlot++)
                    _slotPricesPosition[st + posSlot] = encoder.GetPrice(posSlot);
                for (posSlot = BaseConstants.EndPositionModelIndex; posSlot < _distanceTableSize; posSlot++)
                    _slotPricesPosition[st + posSlot] += ((((posSlot >> 1) - 1) - BaseConstants.AlignBits) << RangeEncoderConstants.BitPriceShiftBits);

                uint st2 = lenToPosState * BaseConstants.FullDistances;
                uint i;
                for (i = 0; i < BaseConstants.StartPositionModelIndex; i++)
                    _distancesPrices[st2 + i] = _slotPricesPosition[st + i];
                for (; i < BaseConstants.FullDistances; i++)
                    _distancesPrices[st2 + i] = _slotPricesPosition[st + GetSlotPosition(i)] + _tempPrices[i];
            }
            _matchPriceCount = 0;
        }

        private void FillAlignPrices()
        {
            for (uint i = 0; i < BaseConstants.AlignTableSize; i++) _alignPrices[i] = _alignEncoderPosition.ReverseGetPrice(i);

            _alignPriceCount = 0;
        }


        private class LiteralEncoder
        {
            // Literal position bits can be 2 for 32-bit data and 0 for other cases.
            // Literal context bits can be 0 for 32-bit data and 3 for other cases.
            // Either case the sum of these bits are maximum 3 so we need a static array for 1 << 3 coders.
            private const byte MaxNumberOfCoders = 1 << 3;


            private Encoder2[] _coders;
            private int _numPrevBits;
            private int _numPosBits;
            private uint _posMask;


            public void Create(int numPosBits, int numPrevBits)
            {
                _coders = new Encoder2[MaxNumberOfCoders];

                if (_numPrevBits != numPrevBits || _numPosBits != numPosBits)
                {
                    _numPosBits = numPosBits;
                    _posMask = ((uint)1 << numPosBits) - 1;
                    _numPrevBits = numPrevBits;
                    uint numStates = (uint)1 << (_numPrevBits + _numPosBits);
                    for (uint i = 0; i < numStates; i++)
                    {
                        _coders[i] = new Encoder2();
                        _coders[i].Create();
                    }
                }
            }

            public void Init(uint[] probPrices)
            {
                uint numStates = (uint)1 << (_numPrevBits + _numPosBits);
                for (uint i = 0; i < numStates; i++)
                    _coders[i].Init(probPrices);
            }

            public Encoder2 GetSubCoder(uint pos, byte prevbyte)
            {
                return _coders[((pos & _posMask) << _numPrevBits) + (uint)(prevbyte >> (8 - _numPrevBits))];
            }


            public class Encoder2
            {
                private BitEncoder[] _encoders;


                public void Create() { _encoders = new BitEncoder[0x300]; }

                public void Init(uint[] probPrices)
                {
                    for (int i = 0; i < 0x300; i++)
                    {
                        _encoders[i] = new BitEncoder();
                        _encoders[i].Init(probPrices);
                    }
                }

                public void Encode(RangeEncoder rangeEncoder, byte symbol)
                {
                    uint context = 1;
                    for (int i = 7; i >= 0; i--)
                    {
                        uint bit = (uint)((symbol >> i) & 1);
                        _encoders[context].Encode(rangeEncoder, bit);
                        context = (context << 1) | bit;
                    }
                }

                public void EncodeMatched(RangeEncoder rangeEncoder, byte matchbyte, byte symbol)
                {
                    uint context = 1;
                    bool same = true;
                    for (int i = 7; i >= 0; i--)
                    {
                        uint bit = (uint)((symbol >> i) & 1);
                        uint state = context;
                        if (same)
                        {
                            uint matchBit = (uint)((matchbyte >> i) & 1);
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
                    int i = 7;
                    if (matchMode)
                    {
                        var run = true;
                        while (run && i >= 0)
                        {
                            uint matchBit = (uint)(matchbyte >> i) & 1;
                            uint bit = (uint)(symbol >> i) & 1;
                            price += _encoders[((1 + matchBit) << 8) + context].GetPrice(bit);
                            context = (context << 1) | bit;
                            if (matchBit != bit)
                            {
                                // Break.
                                run = false;
                            }

                            i--;
                        }
                    }
                    for (; i >= 0; i--)
                    {
                        uint bit = (uint)(symbol >> i) & 1;
                        price += _encoders[context].GetPrice(bit);
                        context = (context << 1) | bit;
                    }
                    return price;
                }
            }
        }


        private class LengthPriceTableEncoder
        {
            private uint[] _prices = new uint[BaseConstants.LenSymbols << BaseConstants.MaxPositionStatesEncodingBits];
            private uint _tableSize;
            private uint[] _counters = new uint[BaseConstants.MaxPositionStatesEncoding];

            #region LenEncoder fields

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


            public void SetTableSize(uint tableSize) { _tableSize = tableSize; }

            public uint GetPrice(uint symbol, uint posState)
            {
                return _prices[posState * BaseConstants.LenSymbols + symbol];
            }

            public void UpdateTables(uint numPosStates)
            {
                for (uint posState = 0; posState < numPosStates; posState++)
                    UpdateTable(posState);
            }

            public void Encode(RangeEncoder rangeEncoder, uint symbol, uint posState)
            {
                EncodeLenEncoder(rangeEncoder, symbol, posState);
                if (--_counters[posState] == 0)
                    UpdateTable(posState);
            }


            private void UpdateTable(uint posState)
            {
                SetPricesLenEncoder(posState, _tableSize, _prices, posState * BaseConstants.LenSymbols);
                _counters[posState] = _tableSize;
            }


            #region LenEncoder Methods

            public void InitLenEncoder(uint numPosStates, uint[] probPrices)
            {
                _lowCoder = new BitTreeEncoder[BaseConstants.MaxPositionStatesEncoding];
                _midCoder = new BitTreeEncoder[BaseConstants.MaxPositionStatesEncoding];
                _highCoder = new BitTreeEncoder(BaseConstants.HighLengthBits);

                for (uint posState = 0; posState < BaseConstants.MaxPositionStatesEncoding; posState++)
                {
                    _lowCoder[posState] = new BitTreeEncoder(BaseConstants.LowLengthBits);
                    _midCoder[posState] = new BitTreeEncoder(BaseConstants.MidLengthBits);
                }

                _choice.Init(probPrices);
                _choice2.Init(probPrices);
                for (uint posState = 0; posState < numPosStates; posState++)
                {
                    _lowCoder[posState].Init(probPrices);
                    _midCoder[posState].Init(probPrices);
                }
                _highCoder.Init(probPrices);
            }

            private void EncodeLenEncoder(RangeEncoder rangeEncoder, uint symbol, uint posState)
            {
                if (symbol < BaseConstants.LowLengthSymbols)
                {
                    _choice.Encode(rangeEncoder, 0);
                    _lowCoder[posState].Encode(rangeEncoder, symbol);
                }
                else
                {
                    symbol -= BaseConstants.LowLengthSymbols;
                    _choice.Encode(rangeEncoder, 1);
                    if (symbol < BaseConstants.MidLengthSymbols)
                    {
                        _choice2.Encode(rangeEncoder, 0);
                        _midCoder[posState].Encode(rangeEncoder, symbol);
                    }
                    else
                    {
                        _choice2.Encode(rangeEncoder, 1);
                        _highCoder.Encode(rangeEncoder, symbol - BaseConstants.MidLengthSymbols);
                    }
                }
            }

            private void SetPricesLenEncoder(uint posState, uint numSymbols, uint[] prices, uint st)
            {
                uint a0 = _choice.GetPrice0();
                uint a1 = _choice.GetPrice1();
                uint b0 = a1 + _choice2.GetPrice0();
                uint b1 = a1 + _choice2.GetPrice1();
                uint i = 0;
                for (i = 0; i < BaseConstants.LowLengthSymbols; i++)
                {
                    if (i < numSymbols) prices[st + i] = a0 + _lowCoder[posState].GetPrice(i);
                }
                for (; i < BaseConstants.LowLengthSymbols + BaseConstants.MidLengthSymbols; i++)
                {
                    if (i < numSymbols) prices[st + i] = b0 + _midCoder[posState].GetPrice(i - BaseConstants.LowLengthSymbols);
                }
                for (; i < numSymbols; i++)
                    prices[st + i] = b1 + _highCoder.GetPrice(i - BaseConstants.LowLengthSymbols - BaseConstants.MidLengthSymbols);
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


            public void MakeAsChar() { PreviousBack = 0xFFFFFFFF; Previous1IsChar = false; }

            public void MakeAsShortRep() { PreviousBack = 0; ; Previous1IsChar = false; }

            public bool IsShortRep() { return (PreviousBack == 0); }
        };

        public class OutResult
        {
            public uint ReturnValue { get; set; }
            public uint OutValue { get; set; }
        }
    }
}
