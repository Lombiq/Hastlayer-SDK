# Hastlayer Readme



## Overview

[Hastlayer](https://hastlayer.com/) - Be the hardware. Transforming .NET assemblies into FPGA hardware for faster execution. This is the PC-side component of Hastlayer, the one that transforms .NET assemblies, programs attached FPGAs and communicates with said FPGAs.

On how to use Hastlayer in your own application see the sample projects in the solution.

Created by [Lombiq Technologies](https://lombiq.com/).

Hastlayer uses [ILSpy](http://ilspy.net/) to process CIL assemblies and [Orchard Application Host](https://github.com/Lombiq/Orchard-Application-Host) to utilize [Orchard](http://orchardproject.net/) as the application framework.


## Notes on Hastlayer's documentation

These text files should only serve as a starting point. On how to use Hastlayer the samples are the best source. The public API of Hastlayer is also documented inline as code comments, so make sure to check those out too if something's not clear. Some projects also have further Readme files.


## Table of contents

- [Getting started](Docs/GettingStarted.md)
- [Working with Hastlayer](Docs/WorkingWithHastlayer.md)
- [Developing Hastlayer](Docs/DevelopingHastlayer.md)
- [Release notes](Docs/ReleaseNotes.md)