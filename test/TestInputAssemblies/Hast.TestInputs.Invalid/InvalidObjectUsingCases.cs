namespace Hast.TestInputs.Invalid;

public class InvalidObjectUsingCases
{
    public void ReferenceAssignment(int input)
    {
        var customObject1 = new MyClass1 { MyProperty = input };
        var customObject2 = customObject1;
        customObject1.MyProperty += 1;
        customObject2.MyProperty += 1;
        // This is not allowed, since to achieve reference-like behavior we need to use VHDL aliases, but this would
        // also overwrite the original variable's value.
        customObject2 = new MyClass1();
        customObject2.MyProperty += 1;
    }

    public void SelfReferencingType()
    {
        var customObject1 = new MyClass2();
        var customObject2 = new MyClass2 { SelfReference = customObject1 };
    }

    private sealed class MyClass1
    {
        public int MyProperty { get; set; }
    }

    private sealed class MyClass2
    {
        public MyClass2 SelfReference { get; set; }
    }
}
