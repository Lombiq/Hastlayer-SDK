// This file is used by Code Analysis to maintain SuppressMessage attributes that are applied to this project.
// Project-level suppressions either have no target or are given a specific target and scoped to a namespace, type,
// member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Minor Code Smell",
    "S1481:Unused local variables should be removed",
    Justification = "Information in the variable name.",
    Scope = "member",
    Target = "~M:Hast.Samples.Demo.Program.Main~System.Threading.Tasks.Task")]
[assembly: SuppressMessage(
    "Globalization",
    "CA1303:Do not pass literals as localized parameters",
    Justification = "This app is not localized.",
    Scope = "member",
    Target = "~M:Hast.Samples.Demo.Program.Main~System.Threading.Tasks.Task")]
[assembly: SuppressMessage(
    "StyleCop.CSharp.ReadabilityRules",
    "SA1123:Do not place regions within elements",
    Justification = "Regions help sectioning the demo making it more suitable for presentation.",
    Scope = "member",
    Target = "~M:Hast.Samples.Demo.Program.Main~System.Threading.Tasks.Task")]
