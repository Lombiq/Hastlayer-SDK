using Hast.Transformer.Vhdl.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hast.Transformer.Vhdl.SubTransformers;

public class MemberTransformer : IMemberTransformer
{
    private readonly IMethodTransformer _methodTransformer;
    private readonly IDisplayClassFieldTransformer _displayClassFieldTransformer;
    private readonly IPocoTransformer _pocoTransformer;

    public MemberTransformer(
        IMethodTransformer methodTransformer,
        IDisplayClassFieldTransformer displayClassFieldTransformer,
        IPocoTransformer pocoTransformer)
    {
        _methodTransformer = methodTransformer;
        _displayClassFieldTransformer = displayClassFieldTransformer;
        _pocoTransformer = pocoTransformer;
    }

    public IEnumerable<Task<IMemberTransformerResult>> TransformMembers(
        AstNode node,
        VhdlTransformationContext transformationContext,
        ICollection<Task<IMemberTransformerResult>> memberTransformerTasks = null)
    {
        memberTransformerTasks ??= new List<Task<IMemberTransformerResult>>();

        var traverseTo = node.Children;

        // If for debugging you want to make the below processing serial instead of it running in parallel then add
        // .Result to every transformation call and wrap them into Task.FromResult() methods.
        return node.NodeType switch
        {
            NodeType.Member =>
                TransformSingleMember(node, memberTransformerTasks, traverseTo, transformationContext),
            NodeType.TypeDeclaration =>
                TransformTypeDeclaration(node, memberTransformerTasks, traverseTo, transformationContext),
            _ => TransformChildMembers(memberTransformerTasks, traverseTo, transformationContext),
        };
    }

    private IEnumerable<Task<IMemberTransformerResult>> TransformSingleMember(
        AstNode node,
        ICollection<Task<IMemberTransformerResult>> memberTransformerTasks,
        IEnumerable<AstNode> traverseTo,
        VhdlTransformationContext transformationContext)
    {
        if (node is MethodDeclaration methodDeclaration)
        {
            memberTransformerTasks
                .Add(_methodTransformer.TransformAsync(methodDeclaration, transformationContext));
        }
        else if (node is FieldDeclaration fieldDeclaration &&
                 _displayClassFieldTransformer.IsDisplayClassField(fieldDeclaration))
        {
            memberTransformerTasks
                .Add(_displayClassFieldTransformer.TransformAsync(fieldDeclaration, transformationContext));
        }
        else if (!_pocoTransformer.IsSupported(node))
        {
            throw new NotSupportedException($"The member {node} is not supported for transformation.");
        }

        return TransformChildMembers(memberTransformerTasks, traverseTo, transformationContext);
    }

    private IEnumerable<Task<IMemberTransformerResult>> TransformTypeDeclaration(
        AstNode node,
        ICollection<Task<IMemberTransformerResult>> memberTransformerTasks,
        IEnumerable<AstNode> traverseTo,
        VhdlTransformationContext transformationContext)
    {
        var typeDeclaration = node as TypeDeclaration;
        switch (typeDeclaration?.ClassType)
        {
            case ClassType.Class:
            case ClassType.Struct:
                if (typeDeclaration.BaseTypes.Any(baseType => baseType.GetActualType().Kind != TypeKind.Interface))
                {
                    throw new NotSupportedException(
                        "Class inheritance is not supported. Affected class: " + node.GetFullName() + ".");
                }

                // Records need to be created only for those types that are neither display classes, nor hardware entry
                // point types or static types
                if (!typeDeclaration.GetFullName().IsDisplayOrClosureClassName() &&
                    !typeDeclaration.Members.Any(member => member.IsHardwareEntryPointMember()) &&
                    !typeDeclaration.Modifiers.HasFlag(Modifiers.Static))
                {
                    memberTransformerTasks.Add(_pocoTransformer.TransformAsync(typeDeclaration, transformationContext));
                }

                traverseTo = traverseTo.Where(n => n.NodeType is NodeType.Member or NodeType.TypeDeclaration);
                return TransformChildMembers(memberTransformerTasks, traverseTo, transformationContext);
            case ClassType.Enum:
                return memberTransformerTasks; // Enums are transformed separately.
            case ClassType.Interface:
                return memberTransformerTasks; // Interfaces are irrelevant here.
            case ClassType.RecordClass:
            default:
                return TransformChildMembers(memberTransformerTasks, traverseTo, transformationContext);
        }
    }

    private ICollection<Task<IMemberTransformerResult>> TransformChildMembers(
        ICollection<Task<IMemberTransformerResult>> memberTransformerTasks,
        IEnumerable<AstNode> traverseTo,
        VhdlTransformationContext transformationContext)
    {
        foreach (var target in traverseTo)
        {
            TransformMembers(target, transformationContext, memberTransformerTasks);
        }

        return memberTransformerTasks;
    }
}
