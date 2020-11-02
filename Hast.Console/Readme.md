# Hastlayer - Console



Provides a console application to access the services of Hastlayer.

To get general help use the `dotnet Hast.Console.dll --help` command. To get help regarding a subcommand use the `dotnet Hast.Console.dll <name> help` command for example `dotnet Hast.Console.dll vitis help`.


## Subcommands

A subcommand must be the very first argument after `dotnet Hast.Console.dll`, other arguments can not precede it. It switches the application to a different mode governed by that `ISubcommand`. The valid options are:
- `vitis`: Provides features specific to Xilinx Vitis.


### Vitis

Features related to `Hast.Vitis.Abstractions`.

- `dotnet Hast.Console.dll vitis help`: get help specific to this subcommand.
- `dotnet Hast.Console.dll vitis build -i HardwareFramework -o kernel_binary.xclbin --platform xilinx_u200_xdma_201830_2 --hash some_text`: build RTL source like the ones generated during Hastlayer usage into xclbin files with the "Xilinx Vitis" toolchain. The `-i` refers to the directory containing the rtl directory. The `--platform` is the directory name in `/opt/xilinx/platforms`. 
- `dotnet Hast.Console.dll vitis json -i `: convert RPT files that contain synthesis or build reports into JSON files. 
