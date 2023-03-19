using CliWrap;
using CliWrap.Buffered;
using CliWrap.EventStream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Hast.Common.Helpers;

public static class CliHelper
{
    /// <summary>
    /// Returns an executable's full path by looking up its command name. Uses the <c>which</c> command in Unix and the
    /// <c>where</c> command in Windows.
    /// </summary>
    /// <param name="name">The application name you can invoke directly in the command line.</param>
    public static async Task<IEnumerable<FileInfo>> WhichAsync(string name)
    {
        var appName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
        var result = await Cli.Wrap(appName)
            .WithArguments(name)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();

        return result.StandardOutput
            .Split('\n')
            .Select(x => x.Trim())
            .Where(File.Exists)
            .Select(x => new FileInfo(x));
    }

    public static async Task StreamAsync(
        string program,
        IList<string> arguments,
        Action<CommandEvent> handler,
        Func<Command, Command> configureCommand = null)
    {
        var command = Cli.Wrap(program);
        if (arguments?.Any() == true) command = command.WithArguments(arguments);
        if (configureCommand != null) command = configureCommand(command);

        await foreach (var commandEvent in command.ListenAsync()) handler(commandEvent);
    }
}
