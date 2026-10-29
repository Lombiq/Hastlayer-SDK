using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;
using System.Collections.Generic;
using System.Linq;
using static Hast.Transformer.Services.ConstantValuesSubstitution.ConstantValuesSubstitutingAstProcessor;

namespace Hast.Transformer.Services.ConstantValuesSubstitution;

internal sealed class ObjectHoldersToSubstitutedConstructorsMappingVisitor : DepthFirstAstVisitor
{
    private readonly ConstantValuesSubstitutingAstProcessor _constantValuesSubstitutingAstProcessor;

    public ObjectHoldersToSubstitutedConstructorsMappingVisitor(
        ConstantValuesSubstitutingAstProcessor constantValuesSubstitutingAstProcessor) =>
        _constantValuesSubstitutingAstProcessor = constantValuesSubstitutingAstProcessor;

    public override void VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
    {
        base.VisitObjectCreateExpression(objectCreateExpression);

        // Substituting everything in the matching constructor (in its copy) and mapping that the object creation. So if
        // the ctor sets some read-only members in a static way then the resulting object's members can get those
        // substituted, without substituting them globally for all instances. This is important for bootstrapping
        // substitution if there is a circular dependency between members and constructors (e.g. a field's value set in
        // the ctor depends on a ctor argument, which in turn depends on the same field of another instance).

        if (objectCreateExpression.Parent.Is(
            assignment => assignment.Left.GetActualType()?.GetFullName() == objectCreateExpression.Type.GetFullName(),
            out AssignmentExpression parentAssignment))
        {
            var constructorDeclaration = objectCreateExpression
                .FindConstructorDeclaration(_constantValuesSubstitutingAstProcessor.TypeDeclarationLookupTable);

            if (constructorDeclaration == null) return;

            var constructorDeclarationClone = constructorDeclaration.Clone<MethodDeclaration>();

            var subConstantValuesTable = _constantValuesSubstitutingAstProcessor.ConstantValuesTable.Clone();

            foreach (var argument in objectCreateExpression.Arguments.Where(argument => argument is PrimitiveExpression))
            {
                var parameter = ConstantValueSubstitutionHelper.FindConstructorParameterForPassedExpression(
                    objectCreateExpression,
                    argument,
                    _constantValuesSubstitutingAstProcessor.TypeDeclarationLookupTable);

                subConstantValuesTable.MarkAsPotentiallyConstant(parameter, (PrimitiveExpression)argument, constructorDeclarationClone);
            }

            new ConstantValuesSubstitutingAstProcessor(
                subConstantValuesTable,
                _constantValuesSubstitutingAstProcessor.TypeDeclarationLookupTable,
                _constantValuesSubstitutingAstProcessor.ArraySizeHolder.Clone(),
                new Dictionary<string, ConstructorReference>(_constantValuesSubstitutingAstProcessor.ObjectHoldersToConstructorsMappings),
                _constantValuesSubstitutingAstProcessor.AstExpressionEvaluator,
                _constantValuesSubstitutingAstProcessor.KnownTypeLookupTable)
            .SubstituteConstantValuesInSubTree(constructorDeclarationClone, reUseOriginalConstantValuesTable: true);

            var constructorReference = new ConstructorReference
            {
                Constructor = constructorDeclarationClone,
                OriginalAssignmentTarget = parentAssignment.Left,
            };

            _constantValuesSubstitutingAstProcessor.ObjectHoldersToConstructorsMappings[parentAssignment.Left.GetFullName()] =
                constructorReference;

            // Also pass the object initialization data to the "this" reference. So if methods are called from the
            // constructor (or other objects created) then there the scope of the ctor will be accessible too.
            var thisReference = constructorDeclaration
                .FindFirstChildOfType<IdentifierExpression>(identifier => identifier.Identifier == "this");
            if (thisReference != null)
            {
                _constantValuesSubstitutingAstProcessor.ObjectHoldersToConstructorsMappings[thisReference.GetFullName()] =
                    constructorReference;
            }
        }
    }
}
