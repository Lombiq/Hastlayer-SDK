using Hast.Transformer.Abstractions.SimpleMemory;
using Lombiq.Arithmetics;
using System.Threading.Tasks;

namespace Hast.Samples.SampleAssembly
{
    /// <summary>
    /// A sample showing how floating point numbers of type posit (<see href="https://posithub.org" />) can be used
    /// with Hastlayer. Using the statically-typed <see cref="Posit32"/> variant here.
    /// </summary>
    public class Posit32Calculator
    {
        public const int CalculateLargeIntegerSumInputInt32Index = 0;
        public const int CalculateLargeIntegerSumOutputInt32Index = 0;
        public const int ParallelizedCalculateLargeIntegerSumInt32NumbersStartIndex = 0;
        public const int ParallelizedCalculateLargeIntegerSumOutputInt32sStartIndex = 0;
        public const int AddPositsInArrayInputPosit32CountIndex = 0;
        public const int AddPositsInArrayInputPosit32sStartIndex = 1;
        public const int AddPositsInArrayOutputPosit32Index = 2;
        public const int CalculatePowerOfRealInputInt32Index = 0;
        public const int CalculatePowerOfRealInputPosit32Index = 1;
        public const int CalculatePowerOfRealOutputPosit32Index = 0;

        // This takes about 75% of a Nexys 4 DDR's FPGA. If only ParallelizedCalculateIntegerSumUpToNumbers is
        // selected as the hardware entry point (i.e. only it will be transformed into hardware, see the config in
        // Posit32CalculatorSampleRunner) then with a MaxDegreeOfParallelism of 5 it'll take 75% as well.
        public const int MaxDegreeOfParallelism = 2;

        public virtual void CalculateIntegerSumUpToNumber(SimpleMemory memory)
        {
            var number = memory.ReadUInt32(CalculateLargeIntegerSumInputInt32Index);

            var a = new Posit32(1);
            var b = a;

            for (uint i = 1; i < number; i++)
            {
                a += b;
            }

            var result = (int)a;
            memory.WriteInt32(CalculateLargeIntegerSumOutputInt32Index, result);
        }

        public virtual void CalculatePowerOfReal(SimpleMemory memory)
        {
            var number = memory.ReadInt32(CalculatePowerOfRealInputInt32Index);
            var positToMultiply = memory.ReadUInt32(CalculatePowerOfRealInputPosit32Index);

            var a = new Posit32(positToMultiply, true);
            var b = a;

            for (uint i = 0; i < number; i++)
            {
                a *= b;
            }

            var result = a.PositBits;
            memory.WriteUInt32(CalculatePowerOfRealOutputPosit32Index, result);
        }

        public virtual void ParallelizedCalculateIntegerSumUpToNumbers(SimpleMemory memory)
        {
            var tasks = new Task<int>[MaxDegreeOfParallelism];

            for (int i = 0; i < MaxDegreeOfParallelism; i++)
            {
                var upToNumber = memory.ReadInt32(ParallelizedCalculateLargeIntegerSumInt32NumbersStartIndex + i);

                tasks[i] = Task.Factory.StartNew(
                    upToNumberObject =>
                    {
                        var a = new Posit32(1);
                        var b = a;

                        for (int j = 1; j < (int)upToNumberObject; j++)
                        {
                            a += b;
                        }

                        return (int)a;
                    }, upToNumber);
            }

            Task.WhenAll(tasks).Wait();

            for (int i = 0; i < MaxDegreeOfParallelism; i++)
            {
                memory.WriteInt32(ParallelizedCalculateLargeIntegerSumOutputInt32sStartIndex + i, tasks[i].Result);
            }
        }

        public virtual void AddPositsInArray(SimpleMemory memory)
        {
            uint numberCount = memory.ReadUInt32(AddPositsInArrayInputPosit32CountIndex);

            var result = new Posit32(memory.ReadUInt32(AddPositsInArrayInputPosit32sStartIndex), true);

            for (int i = 1; i < numberCount; i++)
            {
                result += new Posit32(memory.ReadUInt32(AddPositsInArrayInputPosit32sStartIndex + i), true);
            }

            memory.WriteUInt32(AddPositsInArrayOutputPosit32Index, result.PositBits);
        }
    }
}
