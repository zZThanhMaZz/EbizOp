namespace EbizReports.Core.Integration;

/// <summary>
/// Một kết nối CSDL do host đăng ký và cung cấp cho designer (chỉ để hiển thị/chọn).
/// Thông tin kết nối thật (connection string) do host giữ, không nằm trong định nghĩa report.
/// </summary>
public sealed class ReportConnectionInfo
{
    /// <summary>Tên duy nhất để <see cref="EbizReports.Core.Model.ReportDataSource.Connection"/> tham chiếu.</summary>
    public string Name { get; init; } = "";

    /// <summary>Nhà cung cấp: <c>sqlite</c> hoặc <c>sqlserver</c>.</summary>
    public string Provider { get; init; } = "";

    /// <summary>Nhãn hiển thị trong designer.</summary>
    public string Label { get; init; } = "";
}

/// <summary>
/// Kết quả chạy thử một nguồn dữ liệu SQL: danh sách cột (để bind) + vài dòng mẫu dùng gợi ý.
/// Được host tạo ra và trả về cho designer qua callback <c>TestDataSource</c>.
/// </summary>
public sealed class SqlProbeResult
{
    public bool Ok { get; init; }

    public string? Error { get; init; }

    /// <summary>Tên các cột theo thứ tự truy vấn trả về.</summary>
    public IReadOnlyList<string> Columns { get; init; } = Array.Empty<string>();

    /// <summary>Một số dòng mẫu (tối đa do host quy định) để gợi ý giá trị.</summary>
    public IReadOnlyList<Dictionary<string, object?>> Rows { get; init; } =
        Array.Empty<Dictionary<string, object?>>();

    /// <summary>Tổng số dòng thực tế trả về (có thể lớn hơn số dòng mẫu).</summary>
    public int RowCount { get; init; }

    public long ElapsedMs { get; init; }
}
