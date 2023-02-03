using System.Diagnostics.CodeAnalysis;
using static Hast.TestInputs.Base.SuppressionConstants;

namespace Hast.TestInputs.ClassStructure1.ComplexTypes;

/// <summary>
/// A type demonstrating a "complex" type hierarchy with base classes and interfaces.
/// </summary>
// Class inheritance is not yet supported.
public class ComplexTypeHierarchy : /*BaseClass,*/ IInterface1, IInterface2
{
    // Explicit interface implementation.
    [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = ThatsThePoint)]
    void IInterface1.Interface1Method1() => PrivateMethod();

    // Implicit interface implementation.
    public void Interface1Method2(bool isTrue)
    {
        //// var x = BaseClassMethod1(4);
        //// var y = x + 4;
        //// var z = x + y;

        if (isTrue)
        {
            PrivateMethod();
            StaticMethod();
        }
        else
        {
            PrivateMethod();
        }
    }

    public void Interface2Method1() => BaseInterfaceMethod2();

    // Explicit interface implementation.
    [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = ThatsThePoint)]
    void IBaseInterface.BaseInterfaceMethod1()
    {
        // Intentionally blank, anything here would be optimized out by the compiler anyway, this being a blank and pure
        // method.
    }

    public void BaseInterfaceMethod2() => StaticMethod();

    // A method that can't be a hardware interface since it's not a public virtual or an interface-declared method.
    public void NonVirtualNonInterfaceMehod() => PrivateMethod();

    ////  A generic method. Not yet supported.
    //// public virtual void GenericMethod<T>(T input)
    //// {
    ////     var z = input;
    ////     var y = z;
    //// }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = ThatsThePoint)]
    private void PrivateMethod() => StaticMethod();

    // Method not referenced anywhere.
    [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = ThatsThePoint)]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = ThatsThePoint)]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = ThatsThePoint)]
    private void UnusedMethod()
    {
        // Intentionally blank, anything here would be optimized out by the compiler anyway, this being a blank and pure
        // method.
    }

    private static void StaticMethod()
    {
        // Intentionally blank, anything here would be optimized out by the compiler anyway, this being a blank and pure
        // method.
    }
}
