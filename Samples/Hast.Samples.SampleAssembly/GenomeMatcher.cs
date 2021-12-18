using System;
using System.Text;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Samples.SampleAssembly
{
    /// <summary>
    /// Algorithm for running Smith-Waterman Genome Matcher. Also see <c>GenomeMatcherSampleRunner</c> on what to
    /// configure to make this work.
    ///
    /// NOTE: this sample is not parallelized and thus not really suitable for Hastlayer. We'll rework it in the future.
    /// </summary>
    public class GenomeMatcher
    {
        private const int GetLCSInputOneLengthIndex = 0;
        private const int GetLCSInputTwoLengthIndex = 1;
        private const int GetLCSInputOneStartIndex = 2;
        private const ushort GetLCSTopCellPointerValue = 0;
        private const ushort GetLCSLeftCellPointerValue = 1;
        private const ushort GetLCSDiagonalCellPointerValue = 2;
        private const ushort GetLCSOutOfBorderDiagonalCellPointerValue = 3;

        /// <summary>
        /// Calculates the longest common subsequence of two byte arrays with the Smith-Waterman algorithm.
        /// </summary>
        /// <param name="memory">The <see cref="SimpleMemory"/> object representing the accessible memory space.</param>
        public virtual void CalculateLongestCommonSubsequence(SimpleMemory memory)
        {
            FillTable(memory);
            Traceback(memory);
        }

        /// <summary>
        /// Calculates the longest common subsequence of two strings.
        /// </summary>
        /// <param name="inputOne">The first string to compare.</param>
        /// <param name="inputTwo">The second string to compare.</param>
        /// <returns>Returns the longest common subsequence of the two strings.</returns>
        public string CalculateLongestCommonSubsequence(
            string inputOne,
            string inputTwo,
            IHastlayer hastlayer = null,
            IHardwareGenerationConfiguration configuration = null)
        {
            var simpleMemory = CreateSimpleMemory(inputOne, inputTwo, hastlayer, configuration);

            CalculateLongestCommonSubsequence(simpleMemory);

            return GetResult(simpleMemory, inputOne, inputTwo);
        }

        private void FillTable(SimpleMemory memory)
        {
            ushort inputOneLength = (ushort)memory.ReadUInt32(GetLCSInputOneLengthIndex); // This will be the width of the matrix.
            ushort inputTwoLength = (ushort)memory.ReadUInt32(GetLCSInputTwoLengthIndex); // This will be the height of the matrix.

            ushort inputTwoStartIndex = (ushort)(GetLCSInputOneStartIndex + inputOneLength);
            ushort resultStartIndex = (ushort)(inputTwoStartIndex + inputTwoLength);
            ushort resultLength = (ushort)(inputOneLength * inputTwoLength);

            for (ushort row = 0; row < inputTwoLength; row++)
            {
                for (ushort column = 0; column < inputOneLength; column++)
                {
                    ushort position = (ushort)(resultStartIndex + column + (row * inputOneLength));

                    ushort topCell = 0;
                    ushort leftCell = 0;
                    ushort diagonalCell = 0;
                    ushort currentCell = 0;
                    ushort cellPointer = 0;

                    if (row != 0)
                        topCell = (ushort)memory.ReadUInt32(position - inputOneLength);

                    if (column != 0)
                        leftCell = (ushort)memory.ReadUInt32(position - 1);

                    if (column != 0 && row != 0)
                        diagonalCell = (ushort)memory.ReadUInt32(position - inputOneLength - 1);

                    // Increase the value of the diagonal cell if the current elements are the same, and the diagonal cell exists.
                    if (memory.ReadUInt32(GetLCSInputOneStartIndex + column) == memory.ReadUInt32(inputTwoStartIndex + row))
                        diagonalCell++;

                    // Select the maximum of the three cells and set the value of the current cell and pointer.
                    if (diagonalCell > leftCell)
                    {
                        if (diagonalCell > topCell)
                        {
                            currentCell = diagonalCell;

                            cellPointer = row == 0 || column == 0
                                ? GetLCSOutOfBorderDiagonalCellPointerValue
                                : GetLCSDiagonalCellPointerValue;
                        }
                        else
                        {
                            currentCell = topCell;
                            cellPointer = GetLCSTopCellPointerValue;
                        }
                    }
                    else
                    {
                        if (leftCell > topCell)
                        {
                            currentCell = leftCell;
                            cellPointer = GetLCSLeftCellPointerValue;
                        }
                        else
                        {
                            currentCell = topCell;
                            cellPointer = GetLCSTopCellPointerValue;
                        }
                    }

                    memory.WriteUInt32(position, currentCell);
                    memory.WriteUInt32(position + resultLength, cellPointer);
                }
            }
        }

        private void Traceback(SimpleMemory memory)
        {
            ushort inputOneLength = (ushort)memory.ReadUInt32(GetLCSInputOneLengthIndex);
            ushort inputTwoLength = (ushort)memory.ReadUInt32(GetLCSInputTwoLengthIndex);

            ushort inputTwoStartIndex = (ushort)(GetLCSInputOneStartIndex + inputOneLength);
            ushort resultStartIndex = (ushort)(inputTwoStartIndex + inputTwoLength);

            var resultLength = inputOneLength * inputTwoLength;

            ushort currentPosition = (ushort)(resultStartIndex + resultLength - 1);
            ushort currentCell = (ushort)memory.ReadUInt32(currentPosition);
            ushort previousPosition = 0;
            ushort previousCell = 0;
            ushort pointer = 0;
            short column = (short)inputOneLength;
            short row = (short)inputTwoLength;

            while (column >= 0 && row >= 0 && currentCell > 0)
            {
                // These are necessary to avoid infinite while loops.
                if (column == 0)
                    column--;
                if (row == 0)
                    row--;

                // Get the pointer and the cell value from the pointers position.
                pointer = (ushort)memory.ReadUInt32(currentPosition + resultLength);

                if (pointer == GetLCSDiagonalCellPointerValue)
                {
                    previousPosition = (ushort)(currentPosition - inputOneLength - 1);
                    column--;
                    row--;
                }
                else if (pointer == GetLCSLeftCellPointerValue)
                {
                    previousPosition = (ushort)(currentPosition - 1);
                    column--;
                }
                else if (pointer == GetLCSTopCellPointerValue)
                {
                    previousPosition = (ushort)(currentPosition - inputOneLength);
                    row--;
                }
                else if (pointer == GetLCSOutOfBorderDiagonalCellPointerValue)
                {
                    column--;
                    row--;
                }

                if (previousPosition >= resultStartIndex)
                    previousCell = (ushort)memory.ReadUInt32(previousPosition);

                // Add the current character to the result if the pointer is diagonal and the cell value decreased.
                if (pointer == GetLCSDiagonalCellPointerValue && (currentCell == previousCell + 1 || previousPosition < resultStartIndex))
                {
                    var originalValue = memory.ReadUInt32(GetLCSInputOneStartIndex + column);
                    memory.WriteUInt32(resultStartIndex + (2 * resultLength) + column, originalValue);
                }
                else if (pointer == GetLCSOutOfBorderDiagonalCellPointerValue)
                {
                    var originalValue = memory.ReadUInt32(GetLCSInputOneStartIndex + column);
                    memory.WriteUInt32(resultStartIndex + (2 * resultLength) + column, originalValue);
                }

                currentCell = previousCell;
                currentPosition = previousPosition;
            }
        }

        /// <summary>
        /// Creates a <see cref="SimpleMemory"/> object filled with the input values.
        /// </summary>
        /// <param name="inputOne">The first string to compare.</param>
        /// <param name="inputTwo">The second string to compare.</param>
        /// <returns>Returns a <see cref="SimpleMemory"/> object containing the input values.</returns>
        private SimpleMemory CreateSimpleMemory(
            string inputOne,
            string inputTwo,
            IHastlayer hastlayer = null,
            IHardwareGenerationConfiguration configuration = null)
        {
            var cellCount = 2 +
                inputOne.Length +
                inputTwo.Length +
                (inputOne.Length * inputTwo.Length * 2) +
                Math.Max(inputOne.Length, inputTwo.Length);

            var simpleMemory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(cellCount)
                : hastlayer.CreateMemory(configuration, cellCount);

            simpleMemory.WriteUInt32(GetLCSInputOneLengthIndex, (uint)inputOne.Length);
            simpleMemory.WriteUInt32(GetLCSInputTwoLengthIndex, (uint)inputTwo.Length);

            for (int i = 0; i < inputOne.Length; i++)
            {
                simpleMemory.WriteUInt32(GetLCSInputOneStartIndex + i, Encoding.UTF8.GetBytes(inputOne[i].ToString())[0]);
            }

            for (int i = 0; i < inputTwo.Length; i++)
            {
                simpleMemory.WriteUInt32(GetLCSInputOneStartIndex + i + inputOne.Length, Encoding.UTF8.GetBytes(inputTwo[i].ToString())[0]);
            }

            return simpleMemory;
        }

        /// <summary>
        /// Extracts the longest common subsequence from the <see cref="SimpleMemory"/> object.
        /// </summary>
        /// <param name="simpleMemory">The <see cref="SimpleMemory"/> object that contains the result.</param>
        /// <param name="inputOne">The first string to compare.</param>
        /// <param name="inputTwo">The second string to compare.</param>
        /// <returns>Returns the longest common subsequence.</returns>
        private string GetResult(SimpleMemory simpleMemory, string inputOne, string inputTwo)
        {
            var maxInputLength = Math.Max(inputOne.Length, inputTwo.Length);

            var result = string.Empty;
            var startIndex = GetLCSInputOneStartIndex + inputOne.Length + inputTwo.Length + (inputOne.Length * inputTwo.Length * 2);

            for (int i = 0; i < maxInputLength; i++)
            {
                var currentChar = simpleMemory.ReadUInt32(startIndex + i);
                var currentCharBytes = BitConverter.GetBytes(currentChar);
                var chars = Encoding.UTF8.GetChars(currentCharBytes);

                result += chars[0];
            }

            return result.Replace("\0", string.Empty);
        }
    }
}
