# Getting started



## Overview

To begin working with Hastlayer you'll need the following:

- The SDK and Hardware Framework repositories cloned.
- Access to Hastlayer Remote Services, which does the actual .NET to hardware transformation. Evaluation, academic and personal access is currently free and you can sign up on [hastlayer.com](https://hastlayer.com).
- A compatible FPGA board. You have the following options here:
  - For simpler workloads and testing: The [Nexys A7 (formerly known as Nexys 4 DDR)](https://store.digilentinc.com/nexys-a7-fpga-trainer-board-recommended-for-ece-curriculum/) board (which is **NOT** the same as the non-DDR Nexys 4, be sure to purchase the linked board!) is suitable. The **Nexys A7-100T** version is required. Note that this is a relatively low-end development board that can't fit huge algorithms and it only supports slow communication channels. So with this board Hastlayer is only suitable for simpler algorithms that only need to exchange small amount of data.
  - For academic workloads: Microsoft's FPGA platform, [Project Catapult](https://www.microsoft.com/en-us/research/project/project-catapult/) is supported too, which offers high-end hardware. You'll need to apply for a cloud Catapult node via the [Project Catapult Academic Program](https://www.microsoft.com/en-us/research/academic-program/project-catapult-academic-program/). Be sure to [let us know](https://hastlayer.com/contact) if you'd like to use Catapult and we'll help you get going.
  - For production-level commercial workloads:
    - Using [Xilinx Alveo U50, U200, U250 or U280 Data Center Accelerator Cards](https://www.xilinx.com/products/boards-and-kits/alveo.html) on-premise or in the cloud. In the cloud these cards are currently available at [Nimbix](https://www.nimbix.net/).
    - Using [AWS EC2 F1 instances](https://aws.amazon.com/ec2/instance-types/f1/).
  - For embedded workloads: Using single board/module computers with Xilinx Zynq-7000 series SoC. We officially support the [Trenz Electronic TE0715-04-30-1C](https://shop.trenz-electronic.de/en/TE0715-04-30-1C-SoC-Module-with-Xilinx-Zynq-XC7Z030-1SBG485C-1-GByte-DDR3L-SDRAM-4-x-5-cm) module, but the codebase isn't highly specific to it and you can make your own manifest provider and device driver by inheriting from the same base classes.
- [Visual Studio 2019 or later](https://www.visualstudio.com/downloads/) installed (any edition will work).
- On Linux if you are using `System.Drawing` we suggest transitioning to [ImageSharp](https://github.com/SixLabors/ImageSharp) instead. However if you need to stick with you `System.Drawing` you have to to install the [Mono project's](https://www.mono-project.com/) implementation of [libgdiplus](https://github.com/mono/libgdiplus) too. On CentOS you need the "libgdiplus" package, while on Debian systems such as Ubuntu you need "libgdiplus" and "libc6-dev" too.

Catapult acknowledgment: The authors acknowledge the [Texas Advanced Computing Center (TACC)](http://www.tacc.utexas.edu) at The University of Texas at Austin for providing HPC resources that have contributed to the development of this project. This material is based on work supported by access to [Project Catapult](https://www.microsoft.com/en-us/research/project/project-catapult/) hardware and technology donated by Microsoft.

If you have a compatible FPGA board you can run the default sample even without having access to Hastlayer Remote Services.


## Flavors of Hastlayer

The Hastlayer components come in two "flavors" with corresponding branches in their repositories:

- Developer (*dev* branch): This is used by developers of Hastlayer itself. It includes the full source code. Most possibly you don't need this one.
- Client (*client* branch): Used by end-users of Hastlayer who run Hastlayer in a client mode, accessing *Hast.Core* as a remote service, i.e. Hastlayer Remote Services. *Hast.Core* encompasses those components of Hastlayer that do the heavy lifting of software to hardware transformation. Most possibly you need this one.

You'll see this terminology be used further on.


## First steps

These would be your first steps on starting to work with Hastlayer by getting the samples working:

1. Clone the necessary repositories with git. Always checkout the `client` or `dev` branch corresponding to your flavor. Make sure to allow Git to initialize submodules!
   1. Clone the [Hastlayer SDK repo](https://github.com/Lombiq/Hastlayer-SDK).
   2. Clone of the the Hardware Framework repos corresponding to your choice of hardware platform:
      - The [Hastlayer Hardware Framework - Xilinx repo](https://github.com/Lombiq/Hastlayer-Hardware-Framework---Xilinx) for the Nexys A7.
      - The [Hastlayer Hardware Framework - Xilinx Vitis repo](https://github.com/Lombiq/Hastlayer-Hardware-Framework---Vitis) for Alveo Data Center Accelerator Cards is already included as a git submoule so you don't have to clone it separately.
      - The [Hastlayer Hardware Framework - Catapult repo](https://github.com/Lombiq/Hastlayer-Hardware-Framework---Catapult) for Microsoft Catapult.
2. Set up the hardware project as explained in the Hardware Framework's documentation and program the FPGA for the first time.
3. Open the Visual Studio solution of the SDK corresponding to your flavor of Hastlayer.
4. Set the `Hast.Samples.Consumer` project (under the *Samples* folder) as the startup project here. If you're working in the *client* flavor then you'll need to configure your credentials, see that project's documentation.
5. Start the sample project. That will by default run the sample that is also added by default to the Hardware project.
6. You should be able to see the results of the sample in its console window.

If everything is alright follow up with the rest of this documentation to write your first own Hastlayer-using algorithm. You can also check out the many documented samples under the *Samples* solution folder.


## Device-specific documentation

You can find out more about these devices from their Hastlayer driver's documentation:
- [Xilinx Vitis, Azure NP, Zynq](../Hast.Abstractions/Hast.Vitis.Abstractions/Readme.md)
