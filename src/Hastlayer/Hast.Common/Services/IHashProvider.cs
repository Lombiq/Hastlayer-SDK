namespace Hast.Common.Services;

/// <summary>
/// A  hash provider to generate low-collusion unique strings that can be safely used to identify a section of code or a
/// collection of strings that identify a build context.
/// </summary>
public interface IHashProvider
{
    /// <summary>
    /// Returns the <paramref name="prefix"/> and a new hash based on the <paramref name="sources"/>.
    /// </summary>
    string ComputeHash(string prefix, params string[] sources);
}
