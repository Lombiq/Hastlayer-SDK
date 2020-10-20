# Nimbix usage



To run a Hastlayer applications on the [Nimbix HPC cloud](https://www.nimbix.net/), a compute instance must be launched, the dependencies installed and the host & device applications uploaded. You can find the necessary instructions here.

Be sure to also check out the [general docs](../Readme.md).


## Administrative steps

1. Sign up for the [Nimbix Alveo Trial](https://www.nimbix.net/alveotrial).
2. [Log into the Nimbix platform](https://platform.jarvice.com/) after the registration is complete. [Here](https://support.nimbix.net/hc/en-us/articles/360035258971-Getting-Started-With-Alveo-Trial) is also some more getting started information (note that the costs indicator only updates when you shut down your job).
3. On the right sidebar click "Compute" and find "Xilinx Vitis Unified Software Platform 2020.1" in the search bar. At the time of writing this that option lets you pick U50, U200, and U280.
4. Activate "Desktop Mode with FPGA" and select the machine type with the appropriate board. Note that Nimbix might be overloaded and not every board you can select will actually be available, resulting in the job being queued forever. Check out the availability on the [Nimbix status page](https://status.jarvice.com/) first.
5. You should be sent to the Dashboard section with a thumbnail for the running instance.


## Setup

1. While still on your local machine create a *HardwareFramework* directory in the output directory of the host executable (e.g. the `Hast.Samples.Consumer` project) and copy the whole folder of the Hastlayer Hardware Framework - Xilinx Vitis project into it.
2. Run the host executable configured for the selected device so the RTL source is generated (or you can also do this already on the Nimbix machine but it's simpler locally).
3. Set up access to your JARVICE Storage Vault as explained [in the documentation](https://support.nimbix.net/hc/en-us/articles/208083526-How-do-I-transfer-files-to-and-from-JARVICE-). This will serve as persistent storage and will be accessible even if you have no jobs running. You can e.g. set up an SFTP connection to your Nimbix storage as explained [here](https://support.nimbix.net/hc/en-us/articles/115000157983-How-to-Upload-Data-to-JARVICE-using-SFTP). A suitable SFTP client is e.g. FileZilla (for Total Commander you need the [SFTP plugin](https://www.ghisler.com/plugins.htm) that you can use like explained [here](https://webhosting.platon.org/article.php?support::totalcommander)). (Note that SFTP is FTP via SSH, not to be confused with FTPS, i.e. FTP via SSL.)
4. Navigate to the `/data` directory. Note that only files put here will remain between job shutdowns.
5. Upload the output directory of the host executable, e.g. the directory containing *Hast.Samples.Consumer.dll*. This will include the scripts for the next steps, as well as the newly generated RTL sources (in the *HardwareFramework* directory).
6. In the Nimbix Dashboard click on the instance's image to open the web GUI.
7. Click on the Nimbix ("start") menu at the bottom left and select Terminal Emulator.
8. Type in the following to set up dependencies:
```
cd /data/host_programs_directory
source nimbix-install.sh
```
9. Run `source nimbix-compile.sh` to compile the generated hardware for the current U280 platform available on Nimbix. Alternatively, if you're targeting something else:
    1. `cd` to the RTL source folder.
    2. Find the correct platform name by typing `ls /opt/xilinx/platforms`.
    3. `make all TARGET=hw DEVICE=platform_name` where platform_name is from the previous step. Note that the U50 board is available in two PCIe configurations (x4 and x16) and these are not interchangeable: Use the exact platform for the board attached to the VM.
10. Wait for a long time. The baseline time requirements (when compiling `MemoryTest` for U280) is around 2h 15m. U50, as the smallest board, is the fastest to compile for.

You can have multiple such compilations running at the same time, as there are enough hardware resources, depending on the complexity of the generated hardware (i.e. the input software) and the targeted board. To see the resources usage of the VM you can install System Monitor wit `sudo apt-get --yes --force-yes install gnome-system-monitor` (you'll then find it under the System category in the start menu).


## Prepare and execute host

This is assuming that you are going to run `Hast.Samples.Consumer` with the image procesing sample but other apps will behave similarly.

1. Run the `dotnet Hast.Samples.Consumer.dll -device "Alveo U280" -sample ImageProcessingAlgorithms` command. Add the `-verify` switch if you want to verify whether the hardware output is the same as the software one, i.e. the device works properly. Also add the `-appname` and `-appsecret` switches if you don't have these hard-coded in the app.
3. If it ran successfully, check the output picture by typing `thunar contrast.bmp`.


## Restaring

To save on compute costs, shut down the job (i.e. turn off the instance) when not needed. At that point everything outside the */data* folder is lost, including all personal settings.

You can get back by launching a new instance (Administrative steps 2 - 5) and then running the install script (Setup step 8).


## Tips & notes

- While your instance is running, you can see the SSH credentials by clicking on the ⓘ. Connect to it using SSH/PuTTY to for a better shell with clipboard access.

