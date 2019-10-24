namespace Hast.Samples.FSharpSampleAssembly

module FSharpParallelAlgorithmContainer =
    open Hast.Transformer.Abstractions.SimpleMemory
    open System.Threading.Tasks
    open System

    // A simple parallelized sample showcasing what you can also see in ParallelAlgorithm, but in F#.
    type public FSharpParallelAlgorithm() =
        // Since literals can't be exposed as public consts we can't put a class with extension methods into the 
        // Consumer app. Maybe eventually: https://github.com/fsharp/fslang-suggestions/issues/746.
        [<Literal>]
        let MaxDegreeOfParallelism = 2

        [<Literal>]
        let Run_InputUInt32Index = 0

        [<Literal>]
        let Run_OutputUInt32Index = 0


        abstract member Run: SimpleMemory -> unit
        default this.Run memory = 
            let input = memory.ReadInt32(Run_InputUInt32Index)
            // You need to use Array.zeroCreate when creating an empty array for Hastlayer to understand it.
            let tasks : Task<int> array = Array.zeroCreate MaxDegreeOfParallelism

            // Since the for loop uses an inclusive upper bound we use a while loop here for clarity with the C# 
            // ParallelAlgorithm sample
            let mutable i = 0
            while i < MaxDegreeOfParallelism do
                tasks.[i] <- Task.Factory.StartNew((fun (indexObject : Object) -> 
                    let index : int = indexObject :?> int
                    let mutable result = input + index * 2

                    let mutable even = true
                    for j = 2 to 9999998 do
                        if even then result <- result + index
                        else result <- result - index
                        even <- not(even)

                    result), i)
                i <- i + 1

            Task.WhenAll(tasks).Wait()

            let mutable output = 0
            i <- 0
            while i < MaxDegreeOfParallelism do
                output <- output + tasks.[i].Result
                i <- i + 1

            memory.WriteInt32(Run_InputUInt32Index, output)

        // Instead of extension methods like in C# we use a standard method to do the SimpleMemory initialization.
        member this.Run input : int =
            let memory = new SimpleMemory(1)
            memory.WriteInt32(Run_InputUInt32Index, input)
            this.Run memory
            memory.ReadInt32(Run_OutputUInt32Index)