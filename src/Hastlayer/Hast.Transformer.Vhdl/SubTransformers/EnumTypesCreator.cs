using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Declaration;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Vhdl.SubTransformers;

public class EnumTypesCreator : IEnumTypesCreator
{
    public IEnumerable<IVhdlElement> CreateEnumTypes(SyntaxTree syntaxTree)
    {
        var enumDeclarations = new List<IVhdlElement>();

        syntaxTree.AcceptVisitor(new EnumCheckingVisitor(enumDeclarations));

        return enumDeclarations;
    }

    private sealed class EnumCheckingVisitor : DepthFirstAstVisitor
    {
        private readonly List<IVhdlElement> _enumDeclarations;

        public EnumCheckingVisitor(List<IVhdlElement> enumDeclarations) => _enumDeclarations = enumDeclarations;

        public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
        {
            base.VisitTypeDeclaration(typeDeclaration);

            if (typeDeclaration.ClassType != ClassType.Enum) return;

            var values = typeDeclaration.Members.Select(member => member.GetFullName().ToExtendedVhdlIdValue());
            _enumDeclarations.Add(new Enum(values) { Name = typeDeclaration.GetFullName().ToExtendedVhdlId() });
        }
    }
}
