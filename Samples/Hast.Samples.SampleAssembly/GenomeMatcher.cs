using System;
using System.Text;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Samples.SampleAssembly
{
    /// <summary>
    /// Algorithm for running Smith-Waterman Genome Matcher. Also see <see cref="GenomeMatcherSampleRunner"/> on what 
    /// to configure to make this work.
    /// 
    /// NOTE: this sample is not parallelized and thus not really suitable for Hastlayer. We'll rework it in the future.
    /// </summary>
    public class GenomeMatcher
    {
        public const int GetLCS_InputOneLengthIndex = 0;
        public const int GetLCS_InputTwoLengthIndex = 1;
        public const int GetLCS_InputOneStartIndex = 2;
        public const ushort GetLCS_TopCellPointerValue = 0;
        public const ushort GetLCS_LeftCellPointerValue = 1;
        public const ushort GetLCS_DiagonalCellPointerValue = 2;
        public const ushort GetLCS_OutOfBorderDiagonalCellPointerValue = 3;


        /// <summary>
        /// Calculates the longest common subsequence of two byte arrays with the Smith-Waterman algorithm.
        /// </summary>
        /// <param name="memory">The <see cref="SimpleMemory"/> object representing the accessible memory space.</param>
        public virtual void CalculateLongestCommonSubsequence(SimpleMemory memory)
        {
            FillTable(memory);
            Traceback(memory);
        }


        private void FillTable(SimpleMemory memory)
        {
            ushort inputOneLength = (ushort)memory.ReadUInt32(GetLCS_InputOneLengthIndex); // This will be the width of the matrix.
            ushort inputTwoLength = (ushort)memory.ReadUInt32(GetLCS_InputTwoLengthIndex); // This will be the height of the matrix.

            ushort inputTwoStartIndex = (ushort)(GetLCS_InputOneStartIndex + inputOneLength);
            ushort resultStartIndex = (ushort)(inputTwoStartIndex + inputTwoLength);
            ushort resultLength = (ushort)(inputOneLength * inputTwoLength);

            for (ushort row = 0; row < inputTwoLength; row++)
            {
                for (ushort column = 0; column < inputOneLength; column++)
                {
                    ushort position = (ushort)(resultStartIndex + column + row * inputOneLength);

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
                    if (memory.ReadUInt32(GetLCS_InputOneStartIndex + column) == memory.ReadUInt32(inputTwoStartIndex + row))
                        diagonalCell++;

                    // Select the maximum of the three cells and set the value of the current cell and pointer.
                    if (diagonalCell > leftCell)
                    {
                        if (diagonalCell > topCell)
                        {
                            currentCell = diagonalCell;

                            if (row == 0 || column == 0)
                                cellPointer = GetLCS_OutOfBorderDiagonalCellPointerValue;
                            else
                                cellPointer = GetLCS_DiagonalCellPointerValue;
                        }
                        else
                        {
                            currentCell = topCell;
                            cellPointer = GetLCS_TopCellPointerValue;
                        }
                    }
                    else
                    {
                        if (leftCell > topCell)
                        {
                            currentCell = leftCell;
                            cellPointer = GetLCS_LeftCellPointerValue;
                        }
                        else
                        {
                            currentCell = topCell;
                            cellPointer = GetLCS_TopCellPointerValue;
                        }
                    }

                    memory.WriteUInt32(position, currentCell);
                    memory.WriteUInt32(position + resultLength, cellPointer);
                }
            }
        }

        private void Traceback(SimpleMemory memory)
        {
            ushort inputOneLength = (ushort)memory.ReadUInt32(GetLCS_InputOneLengthIndex);
            ushort inputTwoLength = (ushort)memory.ReadUInt32(GetLCS_InputTwoLengthIndex);

            ushort inputTwoStartIndex = (ushort)(GetLCS_InputOneStartIndex + inputOneLength);
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

                if (pointer == GetLCS_DiagonalCellPointerValue)
                {
                    previousPosition = (ushort)(currentPosition - inputOneLength - 1);
                    column--;
                    row--;
                }
                else if (pointer == GetLCS_LeftCellPointerValue)
                {
                    previousPosition = (ushort)(currentPosition - 1);
                    column--;
                }
                else if (pointer == GetLCS_TopCellPointerValue)
                {
                    previousPosition = (ushort)(currentPosition - inputOneLength);
                    row--;
                }
                else if (pointer == GetLCS_OutOfBorderDiagonalCellPointerValue)
                {
                    column--;
                    row--;
                }

                if (previousPosition >= resultStartIndex)
                    previousCell = (ushort)memory.ReadUInt32(previousPosition);

                // Add the current character to the result if the pointer is diagonal and the cell value decreased.
                if (pointer == GetLCS_DiagonalCellPointerValue && (currentCell == previousCell + 1 || previousPosition < resultStartIndex))
                {
                    var originalValue = memory.ReadUInt32(GetLCS_InputOneStartIndex + column);
                    memory.WriteUInt32(resultStartIndex + 2 * resultLength + column, originalValue);
                }
                else if (pointer == GetLCS_OutOfBorderDiagonalCellPointerValue)
                {
                    var originalValue = memory.ReadUInt32(GetLCS_InputOneStartIndex + column);
                    memory.WriteUInt32(resultStartIndex + 2 * resultLength + column, originalValue);
                }

                currentCell = previousCell;
                currentPosition = previousPosition;
            }
        }
    }


    public static class GenomeMatcherExtensions
    {
        /// <summary>
        /// Calculates the longest common subsequence of two strings.
        /// </summary>
        /// <param name="inputOne">The first string to compare.</param>
        /// <param name="inputTwo">The second string to compare.</param>
        /// <returns>Returns the longest common subsequence of the two strings.</returns>
        public static string CalculateLongestCommonSubsequence(this GenomeMatcher genomeMatcher, string inputOne, string inputTwo)
        {
            var simpleMemory = CreateSimpleMemory(inputOne, inputTwo);

            genomeMatcher.CalculateLongestCommonSubsequence(simpleMemory);

            return GetResult(simpleMemory, inputOne, inputTwo);
        }


        /// <summary>
        /// Creates a <see cref="SimpleMemory"/> object filled with the input values.
        /// </summary>
        /// <param name="inputOne">The first string to compare.</param>
        /// <param name="inputTwo">The second string to compare.</param>
        /// <returns>Returns a <see cref="SimpleMemory"/> object containing the input values.</returns>
        private static SimpleMemory CreateSimpleMemory(string inputOne, string inputTwo)
        {
            var cellCount = 2 + inputOne.Length + inputTwo.Length + (inputOne.Length * inputTwo.Length) * 2 + Math.Max(inputOne.Length, inputTwo.Length);

            var simpleMemory = new SimpleMemory(cellCount);

            simpleMemory.WriteUInt32(GenomeMatcher.GetLCS_InputOneLengthIndex, (uint)inputOne.Length);
            simpleMemory.WriteUInt32(GenomeMatcher.GetLCS_InputTwoLengthIndex, (uint)inputTwo.Length);

            for (int i = 0; i < inputOne.Length; i++)
            {
                simpleMemory.WriteUInt32(GenomeMatcher.GetLCS_InputOneStartIndex + i, Encoding.UTF8.GetBytes(inputOne[i].ToString())[0]);
            }

            for (int i = 0; i < inputTwo.Length; i++)
            {
                simpleMemory.WriteUInt32(GenomeMatcher.GetLCS_InputOneStartIndex + i + inputOne.Length, Encoding.UTF8.GetBytes(inputTwo[i].ToString())[0]);
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
        private static string GetResult(SimpleMemory simpleMemory, string inputOne, string inputTwo)
        {
            var maxInputLength = Math.Max(inputOne.Length, inputTwo.Length);

            var result = "";
            var startIndex = GenomeMatcher.GetLCS_InputOneStartIndex + inputOne.Length + inputTwo.Length + (inputOne.Length * inputTwo.Length) * 2;

            for (int i = 0; i < maxInputLength; i++)
            {
                var currentChar = simpleMemory.ReadUInt32(startIndex + i);
                var currentCharBytes = BitConverter.GetBytes(currentChar);
                var chars = Encoding.UTF8.GetChars(currentCharBytes);

                result += chars[0];
            }

            return result.Replace("\0", "");
        }
    }
}
