# Using Hastlayer with Zynq SoC modules

This section has both generic instructions and specific steps for setting up `Hast.Samples.Consumer` for the _Trenz TE0715-04-30-1C_ module. Use the latter as template for your own projects.

## Before you Start

You need to create your own PetaLinux 2020.2 boot image first. We have a tutorial for that in a separate document [here](ZynqPetaLinux.md)

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

For maximum performance you should compile a Ready-to-Run build of Hastlayer. This requires a Linux host or a Linux virtual machine, for example using Docker. See more details in the _Cross Compilation with Docker_ section in [the root Readme](../Readme.md).

### Running your own Project

You can either introduce a flag or environment checker logic to exit after `hastlayer.GenerateHardware()` was called. Or simply accept that that application will exit with an exception on the cross compiler machine.

You also have to set the `HardwareGenerationConfiguration.SingleBinaryPath` property to the expected path of the _xclbin_ file. You will upload the contents of the _HardwareFramework/bin_ directory on the embedded device and that's what the `SingleBinaryPath` value should reflect. If there is no file in that path then the build will start as normal, so you can hard code it to a suitable value if necessary. For example you include the following line in your code:

```csharp
configuration.SingleBinaryPath = "/media/sd-mmcblk0p1/my-payload.xclbin"
 ```

That won't have an effect on the build machine because no file will be on that path. When the build is complete, you will have the following files in your _HardwareFramework/bin_ directory (the actual file names are hash codes so they will differ):
- 003494f20d9b2a7a3a8cc1d42a18a5ce6313962e565ad03d38cffd1505c391ee.bit.bin
- 003494f20d9b2a7a3a8cc1d42a18a5ce6313962e565ad03d38cffd1505c391ee.xclbin
- 003494f20d9b2a7a3a8cc1d42a18a5ce6313962e565ad03d38cffd1505c391ee.xclbin.info
- 003494f20d9b2a7a3a8cc1d42a18a5ce6313962e565ad03d38cffd1505c391ee.xclbin.name
- 003494f20d9b2a7a3a8cc1d42a18a5ce6313962e565ad03d38cffd1505c391ee.xclbin.set-scale-path

Then rename each file replacing the _003494f20d9b2a7a3a8cc1d42a18a5ce6313962e565ad03d38cffd1505c391ee_ with _my-payload_ and copy them to the SD card using SFTP (_/media/sd-mmcblk0p1/_ directory).

You can see an example of `HardwareGenerationConfiguration.SingleBinaryPath` usage in _src/Samples/Hast.Samples.Consumer/Program.cs_.


## SD Card Preparation Steps

You need to upload some files to your microSD card to have a usable system. Prefer to use a card reader; copying the files via an Android phone may cause the device not to be able to boot from the card for some unknown reason.

1. Format the SD card to FAT32.
2. Copy the _boot.bin_, _boot.scr_ and _image.ub_ files mentioned in _Before you Start_ into your SD root.
3. Download the Linux Arm32 **binary** version of the .Net 5 SDK from [here](https://dotnet.microsoft.com/download/dotnet/5.0) and extract it into a directory with the same name as the _tar.gz_ file on the SD card (e.g. _/dotnet-sdk-5.0.400-linux-arm_). This is important.
4. Copy the application directory with your Hastlayer project, for example _/Hast.Samples.Consumer_.
5. Copy all files from your _HardwareFramework/bin__ directory to the card. Doesn't matter where, in this example we copy it to the _/benchmarks_ directory.
6. Copy the [_zynq-benchmark.dot.sh_](Attachments/zynq-benchmark.dot.sh) file to the root of your card. This is helpful even if you are running a different project.

> ℹ️ If you plan using the _Hast.Samples.Demo_ project, then copy the [_run-demo.sh_](Attachments/run-demo.sh) file to the root as well. This is a launcher script for presentations, it makes use of _zynq-benchmark.dot.sh_ to prepare and launch the Hastlayer demonstration. Just run it with `bash run-demo.sh`.

## Hardware and Network Setup

Connect your device to your router with an Ethernet cable. For the TE0706 carrier board it's the port that's _not_ lit (and won't light up when you plug it in either). Then plug in the power adapter into the barrel jack.

Once powered up, you need to know its IP address. You can use your router's web UI or other network discovery tools (like [Advanced IP Scanner](https://www.advanced-ip-scanner.com/)). Alternatively, if your device has a JTAG connector you can connect using USB serial shell and type `ifconfig` (with e.g. [MobaXterm](https://mobaxterm.mobatek.net/) select the "Serial" session type, choose a serial port and set the speed to 115200 bps). When you have the IP address set up an SSH connection for future convenience. You should be able to log in with "root" as your user name and password.

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
