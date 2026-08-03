# 01-toolchain-and-submodule-baseline: Align the SDK, submodules, and shared upgrade baseline

Validate the .NET 10 SDK and repository toolchain assumptions before changing project targets, then lock in the requested submodule refresh as the migration baseline. This task covers the root solution metadata, any `global.json` or build configuration updates required for .NET 10, and reconciling the now-updated `Lombiq.HelpfulLibraries`, `Lombiq.Analyzers`, `Lombiq.Arithmetics`, and `Vitis` submodule states with the main repository.

The assessment already shows a solution-wide framework move plus compatibility work in shared code, so this task also establishes the package/version baseline that later application tasks inherit. Research should start with SDK pinning, analyzer compatibility, and whether the updated submodules already force specific TFM or package changes into dependent projects.

**Done when**: The repository is pinned to a verified .NET 10-capable toolchain, the refreshed submodule state is recorded in the root repo, and the baseline build constraints for the remaining migration are documented in the task record.

## Research Findings

### Projects and repositories affected
- `Hastlayer.SDK.sln` — all in-solution projects are already SDK-style and currently modified toward `net10.0`, so the baseline task is about validating and documenting the toolchain assumptions rather than introducing a new project format step.
- Root repository metadata — `.github/workflows/build-and-test.yml`, `Directory.Build.props`, and `NuGet.config` define the main build baseline.
- `src/Libraries/External/Lombiq.HelpfulLibraries` — refreshed to commit `7cc0b77` from `origin/dev`; submodule is at the latest dev commit but remains checked out in detached HEAD state and contains local import-condition and project updates needed by the parent solution.
- `tools/Lombiq.Analyzers` — refreshed to commit `a4dc99c` from `origin/dev`; submodule is also at the latest dev commit in detached HEAD state.
- `src/Libraries/External/Lombiq.Arithmetics` and `src/HardwareFrameworks/Vitis` — both remain on local `dev` branches with pending project-file retargeting changes already present in the working tree.

### Files to review and carry forward
- `.github/workflows/build-and-test.yml` — already requests `10.0.x` for the root and NuGet test workflows, so CI has already been aligned with the target SDK.
- `Directory.Build.props` — continues to import the refreshed analyzers submodule from `tools/Lombiq.Analyzers/Lombiq.Analyzers/Build.props`.
- `src/Hastlayer/Hast.Common/Hast.Common.csproj`, `src/Hastlayer/Hast.Console/Hast.Console.csproj`, and many peer project files — already retargeted to `net10.0` in the current working tree, which means later tasks inherit a partially-upgraded baseline rather than starting from pristine `net6.0` sources.
- `src/Libraries/External/Lombiq.HelpfulLibraries/Directory.Build.props`, `.../Lombiq.HelpfulLibraries.Common.csproj`, and `.../Lombiq.HelpfulLibraries.RestEase.csproj` — contain local fallback import adjustments and remain part of the refreshed baseline.

### Toolchain and build constraints
- `validate_dotnet_sdk_installation(net10.0)` succeeded, confirming a compatible local SDK is installed.
- `validate_dotnet_sdk_in_globaljson(net10.0)` reported no `global.json`, so there is no repository SDK pin to update.
- The solution can use `dotnet build` for iterative and final validation because the in-solution projects are SDK-style and currently target modern .NET / .NET Standard TFMs.
- The main baseline risk for subsequent tasks is not SDK availability but reconciling the already-present project and submodule edits with any remaining package/API fixes.

### Decisions made
- Treat the current working tree as the accepted .NET 10 migration baseline because the root workflow, many project TFMs, and the relevant submodule commits were already advanced before task execution resumed.
- Defer any submodule branch-normalization work unless it becomes necessary for committing inside those repositories; their current commits already match the latest `origin/dev` state requested by the user.
