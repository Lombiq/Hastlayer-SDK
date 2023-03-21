using Hast.Layer;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace Hast.Transformer.Services;

public class CustomConverter : IConverter
{
    private readonly List<string> _dependencies = new();

    public string Name { get; set; }
    public IEnumerable<string> Dependencies => _dependencies;

    public Action<SyntaxTree, IHardwareGenerationConfiguration, IKnownTypeLookupTable> ConverterAction { get; set; }

    public void Convert(
        SyntaxTree syntaxTree,
        IHardwareGenerationConfiguration configuration,
        IKnownTypeLookupTable knownTypeLookupTable) =>
        ConverterAction?.Invoke(syntaxTree, configuration, knownTypeLookupTable);

    public CustomConverter WithDependency<T>() => WithDependency(typeof(T).Name);

    public CustomConverter WithDependency(string name)
    {
        _dependencies.Add(name);
        return this;
    }

    public void AddToDictionary(IDictionary<string, IConverter> dictionary) => dictionary.Add(Name!, this);
}
