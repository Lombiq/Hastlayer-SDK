using System.IO;

namespace Hast.Layer.Services
{
    public class AppDataFolder : IAppDataFolder
    {
        private readonly IHastlayerConfiguration _hastlayerConfiguration;
        private readonly DirectoryInfo _appDataFolder;

        public AppDataFolder(IHastlayerConfiguration hastlayerConfiguration)
        {
            _hastlayerConfiguration = hastlayerConfiguration;

            _appDataFolder = new DirectoryInfo(hastlayerConfiguration.AppDataFolderPath);
        }

        public string MapPath(string relativePath) => Path.Combine(_appDataFolder.FullName, relativePath);
        public string Combine(params string[] parts) => MapPath(Path.Combine(parts));

        public bool FileExists(string fileName) => File.Exists(MapPath(fileName));
        public FileStream CreateFile(string fileName) => File.Create(MapPath(fileName));
        public FileStream OpenFile(string fileName) => File.OpenRead(MapPath(fileName));
        public void DeleteFile(string fileName) => File.Delete(MapPath(fileName));
    }
}
