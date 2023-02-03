using Hast.Transformer.Helpers;
using Hast.Transformer.Vhdl.Helpers;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation.Declaration;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;
using System;
using System.Linq;
using System.Threading.Tasks;
using ArrayType = Hast.VhdlBuilder.Representation.Declaration.ArrayType;
using Enum = Hast.VhdlBuilder.Representation.Declaration.Enum;

namespace Hast.Transformer.Vhdl.SubTransformers;

public class TypeConverter : ITypeConverter
{
    private readonly IRecordComposer _recordComposer;

    public TypeConverter(IRecordComposer recordComposer) => _recordComposer = recordComposer;

    public DataType ConvertType(
        IType type,
        IVhdlTransformationContext context)
    {
        var convertType = type.GetFullName() switch
        {
            "System.Boolean" => ConvertPrimitive(KnownTypeCode.Boolean),
            "System.Byte" => ConvertPrimitive(KnownTypeCode.Byte),
            "System.Char" => ConvertPrimitive(KnownTypeCode.Char),
            "System.Decimal" => ConvertPrimitive(KnownTypeCode.Decimal),
            "System.Double" => ConvertPrimitive(KnownTypeCode.Double),
            "System.Int16" => ConvertPrimitive(KnownTypeCode.Int16),
            "System.Int32" => ConvertPrimitive(KnownTypeCode.Int32),
            "System.Int64" => ConvertPrimitive(KnownTypeCode.Int64),
            "System.Object" => ConvertPrimitive(KnownTypeCode.Object),
            "System.SByte" => ConvertPrimitive(KnownTypeCode.SByte),
            "System.String" => ConvertPrimitive(KnownTypeCode.String),
            "System.UInt16" => ConvertPrimitive(KnownTypeCode.UInt16),
            "System.UInt32" => ConvertPrimitive(KnownTypeCode.UInt32),
            "System.UInt64" => ConvertPrimitive(KnownTypeCode.UInt64),
            "System.Void" => ConvertPrimitive(KnownTypeCode.Void),
            _ => null,
        };
        if (convertType != null) return convertType;

        if (type.IsArray())
        {
            return CreateArrayType(ConvertType(type.GetElementType(), context));
        }

        if (IsTaskType(type))
        {
            if (type is ParameterizedType parameterizedType)
            {
                return ConvertType(parameterizedType.TypeArguments.Single(), context);
            }

            return SpecialTypes.Task;
        }

        // This type is a value type but was passed as reference explicitly.
        if (type.IsByRefLike && type.Name.EndsWithOrdinal("&"))
        {
            return ConvertType(type.GetElementType(), context);
        }

        return ConvertTypeInternal(type, context);
    }

    public DataType ConvertAstType(AstType type, IVhdlTransformationContext context) =>
        type switch
        {
            PrimitiveType primitiveType => ConvertPrimitive(primitiveType.KnownTypeCode),
            ComposedType composedType =>
                // For inner classes (member types) the BaseType will contain the actual type (in a strange way the
                // actual type will be the BaseType of itself...).
                type.GetFullName() == composedType.BaseType.GetFullName()
                    ? ConvertAstType(composedType.BaseType, context)
                    : ConvertComposed(composedType, context),
            SimpleType simpleType => ConvertSimple(simpleType, context),
            _ => ConvertTypeInternal(type.GetActualType(), context),
        };

    private static DataType ConvertPrimitive(KnownTypeCode typeCode) =>
        typeCode switch
        {
            KnownTypeCode.Boolean => KnownDataTypes.Boolean,
            KnownTypeCode.Byte => KnownDataTypes.UInt8,
            KnownTypeCode.Char => KnownDataTypes.Character,
            KnownTypeCode.Double => KnownDataTypes.Real,
            KnownTypeCode.Int16 => KnownDataTypes.Int16,
            KnownTypeCode.Int32 => KnownDataTypes.Int32,
            KnownTypeCode.Int64 => KnownDataTypes.Int64,
            KnownTypeCode.Object => KnownDataTypes.StdLogicVector32,
            KnownTypeCode.SByte => KnownDataTypes.Int8,
            KnownTypeCode.String => KnownDataTypes.UnrangedString,
            KnownTypeCode.UInt16 => KnownDataTypes.UInt16,
            KnownTypeCode.UInt32 => KnownDataTypes.UInt32,
            KnownTypeCode.UInt64 => KnownDataTypes.UInt64,
            KnownTypeCode.Void => KnownDataTypes.Void,
            _ => throw new NotSupportedException($"The type \"{typeCode}\" is not supported for transforming."),
        };

    private DataType ConvertComposed(ComposedType type, IVhdlTransformationContext context)
    {
        if (type.IsArray())
        {
            return CreateArrayType(ConvertAstType(type.BaseType, context));
        }

        // If the type is used in an array initialization and is a non-primitive type then the actual type will be the
        // only child.
        if (type.Children.SingleOrDefault() is SimpleType simpleType)
        {
            return ConvertSimple(simpleType, context);
        }

        // If the type is used in an array initialization and is a primitive type then the actual type will be the
        // BaseType.
        if (type.BaseType is PrimitiveType primitiveType)
        {
            return ConvertPrimitive(primitiveType.KnownTypeCode);
        }

        throw new NotSupportedException("The type " + type + " is not supported for transforming.");
    }

    private DataType ConvertSimple(SimpleType type, IVhdlTransformationContext context)
    {
        if (type.Identifier != nameof(Task) || !IsTaskType(type.GetActualType()))
        {
            return ConvertTypeInternal(type.GetActualType(), context);
        }

        if (type.TypeArguments.Count != 1) return SpecialTypes.Task;

        if (!IsTaskType(type.GetActualType())) return ConvertAstType(type.TypeArguments.Single(), context);

        // Changing e.g. Task<bool> to bool. Then it will be handled later what to do with the Task.
        if (!type.TypeArguments.Single().IsArray()) return ConvertAstType(type.TypeArguments.Single(), context);

        try
        {
            ExceptionHelper.ThrowOnlySingleDimensionalArraysSupporterException(type);
        }
        catch (Exception ex)
        {
            throw new NotSupportedException(
                $"Tasks can't return arrays as that would result in multi-dimensional arrays which is not " +
                $"supported. Affected type: {type}.",
                ex);
        }

        return ConvertAstType(type.TypeArguments.Single(), context);
    }

    private DataType ConvertTypeInternal(IType type, IVhdlTransformationContext context)
    {
        if (type.IsEnum())
        {
            return new Enum { Name = type.GetFullName().ToExtendedVhdlId() };
        }

        if (type.IsClass() || type.IsStruct())
        {
            var typeDeclaration = context.TypeDeclarationLookupTable.Lookup(type.GetFullName());

            if (typeDeclaration == null) ExceptionHelper.ThrowDeclarationNotFoundException(type.GetFullName());

            return _recordComposer.CreateRecordFromType(typeDeclaration, context);
        }

        throw new NotSupportedException(
            "The type " + type.GetFullName() + " is not supported for transforming.");
    }

    private static DataType CreateArrayType(DataType elementType) =>
        new ArrayType
        {
            ElementType = elementType,
            Name = ArrayHelper.CreateArrayTypeName(elementType),
        };

    private static bool IsTaskType(IType type) =>
        type != null && type.GetFullName().StartsWithOrdinal(typeof(Task).FullName!);
}
