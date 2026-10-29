# Hastlayer - Dynamic Tests

Dynamic, i.e. hardware-executed tests for various Hastlayer features.

Note that Dynamic Tests shouldn't and aren't executed during GitHub Actions builds. This is due to the project file not being named `*.Tests.*`, since `Invoke-SolutionTests` matches on that. If the tests ever need to be run, just rename the project.
