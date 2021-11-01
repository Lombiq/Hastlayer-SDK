using Castle.Core.Internal;
using Hast.Layer;
using Hast.Samples.Consumer.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.Models
{
    /// <summary>
    /// This class contains the configuration options you can set through the command line arguments and the terminal
    /// GUI. The arguments are all case-insensitive and start with a single dash. The next argument after it is the
    /// value. The <see cref="ArgumentAttribute"/> lists additional aliases. For example <c>-devicename "Nexys A7"</c>
    ///  or <c>-device "Nexys A7"</c>.
    /// </summary>
    public class ConsumerConfiguration
    {
        private const string StorageFileName = nameof(ConsumerConfiguration) + ".json";

        /// <summary>
        /// Gets or sets the name of the device to use. If you leave this empty the first one will be used. If you're
        /// testing Hastlayer locally then you'll need to use the "Nexys A7" or "Nexys4 DDR" devices; for
        /// high-performance local or cloud FPGAs see the docs.
        /// </summary>
        [Argument("device")]
        public string DeviceName { get; set; }

        /// <summary>
        /// Gets or sets the URL of the Hastlayer API.
        /// </summary>
        [Argument]
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the application's name you've specified on the Hastlayer Application Management Dashboard. This
        /// is necessary to authenticate if you're running Hastlayer in the Client flavor.
        /// </summary>
        [Argument]
        public string AppName { get; set; }

        /// <summary>
        /// Gets or sets the application's password you've received when creating the app in the Hastlayer Application
        /// Management Dashboard. This is necessary to authenticate if you're running Hastlayer in the Client flavor. If
        /// this value is specified (not null or empty) then the program switches into Client Flavor on its own.
        /// </summary>
        [Argument]
        public string AppSecret { get; set; }

        /// <summary>
        /// Gets or sets which sample algorithm to transform and run, by name. You can choose one from the list in the
        /// Sample.cs file. Currently the GenomeMatcher sample is not up-to-date enough and shouldn't really be taken as
        /// a good example (check out the other ones).
        /// </summary>
        [Argument("sample")]
        public Sample SampleToRun { get; set; }

        /// <summary>
        /// Gets or sets the value of <see cref="HardwareGenerationConfiguration.Label"/> that helps you identify
        /// individual builds when you run this program in a batch with different configurations.
        /// </summary>
        [Argument("name", "label")]
        public string BuildLabel { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the sample execution should be skipped after the build is completed.
        /// This flag is useful if you are cross-compiling a sample with this program for a different machine.
        /// </summary>
        [Argument("build")]
        public bool DontRun { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the results of the hardware execution should be verified against a
        /// software run as well. If there are any discrepancies the program will throw an exception with details.
        /// </summary>
        [Argument("verify")]
        public bool VerifyResults { get; set; }

        /// <summary>
        /// Gets or sets the path where the hardware framework is located. The file describing the hardware to be
        /// generated will be saved there as well as anything else necessary. If the path is relative (like the
        /// default) then the file will be saved along this project's executable in the bin output directory.
        /// </summary>
        /// <remarks><para>
        /// You should always run this program from its own directory. In other words the working directory should be
        /// where the Hast.Samples.Consumer.dll is located. Otherwise you will see unexpected issues with this and other
        /// relative paths.
        /// </para></remarks>
        [Argument]
        public string HardwareFrameworkPath { get; set; } = "HardwareFramework";

        /// <summary>
        /// Gets or sets the name of this configuration, if you want to save it for later reuse with the <c>-load</c>
        /// argument.
        /// </summary>
        [Argument("save")]
        private string SaveName { get; set; }

        /// <summary>
        /// Returns new instance of <see cref="ConsumerConfiguration"/> with values set according to the arguments in
        /// <paramref name="args"/>.
        /// </summary>
        /// <param name="args">The command line arguments received from the shell.</param>
        /// <param name="consumerConfigurations"></param>
        public static ConsumerConfiguration FromCommandLine(
            IList<string> args,
            Dictionary<string, ConsumerConfiguration> consumerConfigurations)
        {
            var loadIndex = args
                .Select((argument, index) => (Argument: argument.ToUpperInvariant(), Index: index))
                .Aggregate(-1, (current, item) => current == -1 && item.Argument == "-LOAD" ? item.Index : current);

            if (loadIndex >= 0 && loadIndex + 1  < args.Count)
            {
                return consumerConfigurations[args[loadIndex + 1]];
            }

            var configuration = new ConsumerConfiguration();

            var properties = typeof(ConsumerConfiguration)
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(property => property.GetCustomAttributes<ArgumentAttribute>().Any())
                .ToDictionary(
                    property => property,
                    property => property.GetCustomAttributes<ArgumentAttribute>().Single().Aliases);

            foreach (var (property, aliases) in properties)
            {
                var names = aliases
                    .Concat(new [] { property.Name.ToUpperInvariant() })
                    .Select(name => "-" + name)
                    .ToList();

                for (int i = 0; i < args.Count; i++)
                {
                    if (!names.Contains(args[i].ToUpperInvariant())) continue;

                    var value = property.PropertyType == typeof(bool) ? (object)true : args[i + 1];
                    if (property.PropertyType.IsEnum) value = Enum.Parse(property.PropertyType, (string)value);
                    property.SetValue(configuration, value);
                }
            }

            if (!configuration.SaveName.IsNullOrEmpty())
            {
                consumerConfigurations[configuration.SaveName] = configuration;
                SaveConfigurations(consumerConfigurations);
            }

            return configuration;
        }

        public static async Task<Dictionary<string, ConsumerConfiguration>> LoadConfigurationsAsync()
        {
            var configurations = File.Exists(StorageFileName)
                ? JsonConvert.DeserializeObject<Dictionary<string, ConsumerConfiguration>>(
                    await File.ReadAllTextAsync(StorageFileName))
                : new Dictionary<string, ConsumerConfiguration>();

            // Make the name case-insensitive.
            configurations = new Dictionary<string, ConsumerConfiguration>(
                configurations,
                StringComparer.InvariantCultureIgnoreCase);

            return configurations;
        }

        public static void SaveConfigurations(Dictionary<string, ConsumerConfiguration> configurations)
        {
            using var writer = new StreamWriter(StorageFileName, append: false, Encoding.UTF8);
            var serializer = new JsonSerializer { Formatting = Formatting.Indented };

            serializer.Serialize(writer, configurations);
        }
    }
}
