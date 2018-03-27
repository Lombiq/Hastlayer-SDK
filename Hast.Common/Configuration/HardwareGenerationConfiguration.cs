using System.Collections.Generic;

namespace Hast.Layer
{
    public class HardwareGenerationConfiguration : IHardwareGenerationConfiguration
    {
        /// <summary>
        /// Gets or sets a dictionary that can contain settings for non-default configuration options (like ones required 
        /// by specific transformer implementations).
        /// </summary>
        public IDictionary<string, object> CustomConfiguration { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the collection of the full name of those public members that will be accessible as hardware 
        /// implementation. By default all members implemented from interfaces and all public virtual members will 
        /// be included. You can use this to restrict what gets transformed into hardware; if nothing is specified
        /// all suitable members will be transformed.
        /// </summary>
        /// <example>
        /// Specify members with their full name, including the full namespace of the parent type(s) as well as their
        /// return type and the types of their (type) arguments, e.g.:
        /// "System.Boolean Contoso.ImageProcessing.FaceRecognition.FaceDetectors::IsFacePresent(System.Byte[])
        /// </example>
        public IList<string> HardwareEntryPointMemberFullNames { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the collection of the name prefixes of those public members that will be accessible as hardware 
        /// implementation. By default all members implemented from interfaces and all public virtual members will 
        /// be included. You can use this to restrict what gets transformed into hardware; if nothing is specified
        /// all suitable members will be transformed.
        /// </summary>
        /// <example>
        /// Specify members with the leading part of their name as you would access them in C#, e.g.:
        /// "Contoso.ImageProcessing" will include all members under this namespace.
        /// "Contoso.ImageProcessing.FaceRecognition.FaceDetectors" will include all members in this class.
        /// </example>
        public IList<string> HardwareEntryPointMemberNamePrefixes { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets whether the caching of the generated hardware is allowed. If set to <c>false</c> no caching
        /// will happen. Defaults to <c>false</c>. Note that this will have an affect only on client-side caching when
        /// using the Client flavor.
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Gets the name of the FPGA device (board) to transform for. Device-specific configurations are determined
        /// by device drivers.
        /// </summary>
        public string DeviceName { get; }


        /// <summary>
        /// Constructs a new <see cref="HardwareGenerationConfiguration"/> object.
        /// </summary>
        /// <param name="deviceName">
        /// The name of the FPGA device (board) to transform for. Device-specific configurations are determined by 
        /// device drivers. You can fetch the list of supported devices via
        /// </param>
        public HardwareGenerationConfiguration(string deviceName)
        {
            DeviceName = deviceName;
        }
    }
}
