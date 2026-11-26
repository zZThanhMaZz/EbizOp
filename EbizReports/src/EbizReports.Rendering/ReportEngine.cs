using EbizReports.Core.Data;
using EbizReports.Core.Model;
using EbizReports.Core.Rendering;

namespace EbizReports.Rendering;

/// <summary>Điểm vào tiện dụng: chọn renderer theo format.</summary>
public sealed class ReportEngine
{
    private readonly Dictionary<RenderFormat, IReportRenderer> _renderers;

    public ReportEngine(IEnumerable<IReportRenderer>? renderers = null)
    {
        var list = renderers?.ToList() ?? new List<IReportRenderer>();
        if (list.Count == 0)
        {
            list = new List<IReportRenderer>
            {
                new QuestPdfReportRenderer(),
                new ExcelReportRenderer(),
                new CsvReportRenderer(),
            };
        }
        _renderers = list.ToDictionary(r => r.Format, r => r);
    }

    public byte[] Render(ReportDefinition definition, ReportData data, RenderFormat format,
        ReportRenderOptions? options = null)
    {
        if (!_renderers.TryGetValue(format, out var renderer))
            throw new NotSupportedException($"Chưa hỗ trợ định dạng {format}");
        ReportParameterDefaults.Apply(definition, data);
        return renderer.Render(definition, data, options);
    }

    public byte[] Render(ReportDefinition definition, ReportData data, string format,
        ReportRenderOptions? options = null)
    {
        var f = format.ToLowerInvariant() switch
        {
            "pdf" => RenderFormat.Pdf,
            "xlsx" or "excel" => RenderFormat.Xlsx,
            "csv" => RenderFormat.Csv,
            _ => throw new NotSupportedException($"Chưa hỗ trợ định dạng {format}"),
        };
        return Render(definition, data, f, options);
    }
}
