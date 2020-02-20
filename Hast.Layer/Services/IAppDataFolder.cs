using Hast.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Hast.Layer.Services
{
    public interface IAppDataFolder : IDependency
    {
        string MapPath(string relativePath);
        string Combine(params string[] parts);

        bool FileExists(string fileName);
        FileStream CreateFile(string fileName);
        FileStream OpenFile(string fileName);
    }
}
