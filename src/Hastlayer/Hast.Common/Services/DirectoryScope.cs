using System;
using System.IO;

namespace Hast.Common.Services;

/// <summary>
/// Stores the current working directory, moves into a different location and then returns to the original location when
/// the instance is disposed.
/// </summary>
public class DirectoryScope : IDisposable
{
    private readonly string _oldPath;
    private bool _disposed;

    public DirectoryScope(string path)
    {
        _oldPath = Environment.CurrentDirectory;
        Directory.SetCurrentDirectory(string.IsNullOrWhiteSpace(path) ? "." : path.Trim());
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && Directory.Exists(_oldPath))
        {
            Directory.SetCurrentDirectory(_oldPath);
        }

        _disposed = true;
    }
}
