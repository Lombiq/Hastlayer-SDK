using System.IO;

namespace Hast.Common.Services
{
    public interface IAppDataFolder
    {
        string MapPath(string relativePath);
        string Combine(params string[] parts);

        bool FileExists(string fileName);
        FileStream CreateFile(string fileName);
        FileStream OpenFile(string fileName);
        void DeleteFile(string fileName);
    }
}
