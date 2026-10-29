using Hast.Common.Services;
using Lombiq.HelpfulLibraries.Common.Utilities;
using System;
using System.Collections.Concurrent;

namespace Hast.Transformer.Vhdl.Tests.IntegrationTestingServices;

/// <summary>
/// Generates sequential numbers instead of hash codes. This is less efficient, but makes the samples used for VHDL
/// verification tests less fragile to inconsequential changes in the source code.
/// </summary>
public class VerificationTestHashProvider : IHashProvider
{
    private readonly ConcurrentDictionary<string, int> _generatedHashes = new();

    public string ComputeHash(string prefix, params string[] sources)
    {
        var hash = prefix + Sha256Helper.ComputeHash(string.Join(string.Empty, sources));
        var id = _generatedHashes.GetOrAdd(hash, static (_, hashes) => hashes.Count, _generatedHashes);

        return prefix + id.ToTechnicalString();
    }
}
