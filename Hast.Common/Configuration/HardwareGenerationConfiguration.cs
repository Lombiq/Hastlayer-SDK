using System.Collections.Generic;

namespace Hast.Layer
{
    public class HardwareGenerationConfiguration : IHardwareGenerationConfiguration
    {
        /// <summary>
        /// Gets a dictionary that can contain settings for non-default configuration options (like ones required
        /// by specific transformer implementations).
        /// </summary>
        public IDictionary<string, object> CustomConfiguration { get; }

        /// <summary>
        /// Gets or sets a name associated with the hardware generation operation that's meaningful to the consumer. It
        /// may be logged or saved during hardware generation but otherwise it may not used in any activities.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets the collection of the full name of those public members that will be accessible as hardware
        /// implementation. By default all members implemented from interfaces and all public virtual members will
        /// be included. You can use this to restrict what gets transformed into hardware; if nothing is specified
        /// all suitable members will be transformed.
        /// </summary>
        /// <example>
        /// <para>
        /// Specify members with their full name, including the full namespace of the parent type(s) as well as their
        /// return type and the types of their (type) arguments.
        /// </para>
        /// <code>
        /// "System.Boolean Contoso.ImageProcessing.FaceRecognition.FaceDetectors::IsFacePresent(System.Byte[])
        /// </code>
        /// </example>
        public IList<string> HardwareEntryPointMemberFullNames { get; }

        /// <summary>
        /// Gets the collection of the name prefixes of those public members that will be accessible as hardware
        /// implementation. By default all members implemented from interfaces and all public virtual members will
        /// be included. You can use this to restrict what gets transformed into hardware; if nothing is specified
        /// all suitable members will be transformed.
        /// </summary>
        /// <example>
        /// Specify members with the leading part of their name as you would access them in C#, e.g.:
        /// "Contoso.ImageProcessing" will include all members under this namespace.
        /// "Contoso.ImageProcessing.FaceRecognition.FaceDetectors" will include all members in this class.
        /// </example>
        public IList<string> HardwareEntryPointMemberNamePrefixes { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the caching of the generated hardware is allowed. If set to <see langword="false"/> no caching
        /// will happen. Defaults to <see langword="false"/>. Note that this will have an affect only on client-side caching when
        /// using the Client flavor.
        /// </summary>
        public bool EnableCaching { get; set; }

        /// <inheritdoc/>
        public string DeviceName { get; }

        /// <inheritdoc/>
        public string HardwareFrameworkPath { get; }

        /// <summary>
        /// Gets or sets a value indicating whether hardware transformation takes place. If it doesn't then
        /// <see cref="EnableHardwareImplementationComposition"/> will be implied to be <see langword="false"/> too. Defaults to
        /// <see langword="true"/>.
        /// </summary>
        public bool EnableHardwareTransformation { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether a hardware implementation composer should be used to synthesize hardware from the
        /// transformed hardware description. Defaults to <see langword="true"/>.
        /// </summary>
        public bool EnableHardwareImplementationComposition { get; set; } = true;

        /// <summary>
        /// Constructs a new <see cref="HardwareGenerationConfiguration"/> object.
        /// </summary>
        /// <param name="deviceName">
        /// The name of the FPGA device (board) to transform for. Device-specific configurations are determined by
        /// device drivers. You can fetch the list of supported devices via <c>IHastlayer.GetSupportedDevices()</c>.
        /// </param>
        /// <param name="hardwareFrameworkPath">
        /// The file system path here where the hardware framework is located. The file describing the hardware to be
        /// generated will be saved there as well as anything else necessary, and that framework will be used to
        /// implement the hardware and configure the device.
        /// </param>
        public HardwareGenerationConfiguration(
            string deviceName,
            string hardwareFrameworkPath,
            IDictionary<string, object> customConfiguration = null,
            IList<string> hardwareEntryPointMemberFullNames = null,
            IList<string> hardwareEntryPointMemberNamePrefixes = null)
        {
            DeviceName = deviceName;
            HardwareFrameworkPath = string.IsNullOrWhiteSpace(hardwareFrameworkPath)
                ? "HardwareFramework"
                : hardwareFrameworkPath;
            CustomConfiguration = customConfiguration ?? new Dictionary<string, object>();
            HardwareEntryPointMemberFullNames = hardwareEntryPointMemberFullNames ?? new List<string>();
            HardwareEntryPointMemberNamePrefixes = hardwareEntryPointMemberNamePrefixes ?? new List<string>();
        }
    }
}
