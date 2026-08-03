# 04-communication-tester-and-remaining-app-layer: Upgrade the communication tester path and the remaining application-facing dependencies

Upgrade `Hast.Communication.Tester` after the sample and core pipeline work is in place, bringing forward any remaining .NET 10 changes in `Hast.Communication` and adjacent shared projects that were not completed in earlier slices. This task closes the last explicit application entry point in the solution and verifies the communication pipeline against the upgraded transformation and layer stacks.

Assessment data shows one of the highest issue counts in `Hast.Communication`, including API compatibility work, so this task should focus on finishing that surface area inline rather than deferring cleanup. Research should begin with the communication-specific API findings and any package upgrades still isolated to this path.

**Done when**: `Hast.Communication.Tester` and all remaining application-facing communication dependencies target .NET 10, build cleanly, and pass their slice-specific validation.
