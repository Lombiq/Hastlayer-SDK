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


## Using Vitis on Azure NP Servers

If you want work with an Alveo card on an Azure VM, you need to pick the Azure-specific device (currently only `Azure Alveo U250`). This alters some of the automatic compilation steps. After compilation it submits your binary to an attestation server (via Azure Blob Storage) for automatic approval.

### Preparation

You have to set up an NP10s virtual machine via the Azure Portal.
1. Go to the [Select an image](https://portal.azure.com/#create/Microsoft.VirtualMachine) page.
2. Enter "xilinx" into the search bar.
3. Select the Centos 7 deployment image (Xilinx Alveo U250 Deployment VM - Centos7.8).
4. Fill out the _Create a virtual machine_ form:
    - Fill the _Virtual machine name_ field.
    - Set the _Region_ to East US, West US2, West Europe or SouthEast Asia.
    - Click _See all sizes_ and search for "np10s".
    - Set up the administrator account's SSH key or specify the user/password.
5. Click _Next : Disks_ and decide if you need additional disks.
6. The rest should be fine as-is, click _Review + create_.

    
**Troubleshooting**: if you don't see the _Standard_NP10s_ size option, [see the first FAQ item on this page](https://docs.microsoft.com/en-us/azure/virtual-machines/np-series#frequently-asked-questions) as you might need to request quota or the available regions might've changed since the writing of this document.

Once the VM is running, you have to install the correct platform and runtime files from the [Xilinx Lounge](http://www.xilinx.com/member/alveo-platform.html), the Vitis SDK and the .NET runtime or SDK. Transfer all package files into the same directory, navigate into it and type the following to install them at once:

**Ubuntu**
```shell
sudo apt install *.deb
source ubuntu-install.sh
```

**CentOS 7**
```shell
sudo yum localinstall *.rpm
source centos7-install.sh
```

You can use any SSH client of your choice but we recommend checking out [MobaXterm](https://mobaxterm.mobatek.net/).

### Configuration

The approval process requires addition configuration. Fill out and add the below `AzureAttestationConfiguration` property to the `CustomConfiguration` in your `appsettings.json` file.

```json
{
    "HardwareGenerationConfiguration": {
        "CustomConfiguration": {
            "AzureAttestationConfiguration": {
                "StorageAccountName": "From portal.",
                "StorageAccountKey": "From portal.",
                "ClientSubscriptionId": "From portal.",
                "ClientTenantId": "From portal."
            }
        }
    }
}
```

To get this information from your Azure account:
1. Go to the [Azure Portal](https://portal.azure.com/).
2. Click Storage Accounts.
3. Select your account or create a new one with *Blob Storage*.
4. The Subscription ID on the Overview page becomes `ClientSubscriptionId`.
5. Click Settings | Access Keys on the side bar.
6. Click the Show Keys button.
7. The "Storage account name" field becomes `StorageAccountName` and the "Key" becomes `StorageAccountKey`.
8. Go back to the home page and select Active Directory.
9. The Tenant ID on the Overview page becomes `ClientTenantId`.

Additionally, during compilation only a netlist is generated instead of a bitstream so there won't be any reports based on simulation data.


### Execution

As of writing this document, the Azure VMs aren't meant for online compilation and there isn't enough space to install the full Vitis SDK. So you have to build and run attestation on a separate machine. The build machine must have Vitis 2020.2, but doesn't need Alveo hardware.

On the build machine:
1. Navigate to the application directory.
2. Configure *appsettings.json* as described above.
3. Run the Hastlayer application with the "Azure Alveo U250" device selected. It will terminate with an exception after the build is finished because there is no hardware.
4. Copy the application to the Azure VM. If you already did that, copy the *HardwareFramework/bin* directory and the *appsettings.json* file.

On the Azure virtual machine:
1. Run `export XILINX_VITIS=/; source /opt/xilinx/xrt/setup.sh` to set up the environment. You may want to copy this into your `~/.bashrc` for future convenience.
2. Navigate to the application directory.
3. Run the Hastlayer application with the same parameters you did on the build machine.


## Using Hastlayer with Zynq boards

This section has both generic instructions and specific steps for setting up `Hast.Samples.Consumer` for the _Trenz TE0715-04-30-1C_ module. Use the latter as template for your own projects.


### Before you Start

You need to create your own PetaLinux SD card image with the _5.4.0-xilinx-v2020.2_ kernel. You can follow [this page](https://wiki.trenz-electronic.de/display/PD/PetaLinux) on how to create the _boot.bin_, _boot.scr_ and _image.ub_ files that needs to be on your SD card. Since Hastlayer uploads the actual payload at runtime you don't need to worry about the project used. A more Hastlayer-specific setup guide is coming soon.


### Cross Compilation

You can't build _xlbin_ files on a Zynq device. So you have to cross compile on a different machine and copy the prepared files to the SD card. In case of _Hast.Samples.Consumer_ you can run it from your dev machine by calling
```shell
dotnet Hast.Samples.Consumer.dll \
    -device "TE0715-04-30-1C" \
    -sample "ParallelAlgorithm" \
    -name "Parallel Algorithm Sample" \
    -build
``` 

This command will get you through code transformation and build composition without actually trying to execute the result thanks to the `-build` flag. You can run it on any machine with XRT installed even if it doesn't have any FPGAs.

If you are running a different project you can either introduce a flag or environment checker logic to exit after `hastlayer.GenerateHardware()` was called. Or simply accept that that application will exit with an exception on the cross compiler machine. Also, you have to set the `HardwareGenerationConfiguration.SingleBinaryPath` property to ensure the executable on your device knows where to look the for _xclbin_ and its auxiliary files. You can see examples for that in _Samples/Hast.Samples.Consumer/Program.cs_.

For maximum performance you should compile a Ready-to-Run build of Hastlayer. This requires a Linux host or a Linux virtual machine, for example using Docker. See more details in the _Cross Compilation with Docker_ section above.


### SD Card Preparation Steps

You need to upload some files to your microSD card to have a usable system:

1. Copy the _boot.bin_, _boot.scr_ and _image.ub_ files mentioned in _Before you Start_ into your SD root.
2. Download the Linux Arm32 **binary** version of the .Net 5 SDK from [here](https://dotnet.microsoft.com/download/dotnet/5.0) and extract it into a directory with the same name as the _tar.gz_ file on the SD card (e.g. _/dotnet-sdk-5.0.400-linux-arm_). This is important.
3. Copy the application directory with your Hastlayer project, for example _/Hast.Samples.Consumer_.
4. Copy all files from your _HardwareFramework/bin__ directory to the card. Doesn't matter where, in this example we copy it to the _/benchmarks_ directory.
5. Copy the [_zynq-benchmark.dot.sh_](../../Docs/Attachments/zynq-benchmark.dot.sh) file to the root of your card. This is helpful even if you are running a different project.


### Hardware and Network Setup

Connect your device to your router with an Ethernet cable. For the TE0706 carrier board it's the port that's _not_ lit. Then plug in the power adapter into the barrel jack.

Once powered up, you need to know its IP address. You can use your router's web UI or other network discovery tools. Alternatively, if your device has a JTAG connector you can connect using USB serial shell and type `ifconfig`. When you have the IP address set up an SSH connection for future convenience. You should be able to log in with "root" as your user name and password.


### Running your Program through SSH

Once you connect through SSH you are in bash. Type the following and you are good to go:
```shell
cd /media/sd-mmcblk0p1/
. zynq-benchmark.dot.sh
```

This sourceable shell script sets up the environment variables, creates the necessary library symlinks, and adds  helpful functions some of which are specific to benchmarks:
- `run-benchmark filename.xclbin`: Executes the sample in the current directory without verification. 
- `run-and-verify filename.xclbin`: Same verifies the result against the CPU.
- `cls`: It's `clear` for Windows people.
- `title "Some Text"`: Displays a decorative title that helps partition your console output.

If you are running _Hast.Samples.Consumer_ type the following:

```shell
cd /media/sd-mmcblk0p1/benchmarks
for xclbin in *.xclbin; do
  run-and-verify $xclbin
done
```

If you are running your own project make sure to call `fpgautil -b filename.bit.bin` at least once per startup. This uploads the actual binary payload to the FPGA. The _xclbin_ file is only used for meta-data.


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
