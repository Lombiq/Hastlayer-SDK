namespace Hast.Transformer.Vhdl.Abstractions.Configuration
{
    public class VhdlGenerationConfiguration
    {
        /// <summary>
        /// Gets the generated VHDL code will be more readable and will contain debug-level information, though it will be
        /// significantly slower to create. Also, since the identifiers in code are shortened for readability there can
        /// be naming clashes. The hardware will however run with the same performance as with any other configuration.
        /// </summary>
        public static VhdlGenerationConfiguration Debug { get; } = new VhdlGenerationConfiguration
        {
            AddComments = true,
            ShortenNames = true,
        };

        /// <summary>
        /// Gets the generated VHDL code will have the smallest possible size and be more readable due to shortened
        /// identifiers, though it will be significantly slower to create. Also, there can be naming clashes. The
        /// hardware will however run with the same performance as with any other configuration.
        /// </summary>
        public static VhdlGenerationConfiguration Compact { get; } = new VhdlGenerationConfiguration
        {
            ShortenNames = true,
        };

        /// <summary>
        /// Gets the generated VHDL code will be significantly faster to create than in <see cref="Debug"/> or
        /// <see cref="Compact"/> mode, but will be less readable and won't contain debugging information. The hardware
        /// will however run with the same performance as with any other configuration.
        /// </summary>
        public static VhdlGenerationConfiguration Release { get; } = new VhdlGenerationConfiguration();

        /// <summary>
        /// Gets or sets a value indicating whether inline comments are added to the generated VHDL code. If set to <see langword="true"/> the
        /// generated code will contain comments on the structure of the code, notes on implementations and hints aiding
        /// navigating the source file.
        /// </summary>
        public bool AddComments { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to abbreviate type and members names in the generated VHDL code. If set to <see langword="true"/>
        /// the generated code will be more compact and better readable but naming clashes can occur.
        /// </summary>
        public bool ShortenNames { get; set; }
    }
}
