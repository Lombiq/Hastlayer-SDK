using Hast.Common.Services;
using Lombiq.HelpfulLibraries.Common.Utilities;
using System;
using System.Collections.Generic;

namespace Hast.Transformer.Vhdl.Tests.Services;

/// <summary>
/// Generates sequential numbers instead of hash codes. This is less efficient, but makes the samples used for VHDL
/// verification tests less fragile to inconsequential changes in the source code.
/// </summary>
public class VerificationTestHashProvider : IHashProvider
{
    private static readonly object _lock = new();
    private static readonly Dictionary<string, int> _generatedHashes = new();

    public string ComputeHash(string prefix, params string[] sources)
    {
        var hash = Sha256Helper.ComputeHash(string.Join(string.Empty, sources));
        int id;

        lock (_lock)
        {
            if (!_generatedHashes.TryGetValue(hash, out id))
            {
                _generatedHashes.Add(hash, _generatedHashes.Count);
            }
        }

        return prefix + id.ToTechnicalString();
    }
}
