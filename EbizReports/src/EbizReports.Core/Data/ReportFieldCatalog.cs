using EbizReports.Core.Expressions;
using EbizReports.Core.Model;

namespace EbizReports.Core.Data;

/// <summary>Một trường khả dụng để bind trong designer.</summary>
public sealed class ReportFieldInfo
{
    /// <summary>Tên field dùng để bind (vd "Name", "LineTotal").</summary>
    public string Name { get; init; } = "";

    /// <summary>Loại nguồn: detail (dòng) | field (giá trị đơn) | param (tham số).</summary>
    public string Category { get; init; } = "detail";

    /// <summary>Giá trị mẫu (đã format) để gợi ý khi thiết kế.</summary>
    public string? Sample { get; init; }
}

/// <summary>
/// Liệt kê các trường khả dụng từ một <see cref="ReportData"/> + tham số trong definition.
/// Dùng cho panel "Trường dữ liệu" của designer; không phụ thuộc nguồn dữ liệu cụ thể.
/// </summary>
public static class ReportFieldCatalog
{
    public static IReadOnlyList<ReportFieldInfo> Discover(ReportDefinition definition, ReportData data)
    {
        var list = new List<ReportFieldInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1) Trường nằm trong các dòng detail (hợp nhất khoá của mọi dòng, giữ thứ tự xuất hiện).
        var samples = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in data.Details)
        {
            foreach (var kv in row)
            {
                if (!samples.ContainsKey(kv.Key))
                    samples[kv.Key] = kv.Value;
            }
        }
        foreach (var kv in samples)
        {
            if (seen.Add(kv.Key))
                list.Add(new ReportFieldInfo { Name = kv.Key, Category = "detail", Sample = Sample(kv.Value) });
        }

        // 2) Field đơn (header/footer: Store.Name, Bill.BillNo...).
        foreach (var kv in data.Fields)
        {
            if (seen.Add(kv.Key))
                list.Add(new ReportFieldInfo { Name = kv.Key, Category = "field", Sample = Sample(kv.Value) });
        }

        // 3) Tham số: ưu tiên giá trị đang có, sau đó khai báo trong definition.
        foreach (var kv in data.Parameters)
        {
            if (seen.Add(kv.Key))
                list.Add(new ReportFieldInfo { Name = kv.Key, Category = "param", Sample = Sample(kv.Value) });
        }
        foreach (var p in definition.Parameters)
        {
            if (!string.IsNullOrWhiteSpace(p.Name) && seen.Add(p.Name))
                list.Add(new ReportFieldInfo { Name = p.Name, Category = "param", Sample = null });
        }

        return list;
    }

    private static string? Sample(object? value) =>
        value is null ? null : ValueFormatter.Format(value, null);
}
