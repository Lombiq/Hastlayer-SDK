using Hast.VhdlBuilder.Representation.Declaration;

namespace Hast.VhdlBuilder.Representation.Expression;

public class IdentifierValue : Value
{
    public IdentifierValue(string identifier)
    {
        DataType = KnownDataTypes.Identifier;
        Content = identifier;
    }
}
