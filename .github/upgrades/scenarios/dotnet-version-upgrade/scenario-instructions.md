# .NET Version Upgrade

## Preferences
- **Flow Mode**: Automatic
- **Target Framework**: net10.0

## Source Control
- **Source Branch**: dev
- **Working Branch**: upgrade-dotnet-10
- **Commit Strategy**: After Each Task
- **Branch Sync**: Auto (Merge)

## Upgrade Options
**Source**: .github/upgrades/scenarios/dotnet-version-upgrade/upgrade-options.md

### Strategy
- Upgrade Strategy: Top-Down

### Project Structure
- Package Management: Per-Project (defer CPM to post-migration)

### Compatibility
- Unsupported API Handling: Fix Inline

## Strategy
**Selected**: Top-Down
**Rationale**: The solution is already on modern .NET, but its 36 projects, 11-level dependency graph, updated submodule baselines, and API compatibility findings in shared libraries make an incremental application-first migration safer than an all-at-once cutover.

### Execution Constraints
- Upgrade entry-point applications in priority order, pulling in only the shared libraries each application needs at that point.
- Keep package management per project during the migration; defer any CPM adoption until the solution is fully on net10.0 and stable.
- Fix API compatibility issues inline inside the owning upgrade task; do not leave temporary stubs or deferred API cleanup behind.
- Do not remove legacy targets from shared libraries until every consuming application and test project has moved to net10.0.
- Finish with full solution build and test validation after the consolidation pass.

## User Preferences
### Technical Preferences
- Update all git submodules to their latest `dev` state before upgrading the main solution.
- Make required compatibility changes in submodule repositories as part of the .NET 10 upgrade.
