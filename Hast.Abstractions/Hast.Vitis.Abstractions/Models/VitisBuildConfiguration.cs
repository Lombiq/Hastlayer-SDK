namespace Hast.Vitis.Abstractions.Models
{
    public class VitisBuildConfiguration
    {
        /// <summary>
        /// Only run Vivado synthesis to generate a report, don't build via v++. Exits after synthesis is complete.
        /// </summary>
        public bool SynthesisOnly { get; set; }

        /// <summary>
        /// If <see langword="true"/>, the device will be reset via <c>xbutil reset</c> before the first build.
        /// </summary>
        public bool ResetOnFirstRun { get; set; }
    }
}
