using Hast.Transformer.Models;

namespace ICSharpCode.Decompiler.CSharp.Syntax;

internal static class ReferenceMetadataAstNodeExtensions
{
    public static bool IsReferenced(this AstNode node) => node.GetReferenceMetadata()?.IsReferenced == true;

    public static void AddReference(this AstNode node, AstNode from) => node.GetOrAddReferenceMetadata().ReferencedFrom.Add(from);

    public static void SetVisited(this AstNode node) => node.GetOrAddReferenceMetadata().WasVisited = true;

    public static bool WasVisited(this AstNode node) => node.GetOrAddReferenceMetadata().WasVisited;

    public static bool HasReferenceMetadata(this AstNode node) => node.GetReferenceMetadata() != null;

    public static DeclarationReferenceMetadata GetReferenceMetadata(this AstNode node) =>
        node?.Annotation<DeclarationReferenceMetadata>();

    public static DeclarationReferenceMetadata GetOrAddReferenceMetadata(this AstNode node)
    {
        var metadata = node.GetReferenceMetadata();
        if (metadata == null)
        {
            metadata = new DeclarationReferenceMetadata();
            node.AddAnnotation(metadata);
        }

        return metadata;
    }

    public static void RemoveReferenceMetadata(this AstNode node)
    {
        if (node.GetReferenceMetadata() != null)
        {
            node.RemoveAnnotations<DeclarationReferenceMetadata>();
        }
    }
}
