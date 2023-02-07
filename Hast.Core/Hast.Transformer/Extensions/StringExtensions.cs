using ICSharpCode.Decompiler.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace System;

public static class StringExtensions
{
    /// <summary>
    /// Creates a simple dot-delimited name for a full member name, which will include the parent types' and the
    /// wrapping namespace's name. This can be used where name prefixes are required.
    /// </summary>
    public static string ToSimpleName(this string fullName)
    {
        var simpleName = fullName.Partition(" ") is (_, " ", var afterSpace)
            ? afterSpace // Cutting off return type name.
            : fullName;

        // Cutting off everything after an opening bracket (of a method call).
        simpleName = simpleName.Partition("(").Left;

        // Changing the double colons that delimit a member access to a single dot.
        return simpleName?.Replace("::", ".");
    }

    /// <summary>
    /// Checks whether the string looks like the name of a compiler-generated class generated from an F# closure.
    /// </summary>
    /// <example>Such a name is like following: <c>Run@28</c>.</example>
    // // A class name containing "@" would be invalid in standard C#, so this is a fairly safe bet.
    public static bool IsClosureClassName(this string name) => name.RegexIsMatch(@".+\@\d+", RegexOptions.Compiled);

    /// <summary>
    /// Checks whether the string looks like the name of a compiler-generated DisplayClass from C# or one generated from
    /// an F# closure.
    /// </summary>
    /// <example>
    /// <para>Such a name is like following.</para>
    /// <code>
    /// "Hast.Samples.SampleAssembly.PrimeCalculator+&lt;&gt;c__DisplayClass9_0"
    /// "Hast.Samples.SampleAssembly.HastlayerOptimizedAlgorithm+&lt;&gt;c"
    /// Run@28
    /// </code>
    /// </example>
    public static bool IsDisplayOrClosureClassName(this string name) =>
        // A class name containing "<>" would be invalid in standard C#, so this is a fairly safe bet.
        name.Contains("+<>c") || name.IsClosureClassName();

    /// <summary>
    /// Checks whether the string looks like the name of a compiler-generated DisplayClass member.
    /// </summary>
    /// <example>
    /// <para>Such a name is like following.</para>
    /// <code>
    /// "System.UInt32[] Hast.Samples.SampleAssembly.PrimeCalculator+&lt;&gt;c__DisplayClass2::numbers"
    /// "System.UInt32 Hast.Samples.FSharpSampleAssembly.FSharpParallelAlgorithmContainer+Run@28::Invoke(System.UInt32)"
    /// </code>
    /// </example>
    public static bool IsDisplayOrClosureClassMemberName(this string name) =>
        name.IsDisplayOrClosureClassName() && name.Contains("::");

#pragma warning disable S103 // Lines should not be too long
    /// <summary>
    /// Checks whether the string looks like the name of a compiler-generated method that was created in place of a
    /// lambda expression in the original class (not in a DisplayClass).
    /// </summary>
    /// <example>
    /// <para>Such a name is like following.</para>
    /// <code>"System.Boolean Hast.Samples.SampleAssembly.PrimeCalculator::&lt;ParallelizedArePrimeNumbers2&gt;b__9_0(System.Object)"</code>
    /// <code>"Hast.Samples.SampleAssembly.ImageContrastModifier+PixelProcessingTaskOutput Hast.Samples.SampleAssembly.ImageContrastModifier::&lt;ChangeContrast&gt;b__6_0(Hast.Samples.SampleAssembly.ImageContrastModifier+PixelProcessingTaskInput)"</code>
    /// </example>
#pragma warning restore S103 // Lines should not be too long
    public static bool IsInlineCompilerGeneratedMethodName(this string name) =>
        // A name where before the "<" there is nothing is invalid in standard C#, so this is a fairly safe bet.
        name.RegexIsMatch("^.+?::<.+>.+__\\d_\\d\\(", RegexOptions.Compiled);

    /// <summary>
    /// Determines whether the string looks like the name of a compiler-generated field that backs an auto-property.
    /// </summary>
    /// <example>
    /// Such a field's name looks like "&lt;Number&gt;k__BackingField". It will contain the name of the property.
    /// </example>
    public static bool IsBackingFieldName(this string name) => name.RegexIsMatch("<(.*)>.*BackingField");

    /// <summary>
    /// Converts the full name of a property-backing auto-generated field's name to the corresponding property's name.
    /// </summary>
    /// <remarks>
    /// <para>Such a field's name looks like.</para>
    /// <code>"System.UInt32 Hast.TestInputs.Static.ConstantsUsingCases+ArrayHolder1::&lt;ArrayLength&gt;k__BackingField".</code>
    /// <para>
    /// It will contain the name of the property. This needs to be converted into the corresponding full property name.
    /// </para>
    /// <code>"System.UInt32 Hast.TestInputs.Static.ConstantsUsingCases+ArrayHolder1::ArrayLength()"</code>
    /// </remarks>
    public static string ConvertFullBackingFieldNameToPropertyName(this string name) =>
         name.ConvertSimpleBackingFieldNameToPropertyName() + "()";

    /// <summary>
    /// Converts the simple name of a property-backing auto-generated field's name to the corresponding property's name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Such a field's name looks like "&lt;Number&gt;k__BackingField". It will contain the name of the property. This
    /// needs to be converted into the corresponding simple property
    /// name: "Number".
    /// </para>
    /// </remarks>
    public static string ConvertSimpleBackingFieldNameToPropertyName(this string name) =>
         name.RegexReplace("<(.*)>.*BackingField", match => match.Groups[1].Value);

    /// <summary>
    /// Determines whether the string looks like the name of a constructor.
    /// </summary>
    public static bool IsConstructorName(this string name) => name.Contains(".ctor");

    /// <summary>
    /// Adds the full name of the given node's parent entity to the message string. Useful in exception message for
    /// example.
    /// </summary>
    public static string AddParentEntityName(this string message, AstNode node)
    {
        var parentEntity = node.FindFirstParentEntityDeclaration();
        if (parentEntity == null) return message;
        return message + " Parent entity where the affected code is: " + parentEntity.GetFullName();
    }
}
