using Hast.Layer;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Representation.Declaration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hast.Transformer.Abstractions;

public abstract class XdcFileBuilderBase<T> : IXdcFileBuilder
    where T : IDeviceManifest
{
    public Type ManifestType => typeof(T);

    public bool IsTargetType(IDeviceManifest manifest) => manifest is T;

    public abstract Task<XdcFile> BuildManifestAsync(
        IEnumerable<IArchitectureComponentResult> architectureComponentResults,
        Architecture hastIpArchitecture);

    public int CompareTo(IXdcFileBuilder other)
    {
        if (ManifestType == other.ManifestType) return 0;
        if (ManifestType.IsAssignableFrom(other.ManifestType)) return -1; // The other manifest is a derived type.
        if (other.ManifestType.IsAssignableFrom(ManifestType)) return 1; // The other manifest is an ancestor type.

        // When they aren't matched, they should be at least sorted by name. Though this should not happen, if it did
        // that's not really a cause for panic.
        return string.Compare(ManifestType.FullName, other.ManifestType.FullName, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => obj is XdcFileBuilderBase<T>;

    public override int GetHashCode() => typeof(T).GetHashCode();

    public static bool operator ==(XdcFileBuilderBase<T> left, IXdcFileBuilder right) =>
        left?.ManifestType == right?.ManifestType;

    public static bool operator !=(XdcFileBuilderBase<T> left, IXdcFileBuilder right) =>
        left?.ManifestType != right?.ManifestType;

    public static bool operator <(XdcFileBuilderBase<T> left, IXdcFileBuilder right) => left.CompareTo(right) < 0;

    public static bool operator <=(XdcFileBuilderBase<T> left, IXdcFileBuilder right) => left.CompareTo(right) <= 0;

    public static bool operator >(XdcFileBuilderBase<T> left, IXdcFileBuilder right) => left.CompareTo(right) > 0;

    public static bool operator >=(XdcFileBuilderBase<T> left, IXdcFileBuilder right) => left.CompareTo(right) >= 0;
}
