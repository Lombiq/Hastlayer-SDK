# Hastlayer - Dynamic Tests

Dynamic, i.e. hardware-executed tests for various Hastlayer features.

Note that Dynamic Tests shouldn't and aren't executed during GitHub Actions builds. This is due to `IsTestProject` being set to `false` in the project file, since the GitHub Actions test runner (and `dotnet test` for the solution) only executes test projects. They can still be run from Visual Studio's Test Explorer. If the tests ever need to be run in CI, just remove that property.
