# Using Hastlayer with Vitis boards on Azure NP Servers



If you want work with an Alveo card on an Azure VM, you need to pick the Azure-specific device (currently only `Azure Alveo U250`). This alters some of the automatic compilation steps. After compilation it submits your binary to an attestation server (via Azure Blob Storage) for automatic approval.


## Preparation

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


## Configuration

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
5. Click Security + networking â†’ Access Keys on the side bar.
6. Click the Show Keys button.
7. The "Storage account name" field becomes `StorageAccountName` and the "Key" becomes `StorageAccountKey`.
8. Go back to the home page and select Active Directory.
9. The Tenant ID on the Overview page becomes `ClientTenantId`.

Additionally, during compilation only a netlist is generated instead of a bitstream so there won't be any reports based on simulation data.


## Execution

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
