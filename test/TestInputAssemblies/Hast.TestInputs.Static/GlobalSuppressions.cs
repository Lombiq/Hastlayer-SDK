// This file is used by Code Analysis to maintain SuppressMessage attributes that are applied to this project.
// Project-level suppressions either have no target or are given a specific target and scoped to a namespace, type,
// member, etc.

using System.Diagnostics.CodeAnalysis;
using static Hast.TestInputs.Base.SuppressionConstants;

[assembly: SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = ThatsThePoint,
    Scope = "module")]
[assembly: SuppressMessage(
    "Style",
    "IDE0059:Unnecessary assignment of a value",
    Justification = ThatsThePoint,
    Scope = "module")]
[assembly: SuppressMessage(
    "Minor Code Smell",
    "S1481:Unused local variables should be removed",
    Justification = ThatsThePoint,
    Scope = "module")]
[assembly: SuppressMessage(
    "Major Code Smell",
    "S1854:Unused assignments should be removed",
    Justification = ThatsThePoint,
    Scope = "module")]
[assembly: SuppressMessage(
    "Major Bug",
    "S2583:Conditionally executed code should be reachable",
    Justification = ThatsThePoint,
    Scope = "module")]
[assembly: SuppressMessage(
    "Critical Code Smell",
    "S3353:Unchanged local variables should be \"const\"",
    Justification = ThatsThePoint,
    Scope = "module")]
[assembly: SuppressMessage(
    "Reliability",
    "CA2008:Do not create tasks without passing a TaskScheduler",
    Justification = "Can't do it without passing CancellationToken which is not supported.")]
[assembly: SuppressMessage(
    "Usage",
    "VSTHRD105:Avoid method overloads that assume TaskScheduler.Current",
    Justification = "Can't do it without passing CancellationToken which is not supported.")]
