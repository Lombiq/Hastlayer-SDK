# Working with Hastlayer



## Creating a Hastlayer-using application

The Hastlayer developer story is not ideal yet - we're working on improving it by [making the SDK available from NuGet](https://github.com/Lombiq/Hastlayer-SDK/issues/35). For now the below one is the easiest approach to add Hastlayer to your application:

1. Clone the Hastlayer repository into a subfolder of your application.
2. Copy the Hastlayer solution file corresponding to your Hastlayer flavor and use that to add your own projects to (you'll need to change project paths there to point to the Hastlayer subdirectory; [this example](Attachments/Hastlayer.SDK.Client.sln) shows how a Client solution file looks if Hastlayer is cloned to a folder named "Hastlayer", but this is just a static sample, do copy the latest one!). This way you'll have all the necessary projects added. Alternatively you can also add the Hastlayer projects to your existing solution, just make sure to add all of them.
3. In the project where you want to use Hastlayer add the necessary initialization code (as shown in the samples) and the necessary project references (Visual Studio will suggest adding the right projects most of the time, otherwise also take a look at the samples).

When Hastlayer is updated you can just pull in changes from the official Hastlayer repository, but you'll need to keep your solution file up to date by hand.

We suggest starting with the included samples then taking your first Hastlayer steps by writing some small algorithm, then gradually stepping up to more complex applications. You can check out all the samples in the *Samples* solution folder.

Since it's possible that due to bugs with some corner cases the hardware code will produce incorrect results it's good to configure Hastlayer to verify the hardware output while testing (and do tell Lombiq if you've found issues): You can do this by setting `ProxyGenerationConfiguration.VerifyHardwareResults` to `true` when generating proxy objects.


## Writing Hastlayer-compatible .NET code

While Hastlayer supports a lot of features of .NET, it can't support everything (also due to the fundamental differences between executing a program on a CPU and creating hardware logic). Thus limitations apply.

Take a look at the sample projects in the *Samples* solution folder. Those are there to give you a general idea how Hastlayer-compatible code looks like, and they're thoroughly documented. If some language construct is not present in the samples then it is probably not supported.

The `PrimeCalculator` class in the `Hast.Samples.SampleAssembly` project is a good starting point with a basic sample algorithm, `ParallelAlgorithm` is a good example of a highly parallelized algorithm, and the `Hast.Samples.Consumer` project demonstrates how to add Hastlayer to your app (`Hast.Samples.Demo` does the same in a stripped-down manner). You can also run the `Loopback` sample to test FPGA connectivity and Hastlayer Hardware Framework resource usage.

Some general constraints you have to keep in mind:

- Only public virtual methods, or methods that implement a method defined in an interface will be accessible from the outside, i.e. can be hardware entry points. Elsewhere any other kind of methods can be used, including extension methods.
- Always use the smallest data type necessary, e.g. `short` instead of `int` if 16b is enough (or even `byte`), and unsigned types like `uint` if you don't need negative numbers.
- Supported primitive types: `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `char`, `bool`.  Floating-point numbers like `float` and `double` and numbers bigger than 64b are not yet supported, however you can use fixed-point math: multiply up your floats before handing them over to Hastlayer-executed code, then divide them back when receiving the results. If this is not enough you can use the `Fix64` 64b fixed-point number type included in the `Hast.Algorithms` library, see the `Fix64Calculator` sample.
- The most important language constructs like `if` and `else` statements, `while` and `for` loops, type casting, binary operations (e.g. arithmetic, in/equality operators...), conditional expressions (ternary operator) on allowed types are supported. Also, `ref` and `out` parameters in method invocations are supported.
- Algorithms can use a fixed-size (determined at runtime) memory space modeled as an array of 32b values ("cells") in the class `SimpleMemory`. For inputs that should be passed to hardware implementations and outputs that should be sent back this memory space is to be used. For internal method arguments (i.e. for data that isn't coming from the host computer or should be sent back) normal method arguments can be used but you can utilize `SimpleMemory` for any other dynamic memory allocation internally too. Note that there shouldn't be concurrent access to a `SimpleMemory` instance, it's **not** thread-safe (neither in software nor on hardware)!
- Single-dimensional arrays having their size possible to determine compile-time are supported. So apart from instantiating arrays with their sizes specified as constants you can also use variables, fields, properties for array sizes, as well as expressions (and a combination of these), just in the end the size of the array needs to be resolvable at compile-time. If Hastlayer can't figure out the array size for some reason you can configure it manually, see the `UnumCalculator` sample.
- To a limited degree `Array.Copy()` is also supported: only the `Copy(Array sourceArray, Array destinationArray, int length)` override and only with a `length` that can be determined at compile-time. Furthermore, `ImmutableArray` is also supported to a limited degree by converting objects of that type to standard arrays in the background (see the `Lombiq.Arithmetics` project for examples).
- Using objects created of custom classes and structs are supported.
  -  Using these objects as usual (e.g. passing them as method arguments, storing them in arrays) is also supported. However be aware that inheritance is not supported (since polymorphism wouldn't work: on hardware class members need to be "wired in", thus we need to know at compile time what kind of object a variable will hold), nor self-referencing types (i.e. `MyClass` can't have a property of type `MyClass`, so you can't create a linked list implementation for example).
  - Note that hardware entry point types are a slight exception as they can only contain methods, no fields, properties or constructors.
  - Static members apart from methods are not supported (so e.g. while you can't use static fields, you can have static methods).
  - Be careful not to mix reference types (like arrays) into structs' members (fields and properties), keep structs purely value types (this is a good practice any way).
- Operator overloading on custom types is supported.
- Task-based parallelism with TPL is supported to a limited degree, lambda expression are supported as well to an extent needed to use tasks. See the `ParallelAlgorithm` and the `ImageContrastModifier` sample on how parallel code can look like.
- Operation-level, SIMD-like parallelism is supported, see the `SimdCalculator` sample.
- Recursion is supported but recursive code is not really efficient with Hastlayer. Nevertheless if a method call is recursive, even if indirectly, you need to manually configure the recursion depths (see the `RecursiveAlgorithms` sample).
- Method inlining with the `[MethodImpl(MethodImplOptions.AggressiveInlining)]` attribute as usual in .NET or the `TransformerConfiguration().AddAdditionalInlinableMethod<T>()` configuration is supported. It's recommended to inline small but frequently called methods to cut down on the overhead of method invocation. Keep in mind though that inlining has its drawbacks, pretty much [the same as in .NET](https://softwareengineering.stackexchange.com/questions/245802/is-there-a-downside-to-using-aggressiveinlining-on-simple-properties): the size of the hardware design will be bigger (and hardware generation will be slower). This, however can be acceptable for a potentially much greater performance. Be sure not to overdo inlining, because just inlining everything (especially if inlined methods also call inlined methods...) can quickly create giant hardware implementations that won't be just most possibly too big for the FPGA but also very slow to transform. See the `PrimeCalculator` sample on using method inlining.
- Exceptions are not supported and `throw` statements will be handled as a no-op (they won't do anything). The latter is so you can keep throwing exceptions on invalid arguments from methods and utilize them when the code is executed in the standard way; however you need to take special care on not to actually have invalid arguments on hardware.
- Note that you can write unsupported code in a member of a type that will be transformed if that member won't be accessed on the hardware (since unused code is removed from transformation). So e.g. you can implement `ToString()`.
- Check out the `Hast.Algorithms` library for some Hastlayer-compatible useful algorithms.


## Running your code on hardware

Once you've written some Hastlayer-compatible algorithm you can then generate hardware from it. Be sure to use assemblies built in the Debug configuration. Once you've successfully generated hardware from your algorithm then you'll be able to execute it on an FPGA as hardware. At the moment this needs to be configured manually, so check out the docs of the Hastlayer Hardware repo on how to get started with that.


## Performance-optimizing your code

Some simplified basics on the properties of FPGAs first:

- FPGAs are low-power devices running on small clock frequencies (few 100Mhz at most), so we need to be cautious with clock cycles.
- On an FPGA you can do a lot of simpler operations (like variable assignments, arithmetic on smaller numbers) in a single clock cycle even without parallelization.
- However it's only useful to look at FPGAs for performance enhancements if your code is compute-bound and can be massively parallelized. Hastlayer supports three types of parallelism:
    - `Task`-level: this is the most important you need to utilize (elaborated above).
    - Operation-level: can be useful for certain algorithms where it makes sense to run e.g. hundreds of multiplications in parallel (also elaborated above).
    - Device-level: this means that you can use multiple FPGAs which all host the same algorithm and Hastlayer will automatically select the one idle to push work to. This way you can execute the same hardware algorithm in parallel on multiple devices.

As a simple rule of thumb if your code has a loop that works on elements of an array, does a lot of work in the loop body and the order in which the elements are processed doesn't matter (i.e. one loop execution doesn't depend on a previous one) then it's a good candidate for `Task`-based parallelization.

So to write fast code with Hastlayer you need implement massively parallel algorithms and avoid code that adds unnecessary clock cycles. What are the clock cycle sinks to avoid?

- Method invocation and access to custom properties (i.e. properties that have a custom getter or setter, so not auto-properties) cost multiple clock cycles as a baseline. Try to avoid having many small methods and custom properties (or methods you can also inline, see the "Writing Hastlayer-compatible .NET code" section).
- Arithmetic operations take longer with larger number types so always use the smallest data type necessary (e.g. use `int` instead of `long` if its range is enough). This only applies to data types larger than 32b since smaller number types will be cast to `int` any way. However smaller data types lower the resource usage on the FPGA, so it's still beneficial to use them. Do note that if you use differently typed operands for binary operations one of them will automatically be cast, potentially negating any advantages of using smaller data types (for details see [numeric promotions](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/expressions#numeric-promotions)).
- Use constants where applicable so the constant values can be substituted instead of keeping read-only variables.
- Memory access with `SimpleMemory` is relatively slow, so keep memory access to the minimum (use local variables and objects as temporary storage instead).
- Loops with a large number of iterations but with some very basic computation inside them: this is because every iteration is at least one clock cycle, so again multiple operations can't be packed into a single clock cycle. Until Hastlayer does [loop unrolling](https://github.com/Lombiq/Hastlayer-SDK/issues/14) manual unrolling [can help](https://stackoverflow.com/questions/2349211/when-if-ever-is-loop-unrolling-still-useful).

In the ideal case your algorithm will do the following (can happen repeatedly of course):

1. Produces all the data necessary for parallel execution.
2. Feeds this data to multiple parallel `Task`s as their inputs and starts these `Task`s.
3. Waits for the `Task`s to finish and takes their results.

The `ParallelAlgorithm` sample does exactly this.

Note that FPGAs have a finite amount of resources that you can utilize, and the more complex your algorithm, the more resources it will take. With simpler algorithms you can achieve a higher degree of parallelism on a given FPGA, since more copies of it will fit. So you can either have more complex pieces of logic parallelized to a lower degree, or simpler logic parallelized to a higher degree.

Very broadly speaking if you performance-optimize your .NET code and it executes faster as software then most possibly it will also execute faster as hardware. But do measure if your optimizations have the desired effect.


## Troubleshooting

### Errors and warnings
If any error happens during runtime Hastlayer will throw an exception (mostly but not exclusively a `HastlayerException`) and the error will be also logged. Log files are located in the `App_Data\Logs` folder under your app's execution folder.

If during transformation there's a warning (i.e. some issue that doesn't necessarily make the result wrong but you should know about it) then that will be added to the result of the `IHastlayer.GenerateHardware()` call (inside `HardwareDescription`) as well as to the logs and to Visual's Studio's Debug window when run in Debug mode.

### Incorrect hardware results
You can configure Hastlayer to check whether the hardware execution's results are correct by setting `ProxyGenerationConfiguration.VerifyHardwareResults` to `true` when generating proxy objects. This will also run everything as software, compare the software output with the hardware output and throw exceptions if they're off.

If the result of the hardware execution is wrong then you can use `SimpleMemory` to write out intermediate values and check where the execution goes wrongs, something like this:

    var i = 0;
    // Do some stuff.
    memory.WriteInt32(i++, value1);
    // Do some stuff.
    memory.WriteInt32(i++, value2);

Think of these as breakpoints where you read out variable values with the debugger. The point is to get to shave down the code to a state where it's still incorrect, and removing a single thing will make it correct. The difference will show what's faulty and then that can be debugged further.

Even if the algorithm doesn't properly terminate you can use this technique, but you'll need to inspect the content of the memory on the FPGA; for the Nexys A7 you can do this in the Xilinx SDK's Memory window (everything written with `SimpleMemory` starts at the address `0x48fffff0`).

### Checking the decompiled source
When you're working with the Developer flavor of Hastlayer it can also help to see what the decompiled C# source code looks like. You can save that to files, see `Hast.Transformer.DefaultTransformer` and look for `SaveSyntaxTree` (this is enabled in Debug mode by default).

### Dumping (and loading) SimpleMemory content
You can store the contents of a `SimpleMemory` instance in a binary format, also as a file. Similarly you can load them into a `SimpleMemory` too.

You can read such files e.g. with Notepad++'s [HEX-Editor plugin](https://community.notepad-plus-plus.org/topic/17459/why-there-is-no-new-hexeditor-now/2). After the plugin's installation click the H icon to display the file contents in a hex format.


## Extensibility

Hastlayer, apart from the standard Orchard-style extensibility (e.g. the ability to override implementations of services through the DI container) provides three kind of extension points:Hastlayer offers similar extensibility found in standard Orchard 1.x. (Although with the caveat that service implementations don't overrider each other so the user needs to manage it in the HastlayerConfiguration.) Additionally, it provides these extension points:Hastlayer, apart from the standard Orchard-style extensibility (e.g. the ability to override implementations of services through the DI container) provides three kind of extension points:

- .NET-style events: standard .NET events.
- Pipeline steps: unlike event handlers, pipeline steps are executed in deterministic order and usually have a return value that is fed to the next pipeline step.


## Using dynamic constants

If you need to iterate through versions of your code with different FPGA constants, it can be done without recompiling your software. Since .Net automatically substitutes constants with their literal value, your fields have to be `static readonly` instead of `constant` to preserve the variable usage in the compiled code. Annotate this field with the `[Replaceable(key)]` attribute. It takes a parameter representing the key you can add to appdata.json or as a command line switch. For example:

```csharp
[Replaceable(nameof(ParallelAlgorithm) + "." + nameof(MaxDegreeOfParallelism))] // key = "ParallelAlgorithm.MaxDegreeOfParallelism"
public static readonly int MaxDegreeOfParallelism = 260;
```

The value in code will remain the default, when no replacement is specified. Either way the readonly is substituted with the desired literal as a constant would during compilation. To set the replacement from the command line, add the following switch.

```shell script
--HardwareGenerationConfiguration:CustomConfiguration:Replaceable:ParallelAlgorithm.MaxDegreeOfParallelism 123
``` 

The part up to the last colon is fixed, then comes the key you've passed to the `[Replaceable]` attribute and finally the replacement value as a separate word. The value may be a string, boolean or integer. You can automate trials by looping through candidate values in the shell. You can have multiple replacements too.

Alternatively, you can set the same value in the appesttings.json file into the object with the same path like this:

```json
{
  "HardwareGenerationConfiguration": {
    "Replaceable": {
      "ParallelAlgorithm.MaxDegreeOfParallelism": 123
    }
  }
}
```
