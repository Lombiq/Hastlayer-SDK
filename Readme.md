# Hastlayer SDK Readme



## Overview

[Hastlayer](https://hastlayer.com/) - be the hardware. Hastlayer automatically transforms [.NET](https://www.microsoft.com/net/) assemblies into computer chips, improving performance and lowering power consumption.

Hastlayer uses [FPGAs](https://en.wikipedia.org/wiki/Field-programmable_gate_array) (chips that can be "re-wired" on the fly): Simply select the compute-bound part of his .NET program, and Hastlayer seamlessly swaps it out with a generated FPGA implementation. Since .NET Intermediate Language assemblies (but not C#, VisualBasic or other code) are transformed, you can use any .NET language (e.g.,  C#, VB, F#, C++, Python, PHP, and JavaScript) in theory.

Hastlayer was also featured on [.NET Conf 2017](https://channel9.msdn.com/events/dotnetConf/2017/T212). The [recorded session](https://channel9.msdn.com/events/dotnetConf/2017/T212) covers interesting features of Hastlayer (it's also on [YouTube](https://www.youtube.com/watch?v=03Sq5m3eUSs)). Check out the [FAQ](https://hastlayer.com/faq) for more information.
 
This is the PC-side component of Hastlayer, the one that transforms .NET assemblies, programs attached FPGAs, and communicates with said FPGAs.

Created by [Lombiq Technologies](https://lombiq.com/), an open source .NET web development company.

Hastlayer uses [ILSpy](http://ilspy.net/) to process CIL assemblies and [Orchard Application Host](https://github.com/Lombiq/Orchard-Application-Host) to utilize [Orchard](http://orchardproject.net/) as the application framework.


## Notes on Hastlayer's documentation

These text files should only serve as a starting point. The samples are the best source of information on how to use Hastlayer. The public API of Hastlayer is also documented inline as code comments. Please check the comments for clarification. The projects also have README files.


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
