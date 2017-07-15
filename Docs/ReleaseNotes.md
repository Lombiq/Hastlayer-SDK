# Release notes



## v1.0.0.1, 13.07.2017

- Improving documentation.
- Improving the stability of the Remote Worker that does remote transformations.


## v1.0, 12.07.2017

- Custom property support.
- `ImmutableArray` support.
- Support for `break` statements in loops too.
- Support for operator overloading.
- Restructured projects to cut down on what consumers need to depend on.
- Support for remote transformation.
- Support for null reference expressions (i.e. objects can be checked against `null` and `null` can be assigned to reference-holding variables, fields, etc.)
- Improved error messages.
- Various smaller bugfixes and improvements.


## v0.9, 28.05.2017

- Values resolvable at compile-time are substituted as constants, optimizing the resulting hardware and allowing more flexible array usage too.
- `Array.Copy()` support.
- Vastly improving automated test coverage with verification tests.
- Support for `byte` and `sbyte` types.
- Conditional expression support.
- Support for custom constructors.
- Various smaller bugfixes and improvements.


## v0.8, 08.02.2017

- Precise latencies determined for operations, thus making hardware execution more reliable.
- Hardware project updated for Vivado 2016.4
- Instance method support.
- Various smaller bugfixes and improvements.


## v0.7, 29.08.2016

- Generate hardware descriptions are cached.
- Generated VHDL is formatted and contains debug comments.
- Custom object support with classes and structs, but only with auto-properties at this time.
- New Hardware project source control setup where unnecessary files are not kept in the repository.
- Operation-level parallelism.
- Various smaller bugfixes and improvements.


## v0.6, 25.04.2016

- Array support.
- Support for .NET parallel constructs with TPL.
- Support for recursion.
- Ethernet communication channel.
- Hardware execution time is measured.
- 64b primitive types support.
- Various smaller bugfixes and improvements.


## v0.5, 28.02.2016

- Fully functional implementation with support for basic language constructs, using a serial communication channel.
- Documented samples.
- Runs on [Orchard](http://orchardproject.net) with [Orchard Application Host](https://github.com/Lombiq/Orchard-Application-Host).

Before this: Various proof of concept and experimental versions.