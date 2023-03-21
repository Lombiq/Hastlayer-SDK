using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Transformer.Helpers;

public static class AstInsertionHelper
{
    public static void InsertStatementBefore<T>(Statement nextSibling, T statement)
        where T : Statement
        => InsertStatement(nextSibling, statement, before: true);

    public static void InsertStatementAfter<T>(Statement previousSibling, T statement)
        where T : Statement
        => InsertStatement(previousSibling, statement, before: false);

    private static void InsertStatement<T>(Statement adjacentSibling, T statement, bool before)
        where T : Statement
    {
        var enclosingNode = adjacentSibling.Parent;
        if (enclosingNode is BlockStatement enclosingBlock)
        {
            var statements = enclosingBlock.Statements;
            if (before) statements.InsertBefore(adjacentSibling, statement);
            else statements.InsertAfter(adjacentSibling, statement);
        }
        else
        {
            var role = new Role<T>(typeof(T).Name);
            if (before) enclosingNode.InsertChildBefore(adjacentSibling, statement, role);
            else enclosingNode.InsertChildAfter(adjacentSibling, statement, role);
        }
    }
}
