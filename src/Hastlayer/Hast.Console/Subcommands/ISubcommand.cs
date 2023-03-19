namespace Hast.Console.Subcommands;

/// <summary>
/// Command line parser for a special type of first argument that mode switches what the application does.
/// </summary>
public interface ISubcommand
{
    /// <summary>
    /// Once <see cref="Program"/> identifies the subcommand this method performs parsing and any subsequent actions.
    /// </summary>
    void Run();
}
