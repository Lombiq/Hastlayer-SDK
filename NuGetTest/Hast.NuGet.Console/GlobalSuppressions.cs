// This file is used by Code Analysis to maintain SuppressMessage attributes that are applied to this project.
// Project-level suppressions either have no target or are given a specific target and scoped to a namespace, type,
// member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Minor Code Smell",
    "S1481:Unused local variables should be removed",
    Justification = "Information in the variable name.",
    Scope = "module")]
[assembly: SuppressMessage(
    "Globalization",
    "CA1303:Do not pass literals as localized parameters",
    Justification = "This app is not localized.",
    Scope = "module")]
