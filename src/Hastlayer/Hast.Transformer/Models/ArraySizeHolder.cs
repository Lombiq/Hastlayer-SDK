using Hast.Common.Validation;
using Hast.Layer;
using Hast.Transformer.Configuration;
using ICSharpCode.Decompiler.CSharp.Syntax;
using Lombiq.HelpfulLibraries.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Models;

internal sealed class ArraySizeHolder : IArraySizeHolder
{
    private readonly Dictionary<string, IArraySize> _arraySizes = new();

    public ArraySizeHolder()
    {
    }

    public ArraySizeHolder(Dictionary<string, IArraySize> preConfiguredArraySizes) =>
        _arraySizes = new Dictionary<string, IArraySize>(preConfiguredArraySizes);

    public IArraySize GetSize(AstNode arrayHolder)
    {
        _arraySizes.TryGetValue(arrayHolder.GetFullName(), out var arraySize);
        return arraySize;
    }

    public void SetSize(AstNode arrayHolder, int length)
    {
        Argument.ThrowIfNull(arrayHolder, nameof(arrayHolder));

        var holderName = arrayHolder.GetFullName();

        if (_arraySizes.TryGetValue(holderName, out var existingSize))
        {
            if (existingSize.Length != length)
            {
                throw new NotSupportedException(
                    StringHelper.Concatenate(
                        $"Array sizes should be statically defined but the array stored in the array holder ",
                        $"\"{holderName}\" has multiple {nameof(length)} assigned (previously it had a ",
                        $"{nameof(length)} of {existingSize.Length} and secondly a  {nameof(length)} of {length} ",
                        $"specified). Make sure that a variable, field or property always stores an array of the ",
                        $"same size (including target variables, fields and properties when it's passed around)."));
            }

            return;
        }

        _arraySizes[holderName] = new ArraySize { Length = length };
    }

    public IArraySizeHolder Clone() => new ArraySizeHolder(_arraySizes);

    public static ArraySizeHolder FromConfiguration(IHardwareGenerationConfiguration configuration)
    {
        var preConfiguredArrayLengths = configuration
            .TransformerConfiguration()
            .ArrayLengths
            .ToDictionary(kvp => kvp.Key, kvp => (IArraySize)new ArraySize { Length = kvp.Value });
        return new ArraySizeHolder(preConfiguredArrayLengths);
    }
}
