using System;
using System.Runtime.Serialization;

namespace Hast.VhdlBuilder.Testing;

[Serializable]
public class VhdlStructureAssertionFailedException : Exception
{
    // Putting the whole information into the Message is a bit ugly but the Shouldly test assertion package will only
    // display that, so all info should go there.
    public string Description { get; set; }

    public string CodeExcerpt { get; set; }

    public override string Message => string.Join(Environment.NewLine, Description, "Affected code:", CodeExcerpt);

    public VhdlStructureAssertionFailedException() { }

    public VhdlStructureAssertionFailedException(string message)
        : base(message) { }

    public VhdlStructureAssertionFailedException(string message, Exception innerException)
        : base(message, innerException) { }

    public VhdlStructureAssertionFailedException(string description, string codeExcerpt)
    {
        Description = description;
        CodeExcerpt = codeExcerpt;
    }

    protected VhdlStructureAssertionFailedException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext) { }
}
