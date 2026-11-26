using EbizReports.Core.Data;
using EbizReports.Core.Model;
using EbizReports.Core.Rendering;
using MiniExcelLibs;

namespace EbizReports.Rendering;

/// <summary>Xuất report ra XLSX (MiniExcel). V1: bảng phẳng từ band Detail.</summary>
public sealed class ExcelReportRenderer : IReportRenderer
{
    public RenderFormat Format => RenderFormat.Xlsx;

    public byte[] Render(ReportDefinition definition, ReportData data, ReportRenderOptions? options = null)
    {
        var (columns, rows) = TabularProjection.Build(definition, data);

        var list = new List<Dictionary<string, object?>>();
        foreach (var row in rows)
        {
            var dict = new Dictionary<string, object?>();
            for (var i = 0; i < columns.Count; i++)
            {
                var key = string.IsNullOrWhiteSpace(columns[i].Header) ? $"Col{i + 1}" : columns[i].Header;
                if (dict.ContainsKey(key))
                    key = $"{key}_{i + 1}";
                dict[key] = row[i];
            }
            list.Add(dict);
        }

        using var stream = new MemoryStream();
        if (list.Count == 0)
            stream.SaveAs(new List<Dictionary<string, object?>>(), printHeader: true);
        else
            stream.SaveAs(list, printHeader: true);
        return stream.ToArray();
    }
}
