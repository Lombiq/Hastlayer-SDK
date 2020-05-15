# Nimbix Usage

To run a Hastlayer application on Nimbix, a compute instance must be launched, the dependencies installed and the host & device applications uploaded.

## Administrative steps

1. [Log into their platform](https://platform.jarvice.com/)
2. On the right sidebar click "Compute" and find "Xilinx Vitis Unified Software Platform 2019.2" in the search bar. (at the time of this writing this option let you pick U200, U250 and U280 while there is a separate one for U50)
3. Activate "Desktop Mode with FPGA" and select the card (tested on U280).
4. You should be sent to the Dashboard section with a thumbnail for the running instance.

## Setup

1. In the Dashboard click on the ⓘ icon and note the SSH address and password.
2. Log in using an SFTP client such as Filezilla.
3. Navigate to the `/data` directory. This is the only persistent location between shutdowns.
4. Upload the `nimbix-install.sh` from this directory.
5. Upload the host executable (eg the directory containing Hast.Samples.Consumer.dll).
6. Upload the RTL source code.
7. In the Nimbix Dashboard click on the instance's image to open the web GUI.
8. Click on the start menu at the bottom left and select Terminal Emulator.
9. Type in the following to set up dependencies:
```
cd /data
source nimbix-install.sh
```
10. `cd` to the RTL source folder.
11. Find the correct platform name by typing `ls /opt/xilinx/platforms/` .
12. `make all TARGET=hw PLATFORM=platform_name` where platform_name is from the previous step.
13. Wait for a long time.
15. `cp -R xclbin /data/host_programs_directory/` and then `cd` to the same location.

## Prepare and Execute Host

This is assuming that you are going to run Hast.Samples.Consumer but other apps will behave similarly.
1. `dotnet Hast.Samples.Consumer.dll -device "Alveo U280" -sample ImageProcessingAlgorithms`
2. If it fails with a FileNotFoundException, you need to edit the freshly generated VitisCommunicationService.json file. You can edit the file with either of the pre-installed text editors: `vim` or `emacs`.
3. If it ran successfully, verify the output by typing `thunar contrast.bmp`.
