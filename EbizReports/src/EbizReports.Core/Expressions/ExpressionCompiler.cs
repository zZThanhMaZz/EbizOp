using System.Collections.Concurrent;
using EbizReports.Core.Data;

namespace EbizReports.Core.Expressions;

/// <summary>Biên dịch biểu thức thành AST và cache lại để dùng nhiều lần.</summary>
public static class ExpressionCompiler
{
    private static readonly ConcurrentDictionary<string, ExprNode> Cache = new();

    public static ExprNode Compile(string expression) =>
        Cache.GetOrAdd(expression ?? "", ExpressionParser.Parse);

    public static object? Evaluate(string expression, EvalScope scope) =>
        ExpressionEvaluator.Evaluate(Compile(expression), scope);

    public static string EvaluateText(string expression, EvalScope scope) =>
        ValueFormatter.Format(Evaluate(expression, scope), null);
}
