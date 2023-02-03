// The same namespace as KnownDataTypes so common types can be checked simply.
namespace Hast.VhdlBuilder.Representation.Declaration;

public static class SpecialTypes
{
    public static readonly DataType Task = new() { TypeCategory = DataTypeCategory.Identifier, Name = nameof(System.Threading.Tasks.Task) };
}
