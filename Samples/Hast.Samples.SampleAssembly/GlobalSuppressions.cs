// This file is used by Code Analysis to maintain SuppressMessage attributes that are applied to this project.
// Project-level suppressions either have no target or are given a specific target and scoped to a namespace, type,
// member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Style",
    "IDE0059:Unnecessary assignment of a value",
    Justification = "These sample assemblies demonstrate various features of the code transformation framework so less fancy code can be expected.",
    Scope = "module")]
[assembly: SuppressMessage(
    "Style",
    "IDE0060:Remove unused parameter",
    Justification = "Same.",
    Scope = "module")]
[assembly: SuppressMessage(
    "Critical Code Smell",
    "S3776:Cognitive Complexity of methods should not be too high",
    Justification = "Same.",
    Scope = "module")]
[assembly: SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Same.",
    Scope = "module")]
[assembly: SuppressMessage(
    "Minor Code Smell",
    "S4226:Extensions should be in separate namespaces",
    Justification = "Same.",
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

[assembly: SuppressMessage(
    "Globalization",
    "CA1307:Specify StringComparison for clarity",
    Justification = "There is no StringComparison in the transformed code.",
    Scope = "module")]

[assembly: SuppressMessage(
    "Performance",
    "CA1802:Use literals where appropriate",
    Justification = "This is related to the special build-time constant overriding feature, see more info in the WorkingWithHastlayer.md document.",
    Scope = "module")]
[assembly: SuppressMessage(
    "Minor Code Smell",
    "S3962:\"static readonly\" constants should be \"const\" instead",
    Justification = "Same.",
    Scope = "module")]
