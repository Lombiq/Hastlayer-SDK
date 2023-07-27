# Hastlayer - Vitis Driver

[![Hast.Vitis NuGet](https://img.shields.io/nuget/v/Hast.Vitis?label=Hast.Vitis)](https://www.nuget.org/packages/Hast.Vitis/)

## Overview

This project contains the communication service used to connect with Xilinx's [Vitis Unified Software Platform](https://www.xilinx.com/products/design-tools/vitis/vitis-platform.html) devices, such as the [Xilinx Alveo U250](https://www.xilinx.com/products/boards-and-kits/alveo/u250.html) FPGA accelerator card via the [OpenCL](https://www.khronos.org/opencl/) library.

Note that the SH scripts in this project should use LF line endings! You'll get errors such as `-bash: $'\r': command not found` otherwise.

## Requirements

* The system running the FPGA card must be 64-bit Linux (e.g. Ubuntu 18.04.2 LTS or CentOS 7.6). The installation instructions can be found here [in the platform documentation](https://www.xilinx.com/html_docs/xilinx2019_2/vitis_doc/vhc1571429852245.html).
* The device-specific software requirements can be found in the card's Getting Started page, e.g. [Alveo U250](https://www.xilinx.com/products/boards-and-kits/alveo/u250.html#gettingStarted).
* Hastlayer has its own software requirements which can be found in the repository's *GettingStarted.md* file.

## Preparation

Even after everything is installed, you have to make sure that the executing user's environment variables are correctly set by sourcing the setup scripts [as described in the documentation](https://docs.xilinx.com/r/en-US/ug1400-vitis-embedded/Setting-Up-the-Environment-to-Run-the-Vitis-Software-Platform). You can add these commands into the `~/.bashrc` file to avoid having to type them every time. If running from the cloud, like Nimbix, this is probably handled automatically.

For Azure-specific instructions see [the Azure NP docs](Docs/AzureReadme.md).
For Nimbix-specific instructions see [the Nimbix docs](Docs/Nimbix.md).


## Cross Compilation

If you want to build for a platform not in your */opt/xilinx/platforms* directory, you can set the `XILINX_PLATFORM` environment variable to the directory that contains the platform directories. For example: `export XILINX_PLATFORM=$HOME/platforms`. If the environment variable is not set or if its value isn't an existing directory then */opt/xilinx/platforms* is used as fallback.

Be sure that all .NET software dependencies are on the same version on both the target and the source computers. Otherwise the source code won't match during transformation. This will result in a different Transformation ID and the XCLBIN file won't be found, prompting a recompilation on the target machine. To mitigate this risk you can try some of the following strategies:
* Perform a system update on both machines before starting, so both are on the most up-to-date frameworks.
* [Publish your program as self-contained](https://docs.microsoft.com/en-us/dotnet/core/deploying/#publish-self-contained), eg. `dotnet publish -c Release -r linux-x64 -p:PublishReadyToRun=true Hastlayer.SDK.Linux.sln`. Replace `linux-x64` with `linux-arm` to build the binaries for a 32-bit ARM device like the Zynq boards.
  * Note that Ready to Run [has its own restrictions](https://docs.microsoft.com/en-us/dotnet/core/deploying/ready-to-run#cross-platformarchitecture-restrictions). Up to .Net 5 you need a Linux platform for Linux builds. So if you are on Windows you need a Linux virtual machine (for example using Docker as described in the next chapter). Starting with .Net 6 you can build Linux targets from Windows and macOS too. 

### Cross Compilation with Docker

If you are interested in using Docker, especially from a Windows host, [read this](Docs/Docker.md).

## Specialized Targets

Aside from general Vitis accelerator card support, there are a couple specialized targets that have their own documentation:
- [Using Hastlayer with Vitis boards on Azure NP Servers](Docs/AzureReadme.md)
- [Using Hastlayer with Zynq SoC modules](Docs/ZynqReadme.md)
  - [Building PetaLinux 2020.2 SD card image for Hastlayer](Docs/ZynqPetaLinux.md)

## Other Remarks

If you ever get an error *\[XRT\] ERROR: some device is already programmed* due to a crashed or interrupted execution, you can reset the card using `xbutil reset` command. See more info about the Xilinx Board Utility [here](https://xilinx.github.io/XRT/master/html/xbutil.html).

If you just want to generate a simulation report, you can do that without the full build by configuring the `VitisBuildConfiguration.SynthesisOnly` custom configuration in the *appsettings.json* or by adding the following command line argument:

```
--HardwareGenerationConfiguration:CustomConfiguration:VitisBuildConfiguration:SynthesisOnly true
```

Available system memory is an important factor. Unless you have a lot, you won't be able to compile multiple projects at the same time. When V++ runs out of available memory it rather crashes than waits so it's best practice to not do anything involved on the machine during compilation.

HBM is used by default on the cards that support it (Alveo U50 and U280) but only one slot. This means that only 256MB memory can be utilized with HBM. To use larger programs with devices that have both HBM and DDR memory, you must disable HMB during compilation either by editing the `UseHbm` property of `OpenClConfiguration` in *appsettings.json*, programmatically changing the value of this in `HardwareGenerationConfiguration` or using the following command line switch:

```
--HardwareGenerationConfiguration:CustomConfiguration:OpenClConfiguration:UseHbm false
```

Like with every other `HardwareGenerationConfiguration` change, adding this setting will alter the hash resulting in a unique xclbin file so both options can be compiled ahead of time in case you want to automate selecting between them based on the input. Also note, that Alveo U50 only has HBM memory so this option will have no real effect on it.
