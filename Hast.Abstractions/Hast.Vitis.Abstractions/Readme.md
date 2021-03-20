# Hastlayer Vitis Driver



## Overview

This project contains the communication service used to connect with [Vitis Unified Software Platform](https://www.xilinx.com/products/design-tools/vitis/vitis-platform.html) devices, such as the [Xilinx Alveo U250](https://www.xilinx.com/products/boards-and-kits/alveo/u250.html) FPGA accelerator card via the [OpenCL](https://www.khronos.org/opencl/) library.

Note that the SH scripts in this project should use LF line endings! You'll get errors such as `-bash: $'\r': command not found` otherwise.

For Nimbix-specific instructions see [the Nimbix docs](Docs/Nimbix.md).


## Requirements

* The system running the FPGA card must be 64-bit Linux (e.g. Ubuntu 18.04.2 LTS or CentOS 7.6) The installation instructions can be found here [in the platform documentation](https://www.xilinx.com/html_docs/xilinx2019_2/vitis_doc/vhc1571429852245.html).
* The device specific software requirements can be found in the card's Getting Started page, e.g. [Alveo U250](https://www.xilinx.com/products/boards-and-kits/alveo/u250.html#gettingStarted).
* Hastlayer has its own software requirements which can be found in the repository's *GettingStarted.md* file.


## Preparation

Even after everything is installed, you have to make sure that the executing user's environment variables are correctly set by sourcing the setup scripts [as described in the documentation](https://www.xilinx.com/html_docs/xilinx2019_2/vitis_doc/rbk1547656041291.html). You can add these commands into the `~/.bashrc` file to avoid having to type them every time. If running from the cloud, like Nimbix, this is probably handled automatically.

For setup instructions on the Nimbix cloud see the [Nimbix-specific instructions](Docs/Nimbix.md).


## Cross Compilation

If you want to build for a platform not in your */opt/xilinx/platforms* directory, you can set the `XILINX_PLATFORM` environment variable to the directory that contains the platform directories. For example `export XILINX_PLATFORM=$HOME/platforms`. If the environment variable is not set or if its value isn't an existing directory then */opt/xilinx/platforms* is used as fallback.

Be sure that all .NET software dependencies are on the same version on both the target and the source computers. Otherwise the source code won't match during transformation. This will result in a different Transformation ID and the XCLBIN file won't be found, prompting a recompilation on the target machine. To mitigate this risk you can try some of the following strategies:
* Perform a system update on both machines before starting, so both are on the most up-to-date frameworks.
* [Publish your program as self-contained](https://docs.microsoft.com/en-us/dotnet/core/deploying/#publish-self-contained), eg. `dotnet publish -c Release -r linux-x64 -p:PublishReadyToRun=true Samples/Hast.Samples.Consumer/Hast.Samples.Consuumer.csproj` (note that Ready to Run [has its own restrictions](https://docs.microsoft.com/en-us/dotnet/core/deploying/ready-to-run#cross-platformarchitecture-restrictions)).


### Cross Compilation with Docker

This way you can compile on your Windows machine, or just any machine where you don't want to install XRT permanently.

0. [Docker should be installed.](https://docs.docker.com/get-docker/)
1. Clone the Xilinx Base Runtime from their GitHub:
```powershell
git clone https://github.com/Xilinx/Xilinx_Base_Runtime.git
```
2. Navigate to the directory with the 2020.2 image for CentOS 7 and build it:
```powershell
cd Xilinx_Base_Runtime/Dockerfiles/2020.2/centos-7/
docker build -t xrt .
```
3. Open Docker Desktop and select the *Images* tab from the sidebar.
4. Click *Run* and expand the *Optional Settings*.
5. Create a shared directory by selecting a *Host Path* and entering "/data" into the *Container Path* field.
6. Copy the `centos7-install.sh` script and the platform install files to the shared directory.   
7. Switch to the *Containers / Apps* tab in Docker Desktop and click on the CLI (`>_`) button.
8. There is no `sudo` but the CLI starts you as root so use the following command to run the installer:
```sh
cd /data
cat centos7-install.sh | sed '1d' | sed 's/\r$//' | sed 's/sudo //' > centos7-install-docker.sh
sh centos7-install-docker.sh
dotnet --version # Just to verify the successful install.
```
9. Install the platform RPM packages you copied over:
```sh
for package in *.rpm; do rpm -Uvh "$package"; done
```

## Using Azure Attestation

If you want work with an Alveo card on an Azure VM, you need to use the Azure-specific driver. (Currently only `Azure Alveo U250`.) This alters some of the automatic compilation steps and after compilation submits your binary to an attestation server (via Azure Blob Storage) for automatic approval.

### Preparation

You should have received documentation on how to set up the Azure VM, please follow its steps. Once the VM is running, you have to install the correct platform and runtime files from the [Xilinx Lounge](http://www.xilinx.com/member/alveo-platform.html), the Vitis SDK and the .Net runtime or SDK. Transfer all package files into the same directory. Then while in that directory type the following to install them at once:

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

You also need to install .Net 5 and prepare the environment. Source the Ubuntu or CentOS install scripts by typing `source ubuntu-install.sh` or `source centos7-install.sh` respectively. 


### Configuration

This feature is currently in the private preview stage. We can't share some specific setting values as they are confidential and you need to set them using information in the validation scripts. If you don't have access to them, please wait until the project leaves the private phase whereupon we will update the fields' default values so you won't have to set them.

The approval process requires addition configuration. Fill out and add the below `AzureAttestationConfiguration` property to the `CustomConfiguration` in your `appsettings.json` file.

```json
{
    "AzureAttestationConfiguration": {
        "StartFunctionUrl": "Look for $FunctionUrl in Validate-FPGAImage.ps1 inside the validation.zip archive.",
        "PollFunctionUrl": "Look for $FunctionUrl in Monitor-Validation.ps1 inside the validation.zip archive.",
        "StorageAccountName": "From portal.",
        "StorageAccountKey": "From portal.",
        "ClientSubscriptionId": "From portal.",
        "ClientTenantId": "From portal."
    }
}
```

To get the rest from your Azure account:
1. Go to the [Azure Portal](https://portal.azure.com/).
2. Click Storage Accounts.
3. Select your account or create a new one with *Blob Storage*.
4. The Subscription ID in the Overview page becomes `ClientSubscriptionId`.
5. CLick Settings | Access Keys on the side bar.
6. Click the Show Keys button.
7. The "Storage account name" field becomes `StorageAccountName` and the "Key" becomes `StorageAccountKey`.
8. Go back to the home page and select Active Directory.
9. The Tenant ID in the Overview page becomes `ClientTenantId`.

Additionally, during compilation only a netlist is generated instead of a bitstream so there won't be any reports based on simulation data.


### Execution

At least during the private preview, the Azure VMs aren't meant online compilation and there isn't enough space to install the full Vitis SDK. So you have to build and run attestation on a separate machine. The build machine must have Vitis 2020.2, but doesn't need Alveo hardware.

On the build machine:
1. Navigate to the application directory.
2. Configure appsettings.json as described above.
3. Run the Hastlayer application with the "Azure Alveo U250" device selected. It will terminate with an exception after the build is finished because there is no hardware.
4. Copy the application to the Azure VM. If you already did that, copy the `HardwareFramework/bi`n directory and the `appsettings.json` file.

On the Azure virtual machine:
1. Run `export XILINX_VITIS=/; source /opt/xilinx/xrt/setup.sh` to set up the environment. You may want to copy this into your `~/.bashrc` for future convenience.
2. Navigate to the application directory.
3. Run the Hastlayer application with the same parameters you did on the build machine.


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
