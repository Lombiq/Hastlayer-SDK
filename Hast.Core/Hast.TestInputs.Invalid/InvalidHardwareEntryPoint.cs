namespace Hast.TestInputs.Invalid;

public class InvalidHardwareEntryPoint
{
    public int MyProperty { get; set; }

#pragma warning disable S3459 // Unassigned members should be removed
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable CS0649 // Field is never assigned to
    private int _myField;
#pragma warning restore CS0649 // Field is never assigned to
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning restore S3459 // Unassigned members should be removed

    public InvalidHardwareEntryPoint()
    {
        var x = 4;
        var y = x + 3;
    }

    public virtual void EntryPointMethod()
    {
        var x = MyProperty + _myField;
        var y = x + 3;
    }
}
