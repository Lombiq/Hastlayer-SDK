using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Samples.SampleAssembly
{
    /// <summary>
    /// Object-oriented code can be written with Hastlayer as usual. This will also be directly mapped to hardware.
    /// Also see <see cref="ObjectOrientedShowcaseSampleRunner"/> on what to configure to make this work.
    /// </summary>
    public class ObjectOrientedShowcase
    {
        public const int RunInputUInt32Index = 0;
        private const int RunOutputUInt32Index = 0;

        public virtual void Run(SimpleMemory memory)
        {
            var inputNumber = memory.ReadUInt32(RunInputUInt32Index);
            // Or:
            inputNumber = new MemoryContainer(memory).GetInput();

            // Arrays can be initialized as usual, as well as objects.
            var numberContainers1 = new[]
            {
                new NumberContainer { Number = inputNumber },
                new NumberContainer { Number = inputNumber + 4 },
                new NumberContainer { Number = 24 },
                new NumberContainer(9)
            };

            // Array elements can be accessed and modified as usual.
            numberContainers1[0].NumberPlusFive = inputNumber + 10;
            numberContainers1[1].IncreaseNumber(5);
            numberContainers1[2].IncreaseNumberBy10();
            numberContainers1[2].IncreaseNumberBy20();

            // Using ref and out.
            uint increaseBy = 10;
            numberContainers1[3].IncreaseNumberByParameterTimes10(ref increaseBy, out uint originalNumber);
            numberContainers1[3].IncreaseNumber(increaseBy + originalNumber);

            // Note that array dimensions need to be defined compile-time. They needn't bee constants directly used
            // when instantiating the array but the size argument needs to be resolvable compile-time (so if it's a
            // variable then its value should be computable from all other values at compile-time).
            var numberContainers2 = new NumberContainer[1];
            var numberContainer = new NumberContainer
            {
                Number = 5
            };
            numberContainer.Number = numberContainer.NumberPlusFive;
            if (!numberContainer.WasIncreased)
            {
                numberContainer.IncreaseNumber(5);
            }
            numberContainers2[0] = numberContainer;

            for (int i = 0; i < numberContainers1.Length; i++)
            {
                numberContainers1[i].IncreaseNumber(numberContainers2[0].Number);
            }

            // You can also pass arrays and other objects around to other methods.
            memory.WriteUInt32(RunOutputUInt32Index, SumNumberContainers(numberContainers1));
        }

        private uint SumNumberContainers(NumberContainer[] numberContainers)
        {
            uint sum = 0;

            for (int i = 0; i < numberContainers.Length; i++)
            {
                sum += numberContainers[i].Number;
            }

            return sum;
        }

        public uint Run(uint input, IHastlayer hastlayer = null, IHardwareGenerationConfiguration configuration = null)
        {
            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(10)
                : hastlayer.CreateMemory(configuration, 10);
            memory.WriteUInt32(RunInputUInt32Index, input);
            Run(memory);
            return memory.ReadUInt32(RunOutputUInt32Index);
        }
    }

    // Although this is a public class it could also be an inner class and/or a non-public one too.
    public class NumberContainer
    {
        // Auto-properties (also read-only ones) and custom properties can be used.

        // You can initialize properties C# 6-style too.
        public uint Number { get; set; } = 99;

        // Fields can be used too.
        public bool WasIncreased;

        // Fancy custom properties that can do everything a method can.
        public uint NumberPlusFive
        {
            get { return Number + 5; }
            set { Number = value - 5; }
        }

        // Constructors can be used, with or without parameters.
        public NumberContainer()
        {
        }

        public NumberContainer(uint number)
        {
            Number = number;
        }

        // Instance methods can be added as usual.
        public uint IncreaseNumber(uint increaseBy)
        {
            WasIncreased = true;
            return (Number += increaseBy);
        }

        // Methods can call each other as usual.
        public uint IncreaseNumberBy10() => IncreaseNumber(10);

        // Ref and out parameters are supported. Sorry for the forced example!
        public void IncreaseNumberByParameterTimes10(ref uint increaseBy, out uint originalNumber)
        {
            originalNumber = Number;
            increaseBy *= 10;
            IncreaseNumber(increaseBy);
        }
    }

    public static class NumberContainerExtensions
    {
        // You can also write extension methods.
        public static uint IncreaseNumberBy20(this NumberContainer numberContainer) => numberContainer.IncreaseNumber(20);
    }

    public class MemoryContainer
    {
        // The SimpleMemory object can be passed around as usual.
        private readonly SimpleMemory _memory;

        public MemoryContainer(SimpleMemory memory)
        {
            _memory = memory;
        }

        public uint GetInput() => _memory.ReadUInt32(ObjectOrientedShowcase.RunInputUInt32Index);
    }
}
