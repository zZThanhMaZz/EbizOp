namespace EbizReports.Core.Data;

/// <summary>Dữ liệu đầu vào cho renderer: field đơn (scalar), tham số, và các dòng detail.</summary>
public sealed class ReportData
{
    public Dictionary<string, object?> Fields { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, object?> Parameters { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<Dictionary<string, object?>> Details { get; } = new();

    public ReportData SetField(string key, object? value)
    {
        Fields[key] = value;
        return this;
    }

    public ReportData SetParam(string key, object? value)
    {
        Parameters[key] = value;
        return this;
    }

    public ReportData AddRow(params (string Key, object? Value)[] cells)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in cells)
            row[key] = value;
        Details.Add(row);
        return this;
    }

    public ReportData AddRow(Dictionary<string, object?> row)
    {
        Details.Add(row);
        return this;
    }

    /// <summary>
    /// Tra giá trị theo thứ tự: dòng hiện tại → field → tham số. Hỗ trợ key có tiền tố
    /// (ví dụ "Items.Name" sẽ khớp "Name" trong dòng, hoặc "Store.Name" khớp field).
    /// </summary>
    public object? Resolve(string path, IReadOnlyDictionary<string, object?>? row)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (row != null)
        {
            if (row.TryGetValue(path, out var direct))
                return direct;

            var tail = Tail(path);
            if (tail != null && row.TryGetValue(tail, out var byTail))
                return byTail;
        }

        if (Fields.TryGetValue(path, out var field))
            return field;
        if (Parameters.TryGetValue(path, out var param))
            return param;

        var tailName = Tail(path);
        if (tailName != null)
        {
            if (Fields.TryGetValue(tailName, out var f2))
                return f2;
            if (Parameters.TryGetValue(tailName, out var p2))
                return p2;
        }

        return null;
    }

    private static string? Tail(string path)
    {
        var i = path.LastIndexOf('.');
        return i >= 0 && i < path.Length - 1 ? path[(i + 1)..] : null;
    }
}
