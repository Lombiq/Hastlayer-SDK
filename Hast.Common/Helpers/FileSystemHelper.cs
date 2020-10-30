using System.IO;

namespace Hast.Common.Helpers
{
    public static class FileSystemHelper
    {
        /// <summary>
        /// Returns a composed file system path from the <paramref name="pathComponents"/> and creates it as a directory
        /// if it doesn't exist.
        /// </summary>
        public static string EnsureDirectoryExists(params string[] pathComponents)
        {
            var path = Path.Combine(pathComponents);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }
    }
}
