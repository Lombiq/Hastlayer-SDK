using System.Diagnostics.CodeAnalysis;

namespace Hast.TestInputs.Invalid;

public class InvalidLanguageConstructCases
{
    public void CustomValueTypeReferenceEquals()
    {
#pragma warning disable S2995 // "Object.ReferenceEquals" should not be used for value types
#pragma warning disable CA2013 // Do not use ReferenceEquals with value types
        var x = ReferenceEquals(new CustomValueType { MyProperty = 1 }, new CustomValueType { MyProperty = 1 });
#pragma warning restore CA2013 // Do not use ReferenceEquals with value types
#pragma warning restore S2995 // "Object.ReferenceEquals" should not be used for value types
        var y = !x;
    }

    public void InvalidModelUsage()
    {
        var x = InvalidModel.StaticField;
        var y = -x;
    }

    [SuppressMessage("Major Code Smell", "S3898:Value types should implement \"IEquatable<T>\"", Justification = "We don't need it.")]
    private struct CustomValueType
    {
        public int MyProperty { get; set; }
    }
}
