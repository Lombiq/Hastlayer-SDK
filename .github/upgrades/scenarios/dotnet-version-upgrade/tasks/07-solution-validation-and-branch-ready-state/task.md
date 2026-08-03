# 07-solution-validation-and-branch-ready-state: Validate the full upgraded solution and prepare the branch for review

Run the full end-to-end validation pass for the upgraded solution, including a clean build, the relevant automated tests, and final review of root-level changes such as workflow/build configuration, package state, and submodule pointers. This task closes the migration by proving that the root repository and the upgraded submodules work together from a fresh checkout on the upgrade branch.

Use this task to document any intentionally deferred post-upgrade recommendations, such as potential future CPM adoption or broader nullable enablement, but keep the branch itself warning-free and production-ready for review.

**Done when**: The entire solution builds without warnings or errors on .NET 10, the required tests pass, submodule pointers and branch state are consistent, and the upgrade branch is ready for user review.
