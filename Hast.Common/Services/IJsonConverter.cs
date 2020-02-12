using Hast.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hast.Common.Services
{
    public interface IJsonConverter : IDependency
    {
        string Serialize(object source);
    }
}
