namespace Hast.Vitis.Abstractions.Models
{
    public class VitisBuildConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether only Vivado synthesis should be run to generate a report, without any build via v++. If so, exits after synthesis is complete.
        /// </summary>
        public bool SynthesisOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the device will be reset via <c>xbutil reset</c> before the first build.
        /// </summary>
        public bool ResetOnFirstRun { get; set; }
    }
}
