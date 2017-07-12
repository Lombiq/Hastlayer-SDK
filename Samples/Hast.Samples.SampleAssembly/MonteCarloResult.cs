using System.Diagnostics;

namespace Hast.Samples.SampleAssembly
{
    [DebuggerDisplay("{ToString()}")]
    public class MonteCarloResult
    {
        /// <summary>
        /// Weight of the result.
        /// </summary>
        public uint W { get; set; }

        /// <summary>
        /// X coordinate of the result.
        /// </summary>
        public uint X { get; set; }

        /// <summary>
        /// Y coordinate of the result.
        /// </summary>
        public uint Y { get; set; }

        /// <summary>
        /// Z coordinate of the result.
        /// </summary>
        public uint Z { get; set; }

        /// <summary>
        /// Estimated error of the weight.
        /// </summary>
        public uint DW { get; set; }

        /// <summary>
        /// Estimated error of the X coordinate.
        /// </summary>
        public uint DX { get; set; }

        /// <summary>
        /// Estimated error of the Y coordinate.
        /// </summary>
        public uint DY { get; set; }

        /// <summary>
        /// Estimated error of the Z coordinate.
        /// </summary>
        public uint DZ { get; set; }


        public override string ToString()
        {
            return string.Format(
                "W: {0}\r\nX: {1}\r\nY: {2}\r\nZ: {3}\r\n\r\ndW: {4}\r\ndX: {5} \r\ndY: {6} \r\ndZ: {7}",
                W, X, Y, Z, DW, DX, DY, DZ);
        }
    }
}
