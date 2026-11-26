using System.Globalization;
using System.Text;
using EbizReports.Core.Data;
using EbizReports.Core.Expressions;
using EbizReports.Core.Model;

namespace EbizReports.Core.Charts;

/// <summary>Một điểm dữ liệu biểu đồ: nhãn phân loại + giá trị đã tổng hợp.</summary>
public sealed record ChartPoint(string Category, double Value);

/// <summary>
/// Tự dựng biểu đồ dưới dạng SVG vector thuần (không phụ thuộc thư viện vẽ).
/// Hỗ trợ: cột (bar, có phương án ngang), đường (line), tròn (pie).
/// Toạ độ viewBox theo mm để khớp ô item; QuestPDF render SVG nên sắc nét khi in.
/// </summary>
public static class ChartBuilder
{
    public static readonly string[] Palette =
    {
        "#3F51B5", "#26A69A", "#EF5350", "#FFA726",
        "#AB47BC", "#42A5F5", "#66BB6A", "#8D6E63",
    };

    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static string ColorOf(int index) => Palette[((index % Palette.Length) + Palette.Length) % Palette.Length];

    /// <summary>Dữ liệu mẫu (cho designer xem trước khi chưa có dữ liệu thật).</summary>
    public static IReadOnlyList<ChartPoint> Sample(string? type)
    {
        var labels = new[] { "T2", "T3", "T4", "T5", "T6", "T7" };
        var vals = new[] { 12d, 18d, 9d, 22d, 15d, 25d };
        var list = new List<ChartPoint>(labels.Length);
        for (var i = 0; i < labels.Length; i++)
            list.Add(new ChartPoint(labels[i], vals[i]));
        return list;
    }

    // ---------------------------------------------------------------- aggregate

    /// <summary>Tổng hợp các dòng detail thành các điểm (category, value) theo ChartSpec.</summary>
    public static List<ChartPoint> Aggregate(ChartSpec? spec, ReportData data,
        IReadOnlyList<Dictionary<string, object?>> rows)
    {
        spec ??= new ChartSpec();
        var agg = (spec.Aggregate ?? "sum").Trim().ToLowerInvariant();
        var wantCount = agg == "count";

        var order = new List<string>();
        var acc = new Dictionary<string, (double Sum, int N, double Min, double Max)>(StringComparer.Ordinal);

        foreach (var row in rows)
        {
            var scope = new EvalScope { Data = data, Rows = rows, Row = row };
            var cat = EvalText(spec.Category, scope);

            double val;
            if (wantCount)
            {
                val = 1;
            }
            else
            {
                var raw = EvalValue(spec.Value, scope);
                if (raw == null)
                    continue;
                val = ToDouble(raw);
            }

            if (!acc.TryGetValue(cat, out var a))
            {
                order.Add(cat);
                a = (0, 0, double.MaxValue, double.MinValue);
            }
            a.Sum += val;
            a.N += 1;
            a.Min = Math.Min(a.Min, val);
            a.Max = Math.Max(a.Max, val);
            acc[cat] = a;
        }

        var points = new List<ChartPoint>(order.Count);
        foreach (var cat in order)
        {
            var a = acc[cat];
            var v = agg switch
            {
                "avg" => a.N == 0 ? 0 : a.Sum / a.N,
                "min" => a.N == 0 ? 0 : a.Min,
                "max" => a.N == 0 ? 0 : a.Max,
                _ => a.Sum,
            };
            points.Add(new ChartPoint(cat, v));
        }
        return points;
    }

    private static string EvalText(string? expr, EvalScope scope)
    {
        if (string.IsNullOrWhiteSpace(expr))
            return "";
        try { return ValueFormatter.Format(ExpressionCompiler.Evaluate(expr, scope), null); }
        catch { return ""; }
    }

    private static object? EvalValue(string? expr, EvalScope scope)
    {
        if (string.IsNullOrWhiteSpace(expr))
            return null;
        try { return ExpressionCompiler.Evaluate(expr, scope); }
        catch { return null; }
    }

    private static double ToDouble(object? v) => v switch
    {
        null => 0,
        double d => d,
        decimal m => (double)m,
        float f => f,
        int i => i,
        long l => l,
        short s => s,
        byte b => b,
        bool bo => bo ? 1 : 0,
        string str => double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var r)
            ? r
            : double.TryParse(str, NumberStyles.Any, Vi, out var r2) ? r2 : 0,
        _ => 0,
    };

    // ---------------------------------------------------------------- svg

    /// <summary>Dựng SVG cho biểu đồ trong ô kích thước <paramref name="wmm"/> × <paramref name="hmm"/> (mm).</summary>
    public static string BuildSvg(ChartSpec? spec, IReadOnlyList<ChartPoint> points, double wmm, double hmm)
    {
        spec ??= new ChartSpec();
        var W = Math.Max(24, wmm);
        var H = Math.Max(16, hmm);
        var type = (spec.Type ?? "bar").Trim().ToLowerInvariant();

        var sb = new StringBuilder(1024);
        sb.Append("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 ")
          .Append(F(W)).Append(' ').Append(F(H)).Append("' preserveAspectRatio='none'>");
        sb.Append("<rect x='0' y='0' width='").Append(F(W)).Append("' height='").Append(F(H))
          .Append("' fill='#ffffff'/>");

        if (points.Count == 0)
        {
            sb.Append("<rect x='0.4' y='0.4' width='").Append(F(W - 0.8)).Append("' height='").Append(F(H - 0.8))
              .Append("' fill='none' stroke='#cccccc' stroke-width='0.3'/>");
            Text(sb, W / 2, H / 2 + 1, 2.6, "#9e9e9e", "middle", "Không có dữ liệu", null);
            sb.Append("</svg>");
            return sb.ToString();
        }

        if (type == "pie")
            BuildPie(sb, spec, points, W, H);
        else if (type == "line")
            BuildAxis(sb, spec, points, W, H, line: true);
        else
            BuildAxis(sb, spec, points, W, H, line: false);

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static void BuildAxis(StringBuilder sb, ChartSpec spec, IReadOnlyList<ChartPoint> points,
        double W, double H, bool line)
    {
        const double pad = 1.4;
        var title = spec.Title;
        var titleH = string.IsNullOrWhiteSpace(title) ? 0 : Math.Min(6, H * 0.16);

        var legend = points.Count >= 2 ? (spec.Legend ?? "right").Trim().ToLowerInvariant() : "none";
        var legendRight = legend == "right" ? Math.Clamp(W * 0.30, 15, 52) : 0;
        var legendBottom = legend == "bottom" ? Math.Min(H * 0.42, points.Count * 3.4 + 2) : 0;

        var vals = points.Select(p => p.Value).ToList();
        var vmax = Math.Max(0, vals.Max());
        var vmin = Math.Min(0, vals.Min());
        if (Math.Abs(vmax - vmin) < 1e-9)
            vmax = vmin + 1;

        var maxLab = Compact(vmax).Length;
        var axisW = Math.Clamp(maxLab * 1.35 + 1.6, 8, W * 0.30);

        var xLabelH = points.Count > 6 ? 7 : 5;
        var plotX0 = pad + axisW;
        var plotX1 = W - pad - legendRight;
        var plotY0 = pad + titleH;
        var plotY1 = H - pad - legendBottom - xLabelH;
        if (plotX1 - plotX0 < 6 || plotY1 - plotY0 < 5)
        {
            // quá nhỏ → bỏ nhãn trục, vẽ gọn
            plotX0 = pad; plotX1 = W - pad - legendRight; plotY0 = pad + titleH; plotY1 = H - pad;
        }

        if (!string.IsNullOrWhiteSpace(title))
            Text(sb, plotX0, pad + titleH * 0.72, Math.Min(3.0, H * 0.05 + 1.8), "#111111", "start", title!, null, true);

        double Y(double v) => plotY1 - (v - vmin) / (vmax - vmin) * (plotY1 - plotY0);

        // trục + lưới ngang
        const int ticks = 4;
        for (var i = 0; i <= ticks; i++)
        {
            var v = vmin + (vmax - vmin) * i / ticks;
            var y = Y(v);
            sb.Append("<line x1='").Append(F(plotX0)).Append("' y1='").Append(F(y))
              .Append("' x2='").Append(F(plotX1)).Append("' y2='").Append(F(y))
              .Append("' stroke='").Append(i == 0 ? "#bdbdbd" : "#eeeeee").Append("' stroke-width='0.25'/>");
            Text(sb, plotX0 - 0.6, y + 0.8, 2.0, "#757575", "end", Compact(v), null);
        }

        var n = points.Count;
        var band = (plotX1 - plotX0) / n;

        if (!line)
        {
            for (var i = 0; i < n; i++)
            {
                var p = points[i];
                var bw = band * 0.62;
                var x = plotX0 + band * i + (band - bw) / 2;
                var y = Y(p.Value);
                var top = Math.Min(y, Y(0));
                var hgt = Math.Abs(Y(0) - y);
                sb.Append("<rect x='").Append(F(x)).Append("' y='").Append(F(top))
                  .Append("' width='").Append(F(bw)).Append("' height='").Append(F(Math.Max(hgt, 0.2)))
                  .Append("' fill='").Append(ColorOf(i)).Append("'/>");
                if (spec.ShowValues)
                    Text(sb, x + bw / 2, top - 0.5, 1.9, "#424242", "middle", Compact(p.Value), null);
            }
        }
        else
        {
            var pts = new StringBuilder();
            for (var i = 0; i < n; i++)
            {
                var cx = plotX0 + band * i + band / 2;
                var cy = Y(points[i].Value);
                pts.Append(F(cx)).Append(',').Append(F(cy)).Append(' ');
            }
            sb.Append("<polyline points='").Append(pts.ToString().Trim())
              .Append("' fill='none' stroke='").Append(ColorOf(0)).Append("' stroke-width='0.6'/>");
            for (var i = 0; i < n; i++)
            {
                var cx = plotX0 + band * i + band / 2;
                var cy = Y(points[i].Value);
                sb.Append("<circle cx='").Append(F(cx)).Append("' cy='").Append(F(cy))
                  .Append("' r='0.9' fill='").Append(ColorOf(i)).Append("'/>");
                if (spec.ShowValues)
                    Text(sb, cx, cy - 1.2, 1.9, "#424242", "middle", Compact(points[i].Value), null);
            }
        }

        // nhãn trục X
        var rotate = n > 6;
        for (var i = 0; i < n; i++)
        {
            var cx = plotX0 + band * i + band / 2;
            var label = Trunc(points[i].Category, rotate ? 12 : Math.Max(4, (int)(band / 1.5)));
            if (rotate)
                Text(sb, cx - 1, plotY1 + 1.5, 1.9, "#616161", "end", label, "rotate(-32 " + F(cx - 1) + " " + F(plotY1 + 1.5) + ")");
            else
                Text(sb, cx, plotY1 + 3.2, 1.9, "#616161", "middle", label, null);
        }

        DrawLegend(sb, spec, points, W, H, pad, titleH, legendRight, legendBottom, legend);
    }

    private static void BuildPie(StringBuilder sb, ChartSpec spec, IReadOnlyList<ChartPoint> points, double W, double H)
    {
        const double pad = 1.4;
        var title = spec.Title;
        var titleH = string.IsNullOrWhiteSpace(title) ? 0 : Math.Min(6, H * 0.16);
        var legend = (spec.Legend ?? "right").Trim().ToLowerInvariant();
        if (points.Count < 2)
            legend = "none";
        var legendRight = legend == "right" ? Math.Clamp(W * 0.34, 16, 56) : 0;
        var legendBottom = legend == "bottom" ? Math.Min(H * 0.45, points.Count * 3.4 + 2) : 0;

        var plotX0 = pad;
        var plotX1 = W - pad - legendRight;
        var plotY0 = pad + titleH;
        var plotY1 = H - pad - legendBottom;

        if (!string.IsNullOrWhiteSpace(title))
            Text(sb, plotX0, pad + titleH * 0.72, Math.Min(3.0, H * 0.05 + 1.8), "#111111", "start", title!, null, true);

        var cx = (plotX0 + plotX1) / 2;
        var cy = (plotY0 + plotY1) / 2;
        var r = Math.Max(2, Math.Min(plotX1 - plotX0, plotY1 - plotY0) / 2 - 0.8);

        var total = points.Sum(p => Math.Max(0, p.Value));
        if (total <= 0)
        {
            sb.Append("<circle cx='").Append(F(cx)).Append("' cy='").Append(F(cy)).Append("' r='").Append(F(r))
              .Append("' fill='#eeeeee' stroke='#bdbdbd' stroke-width='0.3'/>");
        }
        else if (points.Count == 1 || points.All(p => p.Value <= 0))
        {
            sb.Append("<circle cx='").Append(F(cx)).Append("' cy='").Append(F(cy)).Append("' r='").Append(F(r))
              .Append("' fill='").Append(ColorOf(0)).Append("'/>");
        }
        else
        {
            double a0 = -90;
            for (var i = 0; i < points.Count; i++)
            {
                var v = Math.Max(0, points[i].Value);
                if (v <= 0)
                    continue;
                var sweep = v / total * 360.0;
                var a1 = a0 + sweep;
                var large = sweep > 180 ? 1 : 0;
                var x0 = cx + r * Math.Cos(Rad(a0));
                var y0 = cy + r * Math.Sin(Rad(a0));
                var x1 = cx + r * Math.Cos(Rad(a1));
                var y1 = cy + r * Math.Sin(Rad(a1));
                sb.Append("<path d='M ").Append(F(cx)).Append(' ').Append(F(cy))
                  .Append(" L ").Append(F(x0)).Append(' ').Append(F(y0))
                  .Append(" A ").Append(F(r)).Append(' ').Append(F(r)).Append(" 0 ").Append(large)
                  .Append(" 1 ").Append(F(x1)).Append(' ').Append(F(y1))
                  .Append(" Z' fill='").Append(ColorOf(i)).Append("' stroke='#ffffff' stroke-width='0.3'/>");
                a0 = a1;
            }
        }

        if (legend != "none")
        {
            var rows = new List<string>();
            for (var i = 0; i < points.Count; i++)
            {
                var pct = total > 0 ? points[i].Value / total * 100 : 0;
                rows.Add(points[i].Category + (spec.ShowValues ? $" ({pct:0.#}%)" : ""));
            }
            DrawLegendRows(sb, spec, points, rows, W, H, pad, titleH, legendRight, legendBottom, legend);
        }
    }

    private static void DrawLegend(StringBuilder sb, ChartSpec spec, IReadOnlyList<ChartPoint> points,
        double W, double H, double pad, double titleH, double legendRight, double legendBottom, string legend)
    {
        if (legend == "none")
            return;
        var rows = points.Select((p, i) => p.Category + (spec.ShowValues ? "  " + FmtVal(p.Value) : "")).ToList();
        DrawLegendRows(sb, spec, points, rows, W, H, pad, titleH, legendRight, legendBottom, legend);
    }

    private static void DrawLegendRows(StringBuilder sb, ChartSpec spec, IReadOnlyList<ChartPoint> points,
        List<string> rows, double W, double H, double pad, double titleH, double legendRight, double legendBottom, string legend)
    {
        const double rowH = 3.2;
        const double sw = 2.4;
        if (legend == "right")
        {
            var x = W - legendRight + 1.5;
            var y = pad + titleH + 1.5;
            for (var i = 0; i < rows.Count; i++)
            {
                var yy = y + i * rowH;
                if (yy > H - pad - 1)
                    break;
                sb.Append("<rect x='").Append(F(x)).Append("' y='").Append(F(yy - sw + 0.6))
                  .Append("' width='").Append(F(sw)).Append("' height='").Append(F(sw))
                  .Append("' fill='").Append(ColorOf(i)).Append("'/>");
                var maxChars = Math.Max(4, (int)((legendRight - sw - 3) / 1.25));
                Text(sb, x + sw + 1, yy + 0.8, 1.9, "#424242", "start", Trunc(rows[i], maxChars), null);
            }
        }
        else // bottom
        {
            var rowsPerCol = Math.Max(1, (int)((legendBottom - 1) / rowH));
            var cols = (int)Math.Ceiling(rows.Count / (double)rowsPerCol);
            var colW = (W - 2 * pad) / Math.Max(1, cols);
            var y0 = H - legendBottom + 1.5;
            for (var i = 0; i < rows.Count; i++)
            {
                var c = i / rowsPerCol;
                var rr = i % rowsPerCol;
                var x = pad + c * colW;
                var yy = y0 + rr * rowH;
                sb.Append("<rect x='").Append(F(x)).Append("' y='").Append(F(yy - sw + 0.6))
                  .Append("' width='").Append(F(sw)).Append("' height='").Append(F(sw))
                  .Append("' fill='").Append(ColorOf(i)).Append("'/>");
                var maxChars = Math.Max(4, (int)((colW - sw - 3) / 1.25));
                Text(sb, x + sw + 1, yy + 0.8, 1.9, "#424242", "start", Trunc(rows[i], maxChars), null);
            }
        }
    }

    // ---------------------------------------------------------------- helpers

    private static void Text(StringBuilder sb, double x, double y, double size, string fill,
        string anchor, string text, string? transform = null, bool bold = false)
    {
        if (string.IsNullOrEmpty(text))
            return;
        sb.Append("<text x='").Append(F(x)).Append("' y='").Append(F(y))
          .Append("' font-size='").Append(F(size))
          .Append("' fill='").Append(fill);
        if (bold)
            sb.Append("' font-weight='bold");
        sb.Append("' text-anchor='").Append(anchor).Append('\'');
        if (!string.IsNullOrEmpty(transform))
            sb.Append(" transform='").Append(transform).Append('\'');
        sb.Append('>').Append(Esc(text)).Append("</text>");
    }

    private static double Rad(double deg) => deg * Math.PI / 180.0;

    private static string Compact(double v)
    {
        var a = Math.Abs(v);
        if (a >= 1_000_000)
            return (v / 1_000_000).ToString("0.#", CultureInfo.InvariantCulture) + "M";
        if (a >= 1000)
            return (v / 1000).ToString("0.#", CultureInfo.InvariantCulture) + "K";
        return v.ToString("0.##", CultureInfo.InvariantCulture);
    }

    public static string FmtVal(double v) => v.ToString("N0", Vi);

    private static string Trunc(string s, int max)
    {
        s ??= "";
        if (max < 1 || s.Length <= max)
            return s;
        return max <= 1 ? "…" : s[..(max - 1)] + "…";
    }

    private static string Esc(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
         .Replace("'", "&apos;").Replace("\"", "&quot;");

    private static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
}
