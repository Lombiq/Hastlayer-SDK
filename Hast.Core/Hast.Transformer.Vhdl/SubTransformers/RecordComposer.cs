using Hast.Transformer.Vhdl.Helpers;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation.Declaration;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;

namespace Hast.Transformer.Vhdl.SubTransformers;

public class RecordComposer : IRecordComposer
{
    // Needs Lazy because unfortunately TypeConverter and RecordComposer depend on each other.
    private readonly Lazy<IDeclarableTypeCreator> _declarableTypeCreatorLazy;

    private readonly IMemoryCache _memoryCache;

    public RecordComposer(IMemoryCache memoryCache, Lazy<IDeclarableTypeCreator> declarableTypeCreatorLazy)
    {
        _memoryCache = memoryCache;
        _declarableTypeCreatorLazy = declarableTypeCreatorLazy;
    }

    public bool IsSupported(AstNode node) => node is PropertyDeclaration or FieldDeclaration;

    public NullableRecord CreateRecordFromType(TypeDeclaration typeDeclaration, IVhdlTransformationContext context)
    {
        // Using transient caching because when processing an assembly all references to a class or struct will result
        // in the record being composed.
        var typeFullName = typeDeclaration.GetFullName();

        return _memoryCache.GetOrCreate("ComposedRecord." + typeFullName, _ =>
        {
            var recordName = typeFullName.ToExtendedVhdlId();

            // Process only those fields that aren't backing fields of auto-properties (since those properties are
            // handled as properties).
            var recordFields = typeDeclaration.Members
                .Where(member =>
                    (member is PropertyDeclaration ||
                    member.Is<FieldDeclaration>(field => !field.GetFullName().IsBackingFieldName())) &&
                    !member.GetActualType().IsSimpleMemory())
                .Select(member =>
                {
                    var name = member.Name;

                    if (member is FieldDeclaration declaration)
                    {
                        var variable = declaration.Variables.Single();
                        name = variable.Name;
                    }

                    // If the field stores an instance of this type then we shouldn't declare that, otherwise we'd get a
                    // stack overflow. Since it's not valid in VHDL ("[Synth 8-4702] element type of the record element
                    // is same as the parent record type" in Vivado) we shouldn't even allow it. This won't help against
                    // having a type that contains this type, so indirect circular type dependency.
                    var fieldDataType = member.ReturnType.GetFullName() == typeFullName
                        ? throw new NotSupportedException(
                            "A type referencing itself in its properties or fields (like in a linked list " +
                            "implementation) is not supported. The member " + member.GetFullName() +
                            " references its parent type.".AddParentEntityName(member))
                        : _declarableTypeCreatorLazy.Value.CreateDeclarableType(member, member.ReturnType, context);

                    return new RecordField
                    {
                        DataType = fieldDataType,
                        Name = name.ToExtendedVhdlId(),
                    };
                });

            return RecordHelper.CreateNullableRecord(recordName, recordFields);
        });
    }
}
