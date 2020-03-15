using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Hast.Layer
{
    public interface IHardwareGenerationConfiguration
    {
        /// <summary>
        /// Gets a dictionary that can contain settings for non-default configuration options (like ones required by 
        /// specific transformer implementations).
        /// </summary>
        IDictionary<string, object> CustomConfiguration { get; }

        /// <summary>
        /// Gets the collection of the full name of those public members that will be accessible as hardware 
        /// implementation from the host computer. By default all members implemented from interfaces and all public 
        /// virtual members will be included. You can use this to restrict what gets transformed into hardware; if 
        /// nothing is specified all suitable members will be transformed.
        /// </summary>
        /// <example>
        /// Specify members with their full name, including the full namespace of the parent type(s) as well as their
        /// return type and the types of their (type) arguments, e.g.:
        /// "System.Boolean Contoso.ImageProcessing.FaceRecognition.FaceDetectors::IsFacePresent(System.Byte[])
        /// </example>
        IList<string> HardwareEntryPointMemberFullNames { get; }

        /// <summary>
        /// Gets the collection of the name prefixes of those public members that will be accessible as hardware 
        /// implementation from the host computer. By default all members implemented from interfaces and all public 
        /// virtual members will be included. You can use this to restrict what gets transformed into hardware; if 
        /// nothing is specified all suitable members will be transformed.
        /// </summary>
        /// <example>
        /// Specify members with the leading part of their name as you would access them in C#, e.g.:
        /// "Contoso.ImageProcessing" will include all members under this namespace.
        /// "Contoso.ImageProcessing.FaceRecognition.FaceDetectors" will include all members in this class.
        /// </example>
        IList<string> HardwareEntryPointMemberNamePrefixes { get; }

        /// <summary>
        /// Gets whether the caching of the generated hardware is allowed. If set to <c>false</c> no caching will happen.
        /// </summary>
        bool EnableCaching { get; }

        /// <summary>
        /// Gets the name of the FPGA device (board) to transform for. Device-specific configurations are determined
        /// by device drivers.
        /// </summary>
        string DeviceName { get; }

        /// <summary>
        /// Gets the file system path here where the hardware framework is located. The file describing the hardware to
        /// be generated will be saved there as well as anything else necessary, and that framework will be used to
        /// implement the hardware and configure the device.
        /// </summary>
        string HardwareFrameworkPath { get; }
    }


    public static class HardwareGenerationConfigurationExtensions
    {
        /// <summary>
        /// Gets the custom configuration if it exists or creates and adds it if it doesn't.
        /// </summary>
        /// <typeparam name="T">Type of the configuration object.</typeparam>
        /// <param name="key">Key where the custom configuration object is stored in the 
        /// <see cref="IHardwareGenerationConfiguration"/> instance.</param>
        /// <returns>The existing or newly created configuration object.</returns>
        public static T GetOrAddCustomConfiguration<T>(this IHardwareGenerationConfiguration hardwareConfiguration, string key)
            where T : new() =>
            hardwareConfiguration.CustomConfiguration.GetOrAddCustomConfiguration<T>(key);

        /// <summary>
        /// Adds a public method that will be accessible from the host computer as hardware implementation.
        /// </summary>
        /// <typeparam name="T">The type of the object that will be later fed to the proxy generator.</typeparam>
        /// <param name="expression">An expression with a call to the method.</param>
        public static void AddHardwareEntryPointMethod<T>(
            this IHardwareGenerationConfiguration configuration,
            Expression<Action<T>> expression) =>
            configuration.HardwareEntryPointMemberFullNames.Add(expression.GetMethodFullName());

        /// <summary>
        /// Adds a public type the suitable methods of which will be accessible from the host computer as hardware 
        /// implementation.
        /// </summary>
        /// <typeparam name="T">The type of the object that will be later fed to the proxy generator.</typeparam>
        public static void AddHardwareEntryPointType<T>(this IHardwareGenerationConfiguration configuration)
        {
            // Object base methods are not needed.
            var excludedMethodNames = new[]
            {
                "Boolean Equals(System.Object)",
                "Int32 GetHashCode()", "System.Type GetType()",
                "System.String ToString()"
            };

            // If we'd just add the type's name to HardwareEntryPointMemberNamePrefixes then types with just a different
            // suffix in their names would still be included.
            foreach (var method in typeof(T).GetMethods().Where(method => !excludedMethodNames.Contains(method.ToString())))
            {
                configuration.HardwareEntryPointMemberFullNames.Add(method.GetFullName());
            }
        }

        // Properties could be added similarly once properties are supported for direct hardware invocation. This is
        // unlikely (since it wouldn't be of much use), though properties inside the generated hardware is already
        // supported.
    }
}
