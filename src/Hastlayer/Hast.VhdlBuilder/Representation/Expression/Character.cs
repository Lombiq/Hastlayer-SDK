using Hast.VhdlBuilder.Representation.Declaration;

namespace Hast.VhdlBuilder.Representation.Expression;

public class Character : Value
{
    public Character(char character)
    {
        DataType = KnownDataTypes.Character;
        Content = character.ToString();
    }
}
