# 02-console-and-vitis-stack: Upgrade the console entry point and the Vitis/Xilinx dependency slice

Upgrade `Hast.Console` as the first entry-point application together with the dependency chain it exercises: `Hast.Vitis`, `Hast.Vitis.HardwareFramework`, `Hast.Xilinx`, `Hast.Synthesis`, `Hast.Common`, and the Helpful Libraries REST support it consumes. The assessment flags API issues in `Hast.Vitis`, `Hast.Xilinx`, and related shared libraries, making this the smallest high-value application slice that will expose real .NET 10 code changes early.

This task should also absorb any direct package and source fixes needed in the Vitis hardware submodule and supporting projects so the console scenario can build cleanly on .NET 10. Start research with the CodeDom and API-compatibility findings in the Vitis/Xilinx path, plus any changes introduced by the refreshed submodule revisions.

**Done when**: `Hast.Console` and its Vitis/Xilinx dependency slice target .NET 10 successfully, the relevant projects build warning-free, and any directly affected tests or verification commands for this slice pass.

## Research Findings

### Projects affected
- `src/Hastlayer/Hast.Console/Hast.Console.csproj` — already retargeted to `net10.0`; currently builds, but still emits a `NU1510` warning for an unnecessary `Microsoft.CSharp` reference.
- `src/Hastlayer/Hast.Vitis/Hast.Vitis.csproj` — already retargeted to `net10.0`; carries the inline `BinaryOpenCl` helper change and assessment findings focused on `AzureHardwareImplementationComposerBuildProvider`, `AzureStorageService`, and URI/time-span behavioral differences.
- `src/Hastlayer/Hast.Xilinx/Hast.Xilinx.csproj` — already retargeted to `net10.0`; assessment flags both a package upgrade/deprecation for `Microsoft.Extensions.FileProviders.Embedded` and API compatibility around `System.IO.Ports.SerialPort`.
- `src/Hastlayer/Hast.Common/Hast.Common.csproj` — already retargeted to `net10.0`; assessment recommends upgrading `Microsoft.Extensions.Logging` to `10.0.10` and notes a URI behavioral change in `Services/AppDataFolder.cs`.
- `src/Hastlayer/Hast.Synthesis/Hast.Synthesis.csproj` — already retargeted to `net10.0` and has no additional assessment issues beyond the target framework move.
- `src/HardwareFrameworks/Vitis/Hast.Vitis.HardwareFramework.csproj` — submodule project file is already changed from `netstandard2.1` to `net10.0` and has no additional assessment issues.
- `src/Libraries/External/Lombiq.HelpfulLibraries/Lombiq.HelpfulLibraries.RestEase/Lombiq.HelpfulLibraries.RestEase.csproj` — already on `net10.0` with no assessment issues.

### Files likely to need changes
- `src/Hastlayer/Hast.Common/Hast.Common.csproj` — package version cleanup for `Microsoft.Extensions.Logging` and any now-unnecessary explicit package references.
- `src/Hastlayer/Hast.Console/Hast.Console.csproj` — remove warnings from unnecessary package references.
- `src/Hastlayer/Hast.Xilinx/Hast.Xilinx.csproj` and possibly `src/Hastlayer/Hast.Xilinx/SerialPortConfigurator.cs` — update deprecated package usage and verify whether the serial-port API needs any source adjustment under .NET 10.
- `src/Hastlayer/Hast.Vitis/Hast.Vitis.csproj` plus Vitis service files flagged by assessment — verify whether the build already covers the flagged API usage or whether follow-up code cleanup is still needed.
- `src/HardwareFrameworks/Vitis/Hast.Vitis.HardwareFramework.csproj` — keep the submodule retargeting aligned with the consuming `Hast.Vitis` project.

### Build findings
- `dotnet build src/Hastlayer/Hast.Console/Hast.Console.csproj` succeeds on `net10.0`, so this task is not blocked by compiler errors.
- The console slice still produces warnings in the current state, including `NU1510` on `Hast.Console` and `Hast.Xilinx`, plus downstream warnings in transitive dependencies that the slice pulls in during the build.
- The current task scope is still atomic: it is one application slice with closely related package/reference cleanup and a limited set of compatibility reviews, rather than separate independent migration concerns.

### Package actions to evaluate
- `Microsoft.Extensions.Logging` in `Hast.Common`: `3.1.2` → `10.0.10`.
- `Microsoft.Extensions.FileProviders.Embedded` in `Hast.Xilinx`: `5.0.4` → `10.0.10`.
- Remove explicit `Microsoft.CSharp` references where .NET 10 now resolves them transitively and the build warns they are unnecessary.

### Dependencies and risks
- Building the console slice also compiles `Hast.Transformer.Vhdl` and `Hast.Communication` through the `Hast.Xilinx` dependency chain, so warning cleanup may reveal additional shared-project package issues outside the immediate leaf projects.
- The Vitis stack spans both the root repository and the `src/HardwareFrameworks/Vitis` submodule, so project-file changes may need to stay coordinated across repository boundaries.
