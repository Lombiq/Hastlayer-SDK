# Docker Setup

This way you can compile on your Windows machine, or any machine where you don't want to install XRT permanently. Note that you still need to download the complete Vitis XDK separately for licensing reasons and it takes about 125GB (and at least 50GB more temporarily) to set up the image. Of course you need [Docker installed](https://docs.docker.com/get-docker/) too. However there are no alternatives on Windows so please bear with it. Following these steps you will get a container with Vitis XDK and .NET Core 3.1 SDK installed. Please remember not to distribute the resulting image!


## Installation Steps

1. Download the _Xilinx Vitis 2020.2: All OS Installer Single-File_ version from the [Vitis Downloads](https://www.xilinx.com/support/download/index.html/content/xilinx/en/downloadNav/vitis/2020-2.html).
2. Extract the Xilinx_Unified_2020.2_* folder from it (`tar xzf Xilinx_Unified_2020.2_*`) and copy the folder into _Container_ inside this project.
3. Download the XRT, deployment platform and development platform packages for CentOS 7:
    * You can download the latest released packages from the Getting Started section of the card's product page (eg. [U250](https://www.xilinx.com/products/boards-and-kits/alveo/u250.html#gettingStarted)).
    * If you are using Azure, all device specific packages must be downloaded from the [Xilinx Lounge](http://www.xilinx.com/member/alveo-platform.html) instead. The NP servers currently require the _RedHat / CentOS 7.6 & 7.8_ files from the _Alveo U250 Gen3x16 XDMA Platform 2.1 Downloads_ section. Make sure to download both packages for XRT and Deployment Target Platform, not just the ones marked Azure.
    * If you are using Zynq, you don't need to download a platform package. The _Hast.Vitis.Abstractions.HardwareFramework_ project already copies the package files into the _HardwareFramework/packages_ directory on build.
4. Copy the files into the _Container/platform_ folder.
    - If you downloaded the Azure packages too, make sure you don't end up with multiple versions of the same package. For example at the time of writing this document the main _Deployment Target Platform_ archive contained the package _xilinx-u250-gen3x16-xdma-validate-2.1-**2948414**.noarch.rpm_. However a newer, Azure-specific version _xilinx-u250-gen3x16-xdma-validate-2.1-**3005608**.1.noarch.rpm_ was also up for download. Such a clash causes a multilib version problem. That can be resolved by removing older one and keeping the Azure-specific version.
5. Extract any tar.gz archive in _Container/platform_ and delete the archives. In the end you should only have rpm files.
6. Copy the `centos7-install.sh` and `fake-xterm.sh` to the _Container_ as well.
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


## Using Graphical Applications on a Windows Host

Some necessary third party tools require a graphical user interface to work at all. Generally we aren't in favor of forcing users to install a graphical environment if they otherwise don't need it, and strive to make all of the instructions work from command line. Unless you plan to develop a Linux GUI desktop application (Gtk#, Qml.Net, Avalonia, Uno, etc) we suggest to return to this section only when you are prompted by another document.

First you need an X11 server on your host machine:
1. Download VcXsrv.
   * If you have [Chocolatey](https://chocolatey.org/) type `choco install vcxsrv`.
   * Otherwise download the installer from the project [SourceForge page](https://sourceforge.net/projects/vcxsrv/).
2. Launch _XLaunch_ from the start menu.
3. A configuration wizard will open. You can leave everything as-is, except you should check the _Disable access control_ box on the _Extra settings_ page. 
   * You can click on the _Save configuration_ button on the next page to create a _config.xlaunch_ file to skip these steps in the future.
4. The application runs in the background, you can find its X icon on the notification area.
5. If you mouse over the icon you will see a tooltip in this information "<HOST PC NAME>:0.0". If your doesn't end in "0.0", take note of it and substitute the later commands accordingly.
6. Type `ipconfig` in a command prompt and note the local IP (_Ethernet adapter vEthernet (WSL)_).
7. Combine the two to get something like _172.19.208.1:0.0_. This is going to be your **display value**.

Now that your display server is running, let's head over to the Docker container:
1. Open a command line window using either options:
   * Open Docker Desktop and click on its CLI (`>_`) button.
   * Type `docker exec -it <container name> /bin/sh`
2. Enter `echo 'export DISPLAY=XXX' >> ~/.bashrc` while substituting XXX with the display value mentioned before.
3. Type `bash`, it will configure your display on start.
4. Verify this by typing `echo $DISPLAY`, it should print your display value.
5. Install _xterm_ with `yum install -y xterm`.
6. Type `xterm`, a new terminal window should appear which is sent from inside the container. You can treat this like a very fast Remote Desktop (or RemoteApp) connection with all that implies.
