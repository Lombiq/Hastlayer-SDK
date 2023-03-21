namespace Hast.TestInputs.Static;

public class OptionaParametersCases
{
    public void OmittedOptionalParameters(int input)
    {
        var a = new MyClass(input);
        a.Method(input);
    }

    private sealed class MyClass
    {
#pragma warning disable S4487 // Unread "private" fields should be removed
        private int _state;
#pragma warning restore S4487 // Unread "private" fields should be removed

        public MyClass(int input, int add = 10) => _state = input + add;

        public void Method(int input, int add = 11) => _state = input + add;
    }
}
