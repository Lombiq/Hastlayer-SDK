# Hastlayer SDK - NuGet test solution

This solution serves as a testbed for the Hastlayer NuGet packages. It's also a simple example of how to use Hastlayer in your project: Just check out [`Hast.NuGet.Console`](Hast.NuGet.Console)!

`Hast.Samples.SampleAssembly` uses published packages in this solution rather than the main solution's project references.
The source-only `PositComparisonCalculator`, `PositDivisionCalculator`, `PositPrecisionConversionCalculator`, and
`PositUnaryCalculator` samples are excluded here because the pinned `Lombiq.Arithmetics` package does not yet contain
their E2 APIs. They remain included in the main solution.

Use Release for the package build and static analysis, but run the VHDL approval tests in Debug to preserve the baseline's
IL-derived identifiers. When switching between this solution and `Hastlayer.SDK.sln`, refresh the destination solution's
restore graph before using `--no-restore`: the shared sample project also shares its `obj` assets between the two modes.
