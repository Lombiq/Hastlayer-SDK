# 05-tests-and-input-assemblies: Upgrade the test projects and support assemblies that validate the migrated slices

Upgrade the test estate and supporting input assemblies once the application slices are on .NET 10: `Hast.Algorithms.Tests`, `Hast.DynamicTests`, `Hast.TestBase`, `Hast.Transformer.Tests`, `Hast.Transformer.Vhdl.Tests.Common`, `Hast.Transformer.Vhdl.Tests`, `Hast.Vitis.Tests`, and the `test/TestInputAssemblies` projects. This task also covers retiring the remaining `packages.config` usage, replacing deprecated test dependencies, and reconciling any analyzer or build behavior changes that only appear under test compilation.

The assessment flags deprecated packages across multiple test projects and a very deep dependency chain into `Hast.DynamicTests`, so this task should research test framework/package updates together with any remaining TFM compatibility fixes exposed by the now-upgraded production projects.

**Done when**: All test and support projects target .NET 10, deprecated test dependencies are replaced or updated, `packages.config` usage is removed from the migrated solution, and the affected test suites pass on .NET 10.
