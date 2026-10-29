namespace ICSharpCode.Decompiler.CSharp.Syntax;

public static class AnnotatableExtensions
{
    public static void CopyAnnotationsTo(this IAnnotatable annotable, IAnnotatable toNode)
    {
        foreach (var annotation in annotable.Annotations)
        {
            toNode.AddAnnotation(annotation);
        }
    }

    public static T WithAnnotation<T>(this T node, object annotation)
        where T : IAnnotatable
    {
        node.AddAnnotation(annotation);
        return node;
    }

    /// <summary>
    /// Replaces all annotations with the type of the given new annotation with the supplied instance of the new
    /// annotation.
    /// </summary>
    public static TNode ReplaceAnnotations<TNode, TAnnotation>(this TNode node, TAnnotation annotation)
        where TNode : IAnnotatable
        where TAnnotation : class
    {
        node.RemoveAnnotations<TAnnotation>();
        node.AddAnnotation(annotation);

        return node;
    }
}
