using System.Globalization;
using EbizReports.Core.Data;
using EbizReports.Core.Model;

namespace EbizReports.Core.Expressions;

/// <summary>Một hàm có thể dùng trong biểu thức (cho gợi ý / chèn nhanh).</summary>
public sealed record ExpressionFunction(
    string Name, string Signature, string Description, string Snippet,
    int MinArgs = -1, int MaxArgs = -1);

/// <summary>Kết quả kiểm tra cú pháp hoặc tính thử một biểu thức.</summary>
public sealed record ExpressionCheck(bool Ok, string? Error = null, string? Value = null);

/// <summary>
/// Trợ giúp cho "Expression builder": danh mục hàm, kiểm tra cú pháp (kể cả tên/số đối số hàm),
/// tính thử trên dữ liệu mẫu, và dựng <see cref="ReportData"/> mẫu từ danh sách trường.
/// Không eval mã động — dùng chính <see cref="ExpressionCompiler"/>.
/// </summary>
public static class ExpressionInspector
{
    /// <summary>Danh mục hàm theo đúng bộ mà <see cref="ExpressionEvaluator"/> hỗ trợ.</summary>
    public static IReadOnlyList<ExpressionFunction> Functions { get; } = new List<ExpressionFunction>
    {
        new("Sum", "Sum(x)", "Tổng giá trị x qua các dòng", "Sum(", 1, 1),
        new("Avg", "Avg(x)", "Trung bình cộng của x", "Avg(", 1, 1),
        new("Count", "Count()", "Số dòng dữ liệu", "Count()", 0, 0),
        new("Min", "Min(x)", "Giá trị nhỏ nhất của x", "Min(", 1, 1),
        new("Max", "Max(x)", "Giá trị lớn nhất của x", "Max(", 1, 1),
        new("Iif", "Iif(đk, a, b)", "Chọn a nếu điều kiện đúng, ngược lại b", "Iif(", 3, 3),
        new("Upper", "Upper(s)", "Chuyển chuỗi sang in hoa", "Upper(", 1, 1),
        new("Lower", "Lower(s)", "Chuyển chuỗi sang in thường", "Lower(", 1, 1),
        new("Trim", "Trim(s)", "Bỏ khoảng trắng hai đầu", "Trim(", 1, 1),
        new("Concat", "Concat(a, b, …)", "Nối nhiều giá trị thành chuỗi", "Concat(", 1, -1),
        new("Abs", "Abs(n)", "Trị tuyệt đối", "Abs(", 1, 1),
        new("Round", "Round(n, số lẻ)", "Làm tròn số", "Round(", 2, 2),
        new("Format", "Format(v, mẫu)", "Định dạng số/ngày (vd N0, dd/MM/yyyy)", "Format(", 2, 2),
        new("Len", "Len(s)", "Độ dài chuỗi", "Len(", 1, 1),
        new("Today", "Today()", "Ngày hôm nay", "Today()", 0, 0),
        new("Now", "Now()", "Ngày giờ hiện tại", "Now()", 0, 0),
        new("Page", "Page()", "Số trang hiện tại", "Page()", 0, 0),
        new("Pages", "Pages()", "Tổng số trang", "Pages()", 0, 0),
    };

    private static ExpressionFunction? FindFunction(string name) =>
        Functions.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Kiểm tra cú pháp + tên hàm + số đối số. Trả <see cref="ExpressionCheck.Ok"/> = true nếu hợp lệ.</summary>
    public static ExpressionCheck Validate(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return new ExpressionCheck(true);

        ExprNode node;
        try
        {
            node = ExpressionCompiler.Compile(expression);
        }
        catch (FormatException ex)
        {
            return new ExpressionCheck(false, ex.Message);
        }
        catch (Exception ex)
        {
            return new ExpressionCheck(false, ex.Message);
        }

        var error = CheckNode(node);
        return error is null ? new ExpressionCheck(true) : new ExpressionCheck(false, error);
    }

    private static string? CheckNode(ExprNode node)
    {
        switch (node)
        {
            case CallNode call:
                var fn = FindFunction(call.Name);
                if (fn is null)
                    return $"Hàm không hỗ trợ: {call.Name}";
                if (call.Args.Count < fn.MinArgs || (fn.MaxArgs >= 0 && call.Args.Count > fn.MaxArgs))
                    return $"Hàm {fn.Name} cần {Arity(fn)} đối số";
                foreach (var arg in call.Args)
                {
                    var e = CheckNode(arg);
                    if (e is not null)
                        return e;
                }
                return null;
            case BinaryNode b:
                return CheckNode(b.Left) ?? CheckNode(b.Right);
            case UnaryNode u:
                return CheckNode(u.Operand);
            default:
                return null;
        }
    }

    private static string Arity(ExpressionFunction fn) =>
        fn.MinArgs == fn.MaxArgs ? fn.MinArgs.ToString()
        : fn.MaxArgs < 0 ? $"≥ {fn.MinArgs}"
        : $"{fn.MinArgs}–{fn.MaxArgs}";

    /// <summary>Tính thử biểu thức trên dữ liệu; trả giá trị đã format hoặc lỗi.</summary>
    public static ExpressionCheck Evaluate(string? expression, ReportData data,
        IReadOnlyList<Dictionary<string, object?>>? rows = null,
        IReadOnlyDictionary<string, object?>? row = null)
    {
        var syntax = Validate(expression);
        if (!syntax.Ok)
            return syntax;

        try
        {
            var details = rows ?? data.Details;
            var scope = new EvalScope
            {
                Data = data,
                Rows = details,
                Row = row ?? (details.Count > 0 ? details[0] : null),
            };
            var value = ExpressionCompiler.Evaluate(expression ?? "", scope);
            return new ExpressionCheck(true, null, ValueFormatter.Format(value, null));
        }
        catch (Exception ex)
        {
            return new ExpressionCheck(false, ex.Message);
        }
    }

    /// <summary>
    /// Dựng <see cref="ReportData"/> mẫu từ các trường khả dụng (giá trị mẫu) + tham số mặc định,
    /// để xem trước biểu thức ngay trong designer khi host không cung cấp dữ liệu thật.
    /// </summary>
    public static ReportData SampleData(ReportDefinition? definition, IReadOnlyList<ReportFieldInfo>? fields)
    {
        var data = new ReportData();
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (fields is not null)
        {
            foreach (var f in fields)
            {
                if (string.IsNullOrWhiteSpace(f.Name))
                    continue;
                var value = ParseSample(f.Sample);
                switch (f.Category)
                {
                    case "field":
                        data.SetField(f.Name, value);
                        break;
                    case "param":
                        data.SetParam(f.Name, value);
                        break;
                    default:
                        row[f.Name] = value;
                        break;
                }
            }
        }

        // Tham số chưa có giá trị mẫu → dùng mặc định của định nghĩa.
        if (definition?.Parameters is not null)
        {
            foreach (var p in definition.Parameters)
            {
                if (!string.IsNullOrWhiteSpace(p.Name) && !data.Parameters.ContainsKey(p.Name))
                {
                    var v = ReportParameterDefaults.Convert(p);
                    if (v is not null)
                        data.SetParam(p.Name, v);
                }
            }
        }

        if (row.Count > 0)
            data.AddRow(row);
        return data;
    }

    /// <summary>Đoán kiểu của một giá trị mẫu đã format (số vi-VN, ngày, đúng/sai, còn lại là chuỗi).</summary>
    private static object? ParseSample(string? sample)
    {
        if (string.IsNullOrWhiteSpace(sample))
            return null;

        var vi = CultureInfo.GetCultureInfo("vi-VN");
        if (decimal.TryParse(sample, NumberStyles.Any, vi, out var d))
            return d;
        if (decimal.TryParse(sample, NumberStyles.Any, CultureInfo.InvariantCulture, out var d2))
            return d2;
        if (DateTime.TryParse(sample, vi, DateTimeStyles.None, out var dt))
            return dt;
        if (sample.Equals("Có", StringComparison.OrdinalIgnoreCase) || sample.Equals("true", StringComparison.OrdinalIgnoreCase))
            return true;
        if (sample.Equals("Không", StringComparison.OrdinalIgnoreCase) || sample.Equals("false", StringComparison.OrdinalIgnoreCase))
            return false;
        return sample;
    }
}