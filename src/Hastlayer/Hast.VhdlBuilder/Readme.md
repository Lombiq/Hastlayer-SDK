# Hastlayer - VHDL Builder

[![Hast.VhdlBuilder NuGet](https://img.shields.io/nuget/v/Hast.VhdlBuilder?label=Hast.VhdlBuilder)](https://www.nuget.org/packages/Hast.VhdlBuilder/)

A .NET VHDL code builder, i.e. you can build syntactically valid VHDL code with .NET constructs.

In the `ToVhdl()` methods spaces are concatenated always at the end of lines, even if this needs a separate concatenation of a single-character string, because this allows correct code generation with and without formatting as well.
