// This file is used by Code Analysis to maintain SuppressMessage attributes that are applied to this project.
// Project-level suppressions either have no target or are given a specific target and scoped to a namespace, type,
// member, etc.

using System.Diagnostics.CodeAnalysis;
using static Hast.TestInputs.Base.SuppressionConstants;

[assembly: SuppressMessage(
    "Style",
    "IDE0059:Unnecessary assignment of a value",
    Justification = ThatsThePoint,
    Scope = "module")]
[assembly: SuppressMessage(
    "Major Bug",
    "S1145:Useless \"if(true) {...}\" and \"if(false){...}\" blocks should be removed",
    Justification = ThatsThePoint,
    Scope = "module")]
[assembly: SuppressMessage(
    "Minor Code Smell",
    "S1481:Unused local variables should be removed",
    Justification = ThatsThePoint,
    Scope = "module")]
[assembly: SuppressMessage(
    "Critical Code Smell",
    "S3353:Unchanged local variables should be \"const\"",
    Justification = ThatsThePoint,
    Scope = "module")]
