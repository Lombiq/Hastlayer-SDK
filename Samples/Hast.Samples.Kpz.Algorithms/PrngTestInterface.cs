using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using System.Diagnostics.CodeAnalysis;

namespace Hast.Samples.Kpz.Algorithms;

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
    [SuppressMessage("Minor Code Smell", "S100:Methods and properties should be named in PascalCase", Justification = "It's an acronym.")]
    public virtual void MWC64X(SimpleMemory memory)
    {
        uint stateHighWord = memory.ReadUInt32(1);
        uint stateLowWord = memory.ReadUInt32(0);
        ulong randomState = (stateLowWord * 0xFFFE_B81BUL) + stateHighWord;
        uint randomWord = stateLowWord ^ stateHighWord;

        memory.WriteUInt32(0, (uint)randomState); // LE: 1 is high byte, 0 is low byte
        memory.WriteUInt32(1, (uint)(randomState >> 32));
        memory.WriteUInt32(2, randomWord);
    }

    /// <summary>It runs the PRNG on the FPGA and returns a random 32-bit uint.</summary>
    public uint GetNextRandom(SimpleMemory memory)
    {
        MWC64X(memory);
        return memory.ReadUInt32(2);
    }

    /// <summary>
    /// This copies random seed from the host to the FPGA.
    /// </summary>
    public static SimpleMemory PushRandomSeed(
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
}
