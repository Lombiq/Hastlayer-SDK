using System;
using System.IO;

namespace Hast.Common.Services
{
    public class AppDataFolder : IAppDataFolder
    {
        private readonly DirectoryInfo _appDataFolder;

        public AppDataFolder(string appDataFolderPath)
        {
            // Sets folder to %APPDATA%\Hastlayer on Windows and ~/.config/Hastlayer on Linux.
            if (string.IsNullOrEmpty(appDataFolderPath))
            {
                appDataFolderPath = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData), "Hastlayer");
            }

            _appDataFolder = new DirectoryInfo(appDataFolderPath);
            if (!_appDataFolder.Exists) _appDataFolder.Create();
        }

        public string MapPath(string relativePath) => Path.Combine(_appDataFolder.FullName, relativePath);
        public string Combine(params string[] parts) => MapPath(Path.Combine(parts));

        public bool FileExists(string fileName) => File.Exists(MapPath(fileName));
        public FileStream CreateFile(string fileName) => File.Create(MapPath(fileName));
        public FileStream OpenFile(string fileName) => File.OpenRead(MapPath(fileName));
        public void DeleteFile(string fileName) => File.Delete(MapPath(fileName));
    }
}
