# 03-sample-applications-and-shared-pipeline: Upgrade the sample applications and the shared transformation pipeline they depend on

Upgrade `Hast.Samples.Demo` and `Hast.Samples.Consumer` together with the libraries they bring into the application layer: `Hast.Layer`, `Hast.Transformer`, `Hast.Transformer.Vhdl`, `Hast.VhdlBuilder`, `Hast.Algorithms`, `Hast.Samples.SampleAssembly`, `Hast.Samples.FSharpSampleAssembly`, `Hast.Samples.Kpz.Algorithms`, `Lombiq.Arithmetics`, and any remaining shared dependencies required to keep these applications buildable during the transition. This is the broadest application slice in the solution and touches the projects most likely to surface package and language-version fallout.

The assessment shows API compatibility findings in `Hast.Layer`, `Hast.Transformer`, `Hast.VhdlBuilder`, and `Lombiq.Arithmetics`, plus a vulnerable package in `Hast.Samples.SampleAssembly`. Research should start from those projects and from any target-framework constraints introduced by the refreshed `Lombiq.Arithmetics` and Helpful Libraries submodules.

**Done when**: The sample applications and their shared pipeline projects target .NET 10 successfully, the vulnerable/deprecated packages in this slice are addressed, and the application slice builds and runs through its relevant tests on the new framework.
