using EbizReports.Core.Data;
using EbizReports.Core.Expressions;
using EbizReports.Core.Model;

namespace EbizReports.Core.Rendering;

public sealed class ReportGroupData
{
    public string Key { get; init; } = "";
    public Dictionary<string, object?>? KeyValues { get; init; }
    public List<Dictionary<string, object?>> Rows { get; init; } = new();
}

public static class ReportGrouping
{
    /// <summary>
    /// Chia detail rows theo Grouping trong definition (không grouping = 1 nhóm) và áp Sorting.
    /// Thứ tự: các nhóm theo field nhóm; trong mỗi nhóm theo <see cref="ReportDefinition.Sorting"/>.
    /// </summary>
    public static List<ReportGroupData> Build(ReportDefinition definition, ReportData data)
    {
        var sorts = definition.Sorting.Select(s => (s.Field, s.Dir)).ToList();
        var rows = data.Details;

        if (definition.Grouping.Count == 0)
        {
            var sortedRows = SortRows(rows, sorts);
            return new List<ReportGroupData>
            {
                new() { Key = "", Rows = sortedRows, KeyValues = null },
            };
        }

        var sorted = SortRows(rows, definition.Grouping.Select(g => (g.Field, g.Sort)).ToList());

        var groups = new List<ReportGroupData>();
        foreach (var row in sorted)
        {
            var key = string.Join("|", definition.Grouping.Select(g => ValueFormatter.Format(Lookup(row, g.Field), null)));
            var keyValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var g in definition.Grouping)
            {
                // key hiển thị: tên field sau dấu chấm cuối
                var name = g.Field.Contains('.') ? g.Field[(g.Field.LastIndexOf('.') + 1)..] : g.Field;
                keyValues[name] = Lookup(row, g.Field);
            }

            var current = groups.LastOrDefault();
            if (current == null || current.Key != key)
            {
                current = new ReportGroupData { Key = key, KeyValues = keyValues };
                groups.Add(current);
            }
            current.Rows.Add(row);
        }

        // Sắp xếp bên trong từng nhóm (giữ nguyên thứ tự nhóm).
        if (sorts.Count > 0)
        {
            foreach (var group in groups)
            {
                var ordered = SortRows(group.Rows, sorts);
                group.Rows.Clear();
                group.Rows.AddRange(ordered);
            }
        }

        return groups;
    }

    private static object? Lookup(Dictionary<string, object?> row, string field)
    {
        if (row.TryGetValue(field, out var v))
            return v;
        var tail = field.Contains('.') ? field[(field.LastIndexOf('.') + 1)..] : null;
        if (tail != null && row.TryGetValue(tail, out var v2))
            return v2;
        return null;
    }

    /// <summary>Sắp xếp ổn định (giữ thứ tự gốc khi các khoá bằng nhau).</summary>
    private static List<Dictionary<string, object?>> SortRows(
        IEnumerable<Dictionary<string, object?>> rows,
        IReadOnlyList<(string Field, string Dir)> sorts)
    {
        var list = rows.ToList();
        if (sorts.Count == 0)
            return list;

        var indexed = list.Select((row, index) => (Row: row, Index: index)).ToList();
        indexed.Sort((a, b) =>
        {
            foreach (var (field, dir) in sorts)
            {
                var cmp = CompareValues(Lookup(a.Row, field), Lookup(b.Row, field));
                if (cmp != 0)
                    return dir.Equals("desc", StringComparison.OrdinalIgnoreCase) ? -cmp : cmp;
            }
            return a.Index.CompareTo(b.Index);
        });
        return indexed.Select(x => x.Row).ToList();
    }

    private static int CompareValues(object? a, object? b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;
        if (ValueFormatter.IsNumeric(a) && ValueFormatter.IsNumeric(b))
            return ValueFormatter.ToDecimal(a).CompareTo(ValueFormatter.ToDecimal(b));
        if (a is DateTime da && b is DateTime db) return da.CompareTo(db);
        return string.Compare(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}