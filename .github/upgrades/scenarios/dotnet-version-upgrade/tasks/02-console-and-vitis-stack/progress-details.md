## Files Modified
- .github/upgrades/scenarios/dotnet-version-upgrade/tasks/02-console-and-vitis-stack/task.md
- src/Hastlayer/Hast.Common/Hast.Common.csproj
- src/Hastlayer/Hast.Console/Hast.Console.csproj
- src/Hastlayer/Hast.Xilinx/Hast.Xilinx.csproj

## Build Result
- Errors: 0
- Warnings: 0 in the directly modified slice projects (`Hast.Common`, `Hast.Console`, `Hast.Xilinx`)
- Projects built:
  - src/Hastlayer/Hast.Console/Hast.Console.csproj
  - E:\Projects\Lombiq\Hastlayer\Hastlayer-SDK1\Hastlayer.SDK.sln
- Notes:
  - The targeted console-slice build passed after package cleanup.
  - A full solution build also passed, but it still reports many warnings in projects outside this task's modified scope (for example `Hast.Layer`, `Hast.Transformer.Vhdl`, `Hast.DynamicTests`, and sample projects). Those warnings are left for their owning tasks.

## Test Result
- Tests run: 2
- Passed: 2
- Failed: 0
- Test project:
  - test/Hast.Vitis.Tests/Hast.Vitis.Tests.csproj

## Changes Summary
- Upgraded `Microsoft.Extensions.Logging` in `Hast.Common` from `3.1.2` to `10.0.10` to match the .NET 10 target and assessment recommendation.
- Upgraded `Microsoft.Extensions.FileProviders.Embedded` in `Hast.Xilinx` from `5.0.4` to `10.0.10` to address the recommended/deprecated package issue.
- Removed unnecessary explicit `Microsoft.CSharp` references from `Hast.Console` and `Hast.Xilinx`, eliminating the slice-local `NU1510` warnings.
- Confirmed that `Hast.Vitis`, `Hast.Vitis.HardwareFramework`, and the refreshed Helpful Libraries REST project already built successfully on the current .NET 10 baseline without additional source changes in this task.

## Issues Encountered
- The console slice depends transitively on projects with pre-existing .NET 10 cleanup warnings outside this task's direct modification scope. These do not block the slice build or its targeted tests, but they will need to be handled in subsequent owning tasks.
