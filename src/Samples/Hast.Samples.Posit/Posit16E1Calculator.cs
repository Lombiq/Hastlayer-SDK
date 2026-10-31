using Hast.Transformer.SimpleMemory;
using Lombiq.Arithmetics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Hast.Samples.Posit;

public class Posit16E1Calculator
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

    public const int MaxDegreeOfParallelism = 5;

    public virtual void CalculateIntegerSumUpToNumber(SimpleMemory memory)
    {
        var number = memory.ReadUInt32(CalculateLargeIntegerSumInputInt32Index);

        var a = new Posit16E1(1);
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
        var positToMultiply = (ushort)memory.ReadUInt32(CalculatePowerOfRealInputPosit32Index);

        var a = new Posit16E1(positToMultiply, fromBitMask: true);
        var b = a;

        for (uint i = 0; i < number; i++)
        {
            a *= b;
        }

        var result = a.PositBits;
        memory.WriteUInt32(CalculatePowerOfRealOutputPosit32Index, result);
    }

    [SuppressMessage(
        "Reliability",
        "CA2008:Do not create tasks without passing a TaskScheduler",
        Justification = "There is no task scheduler in hardware.")]
    [SuppressMessage(
        "Usage",
        "VSTHRD105:Avoid method overloads that assume TaskScheduler.Current",
        Justification = "Same.")]
    [SuppressMessage("Usage", "VSTHRD104:Offer async methods", Justification = "Same.")]
    public virtual void ParallelizedCalculateIntegerSumUpToNumbers(SimpleMemory memory)
    {
        var tasks = new Task<int>[MaxDegreeOfParallelism];

        for (int i = 0; i < MaxDegreeOfParallelism; i++)
        {
            var upToNumber = memory.ReadInt32(ParallelizedCalculateLargeIntegerSumInt32NumbersStartIndex + i);

            tasks[i] = Task.Factory.StartNew(
                upToNumberObject =>
                {
                    var a = new Posit16E1(1);
                    var b = a;

                    for (int j = 1; j < (int)upToNumberObject; j++)
                    {
                        a += b;
                    }

                    return (int)a;
                },
                upToNumber);
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

        var result = new Posit16E1(
            (ushort)memory.ReadUInt32(AddPositsInArrayInputPosit32sStartIndex),
            fromBitMask: true);

        for (int i = 1; i < numberCount; i++)
        {
            result += new Posit16E1(
                (ushort)memory.ReadUInt32(AddPositsInArrayInputPosit32sStartIndex + i),
                fromBitMask: true);
        }

        memory.WriteUInt32(AddPositsInArrayOutputPosit32Index, result.PositBits);
    }
}
