# 06-library-consolidation-and-cleanup: Consolidate shared libraries on a single target and remove temporary compatibility scaffolding

After every entry-point application and test project is running on .NET 10, consolidate all shared libraries to their final target framework and remove any temporary dual-targeting, conditional package references, or conditional compilation blocks introduced to keep application slices moving independently. This task spans the shared libraries touched in earlier tasks, including the submodule projects that remain part of the solution build.

This is also where deferred modernization cleanup that is explicitly tied to the migration mechanics should happen, such as normalizing package references once all projects share the same framework and documenting whether Central Package Management should be considered as a post-migration follow-up rather than part of this upgrade.

**Done when**: Every upgraded shared library has a final .NET 10 target configuration, temporary migration-only compatibility scaffolding is removed, and the solution is ready for a single-framework final validation pass.
