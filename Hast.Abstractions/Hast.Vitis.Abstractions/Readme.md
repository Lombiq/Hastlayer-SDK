# Hastlayer Vitis Driver



## Overview

This project contains the communication service used to connect with [Vitis Unified Software Platform](https://www.xilinx.com/products/design-tools/vitis/vitis-platform.html) devices, such as the [Xilinx Alveo U250](https://www.xilinx.com/products/boards-and-kits/alveo/u250.html) FPGA accelerator card via the [OpenCL](https://www.khronos.org/opencl/) library.

Note that the SH scripts in this project should use LF line endings! You'll get errors such as `-bash: $'\r': command not found` otherwise.

For Nimbix-specific instructions see [the Nimbix docs](Docs/Nimbix.md).


## Requirements

* The system running the FPGA card must be 64-bit Linux (e.g. Ubuntu 18.04.2 LTS or CentOS 7.6). The installation instructions can be found here [in the platform documentation](https://www.xilinx.com/html_docs/xilinx2019_2/vitis_doc/vhc1571429852245.html).
* The device-specific software requirements can be found in the card's Getting Started page, e.g. [Alveo U250](https://www.xilinx.com/products/boards-and-kits/alveo/u250.html#gettingStarted).
* Hastlayer has its own software requirements which can be found in the repository's *GettingStarted.md* file.


## Preparation

Even after everything is installed, you have to make sure that the executing user's environment variables are correctly set by sourcing the setup scripts [as described in the documentation](https://www.xilinx.com/html_docs/xilinx2019_2/vitis_doc/rbk1547656041291.html). You can add these commands into the `~/.bashrc` file to avoid having to type them every time. If running from the cloud, like Nimbix, this is probably handled automatically.

For setup instructions on the Nimbix cloud see the [Nimbix-specific instructions](Docs/Nimbix.md).


## Cross Compilation

If you want to build for a platform not in your */opt/xilinx/platforms* directory, you can set the `XILINX_PLATFORM` environment variable to the directory that contains the platform directories. For example: `export XILINX_PLATFORM=$HOME/platforms`. If the environment variable is not set or if its value isn't an existing directory then */opt/xilinx/platforms* is used as fallback.

Be sure that all .NET software dependencies are on the same version on both the target and the source computers. Otherwise the source code won't match during transformation. This will result in a different Transformation ID and the XCLBIN file won't be found, prompting a recompilation on the target machine. To mitigate this risk you can try some of the following strategies:
* Perform a system update on both machines before starting, so both are on the most up-to-date frameworks.
* [Publish your program as self-contained](https://docs.microsoft.com/en-us/dotnet/core/deploying/#publish-self-contained), eg. `dotnet publish -c Release -r linux-x64 -p:PublishReadyToRun=true Hastlayer.SDK.Linux.sln`. Replace `linux-x64` with `linux-arm` to build the binaries for a 32-bit ARM device like the Zynq boards.
  * Note that Ready to Run [has its own restrictions](https://docs.microsoft.com/en-us/dotnet/core/deploying/ready-to-run#cross-platformarchitecture-restrictions). Up to .Net 5 you need a Linux platform for Linux builds. So if you are on Windows you need a Linux virtual machine (for example using Docker as described in the next chapter). Starting with .Net 6 you can build Linux targets from Windows and macOS too. 


### Cross Compilation with Docker

This way you can compile on your Windows machine, or any machine where you don't want to install XRT permanently. Note that you still need to download the complete Vitis XDK separately for licensing reasons and it takes about 125GB (and at least 50GB more temporarily) to set up the image. Of course you need [Docker installed](https://docs.docker.com/get-docker/) too. However there are no alternatives on Windows so please bear with it. Following these steps you will get a container with Vitis XDK and .NET Core 3.1 SDK installed. Please remember not to distribute the resulting image!

1. Download the _Xilinx Vitis 2020.2: All OS Installer Single-File_ version from the [Vitis Downloads](https://www.xilinx.com/support/download/index.html/content/xilinx/en/downloadNav/vitis.html).
2. Extract the Xilinx_Unified_2020.2_* folder from it (`tar xzf Xilinx_Unified_2020.2_*`) and copy the folder into _Container_ inside this project.
3. Download the XRT, deployment platform and development platform packages for CentOS 7:
    * You can download the latest released packages from the Getting Started section of the card's product page (eg. [U250](https://www.xilinx.com/products/boards-and-kits/alveo/u250.html#gettingStarted)).
    * If you are using Azure, all device specific packages must be downloaded from the [Xilinx Lounge](http://www.xilinx.com/member/alveo-platform.html) instead. The NP servers currently require the _RedHat / CentOS 7.6 & 7.8_ files from the _Alveo U250 Gen3x16 XDMA Platform 2.1 Downloads_ section. Make sure to download both packages for XRT and Deployment Target Platform, not just the ones marked Azure.
4. Copy the files into the _Container/platform_ folder.
   - If you downloaded the Azure packages too, make sure you don't end up with multiple versions of the same package. For example at the time of writing this document the main _Deployment Target Platform_ archive contained the package _xilinx-u250-gen3x16-xdma-validate-2.1-**2948414**.noarch.rpm_. However a newer, Azure-specific version _xilinx-u250-gen3x16-xdma-validate-2.1-**3005608**.1.noarch.rpm_ was also up for download. Such a clash causes a multilib version problem. That can be resolved by removing older one and keeping the Azure-specific version.
5. Extract any tar.gz archive in _Container/platform_ and delete the archives. In the end you should only have rpm files.
6. Copy the `centos7-install.sh` to the _Container_ as well.
7. Open a shell of your choice while still in the _Container_ folder and type `docker build -t vitis .` to create an image. This will take a while.
8. Open Docker Desktop to verify that the "vitis" image appeared in the *Images* tab.
9. Clean up after the build is finished with the `docker builder prune -a -f` command. If you notice excessive RAM usage by the Vmmem process then close down Docker Desktop, shut down WSL with the `wsl --shutdown` command, then restart Docker Desktop and continue with the next steps.
10. Go back to Docker Desktop and click *Run* on the "vitis" image.
11. Expand the *Optional Settings* and create a shared directory by selecting a *Host Path* (this can be anywhere), and entering "/data" into the *Container Path* field.
12. Switch to the *Containers / Apps* tab in Docker Desktop and click on the CLI (`>_`) button.
13. A window with `sh` shell will appear. Type `bash` as it already has the XRT setup configured. 
14. Copy your Hastlayer project into the shared folder and access it through the _/data_ directory.
    
As you can see it was as simple as 1, 2, 13!

If you'd like to move the Docker WLS files (which can be upwards of 100 GB) to another folder/drive then follow [this guide](https://github.com/docker/for-win/issues/7348#issuecomment-647160351).


## Specialized Targets

Aside from general Vitis accelerator card support, there are a couple specialized targets that have their own documentation:
- [Using Hastlayer with Vitis boards on Azure NP Servers](Docs/AzureReadme.md)
- [Using Hastlayer with Zynq SoC modules](Docs/ZynqReadme.md)
  - [Building PetaLinux 2020.2 SD card image for Hastlayer](Docs/ZynqPetaLinux.md)


## Other Remarks

If you ever get an error *\[XRT\] ERROR: some device is already programmed* due to a crashed or interrupted execution, you can reset the card using `xbutil reset` command. See more info about the Xilinx Board Utility [here](https://www.xilinx.com/html_docs/xilinx2019_1/sdaccel_doc/yrx1536963262111.html).

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
