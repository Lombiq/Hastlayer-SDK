// This file is used by Code Analysis to maintain SuppressMessage attributes that are applied to this project.
// Project-level suppressions either have no target or are given a specific target and scoped to a namespace, type,
// member, etc.

using System.Diagnostics.CodeAnalysis;
using static Hast.TestInputs.Base.SuppressionConstants;

#pragma warning disable S103 // Lines should not be too long
[assembly: SuppressMessage(
    "Major Code Smell",
    "S107:Methods should not have too many parameters",
    Justification = "It's easy to follow and reducing the argument count would only make the code more complicated and harder to read.",
    Scope = "member",
    Target = "~M:Hast.TestInputs.Dynamic.BinaryAndUnaryOperatorExpressionCases.SaveResult(Hast.Transformer.Abstractions.SimpleMemory.SimpleMemory,System.Int32,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64)")]
[assembly: SuppressMessage(
    "Style",
    "IDE0059:Unnecessary assignment of a value",
    Justification = ThatsThePoint,
    Scope = "module")]
[assembly: SuppressMessage(
    "Major Code Smell",
    "S1854:Unused assignments should be removed",
    Justification = ThatsThePoint,
    Scope = "module")]
#pragma warning restore S103 // Lines should not be too long
