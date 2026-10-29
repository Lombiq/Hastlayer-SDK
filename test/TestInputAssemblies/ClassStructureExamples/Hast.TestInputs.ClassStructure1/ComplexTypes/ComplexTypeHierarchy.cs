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
    [SuppressMessage(
        "Major Bug",
        "S1145:Useless \"if(true) {...}\" and \"if(false){...}\" blocks should be removed",
        Justification = ThatsThePoint)]
    public void Interface1Method2()
    {
        //// var x = BaseClassMethod1(4);
        //// var y = x + 4;
        //// var z = x + y;

        // Fine to test transformation.
#pragma warning disable CS0162 // Unreachable code detected
        if (true)
        {
            PrivateMethod();
            StaticMethod();
        }
        else
        {
            PrivateMethod();
        }
#pragma warning restore CS0162 // Unreachable code detected
    }

    public void Interface2Method1() => BaseInterfaceMethod2();

    // Explicit interface implementation.
    [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = ThatsThePoint)]
    [SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = ThatsThePoint)]
    [SuppressMessage("Minor Code Smell", "S1481:Unused local variables should be removed", Justification = ThatsThePoint)]
    void IBaseInterface.BaseInterfaceMethod1()
    {
        // This is the point of the exercise.
        var x = 1;
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
    [SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = ThatsThePoint)]
    [SuppressMessage("Minor Code Smell", "S1481:Unused local variables should be removed", Justification = ThatsThePoint)]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = ThatsThePoint)]
    private void UnusedMethod()
    {
        var x = 1;
    }

    [SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = ThatsThePoint)]
    [SuppressMessage("Minor Code Smell", "S1481:Unused local variables should be removed", Justification = ThatsThePoint)]
    private static void StaticMethod()
    {
        var x = 1;
    }
}
