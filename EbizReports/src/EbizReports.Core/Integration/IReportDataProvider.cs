using EbizReports.Core.Data;
using EbizReports.Core.Model;

namespace EbizReports.Core.Integration;

/// <summary>
/// Nguồn dữ liệu cho report. Ứng dụng host cài đặt để nối vào service/CSDL nghiệp vụ
/// (ví dụ <c>ReportService</c> của EbizOp) mà không làm module phụ thuộc vào tầng dữ liệu đó.
/// </summary>
public interface IReportDataProvider
{
    /// <param name="definition">Định nghĩa đang render — để biết cần lấy datasource/tham số nào.</param>
    /// <param name="parameters">Tham số hiệu lực: mặc định của định nghĩa đã áp, giá trị người dùng đã ghi đè.</param>
    ReportData GetData(ReportDefinition definition, IReadOnlyDictionary<string, object?> parameters);
}

/// <summary>Adapter đơn giản: bọc một delegate để nhanh chóng cắm nguồn dữ liệu bất kỳ.</summary>
public sealed class DelegateReportDataProvider : IReportDataProvider
{
    private readonly Func<ReportDefinition, IReadOnlyDictionary<string, object?>, ReportData> _factory;

    public DelegateReportDataProvider(
        Func<ReportDefinition, IReadOnlyDictionary<string, object?>, ReportData> factory)
        => _factory = factory ?? throw new ArgumentNullException(nameof(factory));

    public ReportData GetData(ReportDefinition definition, IReadOnlyDictionary<string, object?> parameters)
        => _factory(definition, parameters) ?? new ReportData();
}
