using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.TypeSystem;
using Lombiq.HelpfulLibraries.Common.Utilities;
using System.Linq;

namespace Hast.Transformer.Helpers;

public static class VariableHelper
{
    public static IdentifierExpression DeclareAndReferenceArrayVariable(
        Expression valueHolder,
        AstType arrayElementAstType,
        IType arrayType)
    {
        var declarationType = new ComposedType { BaseType = arrayElementAstType.Clone() }
            .WithAnnotation(arrayType.ToResolveResult());
        declarationType.ArraySpecifiers.Add(
            new ArraySpecifier(((ArrayType)arrayType).Dimensions));

        return DeclareAndReferenceVariable("array", valueHolder, declarationType);
    }

    public static IdentifierExpression DeclareAndReferenceVariable(
        string variableNamePrefix,
        Expression valueHolder,
        AstType astType)
    {
        // Eliminate OS-specific differences caused by CRLF vs LF line endings, so we get the same hash on Windows and
        // Unix-like operating systems.
        var value = valueHolder.GetFullName().Replace("\r\n", "\n");

        return DeclareAndReferenceVariable(
            variableNamePrefix + Sha256Helper.ComputeHash(value),
            valueHolder.GetActualType(),
            astType,
            valueHolder.FindFirstParentStatement());
    }

    public static IdentifierExpression DeclareAndReferenceVariable(
        string variableName,
        IType type,
        AstType astType,
        Statement parentStatement)
    {
        var variableDeclaration = new VariableDeclarationStatement(astType.Clone(), variableName)
            .WithAnnotation(CreateILVariableResolveResult(VariableKind.Local, type, variableName));
        variableDeclaration.Variables.Single().AddAnnotation(type);
        AstInsertionHelper.InsertStatementBefore(parentStatement, variableDeclaration);

        return new IdentifierExpression(variableName)
            .WithAnnotation(CreateILVariableResolveResult(VariableKind.Local, type, variableName));
    }

    public static ILVariableResolveResult CreateILVariableResolveResult(VariableKind variableKind, IType type, string name) =>
        new(new ILVariable(variableKind, type) { Name = name });
}
