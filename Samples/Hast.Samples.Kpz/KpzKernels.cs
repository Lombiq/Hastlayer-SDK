using System.Threading.Tasks;
using Hast.Layer;
using Hast.Algorithms;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Samples.Kpz
{

    /*
    public static class KpzNodeExtensions
    {
        public static int width(this KpzNode[,] grid) { return grid.GetLength(0);}
        public static int height(this KpzNode[,] grid) { return grid.GetLength(1); }
    }
    */
    public class KpzKernelsIndexObject
    {
        public bool[] bramDx;
        public bool[] bramDy;
    }

    public class KpzKernelsInterface
    {
        public virtual void ScheduleIterations(SimpleMemory memory)
        {
            //grid méret (ez lehet akár fix, beledrótozva a VHDL kódba)
            //kicsi grid méret
            //  --> ezeknek a hányadosát is
            //  ezeknek (N*N)/(n*n) oszthatóknak is kellene lenniük egymással, hogy egyszerûbb legyen nekünk

            const int GridSize = 4096;
            const int LocalGridSize = 8;
            const int ParallelTasks = 8; //Number of parallel execution engines
            const int NumberOfIterations = 10;
            int TasksPerIteration = (GridSize * GridSize) / (LocalGridSize * LocalGridSize);
            int SchedulesPerIteration = TasksPerIteration / ParallelTasks;
            const float IterationsPerTask = 0.5;
            int IterationGroupSize = NumberOfIterations / IterationsPerTask;
            int PokesInsideTask = LocalGridSize * LocalGridSize * IterationsPerTask;
            int LocalGridPartitions = GridSize / LocalGridSize;

            KpzKernelsIndexObject[] TaskLocals = new KpzKernelsIndexObject[ParallelTasks];
            for (int TaskLocalsIndex = 0; TaskLocalsIndex < ParallelTasks; TaskLocalsIndex++)
            {
                TaskLocals[TaskLocalsIndex].bramDx = new bool[LocalGridSize * LocalGridSize];
                TaskLocals[TaskLocalsIndex].bramDy = new bool[LocalGridSize * LocalGridSize];
            }

            //Miért van szükség az InterationGroupIndex - re ?
            //Az IterationPerTask egy paraméter, ami a levél szerint 0.5 és 1 között kell, hogy legyen.
            //Ha 10 iterációt akarunk, és 1 teljes task sorozat indítása fél iterációt csinál az egész táblán, akkor bizony 20x kell elindítanunk.

            for (int IterationGroupIndex = 0; IterationGroupIndex < IterationGroupSize; IterationGroupIndex++)
            {
                int RandomXOffset = 0, RandomYOffset = 0;
                for (int ScheduleIndex = 0; ScheduleIndex < SchedulesPerIteration; ScheduleIndex++)
                {
                    var tasks = new Task<KpzKernelsIndexObject>[ParallelTasks];
                    for (int ParallelTaskIndex = 0; ParallelTaskIndex < ParallelTasks; ParallelTaskIndex++)
                    {
                        //Decide the X and Y starting coordinates based on ScheduleIndex and ParallelTaskIndex (and the random added value)
                        int LocalGridIndex = ParallelTaskIndex + ScheduleIndex * ParallelTasks;
                        int PartitionX = LocalGridIndex % LocalGridPartitions; //The X and Y coordinate within the small table (local grid)
                        int PartitionY = LocalGridIndex / LocalGridPartitions;
                        int BaseX = PartitionX * LocalGridSize + RandomXOffset; //The X and Y coordinate within the big table (grid)
                        int BaseY = PartitionY * LocalGridSize + RandomYOffset;

                        //Copy to local memory
                        for (int CopyDstX = 0; CopyDstX < LocalGridSize; CopyDstX++)
                        {
                            for (int CopyDstY = 0; CopyDstY < LocalGridSize; CopyDstY++)
                            {
                                int CopySrcX = (BaseX + CopyDstX) % GridSize;
                                int CopySrcY = (BaseY + CopyDstY) % GridSize;
                                uint value = memory.ReadUInt32(CopySrcX + CopySrcY * GridSize);
                                TaskLocals[ParallelTaskIndex].bramDx[CopyDstX + CopyDstY * LocalGridSize] = (value & 1) == 1;
                                TaskLocals[ParallelTaskIndex].bramDy[CopyDstX + CopyDstY * LocalGridSize] = (value & 2) == 2;
                            }
                        }
                        tasks[ParallelTaskIndex] = Task.Factory.StartNew(
                        rawIndexObject =>
                        {
                            //Then do TasksPerIteration iterations
                            KpzKernelsIndexObject TaskLocal = (KpzKernelsIndexObject)rawIndexObject;
                            for (int PokeIndex = 0; PokeIndex < PokesInsideTask; PokeIndex++)
                            {
                                // ==== <Now randomly switch four cells> ====
                                int randomNumber1 = 0; //GetNextRandom1();
                                int pokeCenterX = (int)(randomNumber1 & (LocalGridSize - 1));
                                int pokeCenterY = (int)((randomNumber1 >> 16) & (LocalGridSize - 1));
                                int pokeCenterIndex = pokeCenterX + pokeCenterY * LocalGridSize;
                                uint randomNumber2 = 0; //GetNextRandom2();
                                uint randomVariable1 = randomNumber2 & ((1 << 16) - 1);
                                uint randomVariable2 = (randomNumber2 >> 16) & ((1 << 16) - 1);
                                int rightNeighbourIndex;
                                int bottomNeighbourIndex;
                                //get neighbour indexes:
                                if (pokeCenterX >= LocalGridSize - 1 || pokeCenterY >= LocalGridSize - 1) continue; //We skip if neighbours would fall out of the local grid
                                int rightNeighbourX = pokeCenterX + 1;
                                int rightNeighbourY = pokeCenterY;
                                int bottomNeighbourX = pokeCenterX;
                                int bottomNeighbourY = pokeCenterY + 1;
                                rightNeighbourIndex = rightNeighbourY * LocalGridSize + rightNeighbourX;
                                bottomNeighbourIndex = bottomNeighbourY * LocalGridSize + bottomNeighbourX;
                                // We check our own {dx,dy} values, and the right neighbour's dx, and bottom neighbour's dx.
                                if (
                                    // If we get the pattern {01, 01} we have a pyramid:
                                    ((TaskLocal.bramDx[pokeCenterIndex] && !TaskLocal.bramDx[rightNeighbourIndex]) &&
                                    (TaskLocal.bramDy[pokeCenterIndex] && !TaskLocal.bramDy[bottomNeighbourIndex]) &&
                                    (forceSwitch || randomVariable1 < integerProbabilityP)) ||
                                    // If we get the pattern {10, 10} we have a hole:
                                    ((!TaskLocal.bramDx[pokeCenterIndex] && TaskLocal.bramDx[rightNeighbourIndex]) &&
                                    (!TaskLocal.bramDy[pokeCenterIndex] && TaskLocal.bramDy[bottomNeighbourIndex]) &&
                                    (forceSwitch || randomVariable2 < integerProbabilityQ))
                                )
                                {
                                    // We make a hole into a pyramid, and a pyramid into a hole.
                                    TaskLocal.bramDx[pokeCenterIndex] = !TaskLocal.bramDx[pokeCenterIndex];
                                    TaskLocal.bramDy[pokeCenterIndex] = !TaskLocal.bramDy[pokeCenterIndex];
                                    TaskLocal.bramDx[rightNeighbourIndex] = !TaskLocal.bramDx[rightNeighbourIndex];
                                    TaskLocal.bramDy[bottomNeighbourIndex] = !TaskLocal.bramDy[bottomNeighbourIndex];
                                }
                                // ==== </Now randomly switch four cells> ====
                            }
                            return TaskLocal; //TODO: egyáltalán kell ezt?
                        }, TaskLocals[ParallelTaskIndex]);
                    }

                    Task.WhenAll(tasks).Wait();

                    //Copy back to SimpleMemory
                    for (int ParallelTaskIndex = 0; ParallelTaskIndex < ParallelTasks; ParallelTaskIndex++)
                    {
                        //calculate these things again
                        int LocalGridIndex = ParallelTaskIndex + ScheduleIndex * ParallelTasks;
                        int PartitionX = LocalGridIndex % LocalGridPartitions; //The X and Y coordinate within the small table (local grid)
                        int PartitionY = LocalGridIndex / LocalGridPartitions;
                        int BaseX = PartitionX * LocalGridSize + RandomXOffset; //The X and Y coordinate within the big table (grid)
                        int BaseY = PartitionY * LocalGridSize + RandomYOffset;

                        for (int CopyDstX = 0; CopyDstX < LocalGridSize; CopyDstX++)
                        {
                            for(int CopyDstY = 0; CopyDstY < LocalGridSize; CopyDstY++)
                            {
                                int CopySrcX = (BaseX + CopyDstX) % GridSize;
                                int CopySrcY = (BaseY + CopyDstY) % GridSize;
                                uint value =
                                    (TaskLocals[ParallelTaskIndex].bramDx[CopyDstX + CopyDstY * LocalGridSize] ? 1 : 0) |
                                    (TaskLocals[ParallelTaskIndex].bramDy[CopyDstX + CopyDstY * LocalGridSize] ? 2 : 0);
                                memory.WriteUInt32(CopySrcX + CopySrcY * GridSize, value);
                            }
                        }
                    }
                }

                }
            }
        }

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

        public virtual void TestAdd(SimpleMemory memory)
        {
            memory.WriteUInt32(2, memory.ReadUInt32(0) + memory.ReadUInt32(1));
        }

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

    public class KpzKernels
    {
        public const int GridWidth = 8;
        public const int GridHeight = 8;
        public const int SmallGridSize = 8;
        uint[] gridRaw = new uint[GridWidth * GridHeight];
        uint integerProbabilityP = 32767, integerProbabilityQ = 32767;
        public bool TestMode = false;
        public uint NumberOfIterations = 1;

        //ulong randomState1 = 7215152093156152310UL; //random seed
        //ulong randomState2 = 8322404672673255311UL; //random seed
        ulong randomState1, randomState2;

        public static int CellIndexOfRandomStates()
        {
            return GridWidth * GridHeight;
        }

        public static int CellIndexOfStepMode()
        {
            return GridWidth * GridHeight + 4;
        }

        public static int CellIndexOfNumberOfIterations()
        {
            return GridWidth * GridHeight + 5;
        }

        public static int SizeOfSimpleMemory()
        {
            return GridWidth * GridHeight + 6;
        }

        public void InitializeParametersFromMemory(SimpleMemory memory)
        {
            randomState1 = (((ulong)memory.ReadUInt32(CellIndexOfRandomStates())) << 32) |
                memory.ReadUInt32(CellIndexOfRandomStates()+1);
            randomState2 = (((ulong)memory.ReadUInt32(CellIndexOfRandomStates()+2)) << 32) |
                memory.ReadUInt32(CellIndexOfRandomStates()+3);
            TestMode = (memory.ReadUInt32(CellIndexOfStepMode()) & 1) == 1;
            NumberOfIterations = memory.ReadUInt32(CellIndexOfNumberOfIterations());
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

        private int getIndexFromXY(int x, int y)
        {
            return x + y * GridWidth;
        }

        private bool getGridDx(int index)
        {
            return (gridRaw[index] & 1) > 0;
        }

        private bool getGridDy(int index)
        {
            return (gridRaw[index] & 2) > 0;
        }

        private void setGridDx(int index, bool value)
        {
            gridRaw[index] = (gridRaw[index] & ~1U) | (value ? 1U : 0);
        }

        private void setGridDy(int index, bool value)
        {
            gridRaw[index] = (gridRaw[index] & ~2U) | (value ? 2U : 0);
        }

        public void CopyToSimpleMemoryFromRawGrid(SimpleMemory memory)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    int index = y * GridWidth + x;
                    memory.WriteUInt32(index, gridRaw[index]);
                }
            }
        }

        public void CopyFromSimpleMemoryToRawGrid(SimpleMemory memory)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    int index = y * GridWidth + x;
                    gridRaw[index] = memory.ReadUInt32(index);
                }
            }
        }
        /// Detects pyramid or hole (if any) at the given coordinates in the <see cref="grid" />, and randomly switch
        /// between pyramid and hole, based on <see cref="probabilityP" /> and <see cref="probabilityQ" /> parameters.
        /// </summary>
        /// <param name="p">
        /// contains the coordinates where the function looks if there is a pyramid or hole in the <see cref="grid" />.
        /// </param>
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
                ((getGridDx(centerIndex) && !getGridDx(rightNeighbourIndex)) && (getGridDy(centerIndex) && !getGridDy(bottomNeighbourIndex)) &&
                (forceSwitch || randomVariable1 < integerProbabilityP)) ||
                // If we get the pattern {10, 10} we have a hole:
                ((!getGridDx(centerIndex) && getGridDx(rightNeighbourIndex)) && (!getGridDy(centerIndex) && getGridDy(bottomNeighbourIndex)) &&
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

    public static class KpzKernelsExtensions
    {

        public static uint TestAddWrapper(this KpzKernelsInterface kernels, uint a, uint b)
        {
            SimpleMemory sm = new SimpleMemory(3);
            sm.WriteUInt32(0, a);
            sm.WriteUInt32(1, b);
            kernels.TestAdd(sm);
            return sm.ReadUInt32(2);
        }

        public static uint[] TestPrngWrapper(this KpzKernelsInterface kernels)
        {
            uint[] numbers = new uint[KpzKernels.GridWidth*KpzKernels.GridHeight];
            SimpleMemory sm = new SimpleMemory(KpzKernels.SizeOfSimpleMemory());

            CopyParametersToMemory(sm, false, 0x5289a3b89ac5f211, 0x5289a3b89ac5f211, 0);

            kernels.TestPrng(sm);

            for (int i = 0; i < KpzKernels.GridWidth*KpzKernels.GridHeight; i++)
            {
                numbers[i] = sm.ReadUInt32(i);
            }

            return numbers;
        }

        public static void CopyParametersToMemory(SimpleMemory memoryDst, bool testMode, ulong randomSeed1, ulong randomSeed2, uint numberOfIterations)
        {
            memoryDst.WriteUInt32(KpzKernels.CellIndexOfRandomStates(), (uint)(randomSeed1&0xFFFFFFFFUL));
            memoryDst.WriteUInt32(KpzKernels.CellIndexOfRandomStates()+1, (uint)((randomSeed1>>32)&0xFFFFFFFFUL));
            memoryDst.WriteUInt32(KpzKernels.CellIndexOfRandomStates()+2, (uint)(randomSeed2&0xFFFFFFFFUL));
            memoryDst.WriteUInt32(KpzKernels.CellIndexOfRandomStates()+3, (uint)((randomSeed2>>32)&0xFFFFFFFFUL));
            memoryDst.WriteUInt32(KpzKernels.CellIndexOfStepMode(), (testMode)?1U:0U);
            memoryDst.WriteUInt32(KpzKernels.CellIndexOfNumberOfIterations(), numberOfIterations);
        }

        public static void DoIterationsWrapper(this KpzKernelsInterface kernels, KpzNode[,] hostGrid, bool pushToFpga, bool testMode, ulong randomSeed1, ulong randomSeed2, uint numberOfIterations)
        {
            SimpleMemory sm = new SimpleMemory(KpzKernels.SizeOfSimpleMemory());
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
                    memoryDst.WriteUInt32(y * KpzKernels.GridWidth + x, node.SerializeToUInt32());
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
                    gridDst[x, y] = KpzNode.DeserializeFromUInt32(memorySrc.ReadUInt32(y * KpzKernels.GridWidth + x));
                }
            }
        }
    }
}
