using ICSharpCode.Decompiler.CSharp.Syntax;
using System;

namespace Hast.Transformer.Models;

/// <summary>
/// Represents the size of an array.
/// </summary>
/// <remarks>
/// <para>
/// A separate model for this so adding support for multi-dimensional arrays will be possible by extending it.
/// </para>
/// </remarks>
public interface IArraySize
{
    /// <summary>
    /// Gets the Length of the array.
    /// </summary>
    int Length { get; }
}

/// <summary>
/// Container for the sizes of statically sized arrays.
/// </summary>
public interface IArraySizeHolder
{
    /// <summary>
    /// Returns the size of the <paramref name="arrayHolder"/>.
    /// </summary>
    IArraySize GetSize(AstNode arrayHolder);

    /// <summary>
    /// Sets the <paramref name="length"/> of the <paramref name="arrayHolder"/>.
    /// </summary>
    void SetSize(AstNode arrayHolder, int length);

    /// <summary>
    /// Clones the node.
    /// </summary>
    IArraySizeHolder Clone();
}

public static class ArraySizeHolderExtensions
{
    public static IArraySize GetSizeOrThrow(this IArraySizeHolder arraySizeHolder, AstNode arrayHolder)
    {
        var size = arraySizeHolder.GetSize(arrayHolder);

        if (size == null)
        {
            throw new NotSupportedException(
                "The length of the array holder \"" + arrayHolder.GetFullName() + "\" couldn't be statically " +
                "determined. Only arrays with dimensions defined at compile-time are supported. If the array size is " +
                "actually static just Hastlayer can't figure it out for some reason then you can configure it " +
                "manually via TransformerConfiguration by using the quoted full name.");
        }

        return size;
    }
}
