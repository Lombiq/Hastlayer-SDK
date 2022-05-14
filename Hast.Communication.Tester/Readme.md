# Hastlayer - Communication Tester



Command line application for testing the communication layer.


## About

This application sends test data to any device that has a matching `ICommunicationService` registered. It does not program
the hardware or alter the input so automatic verification is only useful if the device is already behaving as loopback.
You can upload generated content (see `--payload-type` switch) or any binary files. Both generated input and the output
can be saved to file or displayed on the console.


## Usage

1. Check for available devices: `Hast.Communication.Tester.exe --list`
2. Consult with the help screen to learn about input options: `Hast.Communication.Tester.exe --help`
3. Send test entry, for example: `Hast.Communication.Tester.exe --device Catapult --bytes 1024 --member-id 0 -i CONSOLE -o CONSOLE -n`

The above call sends 1kB payload to Catapult over its default communication service (`CatapultCommmunicationService`)
without testing the output for loopback type equivalency. Both the input and the output are displayed on the console in
a hexdump style.

If you need to iterate the output you can do so by using 

`Hast.Communication.Tester.exe --device Catapult --bytes 1024 --member-id 0 -i CONSOLE -o CONSOLE -n`


## Switches

- `-l`, `--list`
List available devices and exit.
- `-d`, `--device`
Name of the selected device.
- `-b`, `--bytes`
The total size of the payload in bytes.
- `-k`, `--kilo-bytes`
The total size of the payload in kilobytes.
- `-c`, `--cells`
The total size of the payload in number of cells.
- `-m`, `--member-id`
The simulated MemberId.
- `-t`, `--payload-type`
What kind of data to send (`ConstantIntOne`, `Counter`, `Random`, `BinaryFile`).
- `-f`, `--file-type`
Type of the files where input and output are dumped to (`None`, `Hexdump`, `Binary`).
- `-i`, `--input`
Generated data is saved to or payload is read from this file when using BinaryFile as file-type.
- `-o`, `--output`
Output file name. (overrides `-f` to `Hexdump` if it's `None`; use value 'CONSOLE' to write hexdump to the console)
- `-j`, `--json`
Create a summary as JSON file.
- `-n`, `--no-check`
Skips result check at the end.
- `--help`
Display this help screen.
- `--version`
Display version information.


## Payload Types

The first 3 options consider each 4 byte cell in the `SimpleMemory` as an `Int32`.

- `ConstantIntOne`: Sets each cell's value to 1.
- `Counter`: The value counts up from 0 by 1, so it is the same as the cell index.
- `Random`: Each cell is set to a random `int`. Uniqueness is not guaranteed.
- `BinaryFile`: Loads the input from an external file byte-by-byte.

If it's set to BinaryFile then the input is read from `--input` instead of written to.

Random is non-repeatable so if that becomes an issue you can first export a generated input using the
`-t Random -f Binary -i random.bin` options and then use `-t BinaryFile -i random.bin` in consecutive runs.


## File Types

This switch controls both the input and the output file types.

- `None`: They aren't saved. If either input or output file paths are set then it's changed to `Hexdump`.
- `Hexdump`: The data is formatted as hexadecimal in a similar style as seen in a hexdump.
- `Binary`: The data is saved/loaded raw as a binary file. 

If instead of saving you want to display on the command line you can type the value CONSOLE in all capitals as
`-i CONSOLE` or `-o CONSOLE`.

Some additional diagnostics can be saved using the `--json filename.json`. This includes the `Success` property
which shows if the call finished successfully. If the value is true, it contains the `IHardwareExecutionInformation`
as the `Result` property. If false, it contains the serialized exception in the `Exception` property.