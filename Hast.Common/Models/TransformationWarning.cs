using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Layer;

namespace Hast.Common.Models
{
    [DebuggerDisplay("{ToString()}")]
    public class TransformationWarning : ITransformationWarning
    {
        public string Code { get; set; }
        public string Message { get; set; }

        public override string ToString() => Code + " - " + Message;
    }
}
