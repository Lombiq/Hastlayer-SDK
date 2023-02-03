namespace Hast.Transformer.Vhdl.SimpleMemory;

/// <summary>
/// Stores long comments that are inserted into the generated VHDL code to help understand it.
/// </summary>
internal static class LongGeneratedSimpleMemoryCodeComments
{
    // The strange formatting is so the output will be well formatted and e.g. have appropriate indentations.

    /// <summary>
    /// Comment describing how the SimpleMemory ports of the generated hardware component behave.
    /// </summary>
    public const string Ports = Constants.LongGeneratedCodeComments.Ports +
@"
* DataIn: Data read out from the memory (usually on-board DDR RAM, but depends on the framework) should be assigned to
          this port by the framework. The width of this port is always 32b, independent of the hardware platform (if the
          bus to the memory is wider then caching needs to be implemented in the framework to make use of it). Inputs of
          the algorithm implemented in Hast_IP all come through this port.
* DataOut: Data to be written to the memory is assigned to this port. The width of this port is always 32b, independent
           of the hardware platform (if the bus to the memory is wider then caching needs to be implemented in the
           framework to make use of it). Outputs of the algorithm implemented in Hast_IP all go through this port.
* CellIndex: Zero-based index of the SimpleMemory memory cell currently being read or written. Transformed code in
             Hastlayer can access memory in a simplified fashion by addressing 32b ""cells"", the accessible physical
             memory space being divided up in such individually addressable cells.
* ReadEnable: Indicates whether a memory read operation is initiated. The process of a memory read is as following:
    1. ReadEnable is FALSE by default. It's set to TRUE when a memory read is started. CellIndex is set to the index of
       the memory cell to be read out.
    2. Waiting for ReadsDone to be TRUE.
    3. Once ReadsDone is TRUE, data from DataIn will be read out and ReadEnable set to FALSE.
* WriteEnable: Indicates whether a memory write operation is initiated. The process of a memory write is as following:
    1. WriteEnable is FALSE by default. It's set to TRUE when a memory write is started. CellIndex is set to the index
       of the memory cell to be written and the output data is assigned to DataOut.
    2. Waiting for WritesDone to be TRUE.
    3. Once WritesDone is TRUE, WriteEnable is set to FALSE.
* ReadsDone: Indicates whether a memory read operation is completed.
* WritesDone: Indicates whether a memory write operation is completed.";
}
