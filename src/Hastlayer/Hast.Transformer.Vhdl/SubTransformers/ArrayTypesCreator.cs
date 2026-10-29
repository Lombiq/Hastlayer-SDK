using Hast.Transformer.Helpers;
using Hast.Transformer.Vhdl.Helpers;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Representation.Declaration;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System.Collections.Generic;

namespace Hast.Transformer.Vhdl.SubTransformers;

public class ArrayTypesCreator : IArrayTypesCreator
{
    private readonly ITypeConverter _typeConverter;

    public ArrayTypesCreator(ITypeConverter typeConverter) => _typeConverter = typeConverter;

    public IEnumerable<ArrayType> CreateArrayTypes(SyntaxTree syntaxTree, IVhdlTransformationContext context)
    {
        var arrayDeclarations = new Dictionary<string, ArrayType>();

        // This will also find such arrays in DisplayClasses, added for parallel code by the C# compiler:
        //// [Nullable (new byte[] { 0, 2 })]
        //// public Func<object, bool> <>9__0;
        // While having such unnecessary array declarations in VHDL is not nice, it really doesn't matter enough to add
        // an error-prone heuristic to try to filter them out.

        syntaxTree.AcceptVisitor(new ArrayCreationCheckingVisitor(_typeConverter, arrayDeclarations, context));

        return arrayDeclarations.Values;
    }

    private sealed class ArrayCreationCheckingVisitor : DepthFirstAstVisitor
    {
        private readonly ITypeConverter _typeConverter;
        private readonly Dictionary<string, ArrayType> _arrayDeclarations;
        private readonly IVhdlTransformationContext _context;

        public ArrayCreationCheckingVisitor(
            ITypeConverter typeConverter,
            Dictionary<string, ArrayType> arrayDeclarations,
            IVhdlTransformationContext context)
        {
            _typeConverter = typeConverter;
            _arrayDeclarations = arrayDeclarations;
            _context = context;
        }

        public override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            base.VisitAssignmentExpression(assignmentExpression);

            if (SimpleMemoryAssignmentHelper.IsRead4BytesAssignment(assignmentExpression) ||
                SimpleMemoryAssignmentHelper.IsBatchedReadAssignment(assignmentExpression, out var _))
            {
                // ArrayType would be a clashing name so need to use GetElementType() as normal method.
                CreateArrayDeclarationIfNew(TypeHelper
                    .CreateAstType(ICSharpCode.Decompiler.TypeSystem.TypeExtensions
                        .GetElementType(assignmentExpression.GetActualType())));
            }
        }

        public override void VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression)
        {
            base.VisitArrayCreateExpression(arrayCreateExpression);

            CreateArrayDeclarationIfNew(arrayCreateExpression.GetElementType());
        }

        private void CreateArrayDeclarationIfNew(AstType elementAstType)
        {
            var elementType = _typeConverter.ConvertAstType(elementAstType, _context);

            var typeName = ArrayHelper.CreateArrayTypeName(elementType);

            if (_arrayDeclarations.ContainsKey(typeName)) return;

            _arrayDeclarations[typeName] = new ArrayType
            {
                ElementType = elementType,
                Name = typeName,
            };
        }
    }
}
