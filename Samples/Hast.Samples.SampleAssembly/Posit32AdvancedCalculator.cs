using Hast.Transformer.Abstractions.SimpleMemory;
using Lombiq.Arithmetics;

namespace Hast.Samples.SampleAssembly
{
    public class Posit32AdvancedCalculator
    {

        public const int RepeatedDivisionInputInt32Index = 0;
        public const int RepeatedDivisionFirstInputPosit32Index = 1;
        public const int RepeatedDivisionSecondInputPosit32Index = 2;
        public const int RepeatedDivisionOutputPosit32Index = 0;

        public const int SqrtOfPositsInArrayInputPosit32CountIndex = 0;
        public const int SqrtOfPositsInArrayInputPosit32sStartIndex = 1;
        public const int SqrtOfPositsInArrayOutputPosit32StartIndex = 0;

        public virtual void RepeatedDivision(SimpleMemory memory)
        {
            var number = memory.ReadInt32(RepeatedDivisionInputInt32Index);
            var dividendPosit = memory.ReadUInt32(RepeatedDivisionFirstInputPosit32Index);
            var divisorPosit = memory.ReadUInt32(RepeatedDivisionSecondInputPosit32Index);

            var a = new Posit32(dividendPosit, true);
            var b = new Posit32(divisorPosit, true);

            for (uint i = 0; i < number; i++)
            {
                a /= b;
            }

            var result = a.PositBits;
            memory.WriteUInt32(RepeatedDivisionOutputPosit32Index, result);
        }

        public virtual void SqrtOfPositsInArray(SimpleMemory memory)
        {
            uint numberCount = memory.ReadUInt32(SqrtOfPositsInArrayInputPosit32CountIndex);

            var result = new Posit32(memory.ReadUInt32(SqrtOfPositsInArrayInputPosit32sStartIndex), true);

            for (int i = 0; i < numberCount; i++)
            {
                result = Posit32.Sqrt(new Posit32(memory.ReadUInt32(SqrtOfPositsInArrayInputPosit32sStartIndex + i), true));
                memory.WriteUInt32(SqrtOfPositsInArrayOutputPosit32StartIndex + i, result.PositBits);
            }
        }
    }
}
