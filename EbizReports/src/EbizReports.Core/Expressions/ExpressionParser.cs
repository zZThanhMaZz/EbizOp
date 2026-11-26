namespace EbizReports.Core.Expressions;

/// <summary>Bộ phân tích biểu thức kiểu recursive-descent (không eval code động).</summary>
public sealed class ExpressionParser
{
    private readonly List<Token> _tokens;
    private int _p;

    private ExpressionParser(List<Token> tokens) => _tokens = tokens;

    public static ExprNode Parse(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return new LiteralNode(null);

        var tokens = new ExpressionLexer(expression).Tokenize();
        var parser = new ExpressionParser(tokens);
        var node = parser.ParseOr();
        parser.Expect(TokKind.Eof);
        return node;
    }

    private Token Cur => _tokens[_p];

    private bool IsOp(string op) => Cur.Kind == TokKind.Op && Cur.Text == op;

    private Token Next() => _tokens[_p++];

    private void Expect(TokKind kind)
    {
        if (Cur.Kind != kind)
            throw new FormatException($"Biểu thức sai gần '{Cur.Text}' (vị trí {Cur.Pos})");
        _p++;
    }

    private ExprNode ParseOr()
    {
        var left = ParseAnd();
        while (IsOp("||"))
        {
            Next();
            left = new BinaryNode("||", left, ParseAnd());
        }
        return left;
    }

    private ExprNode ParseAnd()
    {
        var left = ParseComparison();
        while (IsOp("&&"))
        {
            Next();
            left = new BinaryNode("&&", left, ParseComparison());
        }
        return left;
    }

    private ExprNode ParseComparison()
    {
        var left = ParseAdditive();
        while (Cur.Kind == TokKind.Op && Cur.Text is "<" or "<=" or ">" or ">=" or "==" or "!=" or "<>")
        {
            var op = Next().Text;
            left = new BinaryNode(op, left, ParseAdditive());
        }
        return left;
    }

    private ExprNode ParseAdditive()
    {
        var left = ParseMultiplicative();
        while (IsOp("+") || IsOp("-"))
        {
            var op = Next().Text;
            left = new BinaryNode(op, left, ParseMultiplicative());
        }
        return left;
    }

    private ExprNode ParseMultiplicative()
    {
        var left = ParseUnary();
        while (IsOp("*") || IsOp("/") || IsOp("%"))
        {
            var op = Next().Text;
            left = new BinaryNode(op, left, ParseUnary());
        }
        return left;
    }

    private ExprNode ParseUnary()
    {
        if (IsOp("-") || IsOp("!") || IsOp("+"))
        {
            var op = Next().Text;
            return new UnaryNode(op, ParseUnary());
        }
        return ParsePostfix();
    }

    private ExprNode ParsePostfix()
    {
        var node = ParsePrimary();
        // gọi hàm: identifier '(' args ')'
        if (node is IdentifierNode id && IsOp("("))
        {
            Next();
            var args = new List<ExprNode>();
            if (!IsOp(")"))
            {
                args.Add(ParseOr());
                while (IsOp(","))
                {
                    Next();
                    args.Add(ParseOr());
                }
            }
            if (!IsOp(")"))
                throw new FormatException($"Thiếu dấu ')' trong lời gọi hàm {id.Name} (vị trí {Cur.Pos})");
            Next();
            return new CallNode(id.Name, args);
        }
        return node;
    }

    private ExprNode ParsePrimary()
    {
        if (IsOp("("))
        {
            Next();
            var inner = ParseOr();
            if (!IsOp(")"))
                throw new FormatException($"Thiếu dấu ')' (vị trí {Cur.Pos})");
            Next();
            return inner;
        }

        var t = Cur;
        switch (t.Kind)
        {
            case TokKind.Number:
                Next();
                return new LiteralNode(t.Value);
            case TokKind.String:
                Next();
                return new LiteralNode(t.Value);
            case TokKind.Identifier:
                Next();
                if (t.Text.Equals("true", StringComparison.OrdinalIgnoreCase))
                    return new LiteralNode(true);
                if (t.Text.Equals("false", StringComparison.OrdinalIgnoreCase))
                    return new LiteralNode(false);
                if (t.Text.Equals("null", StringComparison.OrdinalIgnoreCase))
                    return new LiteralNode(null);
                return new IdentifierNode(t.Text);
            default:
                throw new FormatException($"Biểu thức không mong đợi tại '{t.Text}' (vị trí {t.Pos})");
        }
    }
}
