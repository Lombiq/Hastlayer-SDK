# .NET Version Upgrade Progress

## Overview

Upgrade `Hastlayer.SDK.sln` and its submodule-backed dependencies to .NET 10 using a top-down, application-first migration. The work starts from the refreshed submodule baseline, upgrades application slices together with their required shared libraries, then finishes with consolidation and full-solution validation.
**Progress**: 0/7 tasks complete <progress value="0" max="100"></progress> 0%
**Progress**: 0/7 tasks complete <progress value="0" max="100"></progress> 0%

## Tasks
- 🔄 01-toolchain-and-submodule-baseline: Align the SDK, submodules, and shared upgrade baseline ([Content](tasks/01-toolchain-and-submodule-baseline/task.md))
- 🔲 01-toolchain-and-submodule-baseline: Align the SDK, submodules, and shared upgrade baseline
- 🔲 02-console-and-vitis-stack: Upgrade the console entry point and the Vitis/Xilinx dependency slice
- 🔲 03-sample-applications-and-shared-pipeline: Upgrade the sample applications and the shared transformation pipeline they depend on
- 🔲 04-communication-tester-and-remaining-app-layer: Upgrade the communication tester path and the remaining application-facing dependencies
- 🔲 05-tests-and-input-assemblies: Upgrade the test projects and support assemblies that validate the migrated slices
- 🔲 06-library-consolidation-and-cleanup: Consolidate shared libraries on a single target and remove temporary compatibility scaffolding
- 🔲 07-solution-validation-and-branch-ready-state: Validate the full upgraded solution and prepare the branch for review
