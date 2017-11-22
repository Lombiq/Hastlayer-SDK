
using System.Threading.Tasks;
using Hast.Transformer.Abstractions.SimpleMemory;
using System;
using Hast.Algorithms;

namespace Hast.Samples.Kpz
{
    /// <summary>
    /// It contains the state of each Task instantiated within KpzKernelsG.ScheduleIterations.
    /// </summary>
    public class KpzKernelsTaskState
    {
        public bool[] bramDx;
        public bool[] bramDy;
        public PrngMWC64X prng1;
        public PrngMWC64X prng2;
    }

    //SimpleMemory map:
    // * 0  (1 address)  :
    //      The number of iterations to perform (NumberOfIterations).
    // * 1 .. 1+ParallelTasks*4+2  (ParallelTasks*4+2 addresses)  :
    //      Random seed for PRNGs in each task, and an additional one for generating random grid offsets at scheduler 
    //      level. Each random seed number is 64-bit (2 uints)
    // * 1+ParallelTasks*4+2 .. 1+ParallelTasks*4+2+GridSize^2-1  (GridSize^2 addresses)  :
    //      The input KPZ nodes as 32 bit numbers, with bit 0 as dx and bit 1 as dy.

    /// <summary>
    /// This is an implementation of the KPZ algorithm for FPGAs through Hastlayer, with an architecture
    /// similar to GPUs. It makes use of a given number of Tasks as parallel execution engines 
    /// (see <see cref="ReschedulesPerTaskIteration">).
    /// 
    /// For each iteration:
    /// <list type="bullet">
    /// <item>it loads parts of the grid into local tables (see <see cref="LocalGridSize"/>) within Tasks,</item>
    /// <item>it runs the algorithm on these local tables,</item>
    /// <item>it loads back the local tables into the original grid.</item>
    /// </list>
    /// 
    /// It changes the offset of the local grids within the global grid a given number of times for each iteration 
    /// (see <see cref="ReschedulesPerTaskIteration"/>). 
    /// </summary>
    public class KpzKernelsGInterface
    {
        // ==== <CONFIGURABLE PARAMETERS> ====
        //Full grid width and height.
        public const int GridSize = 64;
        //Local grid width and height. Each Task has a local grid, on which it works. 
        public const int LocalGridSize = 8;
        //Furthermore, for LocalGridSize and GridSize the following expressions should have an integer result:
        //    (GridSize^2)/(LocalGridSize^2)
        //    GridSize/LocalGridSize
        //(Here the ^ operator means power.)
        //Also both GridSize and LocalGridSize should be a power of two.
        //The probability of turning a pyramid into a hole (integerProbabilityP),
        //or a hole into a pyramid (integerProbabilityQ).
        const uint integerProbabilityP = 32767, integerProbabilityQ = 32767;
        //Number of parallel execution engines. (Should be a power of two.) Only 8 will fully fit on the Nexys 4 DDR.
        public const int ParallelTasks = 8; 
        //The number of reschedules (thus global grid offset changing) within one iteration.
        public const int ReschedulesPerTaskIteration = 2; 
        //This should be 1 or 2 (the latter if you want to be very careful). 
        // ==== </CONFIGURABLE PARAMETERS> ====

        //public int MemStartOfRandomValues() { return GridSize * GridSize;  }
        //public int MemStartOfParameters() { return GridSize * GridSize + TasksPerIteration * NumberOfIterations + 1; }
        public const int MemIndexNumberOfIterations = 0;
        public const int MemIndexRandomSeed = MemIndexNumberOfIterations + 1;
        public const int MemIndexGrid = MemIndexRandomSeed + ParallelTasks * 4 + 2;

        public virtual void ScheduleIterations(SimpleMemory memory)
        {
            int NumberOfIterations = memory.ReadInt32(KpzKernelsGInterface.MemIndexNumberOfIterations);
            const int TasksPerIteration = (GridSize * GridSize) / (LocalGridSize * LocalGridSize);
            const int SchedulesPerIteration = TasksPerIteration / ParallelTasks;
            int IterationGroupSize = (int)(NumberOfIterations * ReschedulesPerTaskIteration);
            const int PokesInsideTask = (int)(LocalGridSize * LocalGridSize / ReschedulesPerTaskIteration);
            const int LocalGridPartitions = GridSize / LocalGridSize;
            //const int TotalNumberOfTasks = TasksPerIteration * NumberOfIterations == 
            //  ((GridSize * GridSize) / (LocalGridSize * LocalGridSize)) * NumberOfIterations
            int ParallelTaskRandomIndex = 0;
            uint RandomSeedTemp;
            PrngMWC64X prng0 = new PrngMWC64X();

            KpzKernelsTaskState[] TaskLocals = new KpzKernelsTaskState[ParallelTasks];
            for (int TaskLocalsIndex = 0; TaskLocalsIndex < ParallelTasks; TaskLocalsIndex++)
            {
                TaskLocals[TaskLocalsIndex] = new KpzKernelsTaskState();
                TaskLocals[TaskLocalsIndex].bramDx = new bool[LocalGridSize * LocalGridSize];
                TaskLocals[TaskLocalsIndex].bramDy = new bool[LocalGridSize * LocalGridSize];
                TaskLocals[TaskLocalsIndex].prng1 = new PrngMWC64X();
                TaskLocals[TaskLocalsIndex].prng1.state = 
                    memory.ReadUInt32(MemIndexRandomSeed + ParallelTaskRandomIndex++);
                RandomSeedTemp = memory.ReadUInt32(MemIndexRandomSeed + ParallelTaskRandomIndex++);
                //TaskLocals[TaskLocalsIndex].prng1.state |= ((ulong)RandomSeedTemp) * 4294967296UL;
                TaskLocals[TaskLocalsIndex].prng1.state |= ((ulong)RandomSeedTemp) << 32;

                TaskLocals[TaskLocalsIndex].prng2 = new PrngMWC64X();
                TaskLocals[TaskLocalsIndex].prng2.state = 
                    memory.ReadUInt32(MemIndexRandomSeed + ParallelTaskRandomIndex++);
                RandomSeedTemp = memory.ReadUInt32(MemIndexRandomSeed + ParallelTaskRandomIndex++);
                //TaskLocals[TaskLocalsIndex].prng2.state |= ((ulong)RandomSeedTemp) * 4294967296UL;
                TaskLocals[TaskLocalsIndex].prng2.state |= ((ulong)RandomSeedTemp) << 32;

                //TaskLocals[TaskLocalsIndex].taskRandomState1 = (ulong)0xCAFE;
                //TaskLocals[TaskLocalsIndex].taskRandomState2 = (ulong)0xCAFE;
            }

            //What is IterationGroupIndex good for?
            //IterationPerTask needs to be between 0.5 and 1 based on the e-mail of Mate.
            //If we want 10 iterations, and starting a full series of tasks makes half iteration on the full table,
            //then we need to start it 20 times (thus IterationGroupSize will be 20).

            prng0.state = memory.ReadUInt32(MemIndexRandomSeed + ParallelTaskRandomIndex++);
            RandomSeedTemp = memory.ReadUInt32(MemIndexRandomSeed + ParallelTaskRandomIndex++);
            prng0.state |= ((ulong)RandomSeedTemp) << 32;
            //randomState0 |= ((ulong)RandomSeedTemp) * 4294967296UL;
            //randomState0 = (ulong)0xCAFE;

            for (int IterationGroupIndex = 0; IterationGroupIndex < IterationGroupSize; IterationGroupIndex++)
            {
                //GetNextRandom0
                uint RandomValue0 = prng0.NextUInt32();
                //This supposes that LocalGridSize is 2^N:
                int RandomXOffset = (int)((LocalGridSize - 1) & RandomValue0); 
                int RandomYOffset = (int)((LocalGridSize - 1) & (RandomValue0 >> 16));
                for (int ScheduleIndex = 0; ScheduleIndex < SchedulesPerIteration; ScheduleIndex++)
                {
                    var tasks = new Task<KpzKernelsTaskState>[ParallelTasks];
                    for (int ParallelTaskIndex = 0; ParallelTaskIndex < ParallelTasks; ParallelTaskIndex++)
                    {
                        //Decide the X and Y starting coordinates based on ScheduleIndex and ParallelTaskIndex 
                        //(and the random added value)
                        int LocalGridIndex = ParallelTaskIndex + ScheduleIndex * ParallelTasks;
                        //The X and Y coordinate within the small table (local grid):
                        int PartitionX = LocalGridIndex % LocalGridPartitions; 
                        int PartitionY = LocalGridIndex / LocalGridPartitions;
                        //The X and Y coordinate within the big table (grid):
                        int BaseX = PartitionX * LocalGridSize + RandomXOffset; 
                        int BaseY = PartitionY * LocalGridSize + RandomYOffset;

                        //Console.WriteLine("CopyTo | Task={0}, From: {1},{2}", ParallelTaskIndex, BaseX, BaseY);

                        //Copy to local memory
                        for (int CopyDstX = 0; CopyDstX < LocalGridSize; CopyDstX++)
                        {
                            for (int CopyDstY = 0; CopyDstY < LocalGridSize; CopyDstY++)
                            {
                                //Prevent going out of grid memory area (e.g. reading into random seed):
                                int CopySrcX = (BaseX + CopyDstX) % GridSize;
                                int CopySrcY = (BaseY + CopyDstY) % GridSize; 
                                uint value = memory.ReadUInt32(MemIndexGrid + CopySrcX + CopySrcY * GridSize);
                                TaskLocals[ParallelTaskIndex].bramDx[CopyDstX + CopyDstY * LocalGridSize] = 
                                    (value & 1) == 1;
                                TaskLocals[ParallelTaskIndex].bramDy[CopyDstX + CopyDstY * LocalGridSize] = 
                                    (value & 2) == 2;
                            }
                        }

                        tasks[ParallelTaskIndex] = Task.Factory.StartNew(
                        rawTaskState =>
                        {
                            //Then do TasksPerIteration iterations
                            KpzKernelsTaskState TaskLocal = (KpzKernelsTaskState)rawTaskState;
                            for (int PokeIndex = 0; PokeIndex < PokesInsideTask; PokeIndex++)
                            {
                                // ==== <Now randomly switch four cells> ====

                                //Generating two random numbers:
                                uint taskRandomNumber1 = TaskLocal.prng1.NextUInt32();
                                uint taskRandomNumber2 = TaskLocal.prng2.NextUInt32();

                                //The existstence of var-1 in code is a good indicator of that it is asumed to be 2^N:
                                int pokeCenterX = (int)(taskRandomNumber1 & (LocalGridSize - 1)); 
                                int pokeCenterY = (int)((taskRandomNumber1 >> 16) & (LocalGridSize - 1));
                                int pokeCenterIndex = pokeCenterX + pokeCenterY * LocalGridSize;
                                uint randomVariable1 = taskRandomNumber2 & ((1 << 16) - 1);
                                uint randomVariable2 = (taskRandomNumber2 >> 16) & ((1 << 16) - 1);

                                //get neighbour indexes:
                                int rightNeighbourIndex;
                                int bottomNeighbourIndex;
                                //We skip if neighbours would fall out of the local grid:
                                if (pokeCenterX >= LocalGridSize - 1 || pokeCenterY >= LocalGridSize - 1) continue; 
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
                                    (false || randomVariable1 < integerProbabilityP)) ||
                                    // If we get the pattern {10, 10} we have a hole:
                                    ((!TaskLocal.bramDx[pokeCenterIndex] && TaskLocal.bramDx[rightNeighbourIndex]) &&
                                    (!TaskLocal.bramDy[pokeCenterIndex] && TaskLocal.bramDy[bottomNeighbourIndex]) &&
                                    (false || randomVariable2 < integerProbabilityQ))
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
                            return TaskLocal;
                        }, TaskLocals[ParallelTaskIndex]);
                    }

                    Task.WhenAll(tasks).Wait();

                    //Copy back to SimpleMemory
                    for (int ParallelTaskIndex = 0; ParallelTaskIndex < ParallelTasks; ParallelTaskIndex++)
                    {
                        //calculate these things again
                        int LocalGridIndex = ParallelTaskIndex + ScheduleIndex * ParallelTasks;
                        //The X and Y coordinate within the small table (local grid):
                        int PartitionX = LocalGridIndex % LocalGridPartitions; 
                        int PartitionY = LocalGridIndex / LocalGridPartitions;
                        //The X and Y coordinate within the big table (grid):
                        int BaseX = PartitionX * LocalGridSize + RandomXOffset; 
                        int BaseY = PartitionY * LocalGridSize + RandomYOffset;
                        //Console.WriteLine("CopyBack | Task={0}, To: {1},{2}", ParallelTaskIndex, BaseX, BaseY);

                        for (int CopySrcX = 0; CopySrcX < LocalGridSize; CopySrcX++)
                        {
                            for (int CopySrcY = 0; CopySrcY < LocalGridSize; CopySrcY++)
                            {
                                int CopyDstX = (BaseX + CopySrcX) % GridSize;
                                int CopyDstY = (BaseY + CopySrcY) % GridSize;
                                uint value =
                                    (tasks[ParallelTaskIndex].Result.bramDx[CopySrcX + CopySrcY * LocalGridSize] ? 1U : 0U) |
                                    (tasks[ParallelTaskIndex].Result.bramDy[CopySrcX + CopySrcY * LocalGridSize] ? 2U : 0U);
                                //uint value =
                                //    (TaskLocals[ParallelTaskIndex].bramDx[CopySrcX + CopySrcY * LocalGridSize] ? 1U : 0U) |
                                //    (TaskLocals[ParallelTaskIndex].bramDy[CopySrcX + CopySrcY * LocalGridSize] ? 2U : 0U);
                                //(Either solution to pass TaskLocals does work.)
                                memory.WriteUInt32(MemIndexGrid + CopyDstX + CopyDstY * GridSize, value);
                            }
                        }

                        //Take PRNG current state from Result to feed it to input next time
                        TaskLocals[ParallelTaskIndex].prng1.state = tasks[ParallelTaskIndex].Result.prng1.state;
                        TaskLocals[ParallelTaskIndex].prng2.state = tasks[ParallelTaskIndex].Result.prng2.state;
                    }
                }
            }
        }
    }

    /// <summary>
    /// These are host-side functions for <see cref="KpzKernelsGInterface"/>.
    /// </summary>
    public static class KpzKernelsGExtensions
    {
        /// <summary>
        /// Wrapper for calling <see cref="KpzKernelsG.ScheduleIterations"/>.
        /// </summary>
        /// <param name="hostGrid">The grid that we work on.</param>
        /// <param name="pushToFpga">Force pushing the grid into the FPGA (or work on the grid already there).</param>
        /// <param name="randomSeedEnable">
        /// If it is disabled, preprogrammed random numbers will be written into 
        /// SimpleMemory instead of real random generated numbers. This helps debugging, keeping the output more
        /// consistent across runs.
        /// </param>
        /// <param name="numberOfIterations">The number of iterations to perform.</param>
        public static void DoIterationsWrapper(this KpzKernelsGInterface kernels, KpzNode[,] hostGrid, bool pushToFpga, 
            bool randomSeedEnable, uint numberOfIterations)
        {
            //Use this is random seed is disabled in GUI. This makes the result more preictable while debugging.
            var notRandomSeed = new int[]{
                -2122284207, -805426534, -296351199, 1082586369, -864339821,
                331357875, 1192493543, -851078246, -1091834350, -671234217,
                -1623097030, -100086504, -1516943165, 1569609717, 1695030944,
                -888770401, 341459416, -1970567826, 794279071, 1480098339,
                -2057457019, -257344031, -850635314, -210624876, -678618985,
                -232192458, 2099559718, 1809993314, -43947016, -1478372364,
                -1049983320, -827693764,
                541247270, -762735333, 1201597916, 63792507, 572540375,
                372071952, -1908453570, -79328169, -792331270, -499848108,
                -824609528, -1798425884, 1921971887, 334688140, -1210315495,
                303879804, -854493128, 168364778, 1153057767, 1892111935,
                -1121887497, -411064952, -1153708605, -1236973870, 1909433338,
                840379860, 648328296, 815910809, 1054583403, 641704477,
                -751562304, 1514065758, -1503136866, -290638406, -1465068879,
                -480074650, -1189373896, 1628987870, -1801471129, 1149055452,
                -1213044536, 1501859543, 1766766693, -11506391, 1354826834,
                -1605994989, 1371816845, -1806325888, 899112301, -1972685877,
                -1351549709, 1989386170, -1745931254, -1294330993, -280576358,
                -1135040353, 2064141162, -1550762441, 206482802, -208760219,
                -1763282295, -1559411916, 212239689, -1713858924, 1957674632,
                399597100, -1822066773, -521605668, 442732461, 2139235466,
                1949728360, -1333355612, -1523271090, 1873782401, 109175483,
                1455879393, -1508517739, 22132201, 1503847013, 1121324155,
                -1149836541, -174007501, 1742754517, 514371316, -1438578033,
                605535816, 1415254613, 1944255343, -1057195252, 1981414947,
                -356281736, -589212081, 1146701526, 224674108, 2035824054,
                292251866, 1396937563, 1323024996, -1196314790, 1566610809,
                928352174, 1714994319, -799030393, -462839450, -418035901,
                -1286542568, -1534707733, 985188849, -1960744352, -1463825054,
                -1344050653, 1279472494, -1840938918, 1248877495, 861602743,
                844790112, -1844342060, 1945398439, 309808498, -239141205,
                -54120626, 499261195, -1761618908, 966279259, 217571661,
                93473713, -937734760, -279968717
            }; //generate with python expression: print [random.randint(-2147483648, 2147483647) for x in range(32)]

            //int numTasks = ((KpzKernelsGInterface.GridSize * KpzKernelsGInterface.GridSize) / 
            //  (KpzKernelsGInterface.LocalGridSize * KpzKernelsGInterface.LocalGridSize)) *
            //  KpzKernelsGInterface.NumberOfIterations;
            //int numRandomUints = 2 + (numTasks * 4);
            int numRandomUints = 2 + KpzKernelsGInterface.ParallelTasks * 4;
            SimpleMemory sm = new SimpleMemory(
                KpzKernelsGInterface.GridSize * KpzKernelsGInterface.GridSize + numRandomUints + 1);

            if (pushToFpga) KpzKernelsGExtensions.CopyFromGridToSimpleMemory(hostGrid, sm);

            sm.WriteUInt32(KpzKernelsGInterface.MemIndexNumberOfIterations, numberOfIterations);

            Random rnd = new Random();
            for (int randomWriteIndex=0; randomWriteIndex<numRandomUints; randomWriteIndex++)
            {
                sm.WriteUInt32(KpzKernelsGInterface.MemIndexRandomSeed + randomWriteIndex, 
                    (randomSeedEnable)?(uint)rnd.Next():(uint)notRandomSeed[randomWriteIndex]);
            }

            kernels.ScheduleIterations(sm);

            KpzKernelsGExtensions.CopyFromSimpleMemoryToGrid(hostGrid, sm);
        }

       /// <summary>Push table into FPGA.</summary>
        public static void CopyFromGridToSimpleMemory(KpzNode[,] gridSrc, SimpleMemory memoryDst)
        {
            for (int x = 0; x < KpzKernelsGInterface.GridSize; x++)
            {
                for (int y = 0; y < KpzKernelsGInterface.GridSize; y++)
                {
                    KpzNode node = gridSrc[x, y];
                    memoryDst.WriteUInt32(KpzKernelsGInterface.MemIndexGrid + y * KpzKernelsGInterface.GridSize + x, node.SerializeToUInt32());
                }
            }
        }

        /// <summary>Pull table from the FPGA.</summary>
        public static void CopyFromSimpleMemoryToGrid(KpzNode[,] gridDst, SimpleMemory memorySrc)
        {
            for (int x = 0; x < KpzKernelsGInterface.GridSize; x++)
            {
                for (int y = 0; y < KpzKernelsGInterface.GridSize; y++)
                {
                    gridDst[x, y] = KpzNode.DeserializeFromUInt32(
                        memorySrc.ReadUInt32(KpzKernelsGInterface.MemIndexGrid + y * KpzKernelsGInterface.GridSize + x));
                }
            }
        }
    }
}
