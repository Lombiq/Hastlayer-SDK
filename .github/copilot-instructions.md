# Copilot Instructions

## Project Guidelines
- In the .NET 10 upgrade workflow for Hastlayer-SDK, treat Hast.DynamicTests as non-blocking; all other tests should pass.
- During this .NET 10 upgrade, fix all build warnings too; treat warnings like errors across the solution, including IDE0300 collection initialization simplification warnings and similar code analyzer warnings as part of warning cleanup. Use rebuild-time analyzer output as the source of truth for warning cleanup because RunAnalyzersDuringBuild is enabled in Directory.Build.props. Ensure the whole solution builds with zero warnings, fixing warning-bearing projects outside the current slice as well.
