# Working with Hastlayer



## Writing Hastlayer-compatible .NET code

Take a look at the sample projects in the Sample solution folder. Those are there to give you a general idea how Hastlayer-compatible code looks like, and they're thoroughly documented. If some language construct is not present in the samples then it is probably not supported. The `PrimeCalculator` is a good starting point with a basic sample algorithm.

Some general constraints you have to keep in mind:

- Only public virtual methods, or methods that implement a method defined in an interface will be accessible from the outside, i.e. can be hardware entry points.
- Always use the smallest data type necessary, e.g. `short` instead of `int` if 16b is enough (or even `byte`), and unsigned types like `uint` if you don't need negative numbers.
- Supported primitive types: `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `char`, `bool`.  Floating-point numbers like `float` and `double` and numbers bigger than 64b are not yet supported.
- The most important language constructs like `if` and `else` statements, `while` and `for` loops, type casting, binary operations (e.g. arithmetic, in/equality operators...), conditional expressions (ternary operator) on allowed types are supported.
- Algorithms can use a fixed-size (determined at runtime) memory space modeled as a `byte` array in the class `SimpleMemory`. For inputs that should be passed to hardware implementations and outputs that should be sent back this memory space is to be used. For internal method arguments (i.e. for data that isn't coming from the host computer or should be sent back) normal method arguments can be used. Note that there shouldn't be concurrent access to a `SimpleMemory` instance, it's **not** thread-safe (neither in software nor on hardware)!
- Single-dimensional arrays having their size possible to determine compile-time are supported. So apart from instantiating arrays with their sizes specified as constants you can also use variables, fields, properties for array sizes, as well as expressions (and a combination of these), just in the end the size of the array needs to be resolvable at compile-time. If Hastlayer can't figure out the array size for some reason you can configure it manually, see the `UnumCalculator` sample.
- To a limited degree `Array.Copy()` is also supported: only the `Copy(Array sourceArray, Array destinationArray, int length)` override and only with a constant `length`. Furthermore, `ImmutableArray` is also supported to a limited degree by converting objects of that type to standard arrays in the background (see the `Lombiq.Unum` project for examples).
- Using objects created of custom classes and structs are supported. Using these objects as usual (e.g. passing them as method arguments, storing them in arrays) is also supported. However hardware entry point types can only contain methods. Static members apart from methods are not supported (so e.g. while you can't use static fields, you can have static methods). Also, be careful not to mix reference types (like arrays) into structs' members (fields and properties), keep structs purely value types (this is a good practice any way).
- Task-based parallelism is with TPL is supported to a limited degree. Lambda expression are supported to an extent needed to use tasks (see the `ParallelAlgorithm` sample in the `Hast.Samples.Consumer` project).
- Operation-level, SIMD-like parallelism is supported, see the `SimdCalculator` sample.
- Recursion is supported but recursive code is not really something for Hastlayer. Nevertheless if a method call is recursive, even if indirectly, you need to manually configure the recursion depths (see the `RecursiveAlgorithms` sample).
- Exceptions are not supported and `throw` statements will be handled as a no-op (they won't do anything). The latter is so you can keep throwing exceptions on invalid arguments from methods and utilize them when the code is executed in the standard way; however you need to take special care on not to actually have invalid arguments on hardware.
- Note that you can write unsupported code in a member of a type that will be transformed if that member won't be accessed on the hardware (since unused code is removed from transformation). So e.g. you can implement `ToString()`.


## Performance-optimizing your code

Some simplified basics first on the properties of FPGAs first:

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


## Extensibility

Hastlayer, apart from the standard Orchard-style extensibility (e.g. the ability to override implementations of services through the DI container) provides three kind of extension points:

- .NET-style events: standard .NET events.
- Orchard-style events: event handlers that can be hooked into by implementing the event handler interface.
- Pipeline steps: unlike event handlers, pipeline steps are executed in deterministic order and usually have a return value that is fed to the next pipeline step.