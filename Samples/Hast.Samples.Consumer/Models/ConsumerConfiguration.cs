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

        [Argument("device")]
        [Hint(
            "Which supported hardware device to use? If you leave this empty the first one will be used. If you're",
            "testing Hastlayer locally then you'll need to use the \"Nexys A7\" or \"Nexys4 DDR\" devices; for",
            "high-performance local or cloud FPGAs see the docs.")]
        public string DeviceName { get; set; }

        [Argument]
        [Hint(
            "The base URL for the Hastlayer API. Unless you are testing the Hast.Remote services locally, you should",
              "leave it empty.")]
        public string Endpoint { get; set; }

        [Argument]
        [Hint(
            "The application's name you've specified on the Hastlayer Application Management Dashboard. This is",
            "necessary to authenticate if you're running Hastlayer in the Client flavor.")]
        public string AppName { get; set; }

        [Argument]
        [Hint(
            "The application's password you've received when creating the app in the Hastlayer Application Management",
            "Dashboard. This is necessary to authenticate if you're running Hastlayer in the Client flavor. If this",
            "value is specified (not null or empty) then the program switches into Client Flavor on its own.")]
        public string AppSecret { get; set; }

        [Argument("sample")]
        [Hint(
            "Which sample algorithm to transform and run? Choose one. Currently the GenomeMatcher sample is not",
            "up-to-date enough and shouldn't be really taken as good examples (check out the other ones). If you want",
            "to learn more about what each sample does, check out matching *SampleRunner.cs file in the project's",
            "SampleRunners directory.")]
        public Sample SampleToRun { get; set; }

        /// <summary>
        /// Gets or sets the value of <see cref="HardwareGenerationConfiguration.Label"/>.
        /// </summary>
        [Argument("name", "label")]
        [Hint("This label helps you identify individual builds when you run this program in a batch with different configurations")]
        public string BuildLabel { get; set; }

        [Argument("build")]
        [Hint(
            "Do you want to skip the sample execution? This flag is useful if you are cross-compiling a sample with",
            "this program for a different machine where you can use the",
            nameof(SingleBinaryPath),
            "option or the corresponding \"-bin\" command line switch.")]
        public bool GenerateHardwareOnly { get; set; }

        [Argument("verify")]
        [Hint(
            "Do you want to check the FPGA results against a CPU run? If there are any discrepancies the program will",
            "throw an exception with details.")]
        public bool VerifyResults { get; set; }

        [Argument]
        [Hint(
            "Specify a path here where the hardware framework is located. The file describing the hardware to be",
            "generated will be saved there as well as anything else necessary. If the path is relative (like the",
            "default) then the file will be saved along this project's executable in the bin output directory.\nYou",
            "should always run this program from its own directory. In other words the working directory should be",
            "where the Hast.Samples.Consumer.dll is located. Otherwise you will see unexpected issues with this and",
            "other relative paths.")]
        public string HardwareFrameworkPath { get; set; } = "HardwareFramework";

        [Argument("bin", "binary")]
        [Hint(
            "Do you already have a cross-compiled binary? Enter the path here and the driver will use it instead of",
            "going through the normal hardware generation build process. ")]
        public string SingleBinaryPath { get; set; }

        /// <summary>
        /// Gets or sets the name of this configuration, if you want to save it for later reuse with the <c>-load</c>
        /// argument.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is <see langword="private"/> because it's only used by <see cref="FromCommandLine"/> to immediately
        /// save the parsed object and doesn't need to be made available outside.
        /// </para>
        /// </remarks>
        [Argument("save")]
        private string SaveName { get; set; }

        /// <summary>
        /// Gets a lazy string dictionary that maps the property names that have <see cref="HintAttribute"/> in this
        /// class to their hint texts.
        /// </summary>
        /// <returns></returns>
        public static Lazy<Dictionary<string, string>> HintDictionary { get; } = new(() =>
            GetPropertyAttributes<HintAttribute>()
                .ToDictionary(pair => pair.Property.Name, pair => pair.Attribute.Text));

        private static IEnumerable<(PropertyInfo Property, T Attribute)> GetPropertyAttributes<T>()
            where T : Attribute =>
            typeof(ConsumerConfiguration)
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Select(property => (Property: property, Attribute: property.GetCustomAttributes<T>().FirstOrDefault()))
                .Where(pair => pair.Attribute != null);

        /// <summary>
        /// Returns new instance of <see cref="ConsumerConfiguration"/> with values set according to the arguments in
        /// <paramref name="args"/>.
        /// </summary>
        /// <param name="args">The command line arguments received from the shell.</param>
        /// <param name="consumerConfigurations">
        /// The saved configs loaded from the <c>ConsumerConfiguration.json</c> file.
        /// </param>
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

            var properties = GetPropertyAttributes<ArgumentAttribute>()
                .ToDictionary(pair => pair.Property, pair => pair.Attribute.Aliases);

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
