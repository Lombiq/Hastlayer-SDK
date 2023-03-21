using ICSharpCode.Decompiler.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Helpers;

internal static class MethodDeclarationFactory
{
    public static MethodDeclaration CreateMethod(
        string name,
        IEnumerable<object> annotations,
        AstNodeCollection<AttributeSection> attributes,
        IEnumerable<ParameterDeclaration> parameters,
        BlockStatement body,
        AstType returnType)
    {
        var method = new MethodDeclaration
        {
            Name = name,
        };

        foreach (var annotation in annotations)
        {
            method.AddAnnotation(annotation);
        }

        method.Attributes.AddRange(attributes.Select(attribute => attribute.Clone<AttributeSection>()));

        method.Parameters.AddRange(parameters.Select(parameter => parameter.Clone()));

        method.Body = body.Clone<BlockStatement>();
        method.ReturnType = returnType.Clone();

        return method;
    }
}
