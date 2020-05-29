# Nimbix Usage



To run a Hastlayer applications on the [Nimbix HPC cloud](https://www.nimbix.net/), a compute instance must be launched, the dependencies installed and the host & device applications uploaded.


## Administrative steps

1. Sign up for the [Nimbix Alveo Trial](https://www.nimbix.net/alveotrial).
2. [Log into the Nimbix platform](https://platform.jarvice.com/) after the registration is complete. [Here](https://support.nimbix.net/hc/en-us/articles/360035258971-Getting-Started-With-Alveo-Trial) is also some more getting started information (note that the costs indicator only updates when you shut down your job).
3. On the right sidebar click "Compute" and find "Xilinx Vitis Unified Software Platform 2019.2" in the search bar. (At the time of writing this that option lets you pick U200, U250 and U280 while there is a separate one for U50.)
4. Activate "Desktop Mode with FPGA" and select the machine type with the appropriate board (tested on U280).
5. You should be sent to the Dashboard section with a thumbnail for the running instance.


## Setup

1. Set up an SFTP connection to your Nimbix storage as explained [here](https://support.nimbix.net/hc/en-us/articles/115000157983-How-to-Upload-Data-to-JARVICE-using-SFTP). This will serve as persistent storage and will be accessible even if you have no jobs running. A suitable SFTP client is e.g. FileZilla (for Total Commander you need the [SFTP plugin](https://www.ghisler.com/plugins.htm) that you can use like xplained [here](https://webhosting.platon.org/article.php?support::totalcommander)). (Note that SFTP is FTP via SSH, not to be confused with FTPS, i.e. FTP via SSL.)
2. Navigate to the `/data` directory. Note that only files put here will remain between job shutdowns.
3. Create a *HardwareFramework* directory in the output directory of the host executable (e.g. the `Hast.Samples.Consumer` project) and copy the whole folder of the Hastlayer Hardware Framework - Xilinx Vitis project into it.
4. Run the host executable locally so the RTL source is generated.
5. Upload the output directory of the host executable, e.g. the directory containing *Hast.Samples.Consumer.dll*. This will include the scripts for the next steps, as well as the newly generated RTL sources (in the *HardwareFramework* directory).
6. In the Nimbix Dashboard click on the instance's image to open the web GUI.
7. Click on the start menu at the bottom left and select Terminal Emulator.
8. Type in the following to set up dependencies:
```
cd /data/host_programs_directory
source nimbix-install.sh
```
9. Run `source nimbix-compile.sh` to compile the generated hardware for the current U280 platform available on Nimbix. Alternatively, if you're targeting something else:
    1. `cd` to the RTL source folder.
    2. Find the correct platform name by typing `ls /opt/xilinx/platforms/` .
    3. `make all TARGET=hw DEVICE=platform_name` where platform_name is from the previous step.
10. Wait for a long time. The baseline time requirements (when compiling `MemoryTest`) is around 2h 15m.


## Prepare and Execute Host

This is assuming that you are going to run `Hast.Samples.Consumer` but other apps will behave similarly.
1. `dotnet Hast.Samples.Consumer.dll -device "Alveo U280" -sample ImageProcessingAlgorithms`
3. If it ran successfully, verify the output by typing `thunar contrast.bmp`.


## Restaring

To save on compute costs, shut down the job (i.e. turn off the instance) when not needed. At that point everything outside the */data* folder is lost, including all personal settings.

You can get back by launching a new instance (Administrative steps 2 - 5) and then running the install script (Setup step 9).


## Tips & Notes

- While your instance is running, you can see the SSH credentials by clicking on the ⓘ. Connect to it using SSH/PuTTY to for a better shell with clipboard access.

