using System.Globalization;
using System.Text;

namespace EbizReports.Core.Expressions;

internal enum TokKind { Number, String, Identifier, Op, Eof }

internal readonly record struct Token(TokKind Kind, string Text, object? Value, int Pos);

internal sealed class ExpressionLexer
{
    private readonly string _s;
    private int _i;

    public ExpressionLexer(string s) => _s = s;

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();
        while (true)
        {
            var t = Next();
            tokens.Add(t);
            if (t.Kind == TokKind.Eof)
                break;
        }
        return tokens;
    }

    private Token Next()
    {
        while (_i < _s.Length && char.IsWhiteSpace(_s[_i]))
            _i++;

        if (_i >= _s.Length)
            return new Token(TokKind.Eof, "", null, _i);

        var start = _i;
        var c = _s[_i];

        if (char.IsDigit(c) || (c == '.' && _i + 1 < _s.Length && char.IsDigit(_s[_i + 1])))
            return ReadNumber(start);

        if (c is '\'' or '"')
            return ReadString(start, c);

        if (char.IsLetter(c) || c == '_')
            return ReadIdentifier(start);

        // toán tử 2 ký tự
        if (_i + 1 < _s.Length)
        {
            var two = _s.Substring(_i, 2);
            if (two is "<=" or ">=" or "==" or "!=" or "&&" or "||" or "<>")
            {
                _i += 2;
                return new Token(TokKind.Op, two, null, start);
            }
        }

        if ("+-*/%<>()!,=".IndexOf(c) >= 0)
        {
            _i++;
            return new Token(TokKind.Op, c.ToString(), null, start);
        }

        throw new FormatException($"Ký tự không hợp lệ trong biểu thức: '{c}' (vị trí {start})");
    }

    private Token ReadNumber(int start)
    {
        var sb = new StringBuilder();
        while (_i < _s.Length && (char.IsDigit(_s[_i]) || _s[_i] == '.'))
            sb.Append(_s[_i++]);
        var text = sb.ToString();
        var value = text.Contains('.')
            ? (object)decimal.Parse(text, CultureInfo.InvariantCulture)
            : (object)long.Parse(text, CultureInfo.InvariantCulture);
        return new Token(TokKind.Number, text, value, start);
    }

    private Token ReadString(int start, char quote)
    {
        _i++; // bỏ dấu mở
        var sb = new StringBuilder();
        while (_i < _s.Length && _s[_i] != quote)
        {
            if (_s[_i] == '\\' && _i + 1 < _s.Length)
            {
                _i++;
                sb.Append(_s[_i] switch
                {
                    'n' => '\n',
                    't' => '\t',
                    _ => _s[_i],
                });
                _i++;
            }
            else
            {
                sb.Append(_s[_i++]);
            }
        }
        if (_i >= _s.Length)
            throw new FormatException($"Chuỗi chưa đóng (vị trí {start})");
        _i++; // bỏ dấu đóng
        return new Token(TokKind.String, sb.ToString(), sb.ToString(), start);
    }

    private Token ReadIdentifier(int start)
    {
        var sb = new StringBuilder();
        while (_i < _s.Length && (char.IsLetterOrDigit(_s[_i]) || _s[_i] == '_' || _s[_i] == '.'))
            sb.Append(_s[_i++]);
        var text = sb.ToString();
        if (text.Equals("true", StringComparison.OrdinalIgnoreCase))
            return new Token(TokKind.Identifier, text, true, start);
        if (text.Equals("false", StringComparison.OrdinalIgnoreCase))
            return new Token(TokKind.Identifier, text, false, start);
        if (text.Equals("null", StringComparison.OrdinalIgnoreCase))
            return new Token(TokKind.Identifier, text, null, start);
        return new Token(TokKind.Identifier, text, null, start);
    }
}
