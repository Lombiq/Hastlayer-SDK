using Lombiq.HelpfulLibraries.Common.Utilities;
using System;

namespace Hast.Common.Services;

/// <summary>
/// Generates hashes using SHA-256 algorithm.
/// </summary>
public class Sha256HashProvider : IHashProvider
{
    public string ComputeHash(string prefix, params string[] sources)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            throw new ArgumentException($"{nameof(prefix)} must not be null or empty.", nameof(prefix));
        }

        return prefix + Sha256Helper.ComputeHash(string.Join(string.Empty, sources));
    }
}
