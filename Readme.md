# Hastlayer SDK

[![Hast.Layer NuGet](https://img.shields.io/nuget/v/Hast.Layer?label=Hast.Layer)](https://www.nuget.org/packages/Hast.Layer/)

[![Build and Test](https://github.com/Lombiq/Hastlayer-SDK/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/Lombiq/Hastlayer-SDK/actions/workflows/build-and-test.yml)

## About

[Hastlayer](https://hastlayer.com/) - be the hardware. Hastlayer automatically transforms [.NET](https://www.microsoft.com/net/) assemblies into computer chips, improving performance and lowering power consumption for massively parallel applications. For some figures check out [the benchmarks](Docs/Benchmarks.md).

Hastlayer uses [FPGAs](https://en.wikipedia.org/wiki/Field-programmable_gate_array) (chips that can be "re-wired" on the fly): Simply select the compute-bound part of your .NET program, and Hastlayer seamlessly swaps it out with a generated FPGA implementation. Since .NET Intermediate Language assemblies (but not C#, VisualBasic or other code) are transformed, you can use any .NET language (e.g. C#, VB, F# - sample included, C++, Python, PHP, and JavaScript) in theory.

Hastlayer was also featured on .NET Conf 2017 and [many other conferences and meetups](https://hastlayer.com/hastlayer-world-tour). The [recorded session](https://www.youtube.com/watch?v=03Sq5m3eUSs) covers interesting features of Hastlayer. Check out the [FAQ](https://hastlayer.com/faq) for more information.
 
This is the PC-side component of Hastlayer, the one that transforms .NET assemblies, programs attached FPGAs, and communicates with said FPGAs.

Note that due to a repository purge the repo history doesn't contain anything from before July 2017 though development has been ongoing more or less actively from 2015.

Created by [Lombiq Technologies](https://lombiq.com/), an open source .NET web development company working mostly with [Orchard CMS, i.e. Orchard 1.x and Orchard Core](https://www.orchardcore.net/).

Hastlayer uses [ILSpy](http://ilspy.net/) to process CIL assemblies.

## Notes on Hastlayer's documentation

These text files should only serve as a starting point. The samples are the best source of information on how to use Hastlayer. The public API of Hastlayer is also documented inline as code comments. Please check the comments for clarification. The projects also have README files.

## Table of contents

- [Getting started](Docs/GettingStarted.md)
- [Working with Hastlayer](Docs/WorkingWithHastlayer.md)
- [Developing Hastlayer](Docs/DevelopingHastlayer.md)
- [Benchmarks](Docs/Benchmarks.md)
- [Roadmap](Docs/Roadmap.md)
- [Support](Docs/Support.md)

## Contributing and support

Bug reports, feature requests, comments, questions, code contributions, and love letters are warmly welcome, please do so via GitHub issues and pull requests. Please adhere to our [open-source guidelines](https://lombiq.com/open-source-guidelines) while doing so.

This project is developed by [Lombiq Technologies](https://lombiq.com/). Commercial-grade support is available through Lombiq.
