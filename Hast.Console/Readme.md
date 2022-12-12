# Hastlayer - Console

Provides a console application to access the services of Hastlayer.

To get general help use the `dotnet Hast.Console.dll --help` command. To get help regarding a subcommand use the `dotnet Hast.Console.dll <name> help` command for example `dotnet Hast.Console.dll vitis help`.

## Subcommands

A subcommand must be the very first argument after `dotnet Hast.Console.dll`, other arguments can not precede it. It switches the application to a different mode governed by that `ISubcommand`. The valid options are:
- `vitis`: Provides features specific to Xilinx Vitis.

### Vitis

Features related to `Hast.Vitis.Abstractions`.

- `dotnet Hast.Console.dll vitis help`: Get help specific to this subcommand.
- `dotnet Hast.Console.dll vitis build --input HardwareFramework --output kernel_binary.xclbin --platform xilinx_u200_xdma_201830_2 --hash some_text`: Build RTL source like the ones generated during Hastlayer usage into xclbin files with the "Xilinx Vitis" toolchain. The `--input` sets the `HardwareFramework` directory that contains the `rtl` directory. The `--platform` is the directory name in `/opt/xilinx/platforms`. 
- `dotnet Hast.Console.dll vitis json --input file.rpt --output file.json`: Convert RPT files that contain synthesis or build reports into JSON files. 

## Developing Hast.Console

Any features that can be reasonably grouped together or need custom arguments should be added as subcommands. Make two new classes: 

- One that implements `ISubcommand` for the logic, for example `SampleSubcommand`.
- One that inherits from `MainOptions` for the argument definitions, for example `SampleOptions`.

The `SampleSubcommand` must:

- Be annotated with the `[Subcommand("sample")]` attribute where "sample" is the subcommand name.
- Have a `public SampleSubcommand(string[] rawArguments)` constructor.
- Use `SampleSubcommand.Run()` to parse the arguments received in the constructor.

In `SampleOptions` you should not use single-character switches to avoid confusion.
