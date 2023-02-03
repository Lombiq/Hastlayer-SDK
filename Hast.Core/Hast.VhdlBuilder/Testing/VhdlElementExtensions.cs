using Hast.VhdlBuilder.Representation;
using System;
using System.Linq.Expressions;

namespace Hast.VhdlBuilder.Testing;

public static class VhdlElementExtensions
{
    public static bool ShouldBe<T>(this IVhdlElement element)
        where T : class, IVhdlElement => element.ShouldBe<T>(predicate: null);

    public static bool ShouldBe<T>(this IVhdlElement element, Expression<Func<T, bool>> predicate)
        where T : class, IVhdlElement
    {
        if (element.Is(predicate)) return true;

        throw new VhdlStructureAssertionFailedException(
            $"The {nameof(element)} is not '{typeof(T).Name}'{(predicate == null ? "." : $" matching {predicate}.")}",
            element.ToVhdl(VhdlGenerationOptions.Debug));
    }

    public static bool Is<T>(this IVhdlElement element)
        where T : class, IVhdlElement => element.Is<T>(null);

    public static bool Is<T>(this IVhdlElement element, Expression<Func<T, bool>> predicate)
        where T : class, IVhdlElement =>
        element is T target && (predicate == null || predicate.Compile()(target));
}
