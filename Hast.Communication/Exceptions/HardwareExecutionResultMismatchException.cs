using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Hast.Communication.Exceptions
{
    /// <summary>
    /// Exception thrown when there is a mismatch between the results of the hardware and standard software invocation.
    /// </summary>
    public class HardwareExecutionResultMismatchException : Exception
    {
        public IEnumerable<Mismatch> Mismatches { get; private set; }
        public override string Message { get { return ToString(); } }


        public HardwareExecutionResultMismatchException(IEnumerable<Mismatch> mismatches)
        {
            Mismatches = mismatches;
        }


        public override string ToString()
        {
            return 
                "The hardware and software executions resulted in different results: " + 
                string.Join("; ", Mismatches.Select(mismatch => mismatch.ToString()));
        }


        [DebuggerDisplay("{ToString()}")]
        public class Mismatch
        {
            public int ResultMemoryIndex { get; private set; }
            public byte[] HardwareResult { get; private set; }
            public byte[] SoftwareResult { get; private set; }


            public Mismatch(int resultMemoryIndex, byte[] hardwareResult, byte[] softwareResult)
            {
                ResultMemoryIndex = resultMemoryIndex;
                HardwareResult = hardwareResult;
                SoftwareResult = softwareResult;
            }


            public override string ToString()
            {
                if (HardwareResult == null || SoftwareResult == null)
                {
                    return "The hardware or software result was not supplied.";
                }

                return
                    "index: " + ResultMemoryIndex + ", " +
                    "hardware result: { " + string.Join(", ", HardwareResult) + " }, " +
                    "software result: { " + string.Join(", ", SoftwareResult) + " }";
            }
        }
    }
}
