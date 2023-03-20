using Lombiq.HelpfulLibraries.Common.Utilities;

namespace Hast.Common.Services;

/// <summary>
/// Generates hashes using SHA-256 algorithm.
/// </summary>
public class Sha256HashProvider : IHashProvider
{
    public string ComputeHash(string prefix, params string[] sources)
    {
        prefix ??= string.Empty;

        return prefix + Sha256Helper.ComputeHash(string.Join(string.Empty, sources));
    }
}
