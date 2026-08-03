# 01-toolchain-and-submodule-baseline: Align the SDK, submodules, and shared upgrade baseline

Validate the .NET 10 SDK and repository toolchain assumptions before changing project targets, then lock in the requested submodule refresh as the migration baseline. This task covers the root solution metadata, any `global.json` or build configuration updates required for .NET 10, and reconciling the now-updated `Lombiq.HelpfulLibraries`, `Lombiq.Analyzers`, `Lombiq.Arithmetics`, and `Vitis` submodule states with the main repository.

The assessment already shows a solution-wide framework move plus compatibility work in shared code, so this task also establishes the package/version baseline that later application tasks inherit. Research should start with SDK pinning, analyzer compatibility, and whether the updated submodules already force specific TFM or package changes into dependent projects.

**Done when**: The repository is pinned to a verified .NET 10-capable toolchain, the refreshed submodule state is recorded in the root repo, and the baseline build constraints for the remaining migration are documented in the task record.
