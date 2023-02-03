using Hast.Transformer.Models;
using ICSharpCode.Decompiler.TypeSystem;

namespace Hast.Transformer.Services;

public class KnownTypeLookupTableFactory : IKnownTypeLookupTableFactory
{
    public IKnownTypeLookupTable Create(ICompilation compilation) => new KnownTypeLookupTable(compilation);

    private sealed class KnownTypeLookupTable : IKnownTypeLookupTable
    {
        private readonly ICompilation _compilation;

        public KnownTypeLookupTable(ICompilation compilation) => _compilation = compilation;

        public IType Lookup(KnownTypeCode typeCode) => _compilation.FindType(typeCode);
    }
}
