using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Samples.Kpz
{

    /// <summary>
    /// This is an implementation of the KPZ algorithm for FPGAs through Hastlayer, storing the whole table in the BRAM
    /// or LUT RAM of the FPGA, thus it can only handle small table sizes.
    /// <see cref="KpzKernelsInterface"/> contains the entry points for the algorithms to be ran on the FPGA.
    /// </summary>
    public class KpzKernelsInterface
    {
        /// <summary>
        /// Calling this function on the host starts the KPZ algorithm.
        /// </summary>
        public virtual void DoIterations(SimpleMemory memory)
        {
            KpzKernels kernels = new KpzKernels();
            kernels.CopyFromSimpleMemoryToRawGrid(memory);
            kernels.InitializeParametersFromMemory(memory);
            //assume that GridWidth and GridHeight are 2^N
            var numberOfStepsInIteration = kernels.TestMode ? 1 : KpzKernels.GridWidth * KpzKernels.GridHeight;

            for (int j = 0; j < kernels.NumberOfIterations; j++)
            {
                for (int i = 0; i < numberOfStepsInIteration; i++)
                {
                    // We randomly choose a point on the grid. If there is a pyramid or hole, we randomly switch them.
                    kernels.RandomlySwitchFourCells(kernels.TestMode);
                }
            }
            kernels.CopyToSimpleMemoryFromRawGrid(memory);
        }

        /// <summary>
        /// This function is for testing how Hastlayer works by running a simple add operation between memory cells
        /// 0 and 1, and writing the result to cell 2.
        /// </summary>
        public virtual void TestAdd(SimpleMemory memory)
        {
            memory.WriteUInt32(2, memory.ReadUInt32(0) + memory.ReadUInt32(1));
        }

        /// <summary>
        /// This function is for testing how Hastlayer works by running a random generator, writing the results into
        /// SimpleMemory.
        /// </summary>
        public void TestPrng(SimpleMemory memory)
        {
            KpzKernels kernels = new KpzKernels();
            kernels.InitializeParametersFromMemory(memory);
            var numberOfStepsInIteration = KpzKernels.GridWidth * KpzKernels.GridHeight;
            for (int i = 0; i < numberOfStepsInIteration; i++)
            {
                memory.WriteUInt32(i, kernels.GetNextRandom1());
            }
        }
    }

    /// <summary>
    /// This is an implementation of the KPZ algorithm for FPGAs through Hastlayer, storing the whole table in the BRAM
    /// or LUT RAM of the FPGA, thus it can only handle small table sizes.
    /// <see cref="KpzKernels"/> contains the internal functions and constants to be ran on the FPGA.
    /// </summary>
    public class KpzKernels
    {
        // ==== <CONFIGURABLE PARAMETERS> ====
        //GridWidth and GridHeight should be 2^n
        public const int GridWidth = 8;
        public const int GridHeight = 8;
        //The probability of turning a pyramid into a hole (integerProbabilityP),
        //or a hole into a pyramid (integerProbabilityQ).
        uint integerProbabilityP = 32767, integerProbabilityQ = 32767;
        // ==== </CONFIGURABLE PARAMETERS> ====

        uint[] gridRaw = new uint[GridWidth * GridHeight];
        public bool TestMode = false;
        public uint NumberOfIterations = 1;

        //ulong randomState1 = 7215152093156152310UL; //random seed
        //ulong randomState2 = 8322404672673255311UL; //random seed
        ulong randomState1, randomState2;

        public const int MemIndexNumberOfIterations = 0;
        public const int MemIndexStepMode = 1;
        public const int MemIndexRandomStates = 2;
        public const int MemIndexGrid = 6;
        public const int SizeOfSimpleMemory = GridWidth * GridHeight + 6;

        /// <summary>
        /// It loads the TestMode, NumberOfIterations parameters and also the PRNG seed from the SimpleMemory at
        /// the beginning.
        /// </summary>
        /// <param name="memory"></param>
        public void InitializeParametersFromMemory(SimpleMemory memory)
        {
            randomState1 = (((ulong)memory.ReadUInt32(MemIndexRandomStates)) << 32) |
                memory.ReadUInt32(MemIndexRandomStates+1);
            randomState2 = (((ulong)memory.ReadUInt32(MemIndexRandomStates+2)) << 32) |
                memory.ReadUInt32(MemIndexRandomStates+3);
            TestMode = (memory.ReadUInt32(MemIndexStepMode) & 1) == 1;
            NumberOfIterations = memory.ReadUInt32(MemIndexNumberOfIterations);
        }

        public uint GetNextRandom1()
        {
            uint c = (uint)(randomState1 >> 32);
            uint x = (uint)(randomState1 & 0xFFFFFFFFUL);
            randomState1 = x * ((ulong)4294883355UL) + c;
            return x ^ c;
        }

        public uint GetNextRandom2()
        {
            uint c = (uint)(randomState2 >> 32);
            uint x = (uint)(randomState2 & 0xFFFFFFFFUL);
            randomState2 = x * ((ulong)4294883355UL) + c;
            return x ^ c;
        }

        /// <summary>
        /// It calculates the index offset inside the SimpleMemory for a given item based on the 2D coordinates for 
        /// the item's place in the grid.
        /// </summary>
        private int getIndexFromXY(int x, int y)
        {
            return x + y * GridWidth;
        }

        /// <summary>
        /// In SimpleMemory, the <see cref="KpzNode"/> items are stored as serialized into 32-bit values.
        /// This function returns the dx value of the <see cref="KpzNode"/> from its serialized form.
        /// </summary>
        private bool getGridDx(int index)
        {
            return (gridRaw[index] & 1) > 0;
        }

        /// <summary>
        /// In SimpleMemory, the <see cref="KpzNode"/> items are stored as serialized into 32-bit values.
        /// This function returns the dy value of the <see cref="KpzNode"/> from its serialized form.
        /// </summary>
        private bool getGridDy(int index)
        {
            return (gridRaw[index] & 2) > 0;
        }

        /// <summary>
        /// In SimpleMemory, the <see cref="KpzNode"/> items are stored as serialized into 32-bit values.
        /// This function sets the dx value of the <see cref="KpzNode"/> in its serialized form.
        /// </summary>
        private void setGridDx(int index, bool value)
        {
            gridRaw[index] = (gridRaw[index] & ~1U) | (value ? 1U : 0);
        }

        /// <summary>
        /// In SimpleMemory, the <see cref="KpzNode"/> items are stored as serialized into 32-bit values.
        /// This function sets the dy value of the <see cref="KpzNode"/> in its serialized form.
        /// </summary>
        private void setGridDy(int index, bool value)
        {
            gridRaw[index] = (gridRaw[index] & ~2U) | (value ? 2U : 0);
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
                    int index = y * GridWidth + x;
                    memory.WriteUInt32(MemIndexGrid + index, gridRaw[index]);
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
                    int index = y * GridWidth + x;
                    gridRaw[index] = memory.ReadUInt32(MemIndexGrid+index);
                }
            }
        }
        /// Detects pyramid or hole (if any) at the given coordinates in the <see cref="grid" />, and randomly switches
        /// between pyramid and hole, based on <see cref="probabilityP" /> and <see cref="probabilityQ" /> parameters
        /// (or swithes anyway, if forceSwitch is on).
        /// </summary>
        public void RandomlySwitchFourCells(bool forceSwitch)
        {
            var randomNumber1 = GetNextRandom1();
            var centerX = (int)(randomNumber1 & (GridWidth - 1));
            var centerY = (int)((randomNumber1 >> 16) & (GridHeight - 1));
            int centerIndex = getIndexFromXY(centerX, centerY);
            uint randomNumber2 = GetNextRandom2();
            uint randomVariable1 = randomNumber2 & ((1 << 16) - 1);
            uint randomVariable2 = (randomNumber2 >> 16) & ((1 << 16) - 1);
            int rightNeighbourIndex;
            int bottomNeighbourIndex;
            //get neighbour indexes:
            int rightNeighbourX = (centerX < GridWidth - 1) ? centerX + 1 : 0;
            int rightNeighbourY = centerY;
            int bottomNeighbourX = centerX;
            int bottomNeighbourY = (centerY < GridHeight - 1) ? centerY + 1 : 0;
            rightNeighbourIndex = rightNeighbourY * GridWidth + rightNeighbourX;
            bottomNeighbourIndex = bottomNeighbourY * GridWidth + bottomNeighbourX;
            // We check our own {dx,dy} values, and the right neighbour's dx, and bottom neighbour's dx.
            if (
                // If we get the pattern {01, 01} we have a pyramid:
                ((getGridDx(centerIndex) && !getGridDx(rightNeighbourIndex)) &&
                (getGridDy(centerIndex) && !getGridDy(bottomNeighbourIndex)) &&
                (forceSwitch || randomVariable1 < integerProbabilityP)) ||
                // If we get the pattern {10, 10} we have a hole:
                ((!getGridDx(centerIndex) && getGridDx(rightNeighbourIndex)) && 
                (!getGridDy(centerIndex) && getGridDy(bottomNeighbourIndex)) &&
                (forceSwitch || randomVariable2 < integerProbabilityQ))
            )
            {
                // We make a hole into a pyramid, and a pyramid into a hole.
                setGridDx(centerIndex, !getGridDx(centerIndex));
                setGridDy(centerIndex, !getGridDy(centerIndex));
                setGridDx(rightNeighbourIndex, !getGridDx(rightNeighbourIndex));
                setGridDy(bottomNeighbourIndex, !getGridDy(bottomNeighbourIndex));
            }
        }
    }

    /// <summary>
    /// These are host-side helper functions for <see cref="KpzKernels"/>.
    /// </summary>
    public static class KpzKernelsExtensions
    {

        /// <summary>
        /// This function adds two numbers on the FPGA using <see cref="KpzKernelsInterface.TestAdd(SimpleMemory)"/>.
        /// </summary>
        public static uint TestAddWrapper(this KpzKernelsInterface kernels, uint a, uint b)
        {
            SimpleMemory sm = new SimpleMemory(3);
            sm.WriteUInt32(0, a);
            sm.WriteUInt32(1, b);
            kernels.TestAdd(sm);
            return sm.ReadUInt32(2);
        }

        /// <summary>
        /// This function generates random numbers on the FPGA using 
        /// <see cref="KpzKernelsInterface.TestPrng(SimpleMemory)"/>.
        /// </summary>
        public static uint[] TestPrngWrapper(this KpzKernelsInterface kernels)
        {
            uint[] numbers = new uint[KpzKernels.GridWidth*KpzKernels.GridHeight];
            SimpleMemory sm = new SimpleMemory(KpzKernels.SizeOfSimpleMemory);

            CopyParametersToMemory(sm, false, 0x5289a3b89ac5f211, 0x5289a3b89ac5f211, 0);

            kernels.TestPrng(sm);

            for (int i = 0; i < KpzKernels.GridWidth*KpzKernels.GridHeight; i++)
            {
                numbers[i] = sm.ReadUInt32(i);
            }

            return numbers;
        }

        /// <summary>
        /// This function pushes parameters and PRNG seed to the FPGA.
        /// </summary>
        /// <param name="memoryDst"></param>
        /// <param name="testMode"></param>
        /// <param name="randomSeed1"></param>
        /// <param name="randomSeed2"></param>
        /// <param name="numberOfIterations"></param>
        public static void CopyParametersToMemory(SimpleMemory memoryDst, bool testMode, ulong randomSeed1, 
            ulong randomSeed2, uint numberOfIterations)
        {
            memoryDst.WriteUInt32(KpzKernels.MemIndexRandomStates, (uint)(randomSeed1&0xFFFFFFFFUL));
            memoryDst.WriteUInt32(KpzKernels.MemIndexRandomStates+1, (uint)((randomSeed1>>32)&0xFFFFFFFFUL));
            memoryDst.WriteUInt32(KpzKernels.MemIndexRandomStates+2, (uint)(randomSeed2&0xFFFFFFFFUL));
            memoryDst.WriteUInt32(KpzKernels.MemIndexRandomStates+3, (uint)((randomSeed2>>32)&0xFFFFFFFFUL));
            memoryDst.WriteUInt32(KpzKernels.MemIndexStepMode, (testMode)?1U:0U);
            memoryDst.WriteUInt32(KpzKernels.MemIndexNumberOfIterations, numberOfIterations);
        }

        /// <summary>
        /// This is a wrapper for running the KPZ algorithm on the FPGA.
        /// </summary>
        /// <param name="kernels"></param>
        /// <param name="hostGrid">
        ///     This is the grid of initial <see cref="KpzNode"/> items for the algorithm to work on.
        /// </param>
        /// <param name="pushToFpga">
        ///     If this parameter is false, the FPGA will work on the grid currently available in it, 
        ///     instead of the grid in the <see cref="hostGrid"/> parameter.
        /// </param>
        /// <param name="testMode"> 
        ///     does several things: 
        ///     <list type="bullet">
        ///         <item>
        ///             if it is true, <see cref="KpzKernels.RandomlySwitchFourCells(bool)"/> always switches the cells
        ///             if it finds an adequate place,
        ///         </item>
        ///         <item>
        ///             it also does only a single poke, then sends the grid back to the host so that the algorithm 
        ///             can be analyzed in the step-by-step window. 
        ///         </item>
        ///     </list>
        /// </param>
        /// <param name="randomSeed1">is a random seed for the algorithm.</param>
        /// <param name="randomSeed2">is a random seed for the algorithm.</param>
        /// <param name="numberOfIterations">is the number of iterations to perform.</param>
        public static void DoIterationsWrapper(this KpzKernelsInterface kernels, KpzNode[,] hostGrid, bool pushToFpga, 
            bool testMode, ulong randomSeed1, ulong randomSeed2, uint numberOfIterations)
        {
            SimpleMemory sm = new SimpleMemory(KpzKernels.SizeOfSimpleMemory);
            if (pushToFpga)
            {
                CopyParametersToMemory(sm, testMode, randomSeed1, randomSeed2, numberOfIterations);
                CopyFromGridToSimpleMemory(hostGrid, sm);
            }
            kernels.DoIterations(sm);
            CopyFromSimpleMemoryToGrid(hostGrid, sm);
        }


        /// <summary>Push table into FPGA.</summary>
        public static void CopyFromGridToSimpleMemory(KpzNode[,] gridSrc, SimpleMemory memoryDst)
        {
            for (int x = 0; x < KpzKernels.GridHeight; x++)
            {
                for (int y = 0; y < KpzKernels.GridWidth; y++)
                {
                    KpzNode node = gridSrc[x, y];
                    memoryDst.WriteUInt32(KpzKernels.MemIndexGrid + y * KpzKernels.GridWidth + x, node.SerializeToUInt32());
                }
            }
        }

        /// <summary>Pull table from the FPGA.</summary>
        public static void CopyFromSimpleMemoryToGrid(KpzNode[,] gridDst, SimpleMemory memorySrc)
        {
            for (int x = 0; x < KpzKernels.GridWidth; x++)
            {
                for (int y = 0; y < KpzKernels.GridHeight; y++)
                {
                    gridDst[x, y] = KpzNode.DeserializeFromUInt32(memorySrc.ReadUInt32(KpzKernels.MemIndexGrid + y * KpzKernels.GridWidth + x));
                }
            }
        }
    }
}
