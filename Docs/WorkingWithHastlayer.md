# Working with Hastlayer



## Creating a Hastlayer-using application

The Hastlayer developer story is not ideal yet - we're working on improving it. For now the below one is the easiest approach to add Hastlayer to your application:

1. Clone the Hastlayer repository into a subfolder of your application (if you aren't using Mercurial for you app's source control then you can also add the whole directory to your own repository; however be careful to exclude compiled binaries like how the *.hgignore* file does in the Hastlayer repo).
2. Copy the Hastlayer solution file corresponding to your Hastlayer flavor and use that to add your own projects to (you'll need to change project paths there to point to the Hastlayer subdirectory; [this example](Attachments/Hastlayer.Client.sln) shows how a Client solution file looks if Hastlayer is cloned to a folder named "Hastlayer", but this is just a static sample, do copy the latest one!). This way you'll have all the necessary projects added. Alternatively you can also add the Hastlayer projects to your existing solution, just make sure to add all of them.
3. Instruct NuGet to use the *Orchard\src\packages* folder under the Hastlayer folder. You can do this by adding a *NuGet.config* file to the same folder where your solution file is ([this example](Attachments/NuGet.config) again uses the *Hastlayer* subfolder).
4. In the project where you want to use Hastlayer add the necessary initialization code (as shown in the samples) and the necessary project references (Visual Studio will suggest adding the right projects most of the time, otherwise also take a look at the samples).

When Hastlayer is updated you can just pull in changes from the official Hastlayer repository, but you'll need to keep your solution file up to date by hand.

We suggest starting with the included samples then taking your first Hastlayer steps by writing some small algorithm, then gradually stepping up to more complex applications.

Since it's possible that due to bugs with some corner cases the hardware code will produce incorrect results it's good to configure Hastlayer to verify the hardware output while testing (and do tell Lombiq if you've found issues): You can do this by setting `ProxyGenerationConfiguration.VerifyHardwareResults` when generating proxy objects.


## Writing Hastlayer-compatible .NET code

Take a look at the sample projects in the Sample solution folder. Those are there to give you a general idea how Hastlayer-compatible code looks like, and they're thoroughly documented. If some language construct is not present in the samples then it is probably not supported. The `PrimeCalculator` is a good starting point with a basic sample algorithm.

Some general constraints you have to keep in mind:

- Only public virtual methods, or methods that implement a method defined in an interface will be accessible from the outside, i.e. can be hardware entry points. Elsewhere any other kind of methods can be used.
- Always use the smallest data type necessary, e.g. `short` instead of `int` if 16b is enough (or even `byte`), and unsigned types like `uint` if you don't need negative numbers.
- Supported primitive types: `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `char`, `bool`.  Floating-point numbers like `float` and `double` and numbers bigger than 64b are not yet supported.
- The most important language constructs like `if` and `else` statements, `while` and `for` loops, type casting, binary operations (e.g. arithmetic, in/equality operators...), conditional expressions (ternary operator) on allowed types are supported.
- Algorithms can use a fixed-size (determined at runtime) memory space modeled as a `byte` array in the class `SimpleMemory`. For inputs that should be passed to hardware implementations and outputs that should be sent back this memory space is to be used. For internal method arguments (i.e. for data that isn't coming from the host computer or should be sent back) normal method arguments can be used. Note that there shouldn't be concurrent access to a `SimpleMemory` instance, it's **not** thread-safe (neither in software nor on hardware)!
- Single-dimensional arrays having their size possible to determine compile-time are supported. So apart from instantiating arrays with their sizes specified as constants you can also use variables, fields, properties for array sizes, as well as expressions (and a combination of these), just in the end the size of the array needs to be resolvable at compile-time. If Hastlayer can't figure out the array size for some reason you can configure it manually, see the `UnumCalculator` sample.
- To a limited degree `Array.Copy()` is also supported: only the `Copy(Array sourceArray, Array destinationArray, int length)` override and only with a constant `length`. Furthermore, `ImmutableArray` is also supported to a limited degree by converting objects of that type to standard arrays in the background (see the `Lombiq.Unum` project for examples).
- Using objects created of custom classes and structs are supported.
  -  Using these objects as usual (e.g. passing them as method arguments, storing them in arrays) is also supported. However be aware that inheritance is not supported (since polymorphism wouldn't work: on hardware class members need to be "wired in", thus we need to know at compile time what kind of object a variable will hold).
  - Note that hardware entry point types are a slight exception as they can only contain methods.
  - Static members apart from methods are not supported (so e.g. while you can't use static fields, you can have static methods).
  - Be careful not to mix reference types (like arrays) into structs' members (fields and properties), keep structs purely value types (this is a good practice any way).
- Operator overloading on custom types is supported.
- Task-based parallelism is with TPL is supported to a limited degree, lambda expression are supported as well to an extent needed to use tasks. See the `ParallelAlgorithm` sample in the `Hast.Samples.Consumer` project and the `ImageContrastModifier` sample on how parallel code can look like.
- Operation-level, SIMD-like parallelism is supported, see the `SimdCalculator` sample.
- Recursion is supported but recursive code is not really something for Hastlayer. Nevertheless if a method call is recursive, even if indirectly, you need to manually configure the recursion depths (see the `RecursiveAlgorithms` sample).
- Exceptions are not supported and `throw` statements will be handled as a no-op (they won't do anything). The latter is so you can keep throwing exceptions on invalid arguments from methods and utilize them when the code is executed in the standard way; however you need to take special care on not to actually have invalid arguments on hardware.
- Note that you can write unsupported code in a member of a type that will be transformed if that member won't be accessed on the hardware (since unused code is removed from transformation). So e.g. you can implement `ToString()`.


## Running your code on hardware

Once you've written some Hastlayer-compatible algorithm and successfully generated hardware from it you'll be able to execute it on an FPGA as hardware. At the moment this needs to be configured manually, so check out the docs of the Hastlayer Hardware repo on how to get started with that.


## Performance-optimizing your code

Some simplified basics on the properties of FPGAs first:

- FPGAs are low-power devices running on small clock frequencies (few 100Mhz at most), so we need to be cautious with clock cycles.
- On an FPGA you can do a lot of simpler operations (like variable assignments, arithmetic on smaller numbers) in a single clock cycle even without parallelization.
- However it's only useful to look at FPGAs for performance enhancements if your code can be massively parallelized on a `Task` level.

So to write fast code with Hastlayer you need implement massively parallel algorithms and avoid code that adds unnecessary clock cycles. What are the clock cycle sinks to avoid?

- Method invocation and access to custom properties (i.e. properties that have a custom getter or setter, so not auto-properties) cost multiple clock cycles as a baseline. Try to avoid having many small methods (Hastlayer will eventually inline small methods to cut down on such waste automatically) and custom properties.
- Arithmetic operations take longer with larger number types so always use the smallest data type necessary (e.g. use `short` instead of `int` if its range is enough).
- Memory access with `SimpleMemory` is relatively slow, so keep memory access to the minimum (use local variables and objects as temporary storage instead).

In the ideal case your algorithm will do the following (can happen repeatedly of course):

1. Produces all the data necessary for parallel execution.
2. Feeds this data to multiple parallel `Task`s as their inputs and starts these `Task`s.
3. Waits for the `Task`s to finish and takes their results.

The `ParallelAlgorithm` sample does exactly this.


## Troubleshooting

If any error happens during runtime Hastlayer will throw an exception (mostly but not exclusively a `HastlayerException`) and the error will be also logged. Log files are located in the `App_Data\Logs` folder under your app's execution folder.

If during transformation there's a warning (i.e. some issue that doesn't necessarily make the result wrong but you should know about it) then that will be added to the result of the *IHastlayer.GenerateHardware()* call (inside `HardwareDescription`).

When you're working with the Developer flavor of Hastlayer it can also help to see what the decompiled C# source code looks like. You can save that to files, see `Hast.Transformer.DefaultTransformer` and look for `saveSyntaxTree`.


## Extensibility

Hastlayer, apart from the standard Orchard-style extensibility (e.g. the ability to override implementations of services through the DI container) provides three kind of extension points:

- .NET-style events: standard .NET events.
- Orchard-style events: event handlers that can be hooked into by implementing the event handler interface.
- Pipeline steps: unlike event handlers, pipeline steps are executed in deterministic order and usually have a return value that is fed to the next pipeline step.