using Hast.Layer;
using Hast.Transformer.Helpers;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Services;

/// <summary>
/// Converts inline object initializers into one-by-one property assignments so these can be transformed in a simpler
/// way.
/// </summary>
/// <example>
/// <code>
/// var x = new MyClass { Property1 = value1, Property2 = value2 };
///
/// will be converted to:
///
/// var x = new MyClass();
/// x.Property1 = value1;
/// x.Property2 = value2;
/// </code>
/// </example>
/// <remarks>
/// <para>
/// There is the ObjectOrCollectionInitializers decompiler option with a similar aim. However, that would unpack
/// initializations for compiler-generated methods created from closures and processing that would be painful. Also,
/// with that option a new variable is created for every instantiation even if the new object is immediately assigned to
/// an array element. So it would make the resulting code a bit messier.
/// </para>
/// </remarks>
public class ObjectInitializerExpander : IConverter
{
    public IEnumerable<string> Dependencies { get; } = new[] { nameof(DirectlyAccessedNewObjectVariablesCreator) };

    public void Convert(
        SyntaxTree syntaxTree,
        IHardwareGenerationConfiguration configuration,
        IKnownTypeLookupTable knownTypeLookupTable) =>
        syntaxTree.AcceptVisitor(new ObjectInitializerExpanderVisitor());

    private sealed class ObjectInitializerExpanderVisitor : DepthFirstAstVisitor
    {
        public override void VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
        {
            base.VisitObjectCreateExpression(objectCreateExpression);

            if (!objectCreateExpression.Initializer.Elements.Any()) return;

            // At this point there will be a parent assignment due to IDirectlyAccessedNewObjectVariablesCreator.
            var parentAssignment = objectCreateExpression
                .FindFirstParentOfType<AssignmentExpression>(assignment => assignment.Right == objectCreateExpression);

            var parentStatement = objectCreateExpression.FindFirstParentStatement();

            foreach (var initializerElement in objectCreateExpression.Initializer.Elements)
            {
                if (initializerElement is not NamedExpression namedInitializerExpression)
                {
                    throw new NotSupportedException(
                        "Object initializers can only contain named expressions (i.e. \"Name = expression\" pairs)."
                        .AddParentEntityName(objectCreateExpression));
                }

                var memberReference = new MemberReferenceExpression(parentAssignment.Left.Clone(), namedInitializerExpression.Name);
                namedInitializerExpression.CopyAnnotationsTo(memberReference);
                var propertyAssignmentStatement = new ExpressionStatement(new AssignmentExpression(
                    left: memberReference,
                    right: namedInitializerExpression.Expression.Clone()));

                AstInsertionHelper.InsertStatementAfter(parentStatement, propertyAssignmentStatement);

                initializerElement.Remove();
            }
        }
    }
}
