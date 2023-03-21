using System.Collections.Generic;

namespace Hast.Transformer.Vhdl.SimpleMemory;

internal static class SimpleMemoryPortNames
{
    public const string DataIn = "DataIn";
    public const string DataOut = "DataOut";
    public const string CellIndex = "CellIndex";
    public const string ReadEnable = "ReadEnable";
    public const string WriteEnable = "WriteEnable";
    public const string ReadsDone = "ReadsDone";
    public const string WritesDone = "WritesDone";

    public static IEnumerable<string> GetNames()
    {
        yield return DataIn;
        yield return DataOut;
        yield return CellIndex;
        yield return ReadEnable;
        yield return WriteEnable;
        yield return ReadsDone;
        yield return WritesDone;
    }
}
