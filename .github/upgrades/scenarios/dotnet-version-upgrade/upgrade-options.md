# Upgrade Options — Hastlayer.SDK

Assessment: 36 projects targeting net6.0/netstandard2.0/netstandard2.1 today, 11 dependency levels, API compatibility findings in multiple core libraries, deprecated packages, and one known vulnerable package.

## Strategy

### Upgrade Strategy
The solution is already on modern .NET, but its 36-project size, 11-level dependency graph, and concentration of API issues in shared core libraries make an incremental strategy the safer default.

| Value | Description |
|-------|-------------|
| **Top-Down** (selected) | Upgrade entry-point apps first, temporarily multi-targeting shared libraries as needed so the solution can stay buildable during the migration, then consolidate libraries afterward. |
| All-at-Once | Upgrade all projects in one atomic pass, which is faster but likely leaves the solution broken until every dependent project is updated together. |

## Project Structure

### Package Management
The solution has many projects, but it does not currently use centralized package management and still contains a `packages.config` file, so introducing CPM during the TFM migration would add avoidable churn.

| Value | Description |
|-------|-------------|
| **Per-Project (defer CPM to post-migration)** (selected) | Keep package versions in individual projects during the .NET 10 migration and defer any CPM adoption until the upgraded solution is stable. |
| Central Package Management (CPM) | Create `Directory.Packages.props` and centralize package versions now for consistency across projects. |

## Compatibility

### Unsupported API Handling
The assessment flagged API compatibility issues in several shared projects, including `Hast.Catapult`, `Hast.Communication`, `Hast.Layer`, `Hast.Transformer`, `Hast.VhdlBuilder`, `Hast.Vitis`, `Hast.Xilinx`, and `Lombiq.Arithmetics`, so the plan needs a clear rule for handling those fixes.

| Value | Description |
|-------|-------------|
| **Fix Inline** (selected) | Resolve API changes in the same upgrade task, including complex replacements, so no stubbed follow-up work is left behind. |
| Defer Complex Changes | Apply simple replacements immediately, but keep projects compiling with temporary stubs for complex API changes and resolve them in follow-up subtasks. |
