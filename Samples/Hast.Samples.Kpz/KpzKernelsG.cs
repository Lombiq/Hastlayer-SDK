
using System.Threading.Tasks;
using Hast.Layer;
using Hast.Algorithms;
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

    public class KpzKernelsG
    {
        uint integerProbabilityP = 32767, integerProbabilityQ = 32767;
        ulong randomState0;
        //These parameters are fixed, locked into VHDL code for simplicity
        public const int GridSize = 4096; //Full grid width and height
        //Local grid width and height (GridSize^2)/(LocalGridSize^2) need to be an integer for simplicity
        public const int LocalGridSize = 8;
        public const int ParallelTasks = 8; //Number of parallel execution engines
        public const int NumberOfIterations = 10;

        //public int MemStartOfRandomValues() { return GridSize * GridSize;  }
        //public int MemStartOfParameters() { return GridSize * GridSize + TasksPerIteration * NumberOfIterations + 1; }

        public virtual void ScheduleIterations(SimpleMemory memory)
        {
            int TasksPerIteration = (GridSize * GridSize) / (LocalGridSize * LocalGridSize);
            int SchedulesPerIteration = TasksPerIteration / ParallelTasks;
            const float IterationsPerTask = 0.5F;
            int IterationGroupSize = (int)(NumberOfIterations / IterationsPerTask);
            int PokesInsideTask = (int)(LocalGridSize * LocalGridSize * IterationsPerTask);
            int LocalGridPartitions = GridSize / LocalGridSize;
            int TotalNumberOfTasks = TasksPerIteration * NumberOfIterations;

            KpzKernelsIndexObject[] TaskLocals = new KpzKernelsIndexObject[ParallelTasks];
            for (int TaskLocalsIndex = 0; TaskLocalsIndex < ParallelTasks; TaskLocalsIndex++)
            {
                TaskLocals[TaskLocalsIndex].bramDx = new bool[LocalGridSize * LocalGridSize];
                TaskLocals[TaskLocalsIndex].bramDy = new bool[LocalGridSize * LocalGridSize];
            }

            //What is IterationGroupIndex good for? 
            //IterationPerTask needs to be between 0.5 and 1 based on the e-mail of Mate.
            //If we want 10 iterations, and starting a full series of tasks makes half iteration on the full table,
            //then we need to start it 20 times (thus IterationGroupSize will be 20). 

            int ParallelTaskRandomIndex = 0;

            randomState0 = memory.ReadUInt32(GridSize * GridSize + ParallelTaskRandomIndex++);
            uint RandomSeedTemp = memory.ReadUInt32(GridSize * GridSize + ParallelTaskRandomIndex++);
            randomState0 |= ((ulong)RandomSeedTemp) << 32;

            for (int IterationGroupIndex = 0; IterationGroupIndex < IterationGroupSize; IterationGroupIndex++)
            {
                uint RandomValue = GetNextRandom0();
                int RandomXOffset = LocalGridSize, RandomYOffset = 0;
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

                        TaskLocals[ParallelTaskIndex].taskRandomState1 = memory.ReadUInt32(GridSize * GridSize + ParallelTaskRandomIndex++);
                        RandomSeedTemp = memory.ReadUInt32(GridSize * GridSize + ParallelTaskRandomIndex++);
                        TaskLocals[ParallelTaskIndex].taskRandomState1 |= ((ulong)RandomSeedTemp) << 32;

                        TaskLocals[ParallelTaskIndex].taskRandomState2 = memory.ReadUInt32(GridSize * GridSize + ParallelTaskRandomIndex++);
                        RandomSeedTemp = memory.ReadUInt32(GridSize * GridSize + ParallelTaskRandomIndex++);
                        TaskLocals[ParallelTaskIndex].taskRandomState2 |= ((ulong)RandomSeedTemp) << 32;

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
                                uint c1 = (uint)(TaskLocal.taskRandomState1 >> 32);
                                uint x1 = (uint)(TaskLocal.taskRandomState1 & 0xFFFFFFFFUL);
                                // Creating the value 0xFFFEB81BUL. This literal can't be directly used due to an ILSpy bug, see:
                                // https://github.com/icsharpcode/ILSpy/issues/807
                                uint z11 = 0xFFFE;
                                uint z12 = 0xB81B;
                                uint z1 = (0 << 32) | (z11 << 16) | z12;
                                TaskLocal.taskRandomState1 = x1 * z1 + c1;
                                uint taskRandomNumber1 = x1 ^ c1;

                                //GetNextRandom2
                                uint c2 = (uint)(TaskLocal.taskRandomState2 >> 32);
                                uint x2 = (uint)(TaskLocal.taskRandomState2 & 0xFFFFFFFFUL);
                                // Creating the value 0xFFFEB81BUL. This literal can't be directly used due to an ILSpy bug, see:
                                // https://github.com/icsharpcode/ILSpy/issues/807
                                uint z21 = 0xFFFE;
                                uint z22 = 0xB81B;
                                uint z2 = (0 << 32) | (z21 << 16) | z22;
                                TaskLocal.taskRandomState2 = x2 * z2 + c2;
                                uint taskRandomNumber2 = x2 ^ c2;

                                int pokeCenterX = (int)(taskRandomNumber1 & (LocalGridSize - 1));
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
                                    (randomVariable1 < integerProbabilityP)) ||
                                    // If we get the pattern {10, 10} we have a hole:
                                    ((!TaskLocal.bramDx[pokeCenterIndex] && TaskLocal.bramDx[rightNeighbourIndex]) &&
                                    (!TaskLocal.bramDy[pokeCenterIndex] && TaskLocal.bramDy[bottomNeighbourIndex]) &&
                                    (randomVariable2 < integerProbabilityQ))
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

                        for (int CopyDstX = 0; CopyDstX < LocalGridSize; CopyDstX++)
                        {
                            for (int CopyDstY = 0; CopyDstY < LocalGridSize; CopyDstY++)
                            {
                                int CopySrcX = (BaseX + CopyDstX) % GridSize;
                                int CopySrcY = (BaseY + CopyDstY) % GridSize;
                                uint value =
                                    (TaskLocals[ParallelTaskIndex].bramDx[CopyDstX + CopyDstY * LocalGridSize] ? 1U : 0U) |
                                    (TaskLocals[ParallelTaskIndex].bramDy[CopyDstX + CopyDstY * LocalGridSize] ? 2U : 0U);
                                memory.WriteUInt32(CopySrcX + CopySrcY * GridSize, value);
                            }
                        }
                    }
                }
            }
        }

        public uint GetNextRandom0()
        {
            uint c = (uint)(randomState0 >> 32);
            ulong xl = randomState0 & (0xFFFFFFFFUL);
            uint x = (uint)xl;
            // Creating the value 0xFFFEB81BUL. This literal can't be directly used due to an ILSpy bug, see:
            // https://github.com/icsharpcode/ILSpy/issues/807
            uint z1 = 0xFFFE;
            uint z2 = 0xB81B;
            uint z = (0 << 32) | (z1 << 16) | z2;
            randomState0 = x * z + c;
            return x ^ c;
        }

    }
    public static class KpzKernelsGExtensions
    {
        public static void CopyTo(this KpzKernelsG kernels, SimpleMemory memoryDst, KpzNode[,] gridSrc)
        {
            for (int x = 0; x < KpzKernels.GridHeight; x++)
            {
                for (int y = 0; y < KpzKernelsG.GridSize; y++)
                {
                    KpzNode node = gridSrc[x, y];
                    memoryDst.WriteUInt32(y * KpzKernelsG.GridSize + x, node.SerializeToUInt32());
                }
            }

            uint NumberOfRandomSeedValues = ((KpzKernelsG.GridSize * KpzKernelsG.GridSize) / (KpzKernelsG.LocalGridSize * KpzKernelsG.LocalGridSize) * KpzKernelsG.NumberOfIterations + 1) * 2;
            Random random = new Random();
            for (int RandomSeedCopyIndex = 0; RandomSeedCopyIndex < NumberOfRandomSeedValues; RandomSeedCopyIndex++)
            {
                uint randomNumber = (uint)random.Next();
                memoryDst.WriteUInt32(KpzKernelsG.GridSize * KpzKernelsG.GridSize + RandomSeedCopyIndex, randomNumber);
            }
        }
    }

}
