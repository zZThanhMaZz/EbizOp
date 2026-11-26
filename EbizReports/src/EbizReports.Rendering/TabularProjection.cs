using EbizReports.Core.Data;
using EbizReports.Core.Expressions;
using EbizReports.Core.Model;
using EbizReports.Core.Rendering;

namespace EbizReports.Rendering;

internal static class TabularProjection
{
    public sealed class Column
    {
        public string Header { get; init; } = "";
        public ReportItem Item { get; init; } = new();
    }

    /// <summary>Chiếu report thành bảng phẳng (dùng cho XLSX/CSV).</summary>
    public static (List<Column> Columns, List<object?[]> Rows) Build(ReportDefinition definition, ReportData data)
    {
        var detail = definition.Band(BandKind.Detail);
        var columns = new List<Column>();

        if (detail != null)
        {
            foreach (var item in detail.Items.Where(IsColumnItem))
            {
                columns.Add(new Column
                {
                    Header = HeaderOf(item),
                    Item = item,
                });
            }
        }

        if (columns.Count == 0)
        {
            // fallback: dùng key của dòng đầu tiên
            var first = data.Details.FirstOrDefault();
            if (first != null)
            {
                foreach (var key in first.Keys)
                    columns.Add(new Column { Header = key, Item = new ReportItem { Kind = ItemKind.Field, Binding = key } });
            }
        }

        var rows = new List<object?[]>();
        var groups = ReportGrouping.Build(definition, data);
        foreach (var group in groups)
        {
            foreach (var row in group.Rows)
            {
                var values = new object?[columns.Count];
                for (var i = 0; i < columns.Count; i++)
                    values[i] = EvaluateItem(columns[i].Item, data, group.Rows, row);
                rows.Add(values);
            }
        }
        return (columns, rows);
    }

    private static bool IsColumnItem(ReportItem item) =>
        item.Kind is ItemKind.Text or ItemKind.Field or ItemKind.Expression
            or ItemKind.Barcode or ItemKind.QrCode;

    private static string HeaderOf(ReportItem item) => item.Kind switch
    {
        ItemKind.Text => item.Text ?? "",
        ItemKind.Field => item.Binding ?? "",
        ItemKind.Expression => item.Expression ?? "",
        ItemKind.Barcode or ItemKind.QrCode => item.Binding ?? item.Expression ?? item.Text ?? item.Kind.ToString(),
        _ => "",
    };

    public static object? EvaluateItem(
        ReportItem item,
        ReportData data,
        IReadOnlyList<Dictionary<string, object?>> rows,
        IReadOnlyDictionary<string, object?>? row)
    {
        var scope = new EvalScope { Data = data, Rows = rows, Row = row };
        return item.Kind switch
        {
            ItemKind.Text => item.Text ?? "",
            ItemKind.Field => data.Resolve(item.Binding ?? "", row),
            ItemKind.Expression => ExpressionCompiler.Evaluate(item.Expression ?? "", scope),
            ItemKind.Barcode or ItemKind.QrCode => ResolveSymbol(item, data, rows, row, scope),
            _ => null,
        };
    }

    private static string ResolveSymbol(ReportItem item, ReportData data,
        IReadOnlyList<Dictionary<string, object?>> rows, IReadOnlyDictionary<string, object?>? row, EvalScope scope)
    {
        if (!string.IsNullOrWhiteSpace(item.Binding))
            return Convert.ToString(data.Resolve(item.Binding!, row)) ?? "";
        if (!string.IsNullOrWhiteSpace(item.Expression))
            return Convert.ToString(ExpressionCompiler.Evaluate(item.Expression!, scope)) ?? "";
        return item.Text ?? "";
    }
}
