using Hast.Layer;
using Hast.VhdlBuilder.Representation.Declaration;
using System.Collections.Generic;

namespace Hast.Transformer.Vhdl.Models;

public class TransformedVhdlManifest
{
    public VhdlManifest Manifest { get; set; }
    public MemberIdTable MemberIdTable { get; set; }
    public IEnumerable<ITransformationWarning> Warnings { get; set; }

    /// <summary>
    /// Gets or sets the Xilinx XDC file, only for Xilinx devices.
    /// </summary>
    public XdcFile XdcFile { get; set; }
}
