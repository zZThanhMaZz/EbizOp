using System.Globalization;
using EbizReports.Core.Data;
using EbizReports.Core.Expressions;
using EbizReports.Core.Model;

namespace EbizReports.Core.Tables;

/// <summary>Tính giá trị ô và dòng tổng hợp cho item Bảng (Kind = Table).</summary>
public static class TableHelper
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    /// <summary>
    /// Bộ dòng cho một item bảng theo <see cref="TableSpec.Scope"/>:
    /// "all" → toàn bộ dòng detail của báo cáo; mặc định ("group") → dòng của ngữ cảnh hiện tại
    /// (nhóm khi nằm trong GroupHeader/GroupFooter/Detail).
    /// </summary>
    public static IReadOnlyList<Dictionary<string, object?>> RowsFor(TableSpec? spec,
        IReadOnlyList<Dictionary<string, object?>> contextRows, ReportData data)
        => spec is not null && spec.Scope.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? data.Details
            : contextRows;

    /// <summary>Giá trị một ô theo dòng hiện tại (Binding ưu tiên, rồi Expression).</summary>
    public static string CellText(TableColumn col, ReportData data,
        IReadOnlyList<Dictionary<string, object?>> rows, IReadOnlyDictionary<string, object?>? row)
    {
        if (!string.IsNullOrWhiteSpace(col.Binding))
            return ValueFormatter.Format(data.Resolve(col.Binding!, row), col.Format);

        if (!string.IsNullOrWhiteSpace(col.Expression))
        {
            try
            {
                var scope = new EvalScope { Data = data, Rows = rows, Row = row };
                return ValueFormatter.Format(ExpressionCompiler.Evaluate(col.Expression!, scope), col.Format);
            }
            catch
            {
                return "";
            }
        }
        return "";
    }

    /// <summary>Giá trị dòng tổng hợp cho một cột (rỗng nếu Aggregate = none).</summary>
    public static string FooterText(TableColumn col, ReportData data,
        IReadOnlyList<Dictionary<string, object?>> rows)
    {
        var agg = (col.Aggregate ?? "none").Trim().ToLowerInvariant();
        if (agg == "none")
            return "";
        if (agg == "count")
            return rows.Count.ToString("N0", Vi);

        var vals = new List<decimal>();
        foreach (var row in rows)
        {
            object? v = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(col.Binding))
                    v = data.Resolve(col.Binding!, row);
                else if (!string.IsNullOrWhiteSpace(col.Expression))
                    v = ExpressionCompiler.Evaluate(col.Expression!, new EvalScope { Data = data, Rows = rows, Row = row });
            }
            catch
            {
                v = null;
            }
            if (v == null)
                continue;
            vals.Add(ToDecimal(v));
        }

        decimal result = agg switch
        {
            "avg" => vals.Count == 0 ? 0m : vals.Sum() / vals.Count,
            "min" => vals.Count == 0 ? 0m : vals.Min(),
            "max" => vals.Count == 0 ? 0m : vals.Max(),
            _ => vals.Sum(),
        };
        return ValueFormatter.Format(result, col.Format);
    }

    private static decimal ToDecimal(object v) => v switch
    {
        decimal d => d,
        double db => (decimal)db,
        float f => (decimal)f,
        int i => i,
        long l => l,
        short s => s,
        byte b => b,
        string str => decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var r)
            ? r
            : decimal.TryParse(str, NumberStyles.Any, Vi, out var r2) ? r2 : 0m,
        _ => 0m,
    };
}
