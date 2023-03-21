using Hast.Layer;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Services;

/// <summary>
/// Adjusts constructs only coming from F# and changes them into their equivalents usual in a C# AST.
/// </summary>
public class FSharpIdiosyncrasiesAdjuster : IConverter
{
    public IEnumerable<string> Dependencies { get; } = new[] { nameof(MemberIdentifiersFixer) };

    private readonly ITypeDeclarationLookupTableFactory _typeDeclarationLookupTableFactory;

    public FSharpIdiosyncrasiesAdjuster(ITypeDeclarationLookupTableFactory typeDeclarationLookupTableFactory) =>
        _typeDeclarationLookupTableFactory = typeDeclarationLookupTableFactory;

    public void Convert(
        SyntaxTree syntaxTree,
        IHardwareGenerationConfiguration configuration,
        IKnownTypeLookupTable knownTypeLookupTable) =>
        syntaxTree.AcceptVisitor(new FSharpIdiosyncrasiesAdjustingVisitor(_typeDeclarationLookupTableFactory.Create(syntaxTree)));

    private sealed class FSharpIdiosyncrasiesAdjustingVisitor : DepthFirstAstVisitor
    {
        private readonly ITypeDeclarationLookupTable _typeDeclarationLookupTable;

        public FSharpIdiosyncrasiesAdjustingVisitor(ITypeDeclarationLookupTable typeDeclarationLookupTable) =>
            _typeDeclarationLookupTable = typeDeclarationLookupTable;

        public override void VisitInvocationExpression(InvocationExpression invocationExpression)
        {
            base.VisitInvocationExpression(invocationExpression);

            var targetFullName = invocationExpression.Target.GetFullName();

            if (targetFullName == "Microsoft.FSharp.Collections.ArrayModule.ZeroCreate")
            {
                // Changing an expression like
                //      ArrayModule.ZeroCreate<Task<uint>> (280)
                // into
                //      new Task<uint>[280]

                var arrayCreateExpression = new ArrayCreateExpression();

                invocationExpression.CopyAnnotationsTo(arrayCreateExpression);
                arrayCreateExpression.Type = ((MemberReferenceExpression)invocationExpression.Target).TypeArguments.Single().Clone();
                arrayCreateExpression.Arguments.Add(invocationExpression.Arguments.Single().Clone());

                invocationExpression.ReplaceWith(arrayCreateExpression);
            }
            else if (targetFullName is "Microsoft.FSharp.Collections.ArrayModule.Get" or
                "Microsoft.FSharp.Collections.ArrayModule.Set")
            {
                var indexerExpression = new IndexerExpression();

                var arguments = invocationExpression.Arguments.ToArray();
                invocationExpression.CopyAnnotationsTo(indexerExpression);
                indexerExpression.Target = arguments[0].Clone();
                indexerExpression.Arguments.Add(arguments[1].Clone());

                if (targetFullName == "Microsoft.FSharp.Collections.ArrayModule.Get")
                {
                    // Changing an expression like
                    //      ArrayModule.Get<Task<uint>> (array, i)
                    // into
                    //      array[i]

                    invocationExpression.ReplaceWith(indexerExpression);
                }
                else if (targetFullName == "Microsoft.FSharp.Collections.ArrayModule.Set")
                {
                    // Changing an expression like
                    //      Array.set myArray i 5
                    // into
                    //      array[i] = 5

                    var assignment = new AssignmentExpression(indexerExpression, arguments[2].Clone());
                    invocationExpression.CopyAnnotationsTo(assignment);
                    invocationExpression.ReplaceWith(assignment);
                }
            }
            else if (invocationExpression.IsTaskStart())
            {
                //// The expression is something like if it was created from F# code, and this is what we need to handle:
                //// Task.Factory.StartNew ((Func<object, OutputType>)new NameOfTaskStartingMethod@35 (input).Invoke, (object)inputArgument);
                //// Such shorthand expression when created from C# look like this:
                //// Task.Factory.StartNew((Func<object, OutputType>)this.<NameOfTaskStartingMethod>b__6_0, (object)inputArgument);
                //// The F# closure receives the current value of local variables, not the latest one if in a loop.
                //// Thus to mimic how this works we'll add the parameters of the compiler-generated class' ctor as
                //// further parameters like:
                //// Task.Factory.StartNew ((Func<object, OutputType>)new Run@35 (input).Invoke, (object)inputArgument, other, arguments);
                //// This won't be correct C# but the rest of the Transformer will be tricked into passing them on.

                // Getting the member reference like: new NameOfTaskStartingMethod@35 (input).Invoke
                MemberReferenceExpression invocationMemberReference;
                invocationMemberReference = invocationExpression.Arguments.First().FindFirstChildOfType<MemberReferenceExpression>();
                if (invocationMemberReference?.MemberName == "Invoke" &&
                    invocationMemberReference.Target.GetFullName().IsClosureClassName())
                {
                    invocationExpression.Arguments
                        .AddRange(((ObjectCreateExpression)invocationMemberReference.Target).Arguments.Select(argument => argument.Clone()));

                    // Now that the parameters are added we need to add corresponding parameters to the Invoke() method
                    // as well by taking them from the ctor of the compiler-generated class.

                    var invokeMethod = (MethodDeclaration)invocationMemberReference.FindMemberDeclaration(_typeDeclarationLookupTable);
                    var ctor = (ConstructorDeclaration)invokeMethod
                        .FindFirstParentTypeDeclaration()
                        .Members.Single(member => member is ConstructorDeclaration);

                    invokeMethod.Parameters.AddRange(ctor.Parameters.Select(parameter => parameter.Clone()));

                    // Change all the references to the fields in Invoke() to the parameters.
                    invokeMethod.Body.AcceptVisitor(new ClosureClassMethodFieldReferencesChangingVisitor());

                    // Remove the now unneeded ctor and its references.
                    ctor.Remove();
                    ((ObjectCreateExpression)invocationMemberReference.Target).Arguments.Clear();
                }
            }
        }

        private sealed class ClosureClassMethodFieldReferencesChangingVisitor : DepthFirstAstVisitor
        {
            public override void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
            {
                base.VisitMemberReferenceExpression(memberReferenceExpression);

                if (memberReferenceExpression.Target is not ThisReferenceExpression) return;

                var parameterReference = new IdentifierExpression(memberReferenceExpression.MemberName);
                memberReferenceExpression.CopyAnnotationsTo(parameterReference);
                memberReferenceExpression.ReplaceWith(parameterReference);
            }
        }
    }
}
