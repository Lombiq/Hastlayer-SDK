namespace Hast.Transformer.Vhdl.Abstractions.Configuration
{
    public enum VhdlGenerationMode
    {
        /// <summary>
        /// The generated VHDL code will be more readable and will contain debug-level information, though it will be 
        /// significantly slower to create. Also, since the identifiers in code are shortened for readability there can
        /// be naming clashes. The hardware will however run with the same performance as with <see cref="Compact"/>.
        /// </summary>
        Debug,

        /// <summary>
        /// The generated VHDL code will be significantly faster to create than in <see cref="Debug"/> mode, but will
        /// be less readable and won't contain debugging information.
        /// </summary>
        // Intentionally not named "Release" to avoid the connotation with the .NET Release build target and that it
        // also has a difference in hardware performance.
        Compact
    }


    public class VhdlTransformerConfiguration
    {
        /// <summary>
        /// Gets or sets the configuration on how the VHDL source code will be generated.
        /// </summary>
        public VhdlGenerationMode VhdlGenerationMode { get; set; } = VhdlGenerationMode.Compact;
    }
}
