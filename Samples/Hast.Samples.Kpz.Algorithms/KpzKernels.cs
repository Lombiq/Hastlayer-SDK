using Hast.Algorithms.Random;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Samples.Kpz.Algorithms
{
    /// <summary>
    /// This is an implementation of the KPZ algorithm for FPGAs through Hastlayer, storing the whole table in the BRAM
    /// or LUT RAM of the FPGA, thus it can only handle small table sizes.
    /// <see cref="KpzKernels"/> contains the internal functions and constants to be ran on the FPGA.
    /// </summary>
    public class KpzKernels
    {
        // ==== <CONFIGURABLE PARAMETERS> ====
        // GridWidth and GridHeight should be 2^n
        public const int GridWidth = 8;
        public const int GridHeight = 8;
        // The probability of turning a pyramid into a hole (IntegerProbabilityP),
        // or a hole into a pyramid (IntegerProbabilityQ).
        public const uint IntegerProbabilityP = 32_767;

        // The probability of turning a pyramid into a hole (IntegerProbabilityP),
        // or a hole into a pyramid (IntegerProbabilityQ).
        public const uint IntegerProbabilityQ = 32_767;
        // ==== </CONFIGURABLE PARAMETERS> ====

        public const int MemIndexNumberOfIterations = 0;
        public const int MemIndexStepMode = 1;
        public const int MemIndexRandomStates = 2;
        public const int MemIndexGrid = 6;
        public const int SizeOfSimpleMemory = (GridWidth * GridHeight) + 6;

        private readonly uint[] _gridRaw = new uint[GridWidth * GridHeight];

        public RandomMwc64X Random1 { get; set; }
        public RandomMwc64X Random2 { get; set; }
        public bool TestMode { get; set; }
        public uint NumberOfIterations { get; set; } = 1;

        /// <summary>
        /// It loads the TestMode, NumberOfIterations parameters and also the PRNG seed from the SimpleMemory at
        /// the beginning.
        /// </summary>
        public void InitializeParametersFromMemory(SimpleMemory memory)
        {
            Random1 = new RandomMwc64X
            {
                State =
                    ((ulong)memory.ReadUInt32(MemIndexRandomStates) << 32) | memory.ReadUInt32(MemIndexRandomStates + 1),
            };
            Random2 = new RandomMwc64X
            {
                State =
                    ((ulong)memory.ReadUInt32(MemIndexRandomStates + 2) << 32) | memory.ReadUInt32(MemIndexRandomStates + 3),
            };
            TestMode = (memory.ReadUInt32(MemIndexStepMode) & 1) == 1;
            NumberOfIterations = memory.ReadUInt32(MemIndexNumberOfIterations);
        }

        /// <summary>
        /// Copies the grid data from BRAM/LUT RAM to DDR.
        /// </summary>
        public void CopyToSimpleMemoryFromRawGrid(SimpleMemory memory)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    int index = (y * GridWidth) + x;
                    memory.WriteUInt32(MemIndexGrid + index, _gridRaw[index]);
                }
            }
        }

        /// <summary>
        /// Copies the grid data to BRAM/LUT RAM from DDR.
        /// </summary>
        public void CopyFromSimpleMemoryToRawGrid(SimpleMemory memory)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    int index = (y * GridWidth) + x;
                    _gridRaw[index] = memory.ReadUInt32(MemIndexGrid + index);
                }
            }
        }

        /// <summary>
        /// Detects pyramid or hole (if any) at the given coordinates in the grid, and randomly switches between pyramid
        /// and hole, based on probabilityP and probabilityQ parameters (or switches anyway, if forceSwitch is on).
        /// </summary>
        public void RandomlySwitchFourCells(bool forceSwitch)
        {
            uint randomNumber1 = Random1.NextUInt32();
            var centerX = (int)(randomNumber1 & (GridWidth - 1));
            var centerY = (int)((randomNumber1 >> 16) & (GridHeight - 1));
            int centerIndex = GetIndexFromXY(centerX, centerY);
            uint randomNumber2 = Random2.NextUInt32();
            uint randomVariable1 = randomNumber2 & ((1 << 16) - 1);
            uint randomVariable2 = (randomNumber2 >> 16) & ((1 << 16) - 1);
            int rightNeighbourIndex;
            int bottomNeighbourIndex;
            // Get neighbor indexes:
            int rightNeighbourX = centerX < GridWidth - 1 ? centerX + 1 : 0;
            int rightNeighbourY = centerY;
            int bottomNeighbourX = centerX;
            int bottomNeighbourY = centerY < GridHeight - 1 ? centerY + 1 : 0;
            rightNeighbourIndex = (rightNeighbourY * GridWidth) + rightNeighbourX;
            bottomNeighbourIndex = (bottomNeighbourY * GridWidth) + bottomNeighbourX;
            // We check our own {dx,dy} values, and the right neighbor's dx, and bottom neighbor's dx.
            if (
                // If we get the pattern {01, 01} we have a pyramid:
                (GetGridDx(centerIndex) && !GetGridDx(rightNeighbourIndex) && GetGridDy(centerIndex) && !GetGridDy(bottomNeighbourIndex) && (forceSwitch || randomVariable1 < IntegerProbabilityP)) ||
                // If we get the pattern {10, 10} we have a hole:
                (!GetGridDx(centerIndex) && GetGridDx(rightNeighbourIndex) && !GetGridDy(centerIndex) && GetGridDy(bottomNeighbourIndex) && (forceSwitch || randomVariable2 < IntegerProbabilityQ))
            )
            {
                // We make a hole into a pyramid, and a pyramid into a hole.
                SetGridDx(centerIndex, !GetGridDx(centerIndex));
                SetGridDy(centerIndex, !GetGridDy(centerIndex));
                SetGridDx(rightNeighbourIndex, !GetGridDx(rightNeighbourIndex));
                SetGridDy(bottomNeighbourIndex, !GetGridDy(bottomNeighbourIndex));
            }
        }

        /// <summary>
        /// In SimpleMemory, the <see cref="KpzNode"/> items are stored as serialized into 32-bit values.
        /// This function returns the dx value of the <see cref="KpzNode"/> from its serialized form.
        /// </summary>
        private bool GetGridDx(int index) => (_gridRaw[index] & 1) > 0;

        /// <summary>
        /// In SimpleMemory, the <see cref="KpzNode"/> items are stored as serialized into 32-bit values.
        /// This function returns the dy value of the <see cref="KpzNode"/> from its serialized form.
        /// </summary>
        private bool GetGridDy(int index) => (_gridRaw[index] & 2) > 0;

        /// <summary>
        /// In SimpleMemory, the <see cref="KpzNode"/> items are stored as serialized into 32-bit values.
        /// This function sets the dx value of the <see cref="KpzNode"/> in its serialized form.
        /// </summary>
        private void SetGridDx(int index, bool value) => _gridRaw[index] = (_gridRaw[index] & ~1U) | (value ? 1U : 0);

        /// <summary>
        /// In SimpleMemory, the <see cref="KpzNode"/> items are stored as serialized into 32-bit values.
        /// This function sets the dy value of the <see cref="KpzNode"/> in its serialized form.
        /// </summary>
        private void SetGridDy(int index, bool value) => _gridRaw[index] = (_gridRaw[index] & ~2U) | (value ? 2U : 0);

        /// <summary>
        /// It calculates the index offset inside the SimpleMemory for a given item based on the 2D coordinates for the
        /// item's place in the grid.
        /// </summary>
        private static int GetIndexFromXY(int x, int y) => x + (y * GridWidth);
    }
}
