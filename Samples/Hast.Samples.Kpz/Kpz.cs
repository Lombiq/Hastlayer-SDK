using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Hast.Samples.Kpz
{

    public enum KpzTarget
    {
        Cpu, Fpga, FpgaSimulation, FpgaG, FpgaSimulationG
    }

    /// <summary>
    /// This struct is used to built the grid that the KPZ algorithm works on.
    /// Every node on the grid stores the derivatives of a 2D function in X and Y direction.
    /// </summary>
    public class KpzNode 
    {
        public bool dx; // Right
        public bool dy; // Bottom

        public uint SerializeToUInt32() //TODO: lehet hogy ennek megis structnak kellene lennie, metodusok nelkul?
        {
            return ((dx)?1U:0)|((dy)?2U:0);
        }

        public static KpzNode DeserializeFromUInt32(uint value)
        {
            KpzNode node = new KpzNode();
            node.dx = (value & 1) != 0;
            node.dy = (value & 2) != 0;
            return node;
        }

    }


    /// <summary>
    /// This struct stores the right and bottom neighbours of a particular node on the grid.
    /// It stores both their values and coordinates.
    /// </summary>
    public struct KpzNeighbours
    {
        public KpzNode nx; // Right neighbour value
        public KpzNode ny; // Left neighbour value
        public KpzCoords nxCoords; // Right neighbour coordinates
        public KpzCoords nyCoords; // Left neighbour coordinates
    }

    /// <summary>
    /// This struct stores the coordinates of a <see cref="KpzNode" /> on the grid.
    /// </summary>
    public struct KpzCoords
    {
        public int x;
        public int y;
    }


    /// <summary>
    /// This class performs the calculations of the KPZ algorithm.
    /// </summary>
    public partial class Kpz
    {
        /// <summary>It returns the width of the grid.</summary>
        public int gridWidth { get { return grid.GetLength(0); } }
        /// <summary>It returns the height of the grid.</summary>
        public int gridHeight { get { return grid.GetLength(1); } }
        /// <summary>The 2D grid of <see cref="KpzNode" /> items on which the KPZ algorithm is performed.</summary>
        public KpzNode[,] grid;
        /// <summary>The probability of pyramid to hole change.</summary>
        private double probabilityP = 0.5d;
        /// <summary>The probability of hole to pyramid change.</summary>
        private double probabilityQ = 0.5d;
        /// <summary>The pseudorandom generator is used at various places in the algorithm.</summary>
        private Random random = new Random();
        /// <summary>See <see cref="StateLogger" /></summary>
        private bool enableStateLogger = false;
        /// <summary>
        /// The <see cref="StateLogger" /> (if enabled) allows us to inspect the state of the algorithm at
        /// given steps during its execution. This object can be later passed on to <see cref="InspectForm" />
        /// to graphically represent it on a UI.
        /// <note type="caution">
        /// Use a small grid and a low amount of iterations if enabled. It will use a lot of memory.
        /// </note>
        /// </summary>
        public KpzStateLogger StateLogger;

        private KpzTarget kpzTarget = KpzTarget.Cpu;

        /// <summary>
        /// The constructor initializes the parameters of <see cref="Kpz" />, see:
        /// <see cref="gridWidth" />, <see cref="gridHeight" />,
        /// <see cref="probabilityP" />, <see cref="probabilityQ" />,
        /// <see cref="StateLogger" />.
        /// </summary>
        public Kpz
        (
            int newGridWidth, 
            int newGridHeight, 
            double probabilityP, 
            double probabilityQ, 
            bool enableStateLogger,
            KpzTarget target
        )
        {
            grid = new KpzNode[newGridWidth, newGridHeight];
            this.probabilityP = probabilityP;
            this.probabilityQ = probabilityQ;
            this.enableStateLogger = enableStateLogger;
            if (this.enableStateLogger) StateLogger = new KpzStateLogger();
            kpzTarget = target;
        }

        /// <summary>It fills the <see cref="grid" /> with random data.</summary>
        public void RandomizeGrid()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    grid[x, y] = new KpzNode();
                    grid[x, y].dx = random.Next(0, 2) == 0;
                    grid[x, y].dy = random.Next(0, 2) == 0;
                }
            }
            if (enableStateLogger) StateLogger.AddKpzAction("RandomizeGrid", grid);
        }

        /// <summary>
        /// Fill grid with a pattern that already contains pyramids and holes, so the KPZ algorithm can work on it.
        /// </summary>
        public void InitializeGrid()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    grid[x, y] = new KpzNode();
                    grid[x, y].dx = (bool)((x & 1) != 0);
                    grid[x, y].dy = (bool)((y & 1) != 0);
                }
            }
            if (enableStateLogger) StateLogger.AddKpzAction("InitializeGrid", grid);
        }

        /// <summary>
        /// It is used during heightmap generation.
        /// It converts <see cref="KpzNode.dx" /> and <see cref="KpzNode.dy" /> boolean values to +1 and -1 integer
        /// values.
        /// </summary>
        static int Bool2Delta(bool what)
        {
            return (what) ? 1 : -1;
        }

        /// <summary>
        /// It generates a heightmap from the <see cref="grid" />.
        /// </summary>
        /// <param name="mean"> output is the mean of the heightmap to be used in statistic calculations later.</param>
        /// <param name="periodicityValid">
        /// output is true if the periodicity of <see cref="grid" /> is correct at the boundaries.
        /// </param>
        /// <param name="periodicityInvalidXCount">
        /// output is the number of periodicity errors counted at the borders in the X direction
        /// (between left and right borders).
        /// </param>
        /// <param name="periodicityInvalidYCount">
        /// output is the number of periodicity errors counted at the borders in the Y direction
        /// (between top and bottom borders).
        /// </param>
        /// <returns>the heightmap.</returns>
        public int[,] GenerateHeightMap
        (
            out double mean,
            out bool periodicityValid,
            out int periodicityInvalidXCount,
            out int periodicityInvalidYCount
        )
        {
            const bool doVerboseLoggingToConsole = true;
            int[,] heightMap = new int[gridWidth, gridHeight];
            int heightNow = 0;
            mean = 0;
            periodicityValid = true;
            periodicityInvalidXCount = periodicityInvalidYCount = 0;
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    heightNow += Bool2Delta(grid[(x + 1) % gridWidth, y].dx);
                    heightMap[x, y] = heightNow;
                    mean += heightNow;
                }
                if (heightNow + Bool2Delta(grid[1, y].dx) != heightMap[0, y])
                {
                    periodicityValid = false;
                    periodicityInvalidXCount++;
                    if (doVerboseLoggingToConsole)
                        Console.WriteLine(String.Format("periodicityInvalidX at line {0}", y));
                }
                heightNow += Bool2Delta(grid[0, (y + 1) % gridHeight].dy);
            }
            if (heightMap[0, gridHeight - 1] + Bool2Delta(grid[0, 0].dy) != heightMap[0, 0])
            {
                periodicityValid = false;
                periodicityInvalidYCount++;
                if (doVerboseLoggingToConsole)
                    Console.WriteLine(String.Format("periodicityInvalidY {0} + {1} != {2}", heightMap[0, gridHeight - 1],
                        Bool2Delta(grid[0, 0].dy), heightMap[0, 0]));
            }
            if (enableStateLogger) StateLogger.AddKpzAction("GenerateHeightMap", heightMap);
            mean /= gridWidth * gridHeight;
            return heightMap;
        }

        /// <summary>
        /// It calculates the standard deviation of a heightmap created with <see cref="GenerateHeightMap" />.
        /// The idea behind it is available at
        /// <a href="https://en.wikipedia.org/wiki/Algorithms_for_calculating_variance#Two-pass_algorithm">Wikipedia</a>.
        /// </summary>
        /// <param name="mean">is the mean of the heightmap that was output by <see cref="GenerateHeightMap" />.</param>
        public double HeightMapStandardDeviation(int[,] inputHeightMap, double mean)
        {
            double variance = 0;
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    variance += Math.Pow(inputHeightMap[x, y] - mean, 2);
                }
            }
            variance /= gridWidth * gridHeight - 1;
            double standardDeviation = Math.Sqrt(variance);
            if (enableStateLogger)
                StateLogger.AddKpzAction(String.Format("HeightMapStandardDeviation: {0}", standardDeviation));
            return standardDeviation;
        }

        /// <summary>
        /// Detects pyramid or hole (if any) at the given coordinates in the <see cref="grid" />, and randomly switch
        /// between pyramid and hole, based on <see cref="probabilityP" /> and <see cref="probabilityQ" /> parameters.
        /// </summary>
        /// <param name="p">
        /// contains the coordinates where the function looks if there is a pyramid or hole in the <see cref="grid" />.
        /// </param>
        private void RandomlySwitchFourCells(KpzNode[,] grid, KpzCoords p)
        {
            var neighbours = GetNeighbours(grid, p);
            var currentPoint = grid[p.x, p.y];
            bool changedGrid = false;
            // We check our own {dx,dy} values, and the right neighbour's dx, and bottom neighbour's dx.
            if (
                // If we get the pattern {01, 01} we have a pyramid:
                ((currentPoint.dx && !neighbours.nx.dx) && (currentPoint.dy && !neighbours.ny.dy) &&
                (random.NextDouble() < probabilityP)) ||
                // If we get the pattern {10, 10} we have a hole:
                ((!currentPoint.dx && neighbours.nx.dx) && (!currentPoint.dy && neighbours.ny.dy) &&
                (random.NextDouble() < probabilityQ))
            )
            {
                // We make a hole into a pyramid, and a pyramid into a hole.
                currentPoint.dx = !currentPoint.dx;
                currentPoint.dy = !currentPoint.dy;
                neighbours.nx.dx = !neighbours.nx.dx;
                neighbours.ny.dy = !neighbours.ny.dy;
                changedGrid = true;
            }
            if (enableStateLogger) StateLogger.AddKpzAction("RandomlySwitchFourCells", grid, p, neighbours, changedGrid);
        }

        bool HastlayerGridAlreadyPushed = false;


        /// <summary>
        /// Runs an iteration of the KPZ algorithm (with <see cref="gridWidth"/> × <see cref="gridHeight"/> steps).
        /// </summary>
        public void DoHastIterations(uint numberOfIterations)
        {
            var numberOfStepsInIteration = gridWidth * gridHeight;
            KpzNode[,] gridBefore = (KpzNode[,])grid.Clone();
            if (enableStateLogger) StateLogger.NewKpzIteration();
            Kernels.DoIterationsWrapper(grid, !HastlayerGridAlreadyPushed, false, random.NextUInt64(), random.NextUInt64(), numberOfIterations);
            if (enableStateLogger) StateLogger.AddKpzAction("Kernels.DoHastIterations", grid, gridBefore);
            //HastlayerGridAlreadyPushed = true; //If commented out, push always
        }

        /// <summary>
        /// Runs an iteration of the KPZ algorithm (with <see cref="gridWidth"/> × <see cref="gridHeight"/> steps).
        /// It allows us to debug the steps of the algorithms one by one.
        /// </summary>
        public void DoHastIterationDebug()
        {
            var numberOfStepsInIteration = gridWidth * gridHeight;
            KpzNode[,] gridBefore = (KpzNode[,])grid.Clone();
            if (enableStateLogger) StateLogger.NewKpzIteration();
            for (int i = 0; i < numberOfStepsInIteration; i++)
            {
                Kernels.DoIterationsWrapper(grid, true, true, random.NextUInt64(), random.NextUInt64(), 1);
                if (enableStateLogger) StateLogger.AddKpzAction("Kernels.DoSingleIterationWrapper", grid, gridBefore);
            }
        }

        /// <summary>
        /// Runs an iteration of the KPZ algorithm (with <see cref="gridWidth"/> × <see cref="gridHeight"/> steps).
        /// </summary>
        public void DoIteration()
        {
            var numberOfStepsInIteration = gridWidth * gridHeight;
            if (enableStateLogger) StateLogger.NewKpzIteration();
            for (int i = 0; i < numberOfStepsInIteration; i++)
            {
                // We randomly choose a point on the grid.
                // If there is a pyramid or hole, we randomly switch them.
                var randomPoint = new KpzCoords { x = random.Next(0, gridWidth), y = random.Next(0, gridHeight) };
                RandomlySwitchFourCells(grid, randomPoint);
            }
        }

        /// <summary>
        /// Gets the right and bottom neighbours of the point given with the coordinates <see cref="p" />
        /// in the <see cref="grid" />.
        /// </summary>
        private KpzNeighbours GetNeighbours(KpzNode[,] grid, KpzCoords p)
        {
            KpzNeighbours toReturn;
            toReturn.nxCoords = new KpzCoords
            {
                x = (p.x < gridWidth - 1) ? p.x + 1 : 0,
                y = p.y
            };
            toReturn.nyCoords = new KpzCoords
            {
                x = p.x,
                y = (p.y < gridHeight - 1) ? p.y + 1 : 0
            };
            toReturn.nx = grid[toReturn.nxCoords.x, toReturn.nxCoords.y];
            toReturn.ny = grid[toReturn.nyCoords.x, toReturn.nyCoords.y];
            return toReturn;
        }
    }

    static class RandomExtensions
    {
        public static ulong NextUInt64(this Random random)
        {
            uint val1 = (uint)random.Next();
            uint val2 = (uint)random.Next();
            ulong toReturn = val1 | ((ulong)val2 << 32);
            return toReturn;
        }
    }
}
