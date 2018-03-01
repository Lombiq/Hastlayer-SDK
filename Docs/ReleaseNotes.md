# Release notes



Note that the hardware framework projects have their own release cycle and release notes.


## vNext

- New Loopback sample to test FPGA connectivity and Hastlayer Hardware Framework resource usage.

Note that running Hastlayer now requires Visual Studio 2017 or greater (any edition will work).


## 1.0.8, 13.01.2017

- Adding ability to inline methods to vastly improve performance if the method is small but called a lot of times.
- Adding support for `ref` and `out` parameters, see the [issue](https://github.com/Lombiq/Hastlayer-SDK/issues/15).
- `Fix64` fixed-point number type added for computations with fractions.
- Simplified configuration of parallelized code: no need to manually specify the degree of parallelism any more in most cases (see `ParallelAlgorithmSampleRunner` for example: `Configure()` is just one line now).
- Various smaller bugfixes and improvements.

For all publicly tracked issues resolved with this release [see the corresponding milestone](https://github.com/Lombiq/Hastlayer-SDK/milestone/1?closed=1).


## 1.0.7, 25.11.2017

- Adding sample with an implementation of the 3D [Kardar-Parisi-Zhang](https://en.wikipedia.org/wiki/Kardar%E2%80%93Parisi%E2%80%93Zhang_equation) surface growth simulation algorithm. Made in collaboration with the [Wigner Research Centre for Physics](http://wigner.mta.hu/en/) to test against an existing GPU implementation (testing is upcoming).
- Fixing behavior of binary operator expressions were the operands were cast.
- Fixing exception when using public fields in certain cases.
- More helpful exception when a type was not found.
- The [Hastlayer Hardware Framework - Xilinx project](https://github.com/Lombiq/Hastlayer-Hardware-Framework---Xilinx) got some scripts to automate updating the Vivado project with a new generated hardware design and to program the FPGA and launch the Xilinx SDK. From now on the project has a release cycle independent of the SDK.


## 1.0.6, 21.10.2017

- Transformation warnings are logged (and sent to the Debug console) so they can be detected easier.
- The decompiled .NET code is added to the VHDL output for debugging purposes in `Debug` mode, see the [issue](https://github.com/Lombiq/Hastlayer-SDK/issues/19).
- Fixing that if methods of an object instance called each other the call was wrongly transformed.
- Fixing that for void methods (and constructors) the transformed invocation wasn't properly awaited.
- Fixing that the hardware result was wrong if binary operations were done with <32b types.
- Fixing a constant substitution edge-case for nested if and while statements.
- Improved documentation.


## 1.0.5, 20.09.2017

- Fixing `HardwareGenerationConfiguration.AddHardwareEntryPointType()`, it now only adds the actual type as an entry point, not other types with similar names too.
- Improved documentation.
- Various smaller bugfixes and improvements.


## 1.0.4, 08.09.2017

- The SDK and Hardware Framework for Xilinx FPGAs is open source.
- Fixing transformed shift operator behavior.
- Improved documentation.
- Various smaller bugfixes and improvements.


## 1.0.3, 02.08.2017

- Improved caching and thus smaller response time for repeated transformations.
- Cleaning up and parallelizing the ImageFilter sample.
- Fixing casting/type conversions so now all casts result in hardware code that produces binary compatible results with .NET.
- Moving VHDL libraries to the .NET Transformer, thus allowing easier library updates.
- Improved documentation.
- Various smaller bugfixes and improvements.

**Note** that since Hastlayer Hardware changed as well you'll need to either regenerate the Vivado and SDK projects (after purging or re-cloning the Hardware repo) or manually upgrade Hast_IP if you want to run the sample hardware (in Vivado go to Tools/Report/Report IP Status, then after Hast_IP is listed in the IP Status window as changed you'll need to follow the steps under the Hardware docs' "Running hardware designs" page). Otherwise you'll be able to run your own custom generated hardware as usual, but you'll need to re-generate them after pulling in updates from both repos.


## v1.0.2, 19.07.2017

- More reliable serial port selection.
- Remote Worker is sending telemetry to Azure Application Insights, allowing better production diagnostics.
- Improved documentation.
- Various smaller bugfixes and improvements.


## v1.0.1, 17.07.2017

- Improved support for unary increment/decrement operators (e.g. `number++`). Handling more cases now.
- Support for multi-assignments (e.g. `number1 = number2 = 4`).
- Improved exception messages.
- Improved Remote Worker stability and exception messages.
- Improved caching allowing full caching of generated hardware descriptions and thus faster repeated transformations.
- Improved documentation.
- Various smaller bugfixes and improvements.


## v1.0.0.1, 13.07.2017

- Improved documentation.
- Improved the stability of the Remote Worker that does remote transformations.


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