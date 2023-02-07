using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Reflection;

namespace Hast.Xilinx;

public static class ResourceHelper
{
    public static string GetTimingReport(string tsvName, Assembly assembly = null)
    {
        var provider = new EmbeddedFileProvider(assembly ?? typeof(AlveoU250Driver).Assembly);
        using var stream = provider.GetFileInfo($"TimingReports.{tsvName}.tsv").CreateReadStream();
        using var streamReader = new StreamReader(stream);
        return streamReader.ReadToEnd();
    }
}
