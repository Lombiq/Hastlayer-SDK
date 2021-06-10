using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Samples.Kpz.Algorithms
{
    // SimpleMemory map:
    // * 0 .. 1  :
    //      64-bit random seed
    // * 2  :
    //      32-bit random output

    /// <summary>
    /// This class was used to debug the problems we experienced while trying to get the MWC64X PRNG working
    /// on the hardware. It is the class used when selecting "PRNG test (FPGA)" on the GUI.
    /// I'm not replacing it with Hast.Algorithms.PrngMWC64X on reason, to allow us to play with PRNG implementations
    /// later (without screwing up other algorithms.)
    /// </summary>
    public class PrngTestInterface
    {
        public virtual void Mwc64X(SimpleMemory memory)
        {
            const ulong wordSize = 0x_FFFE_B81B;

            uint stateHighWord = memory.ReadUInt32(1);
            uint stateLowWord = memory.ReadUInt32(0);
            ulong randomState = (stateLowWord * wordSize) + stateHighWord;
            uint randomWord = stateLowWord ^ stateHighWord;

            memory.WriteUInt32(0, (uint)randomState); // LE: 1 is high byte, 0 is low byte
            memory.WriteUInt32(1, (uint)(randomState >> 32));
            memory.WriteUInt32(2, randomWord);
        }

        /// <summary>
        /// This copies random seed from the host to the FPGA.
        /// </summary>
        public SimpleMemory PushRandomSeed(
            ulong seed,
            IHastlayer hastlayer,
            IHardwareGenerationConfiguration configuration)
        {
            var sm = configuration is null ?
                SimpleMemory.CreateSoftwareMemory(3) :
                hastlayer.CreateMemory(configuration, 3);
            sm.WriteUInt32(0, (uint)seed); // LE: 0 is low byte, 1 is high byte
            sm.WriteUInt32(1, (uint)(seed >> 32));
            return sm;
        }

        /// <summary>It runs the PRNG on the FPGA and returns a random 32-bit uint.</summary>
        public uint GetNextRandom(SimpleMemory memory)
        {
            Mwc64X(memory);
            return memory.ReadUInt32(2);
        }
    }
}
