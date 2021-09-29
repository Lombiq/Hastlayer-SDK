# Using Hastlayer with Zynq SoC modules

This section has both generic instructions and specific steps for setting up `Hast.Samples.Consumer` for the _Trenz TE0715-04-30-1C_ module. Use the latter as template for your own projects.


## Before you Start

You need to create your own PetaLinux 2020.2 boot image first. We have a tutorial for that in a separate document [here](BuildingPetaLinuxForHastlayer.md)


## Cross Compilation

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


## SD Card Preparation Steps

You need to upload some files to your microSD card to have a usable system:

1. Copy the _boot.bin_, _boot.scr_ and _image.ub_ files mentioned in _Before you Start_ into your SD root.
2. Download the Linux Arm32 **binary** version of the .Net 5 SDK from [here](https://dotnet.microsoft.com/download/dotnet/5.0) and extract it into a directory with the same name as the _tar.gz_ file on the SD card (e.g. _/dotnet-sdk-5.0.400-linux-arm_). This is important.
3. Copy the application directory with your Hastlayer project, for example _/Hast.Samples.Consumer_.
4. Copy all files from your _HardwareFramework/bin__ directory to the card. Doesn't matter where, in this example we copy it to the _/benchmarks_ directory.
5. Copy the [_zynq-benchmark.dot.sh_](../../../Docs/Attachments/zynq-benchmark.dot.sh) file to the root of your card. This is helpful even if you are running a different project.


## Hardware and Network Setup

Connect your device to your router with an Ethernet cable. For the TE0706 carrier board it's the port that's _not_ lit. Then plug in the power adapter into the barrel jack.

Once powered up, you need to know its IP address. You can use your router's web UI or other network discovery tools. Alternatively, if your device has a JTAG connector you can connect using USB serial shell and type `ifconfig`. When you have the IP address set up an SSH connection for future convenience. You should be able to log in with "root" as your user name and password.

You can also transfer files via SSH using an SFTP client (e.g. FileZilla) so you won't need to take out the SD card from this point. **Don't** upload files to anywhere except _/media/sd-mmcblk0p1/_. The rest of the file system is volatile and will be reset after each reboot. 


## Running your Program through SSH

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
