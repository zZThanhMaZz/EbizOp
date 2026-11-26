namespace EbizReports.Core.Expressions;

public abstract class ExprNode
{
}

public sealed class LiteralNode : ExprNode
{
    public object? Value { get; }
    public LiteralNode(object? value) => Value = value;
}

public sealed class IdentifierNode : ExprNode
{
    public string Name { get; }
    public IdentifierNode(string name) => Name = name;
}

public sealed class UnaryNode : ExprNode
{
    public string Op { get; }
    public ExprNode Operand { get; }
    public UnaryNode(string op, ExprNode operand)
    {
        Op = op;
        Operand = operand;
    }
}

public sealed class BinaryNode : ExprNode
{
    public string Op { get; }
    public ExprNode Left { get; }
    public ExprNode Right { get; }
    public BinaryNode(string op, ExprNode left, ExprNode right)
    {
        Op = op;
        Left = left;
        Right = right;
    }
}

public sealed class CallNode : ExprNode
{
    public string Name { get; }
    public List<ExprNode> Args { get; }
    public CallNode(string name, List<ExprNode> args)
    {
        Name = name;
        Args = args;
    }
}
