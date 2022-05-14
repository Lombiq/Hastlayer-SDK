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

1. (Optional) If you want to save machine time, you can pre-compile the Vitis binaries locally on any machine that has the XRT and the platform library installed. In this case run the host application normally until the hardware generation is finished. If the host attempts execution it will throw an exception but this is normal. The generated files will be in your HardwareFramework directory.   
2. Set up access to your JARVICE Storage Vault as explained [in the documentation](https://support.nimbix.net/hc/en-us/articles/208083526-How-do-I-transfer-files-to-and-from-JARVICE-). This will serve as persistent storage and will be accessible even if you have no jobs running. You can e.g. set up an SFTP connection to your Nimbix storage as explained [here](https://support.nimbix.net/hc/en-us/articles/115000157983-How-to-Upload-Data-to-JARVICE-using-SFTP). A suitable SFTP client is e.g. FileZilla (for Total Commander you need the [SFTP plugin](https://www.ghisler.com/plugins.htm) that you can use like explained [here](https://webhosting.platon.org/article.php?support::totalcommander)). (Note that SFTP is FTP via SSH, not to be confused with FTPS, i.e. FTP via SSL.)
3. Upload the build directory of the host executable (ie. the directory containing *Hast.Samples.Consumer.dll*). This will also contain newly generated RTL sources in the *HardwareFramework* directory. You can zip up the folder and [unzip it](https://linuxize.com/post/how-to-unzip-files-in-linux/)  on the Nimbix machine to avoid a slow upload.
4. In the Nimbix Dashboard click on the instance's image to open the web VNC GUI.
5. Click on the Nimbix ("start") menu at the bottom left and select Terminal Emulator.
6. Type in the following to set up dependencies:<br>
  `cd /data/host_programs_directory`<br>
  `source ubuntu-install.sh`
7. Execute the host program, see below. On first execution it will start to build the new xclbin file. Upon subsequent runs the binary will be detected and the compilation step skipped. 
8. Wait for a long time. The baseline time requirements (when compiling `MemoryTest` for U280) is around 2h 15m. U50, as the smallest board, is the fastest to compile for.

You can have multiple such compilations running at the same time, as there are enough hardware resources, depending on the complexity of the generated hardware (i.e. the input software) and the targeted board. To see the resources usage of the VM you can use the console app [top](https://linux.die.net/man/1/top) or install [System Monitor](https://help.gnome.org/users/gnome-system-monitor/) with `sudo apt-get --yes --force-yes install gnome-system-monitor` (located under the System category in the start menu).


## Prepare and execute host

This is assuming that you are going to run `Hast.Samples.Consumer` with the image processing sample but other apps will behave similarly.

1. Run the `dotnet Hast.Samples.Consumer.dll -device "Alveo U280" -sample ImageProcessingAlgorithms` command. Add the `-verify` switch if you want to verify whether the hardware output is the same as the software one, i.e. the device works properly. Also add the `-appname` and `-appsecret` switches if you don't have these hard-coded in the app.
3. If it ran successfully, check the output picture by typing `thunar contrast.bmp`.


## Restarting

To save on compute costs, shut down the job (i.e. turn off the instance) when not needed. At that point everything outside the */data* folder is lost, including all personal settings.

You can get back by launching a new instance (Administrative steps 2 - 5) and then running the install script (Setup step 8).


## Tips & notes

- While your instance is running, you can see the SSH credentials by clicking on the ⓘ. Connect to it using SSH/PuTTY to for a better shell with clipboard access.

