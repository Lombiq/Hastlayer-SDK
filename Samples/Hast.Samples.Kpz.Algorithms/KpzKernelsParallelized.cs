using Hast.Algorithms.Random;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using System;
using System.Threading.Tasks;

namespace Hast.Samples.Kpz.Algorithms
{
    /// <summary>
    /// It contains the state of each Task instantiated within KpzKernelsG.ScheduleIterations.
    /// </summary>
    public class KpzKernelsTaskState
    {
        public bool[] BramDx;
        public bool[] BramDy;
        public RandomMwc64X Random1;
        public RandomMwc64X Random2;
    }

    // SimpleMemory map:
    // * 0  (1 address)  :
    //      The number of iterations to perform (NumberOfIterations).
    // * 1 .. 1+ParallelTasks*4+2  (ParallelTasks*4+2 addresses)  :
    //      Random seed for PRNGs in each task, and an additional one for generating random grid offsets at scheduler
    //      level. Each random seed number is 64-bit (2 uints)
    // * 1+ParallelTasks*4+2 .. 1+ParallelTasks*4+2+GridSize^2-1  (GridSize^2 addresses)  :
    //      The input KPZ nodes as 32 bit numbers, with bit 0 as dx and bit 1 as dy.

    /// <summary>
    /// This is an implementation of the KPZ algorithm for FPGAs through Hastlayer, with a parallelized architecture
    /// similar to GPUs. It makes use of a given number of Tasks as parallel execution engines
    /// (see <see cref="ReschedulesPerTaskIteration" />).
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
    public class KpzKernelsParallelizedInterface
    {
        // ==== <CONFIGURABLE PARAMETERS> ====
        // Full grid width and height.
        public const int GridSize = 64;
        // Local grid width and height. Each Task has a local grid, on which it works.
        public const int LocalGridSize = 8;
        // Furthermore, for LocalGridSize and GridSize the following expressions should have an integer result:
        //    (GridSize^2)/(LocalGridSize^2)
        //    GridSize/LocalGridSize
        // (Here the ^ operator means power.)
        // Also both GridSize and LocalGridSize should be a power of two.
        // The probability of turning a pyramid into a hole (IntegerProbabilityP),
        // or a hole into a pyramid (IntegerProbabilityQ).
        public const uint IntegerProbabilityP = 32767, IntegerProbabilityQ = 32767;
        // Number of parallel execution engines. (Should be a power of two.) Only 8 will fully fit on the Nexys A7.
        public const int ParallelTasks = 8;
        // The number of reschedules (thus global grid offset changing) within one iteration.
        public const int ReschedulesPerTaskIteration = 2;
        // This should be 1 or 2 (the latter if you want to be very careful).
        // ==== </CONFIGURABLE PARAMETERS> ====

        public const int MemIndexNumberOfIterations = 0;
        public const int MemIndexRandomSeed = MemIndexNumberOfIterations + 1;
        public const int MemIndexGrid = MemIndexRandomSeed + ParallelTasks * 4 + 2;

        public virtual void ScheduleIterations(SimpleMemory memory)
        {
            int numberOfIterations = memory.ReadInt32(MemIndexNumberOfIterations);
            const int TasksPerIteration = (GridSize * GridSize) / (LocalGridSize * LocalGridSize);
            const int SchedulesPerIteration = TasksPerIteration / ParallelTasks;
            int iterationGroupSize = numberOfIterations * ReschedulesPerTaskIteration;
            const int PokesInsideTask = LocalGridSize * LocalGridSize / ReschedulesPerTaskIteration;
            const int LocalGridPartitions = GridSize / LocalGridSize;
            //Note: TotalNumberOfTasks = TasksPerIteration * NumberOfIterations ==
            //  ((GridSize * GridSize) / (LocalGridSize * LocalGridSize)) * NumberOfIterations
            int parallelTaskRandomIndex = 0;
            uint randomSeedTemp;
            var random0 = new RandomMwc64X();

            var taskLocals = new KpzKernelsTaskState[ParallelTasks];
            for (int taskLocalsIndex = 0; taskLocalsIndex < ParallelTasks; taskLocalsIndex++)
            {
                taskLocals[taskLocalsIndex] = new KpzKernelsTaskState
                {
                    BramDx = new bool[LocalGridSize * LocalGridSize],
                    BramDy = new bool[LocalGridSize * LocalGridSize],
                    Random1 = new RandomMwc64X
                    {
                        State = memory.ReadUInt32(MemIndexRandomSeed + parallelTaskRandomIndex++)
                    }
                };
                randomSeedTemp = memory.ReadUInt32(MemIndexRandomSeed + parallelTaskRandomIndex++);
                taskLocals[taskLocalsIndex].Random1.State |= ((ulong)randomSeedTemp) << 32;

                taskLocals[taskLocalsIndex].Random2 = new RandomMwc64X
                {
                    State = memory.ReadUInt32(MemIndexRandomSeed + parallelTaskRandomIndex++)
                };
                randomSeedTemp = memory.ReadUInt32(MemIndexRandomSeed + parallelTaskRandomIndex++);
                taskLocals[taskLocalsIndex].Random2.State |= ((ulong)randomSeedTemp) << 32;
            }

            // What is iterationGroupIndex good for?
            // IterationPerTask needs to be between 0.5 and 1 based on the e-mail of Mate.
            // If we want 10 iterations, and starting a full series of tasks makes half iteration on the full table,
            // then we need to start it 20 times (thus IterationGroupSize will be 20).

            random0.State = memory.ReadUInt32(MemIndexRandomSeed + parallelTaskRandomIndex++);
            randomSeedTemp = memory.ReadUInt32(MemIndexRandomSeed + parallelTaskRandomIndex++);
            random0.State |= ((ulong)randomSeedTemp) << 32;

            for (int iterationGroupIndex = 0; iterationGroupIndex < iterationGroupSize; iterationGroupIndex++)
            {
                uint randomValue0 = random0.NextUInt32();
                // This assumes that LocalGridSize is 2^N:
                int randomXOffset = (int)((LocalGridSize - 1) & randomValue0);
                int randomYOffset = (int)((LocalGridSize - 1) & (randomValue0 >> 16));
                for (int scheduleIndex = 0; scheduleIndex < SchedulesPerIteration; scheduleIndex++)
                {
                    var tasks = new Task<KpzKernelsTaskState>[ParallelTasks];
                    for (int parallelTaskIndex = 0; parallelTaskIndex < ParallelTasks; parallelTaskIndex++)
                    {
                        // Decide the X and Y starting coordinates based on ScheduleIndex and ParallelTaskIndex
                        // (and the random added value)
                        int localGridIndex = parallelTaskIndex + scheduleIndex * ParallelTasks;
                        // The X and Y coordinate within the small table (local grid):
                        int partitionX = localGridIndex % LocalGridPartitions;
                        int partitionY = localGridIndex / LocalGridPartitions;
                        // The X and Y coordinate within the big table (grid):
                        int baseX = partitionX * LocalGridSize + randomXOffset;
                        int baseY = partitionY * LocalGridSize + randomYOffset;

                        // Copy to local memory
                        for (int copyDstX = 0; copyDstX < LocalGridSize; copyDstX++)
                        {
                            for (int CopyDstY = 0; CopyDstY < LocalGridSize; CopyDstY++)
                            {
                                //Prevent going out of grid memory area (e.g. reading into random seed):
                                int copySrcX = (baseX + copyDstX) % GridSize;
                                int copySrcY = (baseY + CopyDstY) % GridSize;
                                uint value = memory.ReadUInt32(MemIndexGrid + copySrcX + copySrcY * GridSize);
                                taskLocals[parallelTaskIndex].BramDx[copyDstX + CopyDstY * LocalGridSize] =
                                    (value & 1) == 1;
                                taskLocals[parallelTaskIndex].BramDy[copyDstX + CopyDstY * LocalGridSize] =
                                    (value & 2) == 2;
                            }
                        }

                        tasks[parallelTaskIndex] = Task.Factory.StartNew(
                        rawTaskState =>
                        {
                            // Then do TasksPerIteration iterations
                            var taskLocal = (KpzKernelsTaskState)rawTaskState;
                            for (int pokeIndex = 0; pokeIndex < PokesInsideTask; pokeIndex++)
                            {
                                // ==== <Now randomly switch four cells> ====

                                // Generating two random numbers:
                                uint taskRandomNumber1 = taskLocal.Random1.NextUInt32();
                                uint taskRandomNumber2 = taskLocal.Random2.NextUInt32();

                                // The existence of var-1 in code is a good indicator of that it is assumed to be 2^N:
                                int pokeCenterX = (int)(taskRandomNumber1 & (LocalGridSize - 1));
                                int pokeCenterY = (int)((taskRandomNumber1 >> 16) & (LocalGridSize - 1));
                                int pokeCenterIndex = pokeCenterX + pokeCenterY * LocalGridSize;
                                uint randomVariable1 = taskRandomNumber2 & ((1 << 16) - 1);
                                uint randomVariable2 = (taskRandomNumber2 >> 16) & ((1 << 16) - 1);

                                // Get neighbour indexes:
                                int rightNeighbourIndex;
                                int bottomNeighbourIndex;
                                // We skip if neighbours would fall out of the local grid:
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
                                    ((taskLocal.BramDx[pokeCenterIndex] && !taskLocal.BramDx[rightNeighbourIndex]) &&
                                    (taskLocal.BramDy[pokeCenterIndex] && !taskLocal.BramDy[bottomNeighbourIndex]) &&
                                    (randomVariable1 < IntegerProbabilityP)) ||
                                    // If we get the pattern {10, 10} we have a hole:
                                    ((!taskLocal.BramDx[pokeCenterIndex] && taskLocal.BramDx[rightNeighbourIndex]) &&
                                    (!taskLocal.BramDy[pokeCenterIndex] && taskLocal.BramDy[bottomNeighbourIndex]) &&
                                    (randomVariable2 < IntegerProbabilityQ))
                                )
                                {
                                    // We make a hole into a pyramid, and a pyramid into a hole.
                                    taskLocal.BramDx[pokeCenterIndex] = !taskLocal.BramDx[pokeCenterIndex];
                                    taskLocal.BramDy[pokeCenterIndex] = !taskLocal.BramDy[pokeCenterIndex];
                                    taskLocal.BramDx[rightNeighbourIndex] = !taskLocal.BramDx[rightNeighbourIndex];
                                    taskLocal.BramDy[bottomNeighbourIndex] = !taskLocal.BramDy[bottomNeighbourIndex];
                                }

                                // ==== </Now randomly switch four cells> ====
                            }

                            return taskLocal;
                        }, taskLocals[parallelTaskIndex]);
                    }

                    Task.WhenAll(tasks).Wait();

                    // Copy back to SimpleMemory
                    for (int parallelTaskIndex = 0; parallelTaskIndex < ParallelTasks; parallelTaskIndex++)
                    {
                        // Calculate these things again
                        int localGridIndex = parallelTaskIndex + scheduleIndex * ParallelTasks;
                        // The X and Y coordinate within the small table (local grid):
                        int partitionX = localGridIndex % LocalGridPartitions;
                        int partitionY = localGridIndex / LocalGridPartitions;
                        // The X and Y coordinate within the big table (grid):
                        int baseX = partitionX * LocalGridSize + randomXOffset;
                        int baseY = partitionY * LocalGridSize + randomYOffset;

                        for (int copySrcX = 0; copySrcX < LocalGridSize; copySrcX++)
                        {
                            for (int copySrcY = 0; copySrcY < LocalGridSize; copySrcY++)
                            {
                                int copyDstX = (baseX + copySrcX) % GridSize;
                                int copyDstY = (baseY + copySrcY) % GridSize;
                                uint value =
                                    (tasks[parallelTaskIndex].Result.BramDx[copySrcX + copySrcY * LocalGridSize] ? 1U : 0U) |
                                    (tasks[parallelTaskIndex].Result.BramDy[copySrcX + copySrcY * LocalGridSize] ? 2U : 0U);
                                // Note: use (tasks[parallelTaskIndex].Result), because
                                //(TaskLocals[ParallelTaskIndex]) won't work.
                                memory.WriteUInt32(MemIndexGrid + copyDstX + copyDstY * GridSize, value);
                            }
                        }

                        // Take PRNG current state from Result to feed it to input next time
                        taskLocals[parallelTaskIndex].Random1.State = tasks[parallelTaskIndex].Result.Random1.State;
                        taskLocals[parallelTaskIndex].Random2.State = tasks[parallelTaskIndex].Result.Random2.State;
                    }
                }
            }
        }
    }

    /// <summary>
    /// These are host-side functions for <see cref="KpzKernelsParallelizedInterface"/>.
    /// </summary>
    public static class KpzKernelsParallelizedExtensions
    {
        /// <summary>
        /// Wrapper for calling <see cref="KpzKernelsParallelizedInterface.ScheduleIterations"/>.
        /// </summary>
        /// <param name="kernels"></param>
        /// <param name="hastlayer">Required to properly create <see cref="SimpleMemory"/>.</param>
        /// <param name="configuration">Required to properly create <see cref="SimpleMemory"/>.</param>
        /// <param name="hostGrid">The grid that we work on.</param>
        /// <param name="pushToFpga">Force pushing the grid into the FPGA (or work on the grid already there).</param>
        /// <param name="randomSeedEnable">
        /// If it is disabled, preprogrammed random numbers will be written into
        /// SimpleMemory instead of real random generated numbers. This helps debugging, keeping the output more
        /// consistent across runs.
        /// </param>
        /// <param name="numberOfIterations">The number of iterations to perform.</param>
        public static void DoIterationsWrapper(
            this KpzKernelsParallelizedInterface kernels,
            IHastlayer hastlayer,
            IHardwareGenerationConfiguration configuration,
            KpzNode[,] hostGrid,
            bool pushToFpga,
            bool randomSeedEnable,
            uint numberOfIterations)
        {
            // The following numbers will be used when random seed is disabled in GUI.
            // This makes the result more predictable while debugging.
            // Add more random numbers manually if you get an out of bounds exception on notRandomSeed.
            // This might happen if you increase KpzKernelsGInterface.ParallelTasks.
            // You can generate these with the following python expression (even in an online tool like:
            // https://www.tutorialspoint.com/execute_python_online.php):
            //    import random
            //    print [random.randint(-2147483648, 2147483647) for x in range(32)]
            var notRandomSeed = new int[]{
                -2122284207, -805426534, -296351199, 1082586369, -864339821, 331357875, 1192493543, -851078246,
                -1091834350, -671234217, -1623097030, -100086504, -1516943165, 1569609717, 1695030944, -888770401,
                341459416, -1970567826, 794279071, 1480098339, -420588859, 299418286, -1342502802, 1667430755,
                -2057457019, -257344031, -850635314, -210624876, -678618985, -1069954593, -1227202130, -513326420,
                -232192458, 2099559718, 1809993314, -43947016, -1478372364, 1027454543, 484420729, 1629446609,
                -1049983320, -827693764, -1496166513, -1539335368, -1628287378, -1503862015, 1088962278, 1529350919,
                541247270, -762735333, 1201597916, 63792507, 572540375, 1424887319, -2111458304, -1412595626,
                372071952, -1908453570, -79328169, -792331270, -499848108, -1938769107, -356810636, 2063051988,
                -824609528, -1798425884, 1921971887, 334688140, -1210315495, -782998033, 1412857768, 676054292,
                303879804, -854493128, 168364778, 1153057767, 1892111935, 1255022400, -1906894318, -1943897452,
                -1121887497, -411064952, -1153708605, -1236973870, 1909433338, 976253398, 1565147040, -76067349,
                840379860, 648328296, 815910809, 1054583403, 641704477, 347743363, -165988295, -513935773, 1886470992,
                -751562304, 1514065758, -1503136866, -290638406, -1465068879, -1122727314, -674164136, 1376761314,
                -480074650, -1189373896, 1628987870, -1801471129, 1149055452, 1623827843, -1014866037, -1349295410,
                -1213044536, 1501859543, 1766766693, -11506391, 1354826834, 1853369605, 1167161889, 1283458193,
                -1605994989, 1371816845, -1806325888, 899112301, -1972685877, 2020361869, 1980217986, -1337742593,
                -1351549709, 1989386170, -1745931254, -1294330993, -280576358, -1901106587, -1529351871, -496188819,
                -1135040353, 2064141162, -1550762441, 206482802, -208760219, -498417100, 158432532, -420745217,
                -1763282295, -1559411916, 212239689, -1713858924, 1957674632, 1114701003, 1240747459, 1586146810,
                399597100, -1822066773, -521605668, 442732461, 2139235466, -517996110, -1142464990, -347623801,
                1949728360, -1333355612, -1523271090, 1873782401, 109175483, -789045849, -1136301216, 1231875761,
                1455879393, -1508517739, 22132201, 1503847013, 1121324155, 1077146859, 1245449568, -79936914,
                -1149836541, -174007501, 1742754517, 514371316, -1438578033, -1846621448, 1157028248, 1672050400,
                605535816, 1415254613, 1944255343, -1057195252, 1981414947, -1232546674, 1039130235, 1530155655,
                -356281736, -589212081, 1146701526, 224674108, 2035824054, -1338064105, -1378614038, 950685393,
                292251866, 1396937563, 1323024996, -1196314790, 1566610809, -1410366307, 1787096854, 356058337,
                928352174, 1714994319, -799030393, -462839450, -418035901, -2039562916, -477068733, -2133273208,
                -1286542568, -1534707733, 985188849, -1960744352, -1463825054, -487643118, -699627691, -443714835,
                -1344050653, 1279472494, -1840938918, 1248877495, 861602743, -570947693, -1118345807, -111877096,
                844790112, -1844342060, 1945398439, 309808498, -239141205, -758285938, -59513544, -1870383944,
                -54120626, 499261195, -1761618908, 966279259, 217571661, 1813251139, 1124806771, 323365414, 595569067,
                93473713, -937734760, -279968717, -1457028170, -389060750, -1888789492, -1109047524, 171427933
            };

            int numRandomUints = 2 + KpzKernelsParallelizedInterface.ParallelTasks * 4;
            var sm = hastlayer.CreateMemory(configuration, KpzKernelsParallelizedInterface.GridSize *
                KpzKernelsParallelizedInterface.GridSize + numRandomUints + 1);

            if (pushToFpga) CopyFromGridToSimpleMemory(hostGrid, sm);

            sm.WriteUInt32(KpzKernelsParallelizedInterface.MemIndexNumberOfIterations, numberOfIterations);

            var rnd = new Random();
            for (int randomWriteIndex = 0; randomWriteIndex < numRandomUints; randomWriteIndex++)
            {
                sm.WriteUInt32(KpzKernelsParallelizedInterface.MemIndexRandomSeed + randomWriteIndex,
                    (randomSeedEnable) ? (uint)rnd.Next() : (uint)notRandomSeed[randomWriteIndex]);
                //See comment on notRandomSeed if you get an index out of bounds error here.
            }

            kernels.ScheduleIterations(sm);

            CopyFromSimpleMemoryToGrid(hostGrid, sm);
        }

        /// <summary>Push table into FPGA.</summary>
        public static void CopyFromGridToSimpleMemory(KpzNode[,] gridSrc, SimpleMemory memoryDst)
        {
            for (int x = 0; x < KpzKernelsParallelizedInterface.GridSize; x++)
            {
                for (int y = 0; y < KpzKernelsParallelizedInterface.GridSize; y++)
                {
                    var node = gridSrc[x, y];
                    memoryDst.WriteUInt32(KpzKernelsParallelizedInterface.MemIndexGrid + y * KpzKernelsParallelizedInterface.GridSize + x, node.SerializeToUInt32());
                }
            }
        }

        /// <summary>Pull table from the FPGA.</summary>
        public static void CopyFromSimpleMemoryToGrid(KpzNode[,] gridDst, SimpleMemory memorySrc)
        {
            for (int x = 0; x < KpzKernelsParallelizedInterface.GridSize; x++)
            {
                for (int y = 0; y < KpzKernelsParallelizedInterface.GridSize; y++)
                {
                    gridDst[x, y] = KpzNode.DeserializeFromUInt32(
                        memorySrc.ReadUInt32(KpzKernelsParallelizedInterface.MemIndexGrid + y * KpzKernelsParallelizedInterface.GridSize + x));
                }
            }
        }
    }
}
