using System.IO;

namespace Hast.Common.Services
{
    /// <summary>
    /// Handles file and path operations in the App_Data folder.
    /// </summary>
    public interface IAppDataFolder
    {
        /// <summary>
        /// Returns an absolute filesystem path from the given relative path.
        /// </summary>
        string MapPath(string relativePath);

        /// <summary>
        /// Returns an absolute path like <see cref="MapPath(string)"/>, but builds the relative path from
        /// <paramref name="parts"/>.
        /// </summary>
        string Combine(params string[] parts);

        /// <summary>
        /// Returns if a file with the relative or absolute path in <paramref name="fileName"/> exists.
        /// </summary>
        bool FileExists(string fileName);

        /// <summary>
        /// Creates a new empty file.
        /// </summary>
        FileStream CreateFile(string fileName);

        /// <summary>
        /// Opens an existing file.
        /// </summary>
        FileStream OpenFile(string fileName);

        /// <summary>
        /// Deletes the file if it exists.
        /// </summary>
        void DeleteFile(string fileName);
    }
}
