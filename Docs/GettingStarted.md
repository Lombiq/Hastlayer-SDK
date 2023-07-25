# Getting started

## Prerequisites

To begin working with Hastlayer you'll need the following:

- A .NET project that requires some hardware acceleration.
- A compatible FPGA board. You have the following options here:
  - For production-level commercial workloads:
    - Using [Xilinx Alveo U50, U200, U250 or U280 Data Center Accelerator Cards](https://www.xilinx.com/products/boards-and-kits/alveo.html) on-premises or in the cloud. In the cloud you can use them on Azure.
    - Using [AWS EC2 F1 instances](https://aws.amazon.com/ec2/instance-types/f1/).
  - For embedded workloads: Using single board/module computers with Xilinx Zynq-7000 series SoC. We officially support the [Trenz Electronic TE0715-04-30-1C](https://shop.trenz-electronic.de/en/TE0715-04-30-1C-SoC-Module-with-Xilinx-Zynq-XC7Z030-1SBG485C-1-GByte-DDR3L-SDRAM-4-x-5-cm) module, but the codebase isn't highly specific to it and you can make your own manifest provider and device driver by inheriting from the same base classes.
  - For simpler workloads and testing: The [Nexys A7 (formerly known as Nexys 4 DDR)](https://store.digilentinc.com/nexys-a7-fpga-trainer-board-recommended-for-ece-curriculum/) board (which is **NOT** the same as the non-DDR Nexys 4, be sure to purchase the linked board!) is suitable. The **Nexys A7-100T** version is required. Note that this is a relatively low-end development board that can't fit huge algorithms and it only supports slow communication channels. So with this board Hastlayer is only suitable for simpler algorithms that only need to exchange small amount of data. Note that to work with Nexys cards, you need to use the Hastlayer SDK from source.
- On Linux if you are using `System.Drawing` we suggest transitioning to [ImageSharp](https://github.com/SixLabors/ImageSharp) instead. However if you need to stick with you `System.Drawing` you have to to install the [Mono project's](https://www.mono-project.com/) implementation of [libgdiplus](https://github.com/mono/libgdiplus) too. On CentOS you need the "libgdiplus" package, while on Debian systems such as Ubuntu you need "libgdiplus" and "libc6-dev" too.

## First steps

These would be your first steps on starting to work with Hastlayer:

1. Create your first Hastlayer-accelerated app by following the [Working with Hastlayer guide](WorkingWithHastlayer.md).
2. Set up your hardware device, following the guide corresponding to the platform you're using:
    - [Xilinx Vitis, Azure NP, Zynq](../src/Hastlayer/Hast.Vitis/Readme.md)
    - [Nexys A7/Nexys DDR](https://github.com/Lombiq/Hastlayer-Hardware-Framework---Xilinx)
