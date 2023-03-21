using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Services;

public class TypeDeclarationLookupTableFactory : ITypeDeclarationLookupTableFactory
{
    public ITypeDeclarationLookupTable Create(SyntaxTree syntaxTree)
    {
        var typeDeclarations = syntaxTree
            .GetAllTypeDeclarations()
            // Attributes can be copied into multiple assemblies having the exact same name and everything so excluding
            // them here.
            .Where(declaration => !declaration.GetActualType().IsAttribute())
            .ToDictionary(declaration => declaration.GetActualTypeFullName());

        return new TypeDeclarationLookupTable(typeDeclarations);
    }

    private sealed class TypeDeclarationLookupTable : ITypeDeclarationLookupTable
    {
        private readonly Dictionary<string, TypeDeclaration> _typeDeclarations;

        public TypeDeclarationLookupTable(Dictionary<string, TypeDeclaration> typeDeclarations) => _typeDeclarations = typeDeclarations;

        public TypeDeclaration Lookup(string fullName)
        {
            _typeDeclarations.TryGetValue(fullName, out var declaration);
            return declaration;
        }
    }
}
