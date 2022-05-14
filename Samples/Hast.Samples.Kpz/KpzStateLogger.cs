using Hast.Samples.Kpz.Algorithms;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Hast.Samples.Kpz;

/// <summary>
/// A KPZ iteration to be logged consists of a list of <see cref="KpzAction"/> items.
/// </summary>
public class KpzIteration
{
    [SuppressMessage(
        "Design",
        "MA0016:Prefer return collection abstraction instead of implementation",
        Justification = "Needs to be both expandable and indexable.")]
    public List<KpzAction> Actions { get; } = new List<KpzAction>();
}

/// <summary>
/// <para>
/// A KPZ action consists of a description, the full grid or heightmap and the highlight in it. There are three typical
/// types of KPZ actions:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Empty <see cref="Grid"/> and <see cref="HeightMap"/>, only <see cref="Description"/>.</description>
/// </item>
/// <item>
/// <description>Only <see cref="Grid"/>, <see cref="Description"/> and optional highlight.</description>
/// </item>
/// <item>
/// <description>Only <see cref="HeightMap"/>, <see cref="Description"/> and optional highlight.</description>
/// </item>
/// </list>
/// </summary>
public class KpzAction
{
    public string Description { get; set; }

    // Can't avoid it without rewriting the whole thing.
#pragma warning disable CA1819 // Properties should not return arrays
    public KpzNode[,] Grid { get; set; }
    public int[,] HeightMap { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

    public ICollection<KpzCoords> HighlightedCoords { get; } = new List<KpzCoords>();
    public Color HightlightColor { get; set; }
}

/// <summary>
/// <para>It logs the state of the KPZ algorithm at particular steps.</para>
/// <note type="caution">
/// <para>As it stores the full KPZ grid at every step, it can use up a lot of memory.</para>
/// </note>
/// </summary>
public class KpzStateLogger
{
    /// <summary>
    /// Gets the KPZ iteration list.
    /// </summary>
    [SuppressMessage(
        "Design",
        "MA0016:Prefer return collection abstraction instead of implementation",
        Justification = "Needs to be both expandable and indexable.")]
    public List<KpzIteration> Iterations { get; } = new List<KpzIteration>();

    /// <summary>
    /// Initializes a new instance of the <see cref="KpzStateLogger"/> class.We add an iteration when the constructor is
    /// called, so actions can be added right away.
    /// </summary>
    public KpzStateLogger() => NewKpzIteration();

    /// <summary>
    /// Add a new <see cref="KpzIteration"/>.
    /// </summary>
    public void NewKpzIteration() => Iterations.Add(new KpzIteration());

    /// <summary>
    /// Adds a deep copy of the grid into the current <see cref="KpzStateLogger"/> iteration.
    /// </summary>
    public void AddKpzAction(string description, Kpz kpz) =>
        Iterations[^1].Actions.Add(new KpzAction
        {
            Description = description,
            Grid = CopyOfGrid(kpz.Grid),
            HeightMap = new int[0, 0],
            HightlightColor = Color.Transparent,
        });

    /// <summary>
    /// Adds a deep copy of the heightmap into the current <see cref="KpzStateLogger"/> iteration.
    /// </summary>
    internal void AddKpzAction(string description, int[,] heightMap) => Iterations[^1].Actions.Add(new KpzAction
    {
        Description = description,
        Grid = new KpzNode[0, 0],
        HeightMap = CopyOfHeightMap(heightMap),
        HightlightColor = Color.Transparent,
    });

    /// <summary>
    /// Adds an action with only description into the current <see cref="KpzStateLogger"/> iteration.
    /// </summary>
    public void AddKpzAction(FormattableString description) =>
        // Adds a deep copy of the grid into the current iteration
        Iterations[^1].Actions.Add(new KpzAction
        {
            Description = description.ToString(CultureInfo.InvariantCulture),
            Grid = new KpzNode[0, 0],
            HeightMap = new int[0, 0],
            HightlightColor = Color.Transparent,
        });

    /// <summary>
    /// Adds a deep copy of the grid into the current <see cref="KpzStateLogger"/> iteration, and highlights all the
    /// cells changed between Grid and GridBefore with yellow color.
    /// </summary>
    internal void AddKpzAction(string description, KpzNode[,] grid, KpzNode[,] gridBefore)
    {
        var highlightedCoords = new List<KpzCoords>();

        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                if (gridBefore[x, y].Dx != grid[x, y].Dx || gridBefore[x, y].Dy != grid[x, y].Dy)
                {
                    highlightedCoords.Add(new KpzCoords { X = x, Y = y });
                }
            }
        }

        var kpzAction = new KpzAction
        {
            Description = description,
            Grid = CopyOfGrid(grid),
            HeightMap = new int[0, 0],
            HightlightColor = Color.Orange,
        };
        kpzAction.HighlightedCoords.AddRange(highlightedCoords);
        Iterations[^1].Actions.Add(kpzAction);
    }

    /// <summary>
    /// Adds a deep copy of the grid into the current <see cref="KpzStateLogger"/> iteration, with cells to highlight (
    /// <paramref name="center"/> and <paramref name="neighbours"/>). If the values in the grid were updated, they are
    /// highlighted with a green color, else they are highlighted with a red color, based on the parameter value of
    /// <paramref name="changedGrid"/>.
    /// </summary>
    internal void AddKpzAction(
        string description,
        KpzNode[,] grid,
        KpzCoords center,
        KpzNeighbours neighbours,
        bool changedGrid)
    {
        var highlightedCoords = new List<KpzCoords>
        {
            new KpzCoords { X = center.X, Y = center.Y },
            new KpzCoords { X = neighbours.NxCoords.X, Y = neighbours.NxCoords.Y },
            new KpzCoords { X = neighbours.NyCoords.X, Y = neighbours.NyCoords.Y },
        };

        var kpzAction = new KpzAction
        {
            Description = description,
            Grid = CopyOfGrid(grid),
            HeightMap = new int[0, 0],
            HightlightColor = changedGrid ? Color.LightGreen : Color.Salmon, // green or red
        };
        kpzAction.HighlightedCoords.AddRange(highlightedCoords);
        Iterations[^1].Actions.Add(kpzAction);
    }

    public void WriteToFiles(string path)
    {
        if (!path.EndsWithOrdinal("\\")) path += "\\";

        int iterationIndex = 0;

        foreach (var iteration in Iterations)
        {
            using var file = new StreamWriter(path + iterationIndex.ToTechnicalString() + ".txt");
            iterationIndex++;

            WriteToFile(iteration, file);
        }
    }

    private static void WriteToFile(KpzIteration iteration, StreamWriter file)
    {
        foreach (var action in iteration.Actions)
        {
            file.WriteLine(action.Description);

            for (int y = 0; y < action.Grid.GetLength(1); y++)
            {
                var lines = Enumerable
                    .Range(0, action.Grid.GetLength(0))
                    .Select(x => (action.Grid[x, y].Dx ? "1" : "0") +
                        (action.Grid[x, y].Dy ? "1" : "0") + " ");

                file.WriteLine(string.Join(string.Empty, lines));
            }
        }
    }

    /// <summary>
    /// Make a deep copy of a heightmap (2D int array).
    /// </summary>
    private static int[,] CopyOfHeightMap(int[,] heightMap) => (int[,])heightMap.Clone();

    /// <summary>
    /// Make a deep copy of a grid (2D <see cref="KpzNode"/> array).
    /// </summary>
    private static KpzNode[,] CopyOfGrid(KpzNode[,] grid)
    {
        var toReturn = new KpzNode[grid.GetLength(0), grid.GetLength(1)];
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                toReturn[x, y] = new KpzNode
                {
                    Dx = grid[x, y].Dx,
                    Dy = grid[x, y].Dy,
                };
            }
        }

        return toReturn;
    }
}
