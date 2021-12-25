// This file is used by Code Analysis to maintain SuppressMessage attributes that are applied to this project.
// Project-level suppressions either have no target or are given a specific target and scoped to a namespace, type,
// member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Performance",
    "CA1814:Prefer jagged arrays over multidimensional",
    Justification = "It's for 2D grids so multidimensional is more appropriate.",
    Scope = "module")]
