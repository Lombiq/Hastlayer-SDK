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
        public const uint IntegerProbabilityP = 32_767, IntegerProbabilityQ = 32_767;
        // Number of parallel execution engines. (Should be a power of two.) Only 8 will fully fit on the Nexys A7.
        public const int ParallelTasks = 8;
        // The number of reschedules (thus global grid offset changing) within one iteration.
        public const int ReschedulesPerTaskIteration = 2;
        // This should be 1 or 2 (the latter if you want to be very careful).
        // ==== </CONFIGURABLE PARAMETERS> ====

        public const int MemIndexNumberOfIterations = 0;
        public const int MemIndexRandomSeed = MemIndexNumberOfIterations + 1;
        public const int MemIndexGrid = MemIndexRandomSeed + (ParallelTasks * 4) + 2;

        public virtual void ScheduleIterations(SimpleMemory memory)
        {
            int numberOfIterations = memory.ReadInt32(MemIndexNumberOfIterations);
            const int TasksPerIteration = GridSize * GridSize / (LocalGridSize * LocalGridSize);
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
                    },
                };
                randomSeedTemp = memory.ReadUInt32(MemIndexRandomSeed + parallelTaskRandomIndex++);
                taskLocals[taskLocalsIndex].Random1.State |= ((ulong)randomSeedTemp) << 32;

                taskLocals[taskLocalsIndex].Random2 = new RandomMwc64X
                {
                    State = memory.ReadUInt32(MemIndexRandomSeed + parallelTaskRandomIndex++),
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
                        int localGridIndex = parallelTaskIndex + (scheduleIndex * ParallelTasks);
                        // The X and Y coordinate within the small table (local grid):
                        int partitionX = localGridIndex % LocalGridPartitions;
                        int partitionY = localGridIndex / LocalGridPartitions;
                        // The X and Y coordinate within the big table (grid):
                        int baseX = (partitionX * LocalGridSize) + randomXOffset;
                        int baseY = (partitionY * LocalGridSize) + randomYOffset;

                        // Copy to local memory
                        for (int copyDstX = 0; copyDstX < LocalGridSize; copyDstX++)
                        {
                            for (int CopyDstY = 0; CopyDstY < LocalGridSize; CopyDstY++)
                            {
                                //Prevent going out of grid memory area (e.g. reading into random seed):
                                int copySrcX = (baseX + copyDstX) % GridSize;
                                int copySrcY = (baseY + CopyDstY) % GridSize;
                                uint value = memory.ReadUInt32(MemIndexGrid + copySrcX + (copySrcY * GridSize));
                                taskLocals[parallelTaskIndex].BramDx[copyDstX + (CopyDstY * LocalGridSize)] =
                                    (value & 1) == 1;
                                taskLocals[parallelTaskIndex].BramDy[copyDstX + (CopyDstY * LocalGridSize)] =
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
                                int pokeCenterIndex = pokeCenterX + (pokeCenterY * LocalGridSize);
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
                                rightNeighbourIndex = (rightNeighbourY * LocalGridSize) + rightNeighbourX;
                                bottomNeighbourIndex = (bottomNeighbourY * LocalGridSize) + bottomNeighbourX;

                                // We check our own {dx,dy} values, and the right neighbour's dx, and bottom neighbour's dx.

                                if (
                                    // If we get the pattern {01, 01} we have a pyramid:
                                    (taskLocal.BramDx[pokeCenterIndex] && !taskLocal.BramDx[rightNeighbourIndex] &&
                                    taskLocal.BramDy[pokeCenterIndex] && !taskLocal.BramDy[bottomNeighbourIndex] &&
                                    (randomVariable1 < IntegerProbabilityP)) ||
                                    // If we get the pattern {10, 10} we have a hole:
                                    (!taskLocal.BramDx[pokeCenterIndex] && taskLocal.BramDx[rightNeighbourIndex] &&
                                    !taskLocal.BramDy[pokeCenterIndex] && taskLocal.BramDy[bottomNeighbourIndex] &&
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
                        },
                        taskLocals[parallelTaskIndex]);
                    }

                    Task.WhenAll(tasks).Wait();

                    // Copy back to SimpleMemory
                    for (int parallelTaskIndex = 0; parallelTaskIndex < ParallelTasks; parallelTaskIndex++)
                    {
                        // Calculate these things again
                        int localGridIndex = parallelTaskIndex + (scheduleIndex * ParallelTasks);
                        // The X and Y coordinate within the small table (local grid):
                        int partitionX = localGridIndex % LocalGridPartitions;
                        int partitionY = localGridIndex / LocalGridPartitions;
                        // The X and Y coordinate within the big table (grid):
                        int baseX = (partitionX * LocalGridSize) + randomXOffset;
                        int baseY = (partitionY * LocalGridSize) + randomYOffset;

                        for (int copySrcX = 0; copySrcX < LocalGridSize; copySrcX++)
                        {
                            for (int copySrcY = 0; copySrcY < LocalGridSize; copySrcY++)
                            {
                                int copyDstX = (baseX + copySrcX) % GridSize;
                                int copyDstY = (baseY + copySrcY) % GridSize;
                                uint value =
                                    (tasks[parallelTaskIndex].Result.BramDx[copySrcX + (copySrcY * LocalGridSize)] ? 1U : 0U) |
                                    (tasks[parallelTaskIndex].Result.BramDy[copySrcX + (copySrcY * LocalGridSize)] ? 2U : 0U);
                                // Note: use (tasks[parallelTaskIndex].Result), because
                                //(TaskLocals[ParallelTaskIndex]) won't work.
                                memory.WriteUInt32(MemIndexGrid + copyDstX + (copyDstY * GridSize), value);
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
            var notRandomSeed = new[]
            {
                -2_122_284_207, -805_426_534, -296_351_199, 1_082_586_369, -864_339_821, 331_357_875, 1_192_493_543, -851_078_246,
                -1_091_834_350, -671_234_217, -1_623_097_030, -100_086_504, -1_516_943_165, 1_569_609_717, 1_695_030_944, -888_770_401,
                341_459_416, -1_970_567_826, 794_279_071, 1_480_098_339, -420_588_859, 299_418_286, -1_342_502_802, 1_667_430_755,
                -2_057_457_019, -257_344_031, -850_635_314, -210_624_876, -678_618_985, -1_069_954_593, -1_227_202_130, -513_326_420,
                -232_192_458, 2_099_559_718, 1_809_993_314, -43_947_016, -1_478_372_364, 1_027_454_543, 484_420_729, 1_629_446_609,
                -1_049_983_320, -827_693_764, -1_496_166_513, -1_539_335_368, -1_628_287_378, -1_503_862_015, 1_088_962_278, 1_529_350_919,
                541_247_270, -762_735_333, 1_201_597_916, 63_792_507, 572_540_375, 1_424_887_319, -2_111_458_304, -1_412_595_626,
                372_071_952, -1_908_453_570, -79_328_169, -792_331_270, -499_848_108, -1_938_769_107, -356_810_636, 2_063_051_988,
                -824_609_528, -1_798_425_884, 1_921_971_887, 334_688_140, -1_210_315_495, -782_998_033, 1_412_857_768, 676_054_292,
                303_879_804, -854_493_128, 168_364_778, 1_153_057_767, 1_892_111_935, 1_255_022_400, -1_906_894_318, -1_943_897_452,
                -1_121_887_497, -411_064_952, -1_153_708_605, -1_236_973_870, 1_909_433_338, 976_253_398, 1_565_147_040, -76_067_349,
                840_379_860, 648_328_296, 815_910_809, 1_054_583_403, 641_704_477, 347_743_363, -165_988_295, -513_935_773, 1_886_470_992,
                -751_562_304, 1_514_065_758, -1_503_136_866, -290_638_406, -1_465_068_879, -1_122_727_314, -674_164_136, 1_376_761_314,
                -480_074_650, -1_189_373_896, 1_628_987_870, -1_801_471_129, 1_149_055_452, 1_623_827_843, -1_014_866_037, -1_349_295_410,
                -1_213_044_536, 1_501_859_543, 1_766_766_693, -11_506_391, 1_354_826_834, 1_853_369_605, 1_167_161_889, 1_283_458_193,
                -1_605_994_989, 1_371_816_845, -1_806_325_888, 899_112_301, -1_972_685_877, 2_020_361_869, 1_980_217_986, -1_337_742_593,
                -1_351_549_709, 1_989_386_170, -1_745_931_254, -1_294_330_993, -280_576_358, -1_901_106_587, -1_529_351_871, -496_188_819,
                -1_135_040_353, 2_064_141_162, -1_550_762_441, 206_482_802, -208_760_219, -498_417_100, 158_432_532, -420_745_217,
                -1_763_282_295, -1_559_411_916, 212_239_689, -1_713_858_924, 1_957_674_632, 1_114_701_003, 1_240_747_459, 1_586_146_810,
                399_597_100, -1_822_066_773, -521_605_668, 442_732_461, 2_139_235_466, -517_996_110, -1_142_464_990, -347_623_801,
                1_949_728_360, -1_333_355_612, -1_523_271_090, 1_873_782_401, 109_175_483, -789_045_849, -1_136_301_216, 1_231_875_761,
                1_455_879_393, -1_508_517_739, 22_132_201, 1_503_847_013, 1_121_324_155, 1_077_146_859, 1_245_449_568, -79_936_914,
                -1_149_836_541, -174_007_501, 1_742_754_517, 514_371_316, -1_438_578_033, -1_846_621_448, 1_157_028_248, 1_672_050_400,
                605_535_816, 1_415_254_613, 1_944_255_343, -1_057_195_252, 1_981_414_947, -1_232_546_674, 1_039_130_235, 1_530_155_655,
                -356_281_736, -589_212_081, 1_146_701_526, 224_674_108, 2_035_824_054, -1_338_064_105, -1_378_614_038, 950_685_393,
                292_251_866, 1_396_937_563, 1_323_024_996, -1_196_314_790, 1_566_610_809, -1_410_366_307, 1_787_096_854, 356_058_337,
                928_352_174, 1_714_994_319, -799_030_393, -462_839_450, -418_035_901, -2_039_562_916, -477_068_733, -2_133_273_208,
                -1_286_542_568, -1_534_707_733, 985_188_849, -1_960_744_352, -1_463_825_054, -487_643_118, -699_627_691, -443_714_835,
                -1_344_050_653, 1_279_472_494, -1_840_938_918, 1_248_877_495, 861_602_743, -570_947_693, -1_118_345_807, -111_877_096,
                844_790_112, -1_844_342_060, 1_945_398_439, 309_808_498, -239_141_205, -758_285_938, -59_513_544, -1_870_383_944,
                -54_120_626, 499_261_195, -1_761_618_908, 966_279_259, 217_571_661, 1_813_251_139, 1_124_806_771, 323_365_414, 595_569_067,
                93_473_713, -937_734_760, -279_968_717, -1_457_028_170, -389_060_750, -1_888_789_492, -1_109_047_524, 171_427_933,
            };

            int numRandomUints = 2 + (KpzKernelsParallelizedInterface.ParallelTasks * 4);
            var sm = hastlayer.CreateMemory(configuration, (KpzKernelsParallelizedInterface.GridSize *
                KpzKernelsParallelizedInterface.GridSize) + numRandomUints + 1);

            if (pushToFpga) CopyFromGridToSimpleMemory(hostGrid, sm);

            sm.WriteUInt32(KpzKernelsParallelizedInterface.MemIndexNumberOfIterations, numberOfIterations);

            var rnd = new Random();
            for (int randomWriteIndex = 0; randomWriteIndex < numRandomUints; randomWriteIndex++)
            {
                sm.WriteUInt32(
                    KpzKernelsParallelizedInterface.MemIndexRandomSeed + randomWriteIndex,
                    randomSeedEnable ? (uint)rnd.Next() : (uint)notRandomSeed[randomWriteIndex]);
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
                    memoryDst.WriteUInt32(
                        KpzKernelsParallelizedInterface.MemIndexGrid + (y * KpzKernelsParallelizedInterface.GridSize) + x,
                        node.SerializeToUInt32());
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
                        memorySrc.ReadUInt32(KpzKernelsParallelizedInterface.MemIndexGrid + (y * KpzKernelsParallelizedInterface.GridSize) + x));
                }
            }
        }
    }
}
