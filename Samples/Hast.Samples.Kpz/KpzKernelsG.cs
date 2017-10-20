
using System.Threading.Tasks;
using Hast.Transformer.Abstractions.SimpleMemory;
using System;

namespace Hast.Samples.Kpz
{
    public class KpzKernelsIndexObject
    {
        public bool[] bramDx;
        public bool[] bramDy;
        public ulong taskRandomState1;
        public ulong taskRandomState2;
    }

    //SimpleMemory map:
    // * 0 .. GridSize^2-1  (GridSize^2 addresses)  :
    //      The input KPZ nodes as 32 bit numbers, with bit 0 as dx and bit 1 as dy.
    // * GridSize^2  (1 address)  :
    //      The number of iterations to perform (NumberOfIterations). 
    // * GridSize^2+1 .. GridSize^2+ParallelTasks*4+2  ParallelTasks*4+2 addresses)  :
    //      Random seed for PRNGs in each task, and an additional one for generating random grid offsets at scheduler level.
    //      Each random seed number is 64-bit (2 uints)

    public class KpzKernelsGInterface
    {
        const uint integerProbabilityP = 32767, integerProbabilityQ = 32767;
        //These parameters are fixed, locked into VHDL code for simplicity
        public const int GridSize = 64; //Full grid width and height
        //Local grid width and height (GridSize^2)/(LocalGridSize^2) need to be an integer for simplicity
        public const int LocalGridSize = 8;
        public const int ParallelTasks = 1; //Number of parallel execution engines

        //public int MemStartOfRandomValues() { return GridSize * GridSize;  }
        //public int MemStartOfParameters() { return GridSize * GridSize + TasksPerIteration * NumberOfIterations + 1; }

        public virtual void ScheduleIterations(SimpleMemory memory)
        {
            int NumberOfIterations = memory.ReadInt32(GridSize * GridSize);
            const int TasksPerIteration = (GridSize * GridSize) / (LocalGridSize * LocalGridSize);
            const int SchedulesPerIteration = TasksPerIteration / ParallelTasks;
            //const float IterationsPerTask = 0.5F;// 0.5F; //TODO: change back to 0.5F
            const int ReschedulesPerTaskIteration = 1; //reciprocal
            int IterationGroupSize = (int)(NumberOfIterations * ReschedulesPerTaskIteration);
            const int PokesInsideTask = (int)(LocalGridSize * LocalGridSize / ReschedulesPerTaskIteration);
            const int LocalGridPartitions = GridSize / LocalGridSize;
            //const int TotalNumberOfTasks = TasksPerIteration * NumberOfIterations == ((GridSize * GridSize) / (LocalGridSize * LocalGridSize)) * NumberOfIterations
            ulong randomState0;
            int ParallelTaskRandomIndex = 1;
            uint RandomSeedTemp;

            KpzKernelsIndexObject[] TaskLocals = new KpzKernelsIndexObject[ParallelTasks];
            for (int TaskLocalsIndex = 0; TaskLocalsIndex < ParallelTasks; TaskLocalsIndex++)
            {
                TaskLocals[TaskLocalsIndex] = new KpzKernelsIndexObject();
                TaskLocals[TaskLocalsIndex].bramDx = new bool[LocalGridSize * LocalGridSize];
                TaskLocals[TaskLocalsIndex].bramDy = new bool[LocalGridSize * LocalGridSize];
                //TaskLocals[TaskLocalsIndex].taskRandomState1 = memory.ReadUInt32(GridSize * GridSize + ParallelTaskRandomIndex++);
                //RandomSeedTemp = memory.ReadUInt32(GridSize * GridSize + ParallelTaskRandomIndex++);
                //TaskLocals[TaskLocalsIndex].taskRandomState1 |= ((ulong)RandomSeedTemp) << 32;

                //TaskLocals[TaskLocalsIndex].taskRandomState2 = memory.ReadUInt32(GridSize * GridSize + ParallelTaskRandomIndex++);
                //RandomSeedTemp = memory.ReadUInt32(GridSize * GridSize + ParallelTaskRandomIndex++);
                //TaskLocals[TaskLocalsIndex].taskRandomState2 |= ((ulong)RandomSeedTemp) << 32;
                TaskLocals[TaskLocalsIndex].taskRandomState1 = (ulong)0xCAFE;
                TaskLocals[TaskLocalsIndex].taskRandomState2 = (ulong)0xCAFE;
            }

            //What is IterationGroupIndex good for?
            //IterationPerTask needs to be between 0.5 and 1 based on the e-mail of Mate.
            //If we want 10 iterations, and starting a full series of tasks makes half iteration on the full table,
            //then we need to start it 20 times (thus IterationGroupSize will be 20).


            //randomState0 = memory.ReadUInt32(GridSize * GridSize + ParallelTaskRandomIndex++);
            //RandomSeedTemp = memory.ReadUInt32(GridSize * GridSize + ParallelTaskRandomIndex++);
            //randomState0 |= ((ulong)RandomSeedTemp) << 32;
            randomState0 = (ulong)0xCAFE;

            for (int IterationGroupIndex = 0; IterationGroupIndex < IterationGroupSize; IterationGroupIndex++)
            {
                //GetNextRandom0
                uint prngC0 = (uint)(randomState0 >> 32);
                uint prngX0 = (uint)randomState0;
                // Creating the value 0xFFFEB81BUL. This literal can't be directly used due to an ILSpy bug, see:
                // https://github.com/icsharpcode/ILSpy/issues/807
                uint prngZLow0 = 0xFFFE;
                uint prngZHigh0 = 0xB81B;
                uint prngZ0 = (0 << 32) | (prngZLow0 << 16) | prngZHigh0;
                randomState0 = (ulong)prngX0 * (ulong)prngZ0 + (ulong)prngC0;
                uint RandomValue0 = prngX0 ^ prngC0;
                //int RandomXOffset = (int)((LocalGridSize - 1) & RandomValue0); //This supposes that LocalGridSize is 2^N
                //int RandomYOffset = (int)((LocalGridSize - 1) & (RandomValue0>>16));
                int RandomXOffset = 0, RandomYOffset = 0; //TODO: remove this
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

                        //Console.WriteLine("CopyTo | Task={0}, From: {1},{2}", ParallelTaskIndex, BaseX, BaseY);

                        //Copy to local memory
                        for (int CopyDstX = 0; CopyDstX < LocalGridSize; CopyDstX++)
                        {
                            for (int CopyDstY = 0; CopyDstY < LocalGridSize; CopyDstY++)
                            {
                                int CopySrcX = (BaseX + CopyDstX) % GridSize;
                                int CopySrcY = (BaseY + CopyDstY) % GridSize; //Prevent going out of grid memory area (e.g. reading into random seed)
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

                                //Generating two random numbers:

                                //GetNextRandom1
                                uint prngC1 = (uint)(TaskLocal.taskRandomState1 >> 32);
                                uint prngX1 = (uint)TaskLocal.taskRandomState1;
                                // Creating the value 0xFFFEB81BUL. This literal can't be directly used due to an ILSpy bug, see:
                                // https://github.com/icsharpcode/ILSpy/issues/807
                                uint prngZLow1 = 0xFFFE;
                                uint prngZHigh1 = 0xB81B;
                                uint prngZ1 = (0 << 32) | (prngZLow1 << 16) | prngZHigh1;
                                TaskLocal.taskRandomState1 = (ulong)prngX1 * (ulong)prngZ1 + (ulong)prngC1;
                                uint taskRandomNumber1 = prngX1 ^ prngC1;

                                //GetNextRandom2
                                uint prngC2 = (uint)(TaskLocal.taskRandomState2 >> 32);
                                uint prngX2 = (uint)TaskLocal.taskRandomState2;
                                // Creating the value 0xFFFEB81BUL. This literal can't be directly used due to an ILSpy bug, see:
                                // https://github.com/icsharpcode/ILSpy/issues/807
                                uint prngZLow2 = 0xFFFE;
                                uint prngZHigh2 = 0xB81B;
                                uint prngZ2 = (0 << 32) | (prngZLow2 << 16) | prngZHigh2;
                                TaskLocal.taskRandomState2 = (ulong)prngX2 * (ulong)prngZ2 + (ulong)prngC2;
                                uint taskRandomNumber2 = prngX2 ^ prngC2;

                                int pokeCenterX = (int)(taskRandomNumber1 & (LocalGridSize - 1)); //The existstence of var-1 in code is a good indicator of that it is asumed to be 2^N
                                int pokeCenterY = (int)((taskRandomNumber1 >> 16) & (LocalGridSize - 1));
                                int pokeCenterIndex = pokeCenterX + pokeCenterY * LocalGridSize;
                                uint randomVariable1 = taskRandomNumber2 & ((1 << 16) - 1);
                                uint randomVariable2 = (taskRandomNumber2 >> 16) & ((1 << 16) - 1);

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
                                    (true || randomVariable1 < integerProbabilityP)) || /*TODO: remove true! */
                                    // If we get the pattern {10, 10} we have a hole:
                                    ((!TaskLocal.bramDx[pokeCenterIndex] && TaskLocal.bramDx[rightNeighbourIndex]) &&
                                    (!TaskLocal.bramDy[pokeCenterIndex] && TaskLocal.bramDy[bottomNeighbourIndex]) &&
                                    (true || randomVariable2 < integerProbabilityQ)) /*TODO: remove true! */
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
                            return TaskLocal; //TODO: do we need this at all?
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
                        //Console.WriteLine("CopyBack | Task={0}, To: {1},{2}", ParallelTaskIndex, BaseX, BaseY);

                        for (int CopyDstX = 0; CopyDstX < LocalGridSize; CopyDstX++)
                        {
                            for (int CopyDstY = 0; CopyDstY < LocalGridSize; CopyDstY++)
                            {
                                int CopySrcX = (BaseX + CopyDstX) % GridSize;
                                int CopySrcY = (BaseY + CopyDstY) % GridSize;
                                uint value =
                                    (tasks[ParallelTaskIndex].Result.bramDx[CopyDstX + CopyDstY * LocalGridSize] ? 1U : 0U) |
                                    (tasks[ParallelTaskIndex].Result.bramDy[CopyDstX + CopyDstY * LocalGridSize] ? 2U : 0U);
                                //uint value =
                                //    (TaskLocals[ParallelTaskIndex].bramDx[CopyDstX + CopyDstY * LocalGridSize] ? 1U : 0U) |
                                //    (TaskLocals[ParallelTaskIndex].bramDy[CopyDstX + CopyDstY * LocalGridSize] ? 2U : 0U);
                                //(Either solution to pass TaskLocals does work.)
                                memory.WriteUInt32(CopySrcX + CopySrcY * GridSize, value);
                            }
                        }

                        //Take PRNG current state from Result to feed it to input next time
                        TaskLocals[ParallelTaskIndex].taskRandomState1 = tasks[ParallelTaskIndex].Result.taskRandomState1;
                        TaskLocals[ParallelTaskIndex].taskRandomState2 = tasks[ParallelTaskIndex].Result.taskRandomState2;
                    }
                }
            }
        }
    }

    public static class KpzKernelsGExtensions
    {
        public static void DoIterationsWrapper(this KpzKernelsGInterface kernels, KpzNode[,] hostGrid, bool pushToFpga, bool randomSeedEnable, uint numberOfIterations)
        {
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

            //int numTasks = ((KpzKernelsGInterface.GridSize * KpzKernelsGInterface.GridSize) / (KpzKernelsGInterface.LocalGridSize * KpzKernelsGInterface.LocalGridSize)) * KpzKernelsGInterface.NumberOfIterations;
            //int numRandomUints = 2 + (numTasks * 4);
            int numRandomUints = 2 + KpzKernelsGInterface.ParallelTasks * 4;
            SimpleMemory sm = new SimpleMemory(KpzKernelsGInterface.GridSize * KpzKernelsGInterface.GridSize + numRandomUints + 1);

            if (pushToFpga) KpzKernelsGExtensions.CopyFromGridToSimpleMemory(hostGrid, sm);

            sm.WriteUInt32(KpzKernelsGInterface.GridSize * KpzKernelsGInterface.GridSize, numberOfIterations);

            Random rnd = new Random();
            for (int randomWriteIndex=0; randomWriteIndex<numRandomUints; randomWriteIndex++)
                sm.WriteUInt32(KpzKernelsGInterface.GridSize * KpzKernelsGInterface.GridSize + 1 + randomWriteIndex, (randomSeedEnable)?(uint)rnd.Next():(uint)notRandomSeed[randomWriteIndex]);

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
                    memoryDst.WriteUInt32(y * KpzKernelsGInterface.GridSize + x, node.SerializeToUInt32());
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
                    gridDst[x, y] = KpzNode.DeserializeFromUInt32(memorySrc.ReadUInt32(y * KpzKernelsGInterface.GridSize + x));
                }
            }
        }
    }
}
