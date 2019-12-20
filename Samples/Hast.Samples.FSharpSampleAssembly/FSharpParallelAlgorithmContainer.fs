namespace Hast.Samples.FSharpSampleAssembly

module FSharpParallelAlgorithmContainer =
    open Hast.Transformer.Abstractions.SimpleMemory
    open System.Threading.Tasks

    // A simple parallelized sample showcasing what you can also see in ParallelAlgorithm, but in F#.
    // Due to how Hastlayer processes the .NET assemblies unfortunately this has to be very similar to the C# sample,
    // so it's more like a curiosity than something useful.

    // The hardware entry point needs to be a class here too.
    type public FSharpParallelAlgorithm() =
        [<Literal>]
        let MaxDegreeOfParallelism = 280

        [<Literal>]
        let Run_InputUInt32Index = 0

        [<Literal>]
        let Run_OutputUInt32Index = 0


        // Since there's no virtual in F# the hardware entry point methods need to be abstracts with the default
        // implementation following them.
        abstract member Run: SimpleMemory -> unit
        default __.Run memory = 
            let input = memory.ReadInt32(Run_InputUInt32Index)
            // You need to use Array.zeroCreate when creating an empty array for Hastlayer to understand it.
            let tasks = Array.zeroCreate MaxDegreeOfParallelism

            // Since the for loop uses an inclusive upper bound we use a while loop here for clarity with the C# 
            // ParallelAlgorithm sample.
            let mutable i = 0
            while i < MaxDegreeOfParallelism do
                tasks.[i] <- Task.Factory.StartNew((fun (indexObject : obj) -> 
                    let index = indexObject :?> int
                    let mutable result = input + index * 2

                    let mutable even = true
                    for j = 2 to 9999998 do
                        if even then result <- result + index
                        else result <- result - index
                        even <- not even

                    result), i)
                i <- i + 1

            Task.WhenAll(tasks).Wait()

            let mutable output = 0
            i <- 0
            while i < MaxDegreeOfParallelism do
                output <- output + tasks.[i].Result
                i <- i + 1

            memory.WriteInt32(Run_InputUInt32Index, output)

        member this.Run input =
            let memory = new SimpleMemory(1)
            memory.WriteInt32(Run_InputUInt32Index, input)
            this.Run memory
            memory.ReadInt32(Run_OutputUInt32Index)