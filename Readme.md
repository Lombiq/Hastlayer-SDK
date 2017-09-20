# Hastlayer SDK Readme



## Overview

[Hastlayer](https://hastlayer.com/) - Be the hardware. Automatically transforming [.NET](https://www.microsoft.com/net/) assemblies into computer chips to improve performance and lower power consumption.

Hastlayer uses [FPGAs](https://en.wikipedia.org/wiki/Field-programmable_gate_array) (chips that can be "re-wired" on the fly): You just need to select the compute-bound part of your .NET program and Hastlayer will seamlessly swap it out with a generated FPGA implementation. Since not C#, VisualBasic or other code but .NET Intermediate Language assemblies are transformed in theory any .NET language can be used, including C#, VB, F#, C++, Python, PHP, JavaScript...

Hastlayer was also featured on [.NET Conf 2017](https://channel9.msdn.com/events/dotnetConf/2017/T212); the [recorded session](https://channel9.msdn.com/events/dotnetConf/2017/T212) covers most of what's interesting about Hastlayer.
 
This is the PC-side component of Hastlayer, the one that transforms .NET assemblies, programs attached FPGAs and communicates with said FPGAs.

Created by [Lombiq Technologies](https://lombiq.com/), an open source .NET web development company.

Hastlayer uses [ILSpy](http://ilspy.net/) to process CIL assemblies and [Orchard Application Host](https://github.com/Lombiq/Orchard-Application-Host) to utilize [Orchard](http://orchardproject.net/) as the application framework.


## Notes on Hastlayer's documentation

These text files should only serve as a starting point. On how to use Hastlayer the samples are the best source. The public API of Hastlayer is also documented inline as code comments, so make sure to check those out too if something's not clear. Some projects also have further Readme files.


## Table of contents

- [Getting started](Docs/GettingStarted.md)
- [Working with Hastlayer](Docs/WorkingWithHastlayer.md)
- [Developing Hastlayer](Docs/DevelopingHastlayer.md)
- [Release notes](Docs/ReleaseNotes.md)
- [Roadmap](Docs/Roadmap.md)
- [Support](Docs/Support.md)


## Repositories and contributions

The project's source is available in two public source repositories, automatically mirrored in both directions with [Git-hg Mirror](https://githgmirror.com):

- [https://bitbucket.org/Lombiq/hastlayer-sdk](https://bitbucket.org/Lombiq/hastlayer-sdk) (Mercurial repository)
- [https://github.com/Lombiq/Hastlayer-SDK](https://github.com/Lombiq/Hastlayer-SDK) (Git repository)

(Note that due to a repository purge the repo history doesn't contain anything from before July 2017 though development has been ongoing more or less actively from 2015.)

Bug reports, feature requests and comments are warmly welcome, **please do so via GitHub**. Feel free to send pull requests too, no matter which source repository you choose for this purpose.

This project is developed by [Lombiq Technologies Ltd](https://lombiq.com/). Commercial-grade support is available through Lombiq.