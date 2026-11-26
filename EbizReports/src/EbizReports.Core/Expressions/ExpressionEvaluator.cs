using System.Collections;
using System.Globalization;
using EbizReports.Core.Data;

namespace EbizReports.Core.Expressions;

public sealed class EvalScope
{
    public required ReportData Data { get; init; }
    public IReadOnlyList<Dictionary<string, object?>> Rows { get; init; } =
        Array.Empty<Dictionary<string, object?>>();
    public IReadOnlyDictionary<string, object?>? Row { get; init; }
    public int Page { get; init; }
    public int Pages { get; init; }
}

public static class ExpressionEvaluator
{
    public static object? Evaluate(ExprNode node, EvalScope scope) => node switch
    {
        LiteralNode l => l.Value,
        IdentifierNode id => ResolveIdentifier(id.Name, scope),
        UnaryNode u => EvalUnary(u, scope),
        BinaryNode b => EvalBinary(b, scope),
        CallNode c => EvalCall(c, scope),
        _ => null,
    };

    private static object? ResolveIdentifier(string name, EvalScope scope)
    {
        if (scope.Row != null && scope.Row.TryGetValue(name, out var rowVal))
            return rowVal;
        return scope.Data.Resolve(name, scope.Row);
    }

    private static object? EvalUnary(UnaryNode u, EvalScope scope)
    {
        var v = Evaluate(u.Operand, scope);
        return u.Op switch
        {
            "-" => -Num(v),
            "+" => Num(v),
            "!" => !Truthy(v),
            _ => null,
        };
    }

    private static object? EvalBinary(BinaryNode b, EvalScope scope)
    {
        if (b.Op == "&&")
            return Truthy(Evaluate(b.Left, scope)) && Truthy(Evaluate(b.Right, scope));
        if (b.Op == "||")
            return Truthy(Evaluate(b.Left, scope)) || Truthy(Evaluate(b.Right, scope));

        var l = Evaluate(b.Left, scope);
        var r = Evaluate(b.Right, scope);

        switch (b.Op)
        {
            case "+":
                if (l is string || r is string)
                    return Str(l) + Str(r);
                return Num(l) + Num(r);
            case "-":
                return Num(l) - Num(r);
            case "*":
                return Num(l) * Num(r);
            case "/":
                var d = Num(r);
                return d == 0 ? null : Num(l) / d;
            case "%":
                var m = Num(r);
                return m == 0 ? null : Num(l) % m;
            case "==":
                return Compare(l, r) == 0;
            case "!=":
            case "<>":
                return Compare(l, r) != 0;
            case "<":
                return Compare(l, r) < 0;
            case "<=":
                return Compare(l, r) <= 0;
            case ">":
                return Compare(l, r) > 0;
            case ">=":
                return Compare(l, r) >= 0;
            default:
                return null;
        }
    }

    private static object? EvalCall(CallNode c, EvalScope scope)
    {
        switch (c.Name.ToLowerInvariant())
        {
            case "sum":
                return Aggregate(scope, c, "sum");
            case "avg":
                return Aggregate(scope, c, "avg");
            case "count":
                return scope.Rows.Count;
            case "min":
                return Aggregate(scope, c, "min");
            case "max":
                return Aggregate(scope, c, "max");
            case "iif":
                return c.Args.Count == 3
                    ? (Truthy(Evaluate(c.Args[0], scope))
                        ? Evaluate(c.Args[1], scope)
                        : Evaluate(c.Args[2], scope))
                    : null;
            case "upper":
                return Arg(c, 0, scope) is { } uv ? Str(uv).ToUpperInvariant() : null;
            case "lower":
                return Arg(c, 0, scope) is { } lv ? Str(lv).ToLowerInvariant() : null;
            case "trim":
                return Arg(c, 0, scope) is { } tv ? Str(tv).Trim() : null;
            case "concat":
                return string.Concat(c.Args.Select(a => Str(Evaluate(a, scope))));
            case "abs":
                return Math.Abs(Num(Arg(c, 0, scope)));
            case "round":
                return Math.Round(Num(Arg(c, 0, scope)), (int)Num(Arg(c, 1, scope)));
            case "format":
                return ValueFormatter.Format(Arg(c, 0, scope), Arg(c, 1, scope) is { } f ? Str(f) : null);
            case "today":
                return DateTime.Today;
            case "now":
                return DateTime.Now;
            case "len":
                return Str(Arg(c, 0, scope)).Length;
            case "page":
                return scope.Page;
            case "pages":
                return scope.Pages;
            default:
                throw new FormatException($"Hàm không hỗ trợ: {c.Name}");
        }
    }

    private static object? Arg(CallNode c, int index, EvalScope scope) =>
        index < c.Args.Count ? Evaluate(c.Args[index], scope) : null;

    private static object? Aggregate(EvalScope scope, CallNode c, string kind)
    {
        var values = new List<decimal>();
        foreach (var row in scope.Rows)
        {
            var rowScope = new EvalScope { Data = scope.Data, Rows = scope.Rows, Row = row, Page = scope.Page, Pages = scope.Pages };
            var v = Evaluate(c.Args[0], rowScope);
            if (v == null)
                continue;
            values.Add(Num(v));
        }

        return kind switch
        {
            "sum" => values.Sum(),
            "avg" => values.Count == 0 ? 0m : values.Sum() / values.Count,
            "min" => values.Count == 0 ? 0m : values.Min(),
            "max" => values.Count == 0 ? 0m : values.Max(),
            _ => 0m,
        };
    }

    private static decimal Num(object? v) => v switch
    {
        null => 0m,
        decimal d => d,
        double db => (decimal)db,
        float f => (decimal)f,
        int i => i,
        long l => l,
        short s => s,
        byte b => b,
        bool bo => bo ? 1m : 0m,
        string str => decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var r)
            ? r
            : decimal.TryParse(str, NumberStyles.Any, CultureInfo.GetCultureInfo("vi-VN"), out var r2) ? r2 : 0m,
        _ => 0m,
    };

    private static string Str(object? v) => v switch
    {
        null => "",
        DateTime dt => dt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.GetCultureInfo("vi-VN")),
        _ => v.ToString() ?? "",
    };

    private static bool Truthy(object? v) => v switch
    {
        null => false,
        bool b => b,
        decimal d => d != 0,
        double db => db != 0,
        int i => i != 0,
        long l => l != 0,
        string s => s.Length > 0 && !s.Equals("false", StringComparison.OrdinalIgnoreCase),
        _ => true,
    };

    private static int Compare(object? l, object? r)
    {
        if (l == null && r == null) return 0;
        if (l == null) return -1;
        if (r == null) return 1;

        if (IsNumeric(l) && IsNumeric(r))
            return Num(l).CompareTo(Num(r));

        if (l is DateTime ld && r is DateTime rd)
            return ld.CompareTo(rd);

        return string.Compare(Str(l), Str(r), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNumeric(object v) =>
        v is decimal or double or float or int or long or short or byte;
}
