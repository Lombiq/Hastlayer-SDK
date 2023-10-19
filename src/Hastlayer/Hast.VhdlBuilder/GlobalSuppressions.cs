// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "This assembly contains representations of .NET/C# code, so partial or full overlap with magic words is expected.",
    Scope = "module")]
[assembly: SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "Same.",
    Scope = "module")]
[assembly: SuppressMessage(
    "Naming",
    "CA1724: Type names should not match namespaces",
    Justification = "Same.",
    Scope = "module")]
