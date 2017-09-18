using System;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Samples.Kpz
{
    //SimpleMemory map:
    // * 0 .. 1  :  
    //      64-bit random seed
    // * 2  :
    //      32-bit random output

    public class PrngTestInterface
    {

        public virtual void MWC64X(SimpleMemory memory)
        {
            ulong randomState;

            randomState = memory.ReadUInt32(0);
            uint randomSeedTemp = memory.ReadUInt32(1);
            randomState |= ((ulong)randomSeedTemp) << 32; //LE: 1 is high byte, 0 is low byte

            uint stateHighWord = (uint)(randomState >> 32);
            ulong stateLowWordLong = randomState & (0xFFFFFFFFUL);
            uint stateLowWord = (uint)stateLowWordLong;
            // Creating the value 0xFFFEB81BUL. This literal can't be directly used due to an ILSpy bug, see:
            // https://github.com/icsharpcode/ILSpy/issues/807
            uint constantHighShort = 0xFFFE;
            uint constantLowShort = 0xB81B;
            uint constantWord = (0 << 32) | (constantHighShort << 16) | constantLowShort;
            randomState = (ulong)stateLowWord * (ulong)constantWord + (ulong)stateHighWord;
            uint randomWord = stateLowWord ^ stateHighWord;

            memory.WriteUInt32(0, (uint)(randomState & 0xFFFFFFFFUL)); //LE: 1 is high byte, 0 is low byte
            memory.WriteUInt32(1, (uint)(randomState >> 32));
            memory.WriteUInt32(2, randomWord);
        }
    }

    public static class PrngTestExtensions
    {
        static SimpleMemory sm;

        public static void PushRandomSeed(this PrngTestInterface kernels, ulong seed)
        {
            sm = new SimpleMemory(3);
            sm.WriteUInt32(0, (uint)(seed & 0xFFFFFFFFUL)); //LE: 0 is low byte, 1 is high byte
            sm.WriteUInt32(1, (uint)(seed >> 32)); 
        }

        public static uint GetNextRandom(this PrngTestInterface kernels)
        {
            kernels.MWC64X(sm);
            return sm.ReadUInt32(2);
        }

    }
}

