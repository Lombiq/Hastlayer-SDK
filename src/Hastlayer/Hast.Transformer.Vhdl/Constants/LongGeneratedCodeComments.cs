﻿namespace Hast.Transformer.Vhdl.Constants;

/// <summary>
/// Stores long comments that are inserted into the generated VHDL code to help understand it.
/// </summary>
internal static class LongGeneratedCodeComments
{
    // The strange formatting is so the output will be well formatted and e.g. have appropriate indentations.

    /// <summary>
    /// Comment added just before VHDL libraries.
    /// </summary>
    public const string Libraries = "VHDL libraries necessary for the generated code to work. These libraries " +
        "are included here instead of being managed separately in the Hardware Framework so they can be more " +
        "easily updated.";

    /// <summary>
    /// Comment describing how the ports of the generated hardware component behave.
    /// </summary>
    public const string Ports =
@"Hast_IP's simple interface makes it suitable to plug it into any hardware implementation. The meaning and usage of the
ports are as below:
* MemberId: Each transformed .NET hardware entry point member (i.e. methods that are configured to be available to be
            called from the host PC) has a unique zero-based numeric ID. When selecting which one to execute this ID
            should be used.
* Started: Indicates whether the execution of a given hardware entry point member is started. Used in the following way:
    1. Started is set to TRUE by the consuming framework, after which the execution of the given member starts
       internally. The Finished port will be initially set to FALSE.
    2. Once the execution is finished, the Finished port will be set to TRUE.
    3. The consuming framework sets Started to FALSE, after which Finished will also be set to FALSE.
* Finished: Indicates whether the execution of a given hardware entry point member is complete. See the documentation of
            the Started port above on how it is used.
* Reset: Synchronous reset.
* Clock: The main clock.";

    /// <summary>
    /// Stores an overview comment that is inserted into the generated VHDL code to help understand it.
    /// </summary>
    public const string Overview =
@"This IP was generated by Hastlayer from .NET code to mimic the original logic. Note the following:
* For each member (methods, functions, properties) in .NET a state machine was generated. Each state machine's name
  corresponds to the original member's name.
* Inputs and outputs are passed between state machines as shared objects.
* There are operations that take multiple clock cycles like interacting with the memory and long-running arithmetic
  operations (modulo, division, multiplication). These are awaited in subsequent states but be aware that some states
  can take more than one clock cycle to produce their output.
* The ExternalInvocationProxy process dispatches invocations that were started from the outside to the state machines.
* The InternalInvocationProxy processes dispatch invocations between state machines.";
}