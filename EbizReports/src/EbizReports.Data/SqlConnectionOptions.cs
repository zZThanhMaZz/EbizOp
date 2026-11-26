namespace EbizReports.Data;

/// <summary>Thông tin một kết nối CSDL (host giữ, không lưu vào định nghĩa report).</summary>
public sealed class SqlConnectionOptions
{
    /// <summary>Tên duy nhất để report tham chiếu.</summary>
    public string Name { get; init; } = "";

    /// <summary>Nhà cung cấp: <c>sqlite</c> hoặc <c>sqlserver</c>.</summary>
    public string Provider { get; init; } = "sqlite";

    /// <summary>Chuỗi kết nối ADO.NET.</summary>
    public string ConnectionString { get; init; } = "";

    /// <summary>Nhãn hiển thị trong designer (mặc định lấy theo <see cref="Name"/>).</summary>
    public string Label { get; init; } = "";
}
