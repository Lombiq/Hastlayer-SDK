using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hast.Transformer.Vhdl.Abstractions.Configuration
{
    public class VhdlGenerationConfiguration
    {
        private static readonly VhdlGenerationConfiguration _debug = new VhdlGenerationConfiguration
        {
            AddComments = true,
            ShortenNames = true
        };

        /// <summary>
        /// The generated VHDL code will be more readable and will contain debug-level information, though it will be 
        /// significantly slower to create. Also, since the identifiers in code are shortened for readability there can
        /// be naming clashes. The hardware will however run with the same performance as with any other configuration.
        /// </summary>
        public static VhdlGenerationConfiguration Debug => _debug;

        private static readonly VhdlGenerationConfiguration _compact = new VhdlGenerationConfiguration
        {
            ShortenNames = true
        };

        /// <summary>
        /// The generated VHDL code will have the smallest possible size and be more readable due to shortened 
        /// identifiers, though it will be significantly slower to create. Also, there can be naming clashes. The 
        /// hardware will however run with the same performance as with any other configuration.
        /// </summary>
        public static VhdlGenerationConfiguration Compact = _compact;

        private static readonly VhdlGenerationConfiguration _release = new VhdlGenerationConfiguration();

        /// <summary>
        /// The generated VHDL code will be significantly faster to create than in <see cref="Debug"/> or 
        /// <see cref="Compact"/> mode, but will be less readable and won't contain debugging information. The hardware
        /// will however run with the same performance as with any other configuration.
        /// </summary>
        public static VhdlGenerationConfiguration Release = _release;


        /// <summary>
        /// Gets or sets whether inline comments are added to the generated VHDL code. If set to <c>true</c> the
        /// generated code will contain comments on the structure of the code, notes on implementations and hints aiding
        /// navigating the source file.
        /// </summary>
        public bool AddComments { get; set; }

        /// <summary>
        /// Gets or sets whether to abbreviate type and members names in the generated VHDL code. If set to <c>true</c>
        /// the generated code will be more compact and better readable but naming clashes can occur.
        /// </summary>
        public bool ShortenNames { get; set; }
    }
}
