using Hast.Layer;
using Hast.Transformer.Helpers;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Services;

/// <summary>
/// Processes binary and unary operator expressions and if the operands or the result lacks necessary explicit casts,
/// adds them.
///
/// Arithmetic binary operations in .NET should have set operand and result types, but these are not alway reflected
/// with explicit casts in the AST. The rules for determining the type of operands are similar to that in C/C++ ( <see
/// href="https://docs.microsoft.com/en-us/cpp/c-language/usual-arithmetic-conversions"/>) and are called "numeric
/// promotions" <see href="https://github.com/dotnet/csharplang/blob/master/spec/expressions.md#numeric-promotions"/>).
/// But the AST won't always contain all casts due to implicit casting. Take the following code for example:
///
/// byte a = ...;
/// byte b = ...;
/// var x = (short)(a + b)
///
/// This explicitly cast byte operation will contain implicit casts to the operands like this:
///
/// var x = (short)((int)a + (int)b)
///
/// But only the top code will be in the AST. This service adds explicit casts for all implicit ones for easier
/// processing later.
///
/// You can read about the background of why .NET works this way here:
/// https://stackoverflow.com/questions/941584/byte-byte-int-why.
/// </summary>
public class BinaryAndUnaryOperatorExpressionsCastAdjuster : IConverter
{
    public IEnumerable<string> Dependencies { get; } = new[] { nameof(ImmutableArraysToStandardArraysConverter) };

    public void Convert(
        SyntaxTree syntaxTree,
        IHardwareGenerationConfiguration configuration,
        IKnownTypeLookupTable knownTypeLookupTable) =>
        syntaxTree.AcceptVisitor(new BinaryAndUnaryOperatorExpressionsCastAdjusterVisitor(knownTypeLookupTable));

    private sealed class BinaryAndUnaryOperatorExpressionsCastAdjusterVisitor : DepthFirstAstVisitor
    {
        // Note that while shifts are missing from the list under
        // https://github.com/dotnet/csharplang/blob/master/spec/expressions.md#numeric-promotions they still get kind
        // of a numeric promotion. See the page about bitwise and shift operators
        // (https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/bitwise-and-shift-operators)
        // mentions that "When operands are of different integral types, their values are converted to the closest
        // containing integral type."
        private static readonly BinaryOperatorType[] _binaryOperatorsWithNumericPromotions = new[]
        {
            BinaryOperatorType.Add,
            BinaryOperatorType.Subtract,
            BinaryOperatorType.Multiply,
            BinaryOperatorType.Divide,
            BinaryOperatorType.Modulus,
            BinaryOperatorType.BitwiseAnd,
            BinaryOperatorType.BitwiseOr,
            BinaryOperatorType.ExclusiveOr,
            BinaryOperatorType.Equality,
            BinaryOperatorType.InEquality,
            BinaryOperatorType.GreaterThan,
            BinaryOperatorType.LessThan,
            BinaryOperatorType.GreaterThanOrEqual,
            BinaryOperatorType.LessThanOrEqual,
            BinaryOperatorType.ShiftLeft,
            BinaryOperatorType.ShiftRight,
        };

        private static readonly BinaryOperatorType[] _binaryOperatorsProducingNumericResults = new[]
        {
            BinaryOperatorType.Add,
            BinaryOperatorType.Subtract,
            BinaryOperatorType.Multiply,
            BinaryOperatorType.Divide,
            BinaryOperatorType.Modulus,
            BinaryOperatorType.BitwiseAnd,
            BinaryOperatorType.BitwiseOr,
            BinaryOperatorType.ExclusiveOr,
            BinaryOperatorType.ShiftLeft,
            BinaryOperatorType.ShiftRight,
        };

        private static readonly string[] _numericTypes = new[]
        {
            typeof(byte).FullName,
            typeof(sbyte).FullName,
            typeof(short).FullName,
            typeof(ushort).FullName,
            typeof(int).FullName,
            typeof(uint).FullName,
            typeof(long).FullName,
            typeof(ulong).FullName,
        };

        // Those types that have arithmetic, relational and bitwise operations defined for them, see:
        // https://github.com/dotnet/csharplang/blob/master/spec/expressions.md#arithmetic-operators
        private static readonly string[] _numericTypesSupportingNumericPromotionOperations = new[]
        {
            typeof(int).FullName,
            typeof(uint).FullName,
            typeof(long).FullName,
            typeof(ulong).FullName,
        };

        private static readonly UnaryOperatorType[] _unaryOperatorsWithNumericPromotions = new[]
        {
            UnaryOperatorType.Plus,
            UnaryOperatorType.Minus,
            UnaryOperatorType.BitNot,
        };

        private static readonly string[] _typesConvertedToIntInUnaryOperations = new[]
        {
            typeof(byte).FullName,
            typeof(sbyte).FullName,
            typeof(short).FullName,
            typeof(ushort).FullName,
            typeof(char).FullName,
        };

        private readonly IKnownTypeLookupTable _knownTypeLookupTable;

        public BinaryAndUnaryOperatorExpressionsCastAdjusterVisitor(IKnownTypeLookupTable knownTypeLookupTable) =>
            _knownTypeLookupTable = knownTypeLookupTable;

        // Adding implicit casts as explained here:
        // https://github.com/dotnet/csharplang/blob/master/spec/expressions.md#numeric-promotions Also handling shifts
        // where the left operand needs to be u/int or u/long, see:
        // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/bitwise-and-shift-operators

        public override void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
        {
            base.VisitBinaryOperatorExpression(binaryOperatorExpression);

            if (!_binaryOperatorsWithNumericPromotions.Contains(binaryOperatorExpression.Operator)) return;

            var leftType = binaryOperatorExpression.Left.GetActualType();
            var rightType = binaryOperatorExpression.Right.GetActualType();

            // If no type can be determined then nothing to do.
            if (leftType == null && rightType == null) return;

            leftType ??= rightType;
            rightType ??= leftType;

            var leftTypeFullName = leftType.GetFullName();
            var rightTypeFullName = rightType.GetFullName();

            if (!_numericTypes.Contains(leftTypeFullName) || !_numericTypes.Contains(rightTypeFullName)) return;

            void CastConditional(bool rightToLeft)
            {
                if (rightToLeft) ReplaceRight(leftType);
                else ReplaceLeft(rightType);
            }

            void ReplaceLeft(IType type)
            {
                binaryOperatorExpression.Left.ReplaceWith(CreateCast(type, binaryOperatorExpression.Left, out _));
                SetResultTypeReference(type);
            }

            void ReplaceRight(IType type)
            {
                binaryOperatorExpression.Right.ReplaceWith(CreateCast(type, binaryOperatorExpression.Right, out _));
                SetResultTypeReference(type);
            }

            var resultTypeReferenceIsSet = false;
            void SetResultTypeReference(IType type)
            {
                if (resultTypeReferenceIsSet) return;
                resultTypeReferenceIsSet = true;

                // Changing the result type to align it with the operands' type (it will be always the same, but only
                // for operations with numeric results, like +, -, but not for e.g. <=).
                if (!_binaryOperatorsProducingNumericResults.Contains(binaryOperatorExpression.Operator))
                {
                    return;
                }

                // We should also put a cast around it if necessary so it produces the same type as before. But only if
                // this binary operator expression is not also in another binary operator expression, when it will be
                // cast again.
                var firstNonParenthesizedExpressionParent = binaryOperatorExpression.FindFirstNonParenthesizedExpressionParent();
                if (firstNonParenthesizedExpressionParent is not CastExpression and not BinaryOperatorExpression)
                {
                    var castExpression = CreateCast(
                        binaryOperatorExpression.GetResultType(),
                        binaryOperatorExpression,
                        out var clonedBinaryOperatorExpression);
                    binaryOperatorExpression.ReplaceWith(castExpression);
                    binaryOperatorExpression = clonedBinaryOperatorExpression;
                }

                binaryOperatorExpression.ReplaceAnnotations(type.ToResolveResult());
            }

            CastOrReplaceBinaryExpression(
                binaryOperatorExpression,
                leftTypeFullName,
                rightTypeFullName,
                ReplaceLeft,
                ReplaceRight,
                CastConditional);
        }

        public override void VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression)
        {
            base.VisitUnaryOperatorExpression(unaryOperatorExpression);

            if (!_unaryOperatorsWithNumericPromotions.Contains(unaryOperatorExpression.Operator)) return;

            var type = unaryOperatorExpression.Expression.GetActualType();
            var isCast = unaryOperatorExpression.Expression is CastExpression;
            var expectedType = unaryOperatorExpression.Expression.GetActualType();

            void Replace(IType newType) =>
                unaryOperatorExpression.Expression.ReplaceWith(
                    CreateCast(newType, unaryOperatorExpression.Expression, out var _));

            if (_typesConvertedToIntInUnaryOperations.Contains(type.GetFullName()) &&
                (!isCast || expectedType.GetFullName() != typeof(int).FullName))
            {
                Replace(_knownTypeLookupTable.Lookup(KnownTypeCode.Int32));
            }
            else if (unaryOperatorExpression.Operator == UnaryOperatorType.Minus &&
                type.GetFullName() == typeof(uint).FullName &&
                (!isCast || expectedType.GetFullName() != typeof(long).FullName))
            {
                Replace(_knownTypeLookupTable.Lookup(KnownTypeCode.Int64));
            }
            else if (unaryOperatorExpression.Operator == UnaryOperatorType.Minus &&
                unaryOperatorExpression.Expression is CastExpression { Type: PrimitiveType { KnownTypeCode: KnownTypeCode.UInt32 } } castExpression)
            {
                // For an int value the AST can contain -(uint)value if the original code was (uint)-value. Fixing that
                // here.
                castExpression.ReplaceWith(castExpression.Expression);
            }
        }

        private void CastOrReplaceBinaryExpression(
            BinaryOperatorExpression binaryOperatorExpression,
            string leftTypeFullName,
            string rightTypeFullName,
            Action<IType> replaceLeft,
            Action<IType> replaceRight,
            Action<bool> castConditional)
        {
            var longFullName = typeof(long).FullName;
            var ulongFullName = typeof(ulong).FullName;
            var intFullName = typeof(int).FullName;
            var uintFullName = typeof(uint).FullName;
            var typesConvertedToLongForUint = new[] { typeof(sbyte).FullName, typeof(short).FullName, intFullName };

            // First handling shifts which are different from other affected binary operators because only the left
            // operand is promoted, and only everything below int to int.
            if (binaryOperatorExpression.Operator is BinaryOperatorType.ShiftLeft or BinaryOperatorType.ShiftRight)
            {
                if (!new[] { longFullName, ulongFullName, intFullName, uintFullName }.Contains(leftTypeFullName))
                {
                    replaceLeft(_knownTypeLookupTable.Lookup(KnownTypeCode.Int32));
                }

                return;
            }

            // Omitting decimal, double, float rules as those are not supported any way.
            if (leftTypeFullName == ulongFullName != (rightTypeFullName == ulongFullName))
            {
                castConditional(leftTypeFullName == ulongFullName);
            }
            else if (leftTypeFullName == longFullName != (rightTypeFullName == longFullName))
            {
                castConditional(leftTypeFullName == longFullName);
            }
            else if ((leftTypeFullName == uintFullName && typesConvertedToLongForUint.Contains(rightTypeFullName)) ||
                     (rightTypeFullName == uintFullName && typesConvertedToLongForUint.Contains(leftTypeFullName)))
            {
                var longType = _knownTypeLookupTable.Lookup(KnownTypeCode.Int64);
                replaceLeft(longType);
                replaceRight(longType);
            }
            else if (leftTypeFullName == uintFullName != (rightTypeFullName == uintFullName))
            {
                castConditional(leftTypeFullName == uintFullName);
            }

            // While not specified under the numeric promotions language reference section, this condition cares about
            // types that define all operators in questions. E.g. an equality check between two uints shouldn't force an
            // int cast.
            else if (leftTypeFullName != rightTypeFullName ||
                     !_numericTypesSupportingNumericPromotionOperations.Contains(leftTypeFullName))
            {
                var intType = _knownTypeLookupTable.Lookup(KnownTypeCode.Int32);
                replaceLeft(intType);
                replaceRight(intType);
            }
        }

        private static CastExpression CreateCast<T>(IType toType, T expression, out T clonedExpression)
            where T : Expression
        {
            var castExpression = new CastExpression { Type = TypeHelper.CreateAstType(toType) };

            clonedExpression = expression.Clone<T>();
            castExpression.Expression = new ParenthesizedExpression(clonedExpression);
            castExpression.Expression.AddAnnotation(expression.CreateResolveResultFromActualType());
            castExpression.AddAnnotation(toType.ToResolveResult());

            return castExpression;
        }
    }
}
