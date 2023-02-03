using Hast.Common.Interfaces;
using Hast.Transformer.Models;
using Hast.Transformer.Vhdl.Helpers;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Representation.Declaration;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;

namespace Hast.Transformer.Vhdl.SubTransformers;

/// <summary>
/// A service for converting from C# data types to VHDL data types.
/// </summary>
public interface ITypeConverter : IDependency
{
    /// <summary>
    /// Converts the given system <paramref name="type"/> to a VHDL <see cref="DataType"/>.
    /// </summary>
    DataType ConvertType(
        IType type,
        IVhdlTransformationContext context);

    /// <summary>
    /// Converts the given ICSharpCode <paramref name="type"/> to a VHDL <see cref="DataType"/>.
    /// </summary>
    DataType ConvertAstType(AstType type, IVhdlTransformationContext context);
}

public static class TypeConvertedExtensions
{
    public static DataType ConvertParameterType(
        this ITypeConverter typeConverter,
        ParameterDeclaration parameter,
        IVhdlTransformationContext context)
    {
        var parameterType = parameter.GetActualType();

        // This is an out or ref parameter.
        if (parameterType.IsByRefLike)
        {
            parameterType = ((ByReferenceType)parameterType).ElementType;
        }

        if (!parameterType.IsArray())
        {
            return typeConverter.ConvertType(parameterType, context);
        }

        return ArrayHelper.CreateArrayInstantiation(
            typeConverter.ConvertType(parameterType.GetElementType(), context),
            context.ArraySizeHolder.GetSizeOrThrow(parameter).Length);
    }
}
