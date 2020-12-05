using Hast.Samples.Kpz.Algorithms;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;

namespace Hast.Samples.Kpz
{
    /// <summary>
    /// A KPZ iteration to be logged consists of a list of <see cref="KpzAction" /> items.
    /// </summary>
    public class KpzIteration
    {
        public List<KpzAction> Actions { get; set; } = new List<KpzAction>();
    }

    /// <summary>
    /// A KPZ action consists of a description, the full grid or heightmap and the highlight in it.
    /// There are three typical types of KPZ actions:
    /// <list type="bullet">
    /// <item><description>Empty <see cref="Grid" /> and <see cref="HeightMap" />, only <see cref="Description" />.
    /// </item></description>
    /// <item><description>Only <see cref="Grid" />, <see cref="Description" /> and optional highlight.
    /// </item></description>
    /// <item><description>Only <see cref="HeightMap" />, <see cref="Description" /> and optional highlight.
    /// </item></description>
    /// </list>
    /// </summary>
    [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "The type's never checked for equality.")]
    public struct KpzAction
    {
        public string Description { get; set; }
        public KpzNode[,] Grid { get; set; }
        public int[,] HeightMap { get; set; }
        public List<KpzCoords> HighlightedCoords { get; set; }
        public Color HightlightColor { get; set; }
    }

    /// <summary>
    /// It logs the state of the KPZ algorithm at particular steps.
    /// <note type="caution"><para>As it stores the full KPZ grid at every step, it can use up a lot of memory.</para></note>
    /// </summary>
    public class KpzStateLogger
    {
        /// <summary>The KPZ iteration list.</summary>
        public List<KpzIteration> Iterations { get; set; } = new List<KpzIteration>();

        /// <summary>We add an iteration when the constructor is called, so actions can be added right away.</summary>
        public KpzStateLogger() => NewKpzIteration();

        /// <summary>Add a new <see cref="KpzIteration" />.</summary>
        public void NewKpzIteration() => Iterations.Add(new KpzIteration());

        /// <summary>
        /// Adds a deep copy of the grid into the current <see cref="KpzStateLogger" /> iteration.
        /// </summary>
        public void AddKpzAction(string Description, KpzNode[,] Grid) => Iterations[^1].Actions.Add(new KpzAction
        {
            Description = Description,
            Grid = CopyOfGrid(Grid),
            HeightMap = new int[0, 0],
            HightlightColor = Color.Transparent,
            HighlightedCoords = new List<KpzCoords>(),
        });

        /// <summary>
        /// Adds a deep copy of the heightmap into the current <see cref="KpzStateLogger" /> iteration.
        /// </summary>
        public void AddKpzAction(string Description, int[,] HeightMap) => Iterations[^1].Actions.Add(new KpzAction
        {
            Description = Description,
            Grid = new KpzNode[0, 0],
            HeightMap = CopyOfHeightMap(HeightMap),
            HightlightColor = Color.Transparent,
            HighlightedCoords = new List<KpzCoords>(),
        });

        /// <summary>
        /// Adds an action with only description into the current <see cref="KpzStateLogger" /> iteration.
        /// </summary>
        public void AddKpzAction(string Description) =>
            // Adds a deep copy of the grid into the current iteration
            Iterations[^1].Actions.Add(new KpzAction
            {
                Description = Description,
                Grid = new KpzNode[0, 0],
                HeightMap = new int[0, 0],
                HightlightColor = Color.Transparent,
                HighlightedCoords = new List<KpzCoords>(),
            });

        /// <summary>
        /// Adds a deep copy of the grid into the current <see cref="KpzStateLogger" /> iteration, and highlights all
        /// the cells changed between <see cref="Grid" /> and <see cref="GridBefore" /> with yellow color.
        /// </summary>
        public void AddKpzAction(string Description, KpzNode[,] Grid, KpzNode[,] GridBefore)
        {
            var highlightedCoords = new List<KpzCoords>();

            for (int x = 0; x < Grid.GetLength(0); x++)
            {
                for (int y = 0; y < Grid.GetLength(1); y++)
                {
                    if (GridBefore[x, y].dx != Grid[x, y].dx || GridBefore[x, y].dy != Grid[x, y].dy)
                    {
                        highlightedCoords.Add(new KpzCoords { x = x, y = y });
                    }
                }
            }

            Iterations[^1].Actions.Add(new KpzAction
            {
                Description = Description,
                Grid = CopyOfGrid(Grid),
                HeightMap = new int[0, 0],
                HightlightColor = Color.Orange,
                HighlightedCoords = highlightedCoords,
            });
        }

        /// <summary>
        /// Adds a deep copy of the grid into the current <see cref="KpzStateLogger" /> iteration, with cells
        /// to highlight (<see cref="Center" /> and <see cref="Neighbours" />). If the values in the grid were updated,
        /// they are highlighted with a green color, else they are highlighted with a red color, based on the parameter
        /// value of <see cref="ChangedGrid" />.
        /// </summary>
        public void AddKpzAction(string Description, KpzNode[,] Grid, KpzCoords Center,
            KpzNeighbours Neighbours, bool ChangedGrid)
        {
            var highlightedCoords = new List<KpzCoords>
            {
                new KpzCoords { x = Center.x, y = Center.y },
                new KpzCoords { x = Neighbours.nxCoords.x, y = Neighbours.nxCoords.y },
                new KpzCoords { x = Neighbours.nyCoords.x, y = Neighbours.nyCoords.y },
            };

            Iterations[^1].Actions.Add(new KpzAction
            {
                Description = Description,
                Grid = CopyOfGrid(Grid),
                HeightMap = new int[0, 0],
                HightlightColor = (ChangedGrid) ? Color.LightGreen : Color.Salmon, // green or red
                HighlightedCoords = highlightedCoords,
            });
        }

        public void WriteToFiles(string path)
        {
            if (!path.EndsWith("\\", StringComparison.Ordinal)) path += "\\";

            int iterationIndex = 0;

            foreach (var iteration in Iterations)
            {
                using var file = new System.IO.StreamWriter(path + (iterationIndex++).ToString(CultureInfo.InvariantCulture) + ".txt");
                foreach (var action in iteration.Actions)
                {
                    file.WriteLine(action.Description);

                    for (int y = 0; y < action.Grid.GetLength(1); y++)
                    {
                        string line = "";

                        for (int x = 0; x < action.Grid.GetLength(0); x++)
                        {
                            line += ((action.Grid[x, y].dx) ? "1" : "0") +
                                ((action.Grid[x, y].dy) ? "1" : "0") + " ";
                        }

                        file.WriteLine(line);
                    }
                }
            }
        }

        /// <summary>Make a deep copy of a heightmap (2D int array).</summary>
        private static int[,] CopyOfHeightMap(int[,] HeightMap) => (int[,])HeightMap.Clone();

        /// <summary>Make a deep copy of a grid (2D <see cref="KpzNode" /> array).</summary>
        private static KpzNode[,] CopyOfGrid(KpzNode[,] Grid)
        {
            var toReturn = new KpzNode[Grid.GetLength(0), Grid.GetLength(1)];
            for (int x = 0; x < Grid.GetLength(0); x++)
            {
                for (int y = 0; y < Grid.GetLength(1); y++)
                {
                    toReturn[x, y] = new KpzNode
                    {
                        dx = Grid[x, y].dx,
                        dy = Grid[x, y].dy,
                    };
                }
            }

            return toReturn;
        }
    }
}
