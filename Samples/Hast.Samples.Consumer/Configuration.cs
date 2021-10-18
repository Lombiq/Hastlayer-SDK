namespace Hast.Samples.Consumer
{
    internal static class Configuration
    {
        /// <summary>
        /// Which supported hardware device to use? If you leave this empty the first one will be used. If you're
        /// testing Hastlayer locally then you'll need to use the "Nexys A7" or "Nexys4 DDR" devices; for
        /// high-performance local or cloud FPGAs see the docs.
        /// You can also provide this in the -device command line argument.
        /// </summary>
        public static string DeviceName = "Nexys A7";

        /// <summary>
        /// If you're running Hastlayer in the Client flavor, you need to configure your credentials here. Here the
        /// name of your app.
        /// You can also provide this in the -appname command line argument.
        /// </summary>
        public static string AppName = "appname";

        /// <summary>
        /// If you're running Hastlayer in the Client flavor, you need to configure your credentials here. Here the
        /// app secret corresponding to of your app.
        /// You can also provide this in the -appsecret command line argument.
        /// </summary>
        public static string AppSecret = "appsecret";

        /// <summary>
        /// Which sample algorithm to transform and run? Choose one. Currently the GenomeMatcher sample is not
        /// up-to-date enough and shouldn't be really taken as good examples (check out the other ones).
        /// You can also provide this in the -sample command line argument.
        /// </summary>
        public static Sample SampleToRun = Sample.Loopback;

        /// <summary>
        /// Specify a path here where the hardware framework is located. The file describing the hardware to be
        /// generated will be saved there as well as anything else necessary. If the path is relative (like the
        /// default) then the file will be saved along this project's executable in the bin output directory.
        /// </summary>
        public static string HardwareFrameworkPath = "HardwareFramework";
    }
}
