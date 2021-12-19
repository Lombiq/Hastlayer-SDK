using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace Hast.Communication.Exceptions
{
    /// <summary>
    /// Exception thrown when there is a mismatch between the results of the hardware and standard software invocation.
    /// </summary>
    [Serializable]
    public class HardwareExecutionResultMismatchException : Exception
    {
        public IEnumerable<Mismatch> Mismatches { get; private set; }
        public override string Message => ToString();

        public HardwareExecutionResultMismatchException(IEnumerable<Mismatch> mismatches) => Mismatches = mismatches;
        public HardwareExecutionResultMismatchException() { }

        public HardwareExecutionResultMismatchException(string message)
            : base(message) { }

        public HardwareExecutionResultMismatchException(string message, Exception innerException)
            : base(message, innerException) { }

        protected HardwareExecutionResultMismatchException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext) { }

        public override string ToString() =>
            "The hardware and software executions resulted in different results: " +
            string.Join("; ", Mismatches.Select(mismatch => mismatch.ToString()));

        [DebuggerDisplay("{ToString()}")]
        public class Mismatch
        {
            public int ResultMemoryIndex { get; }
            public IReadOnlyList<byte> HardwareResult { get; }
            public IReadOnlyList<byte> SoftwareResult { get; }

            public Mismatch(int resultMemoryIndex, byte[] hardwareResult, byte[] softwareResult)
            {
                ResultMemoryIndex = resultMemoryIndex;
                HardwareResult = hardwareResult;
                SoftwareResult = softwareResult;
            }

            public override string ToString() =>
                HardwareResult == null || SoftwareResult == null ?
                    "The hardware or software result was not supplied." :
                    $"index: {ResultMemoryIndex}, " +
                        $"hardware result: {{ {string.Join(", ", HardwareResult)} }} " +
                        $"{{ {string.Join(", ", HardwareResult.Select(r => Convert.ToString(r, 16)))} }}, " +
                        $"software result: {{ {string.Join(", ", SoftwareResult)} }} " +
                        $"{{ {string.Join(", ", SoftwareResult.Select(r => Convert.ToString(r, 16)))} }}";
        }

        [DebuggerDisplay("{ToString()}")]
        public class LengthMismatch : Mismatch
        {
            public int HardwareCellCount { get; private set; }
            public int SoftwareCellCount { get; private set; }

            public LengthMismatch(int hardwareCellCount, int softwareCellCount, int overflowIndex, byte[] hardwareResult, byte[] softwareResult)
                : base(overflowIndex, hardwareResult, softwareResult)
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
