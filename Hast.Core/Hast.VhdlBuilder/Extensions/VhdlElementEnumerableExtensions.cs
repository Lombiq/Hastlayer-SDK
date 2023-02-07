using Hast.VhdlBuilder.Representation;
using System.Linq;
using System.Text;

namespace System.Collections.Generic;

public static class VhdlElementEnumerableExtensions
{
    public static string ToVhdl<T>(
        this IEnumerable<T> elements,
        IVhdlGenerationOptions vhdlGenerationOptions)
        where T : IVhdlElement => elements.ToVhdl(vhdlGenerationOptions, string.Empty);

    public static string ToVhdl<T>(
        this IEnumerable<T> elements,
        IVhdlGenerationOptions vhdlGenerationOptions,
        string elementTerminator,
        string lastElementTerminator = null)
        where T : IVhdlElement
    {
        if (elements == null) return string.Empty;

        // It's efficient to run this parallelized implementation even with a low number of items (or even one) because
        // the overhead of checking whether there are more than a few elements is bigger than the below ceremony.
        var elementsList = elements.AsList();
        if (!elementsList.Any()) return string.Empty;

        var lastElement = elementsList[^1];
        var resultArray = new string[elementsList.Count];

        Threading.Tasks.Parallel.For(0, elementsList.Count - 1, i =>
        {
            resultArray[i] = elementsList[i].ToVhdl(vhdlGenerationOptions) + elementTerminator;
        });

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendJoin(string.Empty, resultArray);

        lastElementTerminator ??= elementTerminator;
        stringBuilder.Append(lastElement.ToVhdl(vhdlGenerationOptions) + lastElementTerminator);

        return stringBuilder.ToString();
    }
}
