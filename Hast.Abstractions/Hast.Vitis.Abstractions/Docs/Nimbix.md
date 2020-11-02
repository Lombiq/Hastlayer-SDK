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

1. Run the host executable configured for the selected Vitis device. This generates the RTL source as VHDL. It will terminate with an exception saying "The OpenCL binary (xclbin) is required to start the kernel. The host can't launch without it." This is normal. (You can also do this already on the Nimbix machine but it's simpler locally).
2. Set up access to your JARVICE Storage Vault as explained [in the documentation](https://support.nimbix.net/hc/en-us/articles/208083526-How-do-I-transfer-files-to-and-from-JARVICE-). This will serve as persistent storage and will be accessible even if you have no jobs running. You can e.g. set up an SFTP connection to your Nimbix storage as explained [here](https://support.nimbix.net/hc/en-us/articles/115000157983-How-to-Upload-Data-to-JARVICE-using-SFTP). A suitable SFTP client is e.g. FileZilla (for Total Commander you need the [SFTP plugin](https://www.ghisler.com/plugins.htm) that you can use like explained [here](https://webhosting.platon.org/article.php?support::totalcommander)). (Note that SFTP is FTP via SSH, not to be confused with FTPS, i.e. FTP via SSL.)
3. Upload the build directory of the host executable (ie. the directory containing *Hast.Samples.Consumer.dll*). This contains the scripts for the next steps, as well as the newly generated RTL sources in the *HardwareFramework* directory.
4. In the Nimbix Dashboard click on the instance's image to open the web VNC GUI.
5. Click on the Nimbix ("start") menu at the bottom left and select Terminal Emulator.
6. Type in the following to set up dependencies:<br>
  `cd /data/host_programs_directory`<br>
  `source nimbix-install.sh`
7. Execute the host program, on first execution it will start to build the new xclbin file. Upon subsequent runs the binary will be detected and the compilation step skipped. 
8. Wait for a long time. The baseline time requirements (when compiling `MemoryTest` for U280) is around 2h 15m. U50, as the smallest board, is the fastest to compile for.

You can have multiple such compilations running at the same time, as there are enough hardware resources, depending on the complexity of the generated hardware (i.e. the input software) and the targeted board. To see the resources usage of the VM you can use the console app [top](https://linux.die.net/man/1/top) or install [System Monitor](https://help.gnome.org/users/gnome-system-monitor/) with `sudo apt-get --yes --force-yes install gnome-system-monitor` (located under the System category in the start menu).


## Prepare and execute host

This is assuming that you are going to run `Hast.Samples.Consumer` with the image procesing sample but other apps will behave similarly.

1. Run the `dotnet Hast.Samples.Consumer.dll -device "Alveo U280" -sample ImageProcessingAlgorithms` command. Add the `-verify` switch if you want to verify whether the hardware output is the same as the software one, i.e. the device works properly. Also add the `-appname` and `-appsecret` switches if you don't have these hard-coded in the app.
3. If it ran successfully, check the output picture by typing `thunar contrast.bmp`.


## Restaring

To save on compute costs, shut down the job (i.e. turn off the instance) when not needed. At that point everything outside the */data* folder is lost, including all personal settings.

You can get back by launching a new instance (Administrative steps 2 - 5) and then running the install script (Setup step 8).


## Tips & notes

- While your instance is running, you can see the SSH credentials by clicking on the ⓘ. Connect to it using SSH/PuTTY to for a better shell with clipboard access.

