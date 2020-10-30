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


        /// <summary>
        /// Recursively copies the contents fo <paramref name="source"/> to the path of <paramref name="target"/>.
        /// </summary>
        /// <remarks>Source: https://stackoverflow.com/a/690980</remarks>
        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (var file in source.GetFiles())
            {
                System.Console.WriteLine(@"Copying {0}\{1}", target.FullName, file.Name);
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (var subDirectory in source.GetDirectories())
            {
                var nextTargetSubDir = target.CreateSubdirectory(subDirectory.Name);
                CopyAll(subDirectory, nextTargetSubDir);
            }
        }
    }
}
