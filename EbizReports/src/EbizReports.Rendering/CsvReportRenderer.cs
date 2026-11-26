using System.Globalization;
using System.Text;
using EbizReports.Core.Data;
using EbizReports.Core.Model;
using EbizReports.Core.Rendering;

namespace EbizReports.Rendering;

/// <summary>Xuất report ra CSV (UTF-8 BOM để Excel mở đúng tiếng Việt).</summary>
public sealed class CsvReportRenderer : IReportRenderer
{
    public RenderFormat Format => RenderFormat.Csv;

    public byte[] Render(ReportDefinition definition, ReportData data, ReportRenderOptions? options = null)
    {
        var (columns, rows) = TabularProjection.Build(definition, data);

        var sb = new StringBuilder();
        sb.Append(string.Join(',', columns.Select(c => Quote(c.Header)))).Append('\n');
        foreach (var row in rows)
            sb.Append(string.Join(',', row.Select(Quote))).Append('\n');

        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(sb.ToString());
    }

    private static string Quote(object? value) => "\"" + Cell(value).Replace("\"", "\"\"") + "\"";

    private static string Cell(object? value)
    {
        if (value == null)
            return "";
        var vi = CultureInfo.GetCultureInfo("vi-VN");
        return value switch
        {
            DateTime dt => dt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
            decimal d => d.ToString(vi),
            double db => db.ToString(vi),
            int i => i.ToString(vi),
            long l => l.ToString(vi),
            _ => value.ToString() ?? "",
        };
    }
}
