using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Hast.Common.Services
{
    public class AppDataFolder : IAppDataFolder
    {
        private readonly DirectoryInfo _appDataFolder;

        public AppDataFolder()
        {
            _appDataFolder = new DirectoryInfo("App_Data");
        }

        public string MapPath(string relativePath) => Path.Combine(_appDataFolder.FullName, relativePath);
        public string Combine(params string[] parts) => MapPath(Path.Combine(parts));

        public bool FileExists(string fileName) => File.Exists(MapPath(fileName));
        public FileStream CreateFile(string fileName) => File.Create(MapPath(fileName));
        public FileStream OpenFile(string fileName) => File.OpenRead(MapPath(fileName));
    }
}
