
using System.Threading.Tasks;
using Hast.Layer;
using Hast.Algorithms;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Samples.Kpz
{
    public class KpzKernelsG
    {
        public virtual void ScheduleIterations(SimpleMemory memory)
        {
            //grid méret (ez lehet akár fix, beledrótozva a VHDL kódba)
            //kicsi grid méret
            //  --> ezeknek a hányadosát is
            //  ezeknek (N*N)/(n*n) oszthatóknak is kellene lenniük egymással, hogy egyszerűbb legyen nekünk

            const int GridSize = 4096;
            const int LocalGridSize = 8;
            const int ParallelTasks = 8; //Number of parallel execution engines
            const int NumberOfIterations = 10;
            int TasksPerIteration = (GridSize * GridSize) / (LocalGridSize * LocalGridSize);
            int SchedulesPerIteration = TasksPerIteration / ParallelTasks;
            const float IterationsPerTask = 0.5F;
            int IterationGroupSize = (int) (NumberOfIterations / IterationsPerTask);
            int PokesInsideTask = (int)(LocalGridSize * LocalGridSize * IterationsPerTask);
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
    }
