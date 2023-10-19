using System.Diagnostics.CodeAnalysis;

namespace Hast.TestInputs.Static;

public class ObjectUsingCases
{
    [SuppressMessage("Style", "IDE0270:Use coalesce expression", Justification = "We want to test this specific syntax.")]
    [SuppressMessage("Style", "IDE0074:Use compound assignment", Justification = "Same.")]
    [SuppressMessage("Minor Code Smell", "S3240:The simplest possible condition syntax should be used", Justification = "Same.")]
    public void NullUsage()
    {
        var customObject = new MyClass { MyProperty = 5 };
        if (customObject == null)
        {
            customObject = new MyClass();
        }

        customObject = null;

        if (customObject != null)
        {
            customObject.MyProperty = 10;
        }
    }

    public void VoidReturn(int input)
    {
        // Formerly before object support void methods apart from hardware entry points weren't useful.
        var customObject = new MyClass { MyProperty = input };
        VoidMethod(customObject);
    }

    public void ReferenceAssignment(int input)
    {
        var customObject1 = new MyClass { MyProperty = input };
        var customObject2 = customObject1;
        customObject1.MyProperty += 1;
        customObject2.MyProperty += 1;
    }

    private void VoidMethod(MyClass myClass)
    {
        // A nested if statement is needed for the return to remain in the syntax tree and not be optimized away by the
        // compiler.
        if (myClass.MyProperty < 10)
        {
            myClass.MyProperty *= 10;

            if (myClass.MyProperty == 10) return;
        }

        myClass.MyProperty = 5;
    }

    private sealed class MyClass
    {
        public int MyProperty { get; set; }
    }
}
