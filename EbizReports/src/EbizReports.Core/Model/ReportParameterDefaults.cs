using System.Globalization;
using EbizReports.Core.Data;

namespace EbizReports.Core.Model;

/// <summary>
/// Chuyển giá trị mặc định (chuỗi) của tham số sang đúng kiểu, và áp vào <see cref="ReportData"/>
/// khi tham số chưa được cung cấp. Nhờ vậy report luôn render được kể cả khi không có UI nhập tham số.
/// </summary>
public static class ReportParameterDefaults
{
    /// <summary>Điền các tham số còn thiếu bằng giá trị mặc định của định nghĩa.</summary>
    public static void Apply(ReportDefinition definition, ReportData data)
    {
        if (definition?.Parameters is null || data is null)
            return;

        foreach (var p in definition.Parameters)
        {
            if (string.IsNullOrWhiteSpace(p.Name) || data.Parameters.ContainsKey(p.Name))
                continue;

            var value = Convert(p);
            if (value is not null)
                data.Parameters[p.Name] = value;
        }
    }

    /// <summary>Chuyển Default (chuỗi) của tham số sang kiểu tương ứng.</summary>
    public static object? Convert(ReportParameter p) => FromString(p.Type, p.Default);

    public static object? FromString(ReportParameter p, string? s) => FromString(p.Type, s);

    /// <summary>Chuyển giá trị do người dùng nhập (chuỗi) thành kiểu của tham số.</summary>
    public static object? FromString(ReportParameterType type, string? s)
    {
        switch (type)
        {
            case ReportParameterType.String:
                return s;
            case ReportParameterType.Int:
                return TryNumber(s) is { } i ? (int)Math.Round(i) : null;
            case ReportParameterType.Decimal:
                return TryNumber(s);
            case ReportParameterType.Date:
                return TryDate(s);
            case ReportParameterType.Bool:
                return string.IsNullOrWhiteSpace(s) ? false : TryBool(s);
            default:
                return s;
        }
    }

    private static decimal? TryNumber(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null
        : decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d
        : decimal.TryParse(s, NumberStyles.Any, CultureInfo.GetCultureInfo("vi-VN"), out d) ? d
        : null;

    private static DateTime? TryDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return null;
        return DateTime.TryParse(s, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var dt) ? dt
            : DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt) ? dt
            : null;
    }

    private static bool TryBool(string s) =>
        bool.TryParse(s, out var b) ? b
        : s == "1" || s.Equals("có", StringComparison.OrdinalIgnoreCase) || s.Equals("co", StringComparison.OrdinalIgnoreCase);
}
