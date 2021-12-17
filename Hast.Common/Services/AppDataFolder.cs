using System;
using System.IO;
using System.Reflection;

namespace Hast.Common.Services
{
    public class AppDataFolder : IAppDataFolder
    {
        private readonly DirectoryInfo _appDataFolder;

        private static string _assemblyDirectory = null;
        public static string AssemblyDirectory
        {
            get
            {
                if (_assemblyDirectory == null)
                {
                    string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                    UriBuilder uri = new UriBuilder(codeBase);
                    string path = Uri.UnescapeDataString(uri.Path);
                    _assemblyDirectory = Path.GetDirectoryName(path);
                }

                return _assemblyDirectory;
            }
        }

        public AppDataFolder(string appDataFolderPath)
        {
            if (string.IsNullOrEmpty(appDataFolderPath))
            {
                appDataFolderPath = Path.Combine(AssemblyDirectory, "App_Data");
            }

            _appDataFolder = new DirectoryInfo(appDataFolderPath);
            if (!_appDataFolder.Exists) _appDataFolder.Create();
        }

        public string MapPath(string relativePath) => Path.Combine(_appDataFolder.FullName, relativePath);
        public string Combine(params string[] parts) => MapPath(Path.Combine(parts));

        public bool FileExists(string fileName) => File.Exists(MapPath(fileName));

        public FileStream CreateFile(string fileName)
        {
            var directoryName = Path.GetDirectoryName(fileName);
            if (!Directory.Exists(directoryName)) Directory.CreateDirectory(directoryName);
            return File.Create(MapPath(fileName));
        }

        public FileStream OpenFile(string fileName) => File.OpenRead(MapPath(fileName));

        public void DeleteFile(string fileName)
        {
            if (File.Exists(fileName)) File.Delete(MapPath(fileName));
        }
    }
}
