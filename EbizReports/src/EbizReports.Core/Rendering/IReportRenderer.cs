using EbizReports.Core.Model;

namespace EbizReports.Core.Rendering;

public sealed class ReportRenderOptions
{
    /// <summary>Thư mục chứa font .ttf (Lato). Null = dùng font mặc định.</summary>
    public string? FontDirectory { get; set; }

    public string FontFamily { get; set; } = "Lato";

    /// <summary>Đóng dấu mờ (chưa dùng ở V1).</summary>
    public string? Watermark { get; set; }

    /// <summary>
    /// Giải mã report con cho item <see cref="Model.ItemKind.Subreport"/>: nhận cấu hình report con,
    /// trả về định nghĩa + dữ liệu đã chuẩn bị (hoặc null nếu không tìm thấy / không có quyền).
    /// Renderer không tự truy cập kho dữ liệu nên có thể dùng ngoài ứng dụng host.
    /// </summary>
    public Func<Model.SubreportSpec, SubreportResolution?>? SubreportResolver { get; set; }
}

/// <summary>Kết quả giải mã một report con: định nghĩa + dữ liệu kèm tham số hiệu lực.</summary>
public sealed class SubreportResolution
{
    public required ReportDefinition Definition { get; init; }
    public Data.ReportData Data { get; init; } = new();
}

public interface IReportRenderer
{
    RenderFormat Format { get; }
    byte[] Render(ReportDefinition definition, Data.ReportData data, ReportRenderOptions? options = null);
}
