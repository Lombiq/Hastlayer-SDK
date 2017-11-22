using System;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Samples.Kpz
{
    //SimpleMemory map:
    // * 0 .. 1  :  
    //      64-bit random seed
    // * 2  :
    //      32-bit random output

    /// <summary>
    /// This class was used to debug the problems we experienced while trying to get the MWC64X PRNG working 
    /// on the hardware. It is the class used when selecting "PRNG test (FPGA)" on the GUI.
    /// </summary>
    public class PrngTestInterface
    {

        public virtual void MWC64X(SimpleMemory memory)
        {
            uint stateHighWord = memory.ReadUInt32(1);
            uint stateLowWord = memory.ReadUInt32(0); ;
            // Creating the value 0xFFFEB81BUL. This literal can't be directly used due to an ILSpy bug, see:
            // https://github.com/icsharpcode/ILSpy/issues/807
            uint constantHighShort = 0xFFFE;
            uint constantLowShort = 0xB81B;
            uint constantWord = (0 << 32) | (constantHighShort << 16) | constantLowShort;
            ulong randomState = (ulong)stateLowWord * (ulong)constantWord + (ulong)stateHighWord;
            uint randomWord = stateLowWord ^ stateHighWord;

            memory.WriteUInt32(0, (uint)randomState); //LE: 1 is high byte, 0 is low byte
            memory.WriteUInt32(1, (uint)(randomState >> 32));
            memory.WriteUInt32(2, randomWord);
        }
    }

    /// <summary>
    /// These are host-side functions for <see cref="PrngTestExtensions"/>.
    /// </summary>
    public static class PrngTestExtensions
    {
        /// <summary>
        /// This copies random seed from the host to the FPGA.
        /// </summary>
        public static SimpleMemory PushRandomSeed(this PrngTestInterface kernels, ulong seed)
        {
            SimpleMemory sm = new SimpleMemory(3);
            sm.WriteUInt32(0, (uint)seed); //LE: 0 is low byte, 1 is high byte
            sm.WriteUInt32(1, (uint)(seed >> 32));
            return sm;
        }

        /// <summary>It runs the PRNG on the FPGA and returns a random 32-bit uint.</summary>
        public static uint GetNextRandom(this PrngTestInterface kernels, SimpleMemory memory)
        {
            kernels.MWC64X(memory);
            return memory.ReadUInt32(2);
        }

    }
}

