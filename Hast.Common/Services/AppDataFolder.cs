using System;
using System.IO;
using System.Reflection;

namespace Hast.Common.Services
{
    public class AppDataFolder : IAppDataFolder
    {
        private readonly DirectoryInfo _appDataFolder;

        private static string _currentDirectory = null;
        public static string CurrentDirectory
        {
            get
            {
                if (_currentDirectory == null)
                {
                    _currentDirectory = new FileInfo(Assembly.GetEntryAssembly().Location).Directory.FullName;
                }
                return _currentDirectory;
            }
        }

        public AppDataFolder(string appDataFolderPath)
        {
            if (string.IsNullOrEmpty(appDataFolderPath))
            {
                appDataFolderPath = Path.Combine(CurrentDirectory, "App_Data");
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
