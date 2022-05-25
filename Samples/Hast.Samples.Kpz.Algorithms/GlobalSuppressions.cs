// This file is used by Code Analysis to maintain SuppressMessage attributes that are applied to this project.
// Project-level suppressions either have no target or are given a specific target and scoped to a namespace, type,
// member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Performance",
    "CA1814:Prefer jagged arrays over multidimensional",
    Justification = "It is known to be a rectangle grid and all the code is written for it.",
    Scope = "module")]
[assembly: SuppressMessage(
    "Blocker Code Smell",
    "S2368:Public methods should not have multidimensional array parameters",
    Justification = "KPZ operates with a grid, it's necessary to manage a two-dimensional array of KpzNodes.")]
