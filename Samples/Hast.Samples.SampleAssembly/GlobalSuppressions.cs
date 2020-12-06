// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Usage",
    "VSTHRD105:Avoid method overloads that assume TaskScheduler.Current",
    Justification = "There is no TaskScheduler support.",
    Scope = "module")]
[assembly: SuppressMessage(
    "Reliability",
    "CA2008:Do not create tasks without passing a TaskScheduler",
    Justification = "There is no TaskScheduler support.",
    Scope = "module")]

[assembly: SuppressMessage(
    "Usage",
    "VSTHRD104:Offer async methods",
    Justification = "The samples are intentionally not doing that.",
    Scope = "module")]

[assembly: SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Not supported.",
    Scope = "module")]
[assembly: SuppressMessage(
    "Critical Code Smell",
    "S3776:Cognitive Complexity of methods should not be too high",
    Justification = "Not supported.",
    Scope = "module")]

[assembly: SuppressMessage(
    "Minor Code Smell",
    "S1643:Strings should not be concatenated using '+' in a loop",
    Justification = "Don't use StringBuilder in code to be translated.",
    Scope = "module")]

[assembly: SuppressMessage(
    "Minor Code Smell",
    "S4136:Method overloads should be grouped together",
    Justification = "We want to keep the host and device methods separate in the samples.",
    Scope = "module")]

[assembly: SuppressMessage(
    "Performance",
    "CA1802:Use literals where appropriate",
    Justification = "Required for the readonly substitution feature.",
    Scope = "module")]
[assembly: SuppressMessage(
    "Minor Code Smell",
    "S3962:\"static readonly\" constants should be \"const\" instead",
    Justification = "Required for the readonly substitution feature.",
    Scope = "module")]

[assembly: SuppressMessage(
    "Major Code Smell",
    "S107:Methods should not have too many parameters",
    Justification = "We don't want to pass around objects so it's necessary.",
    Scope = "module")]
