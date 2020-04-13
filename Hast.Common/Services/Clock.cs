using System;
using System.Collections.Generic;
using System.Text;

namespace Hast.Common.Services
{
    public class Clock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
