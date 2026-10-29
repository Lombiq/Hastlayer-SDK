// This file is used by Code Analysis to maintain SuppressMessage attributes that are applied to this project.
// Project-level suppressions either have no target or are given a specific target and scoped to a namespace, type,
// member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Minor Code Smell",
    "S4226:Extensions should be in separate namespaces",
    Justification = "The generated calculators keep their extensions next to them, like the other samples.",
    Scope = "module")]
[assembly: SuppressMessage(
    "Usage",
    "VSTHRD105:Avoid method overloads that assume TaskScheduler.Current",
    Justification = "There is no task scheduler in the converted code.",
    Scope = "module")]
[assembly: SuppressMessage(
    "Reliability",
    "CA2008:Do not create tasks without passing a TaskScheduler",
    Justification = "Same.",
    Scope = "module")]
[assembly: SuppressMessage(
    "Usage",
    "VSTHRD104:Offer async methods",
    Justification = "Same.",
    Scope = "module")]
