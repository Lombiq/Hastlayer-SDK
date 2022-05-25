using Hast.Common.Interfaces;
using Hast.Communication.Models;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using System.Threading.Tasks;

namespace Hast.Communication.Services;

/// <summary>
/// Interface for implementing the basic communication with the FPGA board.
/// </summary>
public interface ICommunicationService : IDependency
{
    /// <summary>
    /// Gets the name of the channel used for the communication.
    /// </summary>
    string ChannelName { get; }

    /// <summary>
    /// Gets or sets the TextWriter which the communication service may use to communicate diagnostics information.
    /// </summary>
    System.IO.TextWriter TesterOutput { get; set; }

    /// <summary>
    /// Executes the given member on hardware.
    /// </summary>
    /// <param name="simpleMemory">
    /// The <see cref="SimpleMemory"/> object representing the memory space the logic works in.
    /// </param>
    /// <param name="memberId">The member ID identifies the class member that we want to run on the FPGA board.</param>
    /// <param name="executionContext">The contextual information of the execution.</param>
    /// <returns>
    /// An <see cref="IHardwareExecutionInformation"/> object containing debug and runtime information about the
    /// hardware execution.
    /// </returns>
    Task<IHardwareExecutionInformation> ExecuteAsync(
        SimpleMemory simpleMemory,
        int memberId,
        IHardwareExecutionContext executionContext);
}
