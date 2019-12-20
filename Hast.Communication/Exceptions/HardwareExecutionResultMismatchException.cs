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


        public override string ToString() =>
            "The hardware and software executions resulted in different results: " + 
            string.Join("; ", Mismatches.Select(mismatch => mismatch.ToString()));


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


            public override string ToString() =>
                HardwareResult == null || SoftwareResult == null ?
                    "The hardware or software result was not supplied." :
                    "index: " + ResultMemoryIndex + ", " +
                        "hardware result: { " + string.Join(", ", HardwareResult) + " }, " +
                        "software result: { " + string.Join(", ", SoftwareResult) + " }";
        }

        [DebuggerDisplay("{ToString()}")]
        public class LengthMismatch : Mismatch
        {
            public int HardwareCellCount { get; private set; }
            public int SoftwareCellCount { get; private set; }


            public LengthMismatch(int hardwareCellCount, int softwareCellCount, int overflowIndex, byte[] hardwareResult, byte[] softwareResult) : 
                base(overflowIndex, hardwareResult, softwareResult)
            {
                HardwareCellCount = hardwareCellCount;
                SoftwareCellCount = softwareCellCount;
            }


            public override string ToString() =>
                "The hardware and software results don't have the same length. The hardware result is " +
                HardwareCellCount + " cell(s) and the software result is " + SoftwareCellCount + ".";
        }
    }
}
