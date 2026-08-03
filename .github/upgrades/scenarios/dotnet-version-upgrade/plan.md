# .NET Version Upgrade Plan

## Overview

**Target**: Upgrade `Hastlayer.SDK.sln` and its participating submodules from .NET 6 / .NET Standard 2.x to .NET 10, including required package and source compatibility fixes.
**Scope**: Large solution, 36 analyzed projects, deep 11-level dependency graph, refreshed submodule baselines for `Lombiq.HelpfulLibraries` and `Lombiq.Analyzers`, and additional code/package work expected in `Lombiq.Arithmetics` and the Vitis stack.

### Selected Strategy
**Top-Down (Application-First)** — Applications upgraded first, libraries multi-targeted temporarily where needed.
**Rationale**: The solution is too large and interconnected for a safe all-at-once bump, while all projects are already on modern .NET and can be migrated incrementally by application slice.

Applications, in priority order: `Hast.Console`, `Hast.Samples.Demo`, `Hast.Samples.Consumer`, `Hast.Communication.Tester`, plus the top-level test executables that validate each slice.
Libraries expected to participate during Phase 1: `Hast.Vitis`, `Hast.Vitis.HardwareFramework`, `Hast.Xilinx`, `Hast.Layer`, `Hast.Communication`, `Hast.Transformer`, `Hast.Transformer.Vhdl`, `Hast.VhdlBuilder`, `Hast.Synthesis`, `Hast.Common`, `Hast.Algorithms`, `Lombiq.Arithmetics`, and the updated Helpful Libraries projects.
Phase 2 begins once every entry-point application and its tests run on .NET 10, at which point temporary dual-targeting and conditional compatibility code can be removed from shared libraries.

## Tasks

### 01-toolchain-and-submodule-baseline: Align the SDK, submodules, and shared upgrade baseline

Validate the .NET 10 SDK and repository toolchain assumptions before changing project targets, then lock in the requested submodule refresh as the migration baseline. This task covers the root solution metadata, any `global.json` or build configuration updates required for .NET 10, and reconciling the now-updated `Lombiq.HelpfulLibraries`, `Lombiq.Analyzers`, `Lombiq.Arithmetics`, and `Vitis` submodule states with the main repository.

The assessment already shows a solution-wide framework move plus compatibility work in shared code, so this task also establishes the package/version baseline that later application tasks inherit. Research should start with SDK pinning, analyzer compatibility, and whether the updated submodules already force specific TFM or package changes into dependent projects.

**Done when**: The repository is pinned to a verified .NET 10-capable toolchain, the refreshed submodule state is recorded in the root repo, and the baseline build constraints for the remaining migration are documented in the task record.

---

### 02-console-and-vitis-stack: Upgrade the console entry point and the Vitis/Xilinx dependency slice

Upgrade `Hast.Console` as the first entry-point application together with the dependency chain it exercises: `Hast.Vitis`, `Hast.Vitis.HardwareFramework`, `Hast.Xilinx`, `Hast.Synthesis`, `Hast.Common`, and the Helpful Libraries REST support it consumes. The assessment flags API issues in `Hast.Vitis`, `Hast.Xilinx`, and related shared libraries, making this the smallest high-value application slice that will expose real .NET 10 code changes early.

This task should also absorb any direct package and source fixes needed in the Vitis hardware submodule and supporting projects so the console scenario can build cleanly on .NET 10. Start research with the CodeDom and API-compatibility findings in the Vitis/Xilinx path, plus any changes introduced by the refreshed submodule revisions.

**Done when**: `Hast.Console` and its Vitis/Xilinx dependency slice target .NET 10 successfully, the relevant projects build warning-free, and any directly affected tests or verification commands for this slice pass.

---

### 03-sample-applications-and-shared-pipeline: Upgrade the sample applications and the shared transformation pipeline they depend on

Upgrade `Hast.Samples.Demo` and `Hast.Samples.Consumer` together with the libraries they bring into the application layer: `Hast.Layer`, `Hast.Transformer`, `Hast.Transformer.Vhdl`, `Hast.VhdlBuilder`, `Hast.Algorithms`, `Hast.Samples.SampleAssembly`, `Hast.Samples.FSharpSampleAssembly`, `Hast.Samples.Kpz.Algorithms`, `Lombiq.Arithmetics`, and any remaining shared dependencies required to keep these applications buildable during the transition. This is the broadest application slice in the solution and touches the projects most likely to surface package and language-version fallout.

The assessment shows API compatibility findings in `Hast.Layer`, `Hast.Transformer`, `Hast.VhdlBuilder`, and `Lombiq.Arithmetics`, plus a vulnerable package in `Hast.Samples.SampleAssembly`. Research should start from those projects and from any target-framework constraints introduced by the refreshed `Lombiq.Arithmetics` and Helpful Libraries submodules.

**Done when**: The sample applications and their shared pipeline projects target .NET 10 successfully, the vulnerable/deprecated packages in this slice are addressed, and the application slice builds and runs through its relevant tests on the new framework.

---

### 04-communication-tester-and-remaining-app-layer: Upgrade the communication tester path and the remaining application-facing dependencies

Upgrade `Hast.Communication.Tester` after the sample and core pipeline work is in place, bringing forward any remaining .NET 10 changes in `Hast.Communication` and adjacent shared projects that were not completed in earlier slices. This task closes the last explicit application entry point in the solution and verifies the communication pipeline against the upgraded transformation and layer stacks.

Assessment data shows one of the highest issue counts in `Hast.Communication`, including API compatibility work, so this task should focus on finishing that surface area inline rather than deferring cleanup. Research should begin with the communication-specific API findings and any package upgrades still isolated to this path.

**Done when**: `Hast.Communication.Tester` and all remaining application-facing communication dependencies target .NET 10, build cleanly, and pass their slice-specific validation.

---

### 05-tests-and-input-assemblies: Upgrade the test projects and support assemblies that validate the migrated slices

Upgrade the test estate and supporting input assemblies once the application slices are on .NET 10: `Hast.Algorithms.Tests`, `Hast.DynamicTests`, `Hast.TestBase`, `Hast.Transformer.Tests`, `Hast.Transformer.Vhdl.Tests.Common`, `Hast.Transformer.Vhdl.Tests`, `Hast.Vitis.Tests`, and the `test/TestInputAssemblies` projects. This task also covers retiring the remaining `packages.config` usage, replacing deprecated test dependencies, and reconciling any analyzer or build behavior changes that only appear under test compilation.

The assessment flags deprecated packages across multiple test projects and a very deep dependency chain into `Hast.DynamicTests`, so this task should research test framework/package updates together with any remaining TFM compatibility fixes exposed by the now-upgraded production projects.

**Done when**: All test and support projects target .NET 10, deprecated test dependencies are replaced or updated, `packages.config` usage is removed from the migrated solution, and the affected test suites pass on .NET 10.

---

### 06-library-consolidation-and-cleanup: Consolidate shared libraries on a single target and remove temporary compatibility scaffolding

After every entry-point application and test project is running on .NET 10, consolidate all shared libraries to their final target framework and remove any temporary dual-targeting, conditional package references, or conditional compilation blocks introduced to keep application slices moving independently. This task spans the shared libraries touched in earlier tasks, including the submodule projects that remain part of the solution build.

This is also where deferred modernization cleanup that is explicitly tied to the migration mechanics should happen, such as normalizing package references once all projects share the same framework and documenting whether Central Package Management should be considered as a post-migration follow-up rather than part of this upgrade.

**Done when**: Every upgraded shared library has a final .NET 10 target configuration, temporary migration-only compatibility scaffolding is removed, and the solution is ready for a single-framework final validation pass.

---

### 07-solution-validation-and-branch-ready-state: Validate the full upgraded solution and prepare the branch for review

Run the full end-to-end validation pass for the upgraded solution, including a clean build, the relevant automated tests, and final review of root-level changes such as workflow/build configuration, package state, and submodule pointers. This task closes the migration by proving that the root repository and the upgraded submodules work together from a fresh checkout on the upgrade branch.

Use this task to document any intentionally deferred post-upgrade recommendations, such as potential future CPM adoption or broader nullable enablement, but keep the branch itself warning-free and production-ready for review.

**Done when**: The entire solution builds without warnings or errors on .NET 10, the required tests pass, submodule pointers and branch state are consistent, and the upgrade branch is ready for user review.
