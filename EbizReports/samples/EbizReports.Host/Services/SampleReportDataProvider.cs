using EbizReports.Core.Data;
using EbizReports.Core.Integration;
using EbizReports.Core.Model;

namespace EbizReports.Host.Services;

/// <summary>
/// Adapter nối <see cref="SampleReports"/> (dữ liệu mẫu) vào interface <see cref="IReportDataProvider"/>
/// của module. Khi tích hợp EbizOp thật, thay bằng adapter gọi ReportService nghiệp vụ.
/// </summary>
public sealed class SampleReportDataProvider : IReportDataProvider
{
    private readonly SampleReports _samples;

    public SampleReportDataProvider(SampleReports samples) => _samples = samples;

    public ReportData GetData(ReportDefinition definition, IReadOnlyDictionary<string, object?> parameters)
        => _samples.GetData(definition.Code);
}
