using Hast.Common.Helpers;
using Hast.Common.Models;
using System.IO;
using System.Threading.Tasks;

namespace Hast.Synthesis.Abstractions.Helpers;

public static class VhdlHelper
{
    /// <summary>
    /// Creates the VHD and XDC files on the given path from the source codes contained in the <paramref
    /// name="context"/>. <see cref="IHardwareImplementationCompositionContext.HardwareDescription"/> that must be <see
    /// cref="VhdlHardwareDescription"/>.
    /// </summary>
    /// <returns>
    /// The result is <see langword="true"/> if files were created, <see langword="false"/> if the hash matched and
    /// there was nothing to do.
    /// </returns>
    public static async Task<bool> CreateVhdlAndXdcFilesAsync(
        IHardwareImplementationCompositionContext context,
        string xdcFilePath,
        string vhdlFilePath)
    {
        var hashId = context.HardwareDescription.TransformationId;
        var hashFile = vhdlFilePath + ".hash";
        if (File.Exists(hashFile) && (await File.ReadAllTextAsync(hashFile)).Trim() == hashId) return false;

        var name = context.Configuration.Label;
        if (!string.IsNullOrWhiteSpace(name)) await File.WriteAllTextAsync(vhdlFilePath + ".name", name);

        var vhdlHardwareDescription = (VhdlHardwareDescription)context.HardwareDescription;

        FileSystemHelper.EnsureDirectoryExists(Path.GetDirectoryName(vhdlFilePath));
        await File.WriteAllTextAsync(vhdlFilePath, vhdlHardwareDescription.VhdlSource);

        var xdcFileTemplatePath = xdcFilePath + "_template";
        FileSystemHelper.EnsureDirectoryExists(Path.GetDirectoryName(xdcFilePath));

        // Using the original XDC file as a template and then adding constraints to it.
        if (File.Exists(xdcFilePath) && !File.Exists(xdcFileTemplatePath))
        {
            File.Copy(xdcFilePath, xdcFileTemplatePath);
        }
        else if (File.Exists(xdcFileTemplatePath))
        {
            File.Copy(xdcFileTemplatePath, xdcFilePath, overwrite: true);
        }

        if (!string.IsNullOrEmpty(vhdlHardwareDescription.XdcSource))
        {
            await File.AppendAllTextAsync(xdcFilePath, vhdlHardwareDescription.XdcSource);
        }
        else if (File.Exists(xdcFileTemplatePath))
        {
            // The XDC file can contain constraints of previous hardware designs so clearing those out.
            File.Copy(xdcFileTemplatePath, xdcFilePath, overwrite: true);
        }

        await File.WriteAllTextAsync(hashFile, hashId);
        return true;
    }
}
