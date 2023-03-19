using Hast.Common.Interfaces;
using Hast.Layer;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Representation.Declaration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hast.Transformer.Vhdl.Services;

/// <summary>
/// Creates the device-specific <see cref="XdcFile"/> for the <see cref="TransformedVhdlManifest"/>.
/// </summary>
public interface IXdcFileBuilder : IDependency, IComparable<IXdcFileBuilder>
{
    /// <summary>
    /// Gets the supported manifest's type.
    /// </summary>
    Type ManifestType { get; }

    /// <summary>
    /// Returns <see langword="true"/> if manifest is of the target type. Use the <see cref="XdcFileBuilderBase{T}"/>
    /// generic base class and don't implement this yourself.
    /// </summary>
    bool IsTargetType(IDeviceManifest manifest);

    /// <summary>
    /// Performs device-specific steps to create the <see cref="XdcFile"/>.
    /// </summary>
    Task<XdcFile> BuildManifestAsync(
        IEnumerable<IArchitectureComponentResult> architectureComponentResults,
        Architecture hastIpArchitecture);
}
