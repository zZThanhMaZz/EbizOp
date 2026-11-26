using System.Text;
using EbizReports.Core.Charts;
using EbizReports.Core.Data;
using EbizReports.Core.Expressions;
using EbizReports.Core.Model;
using EbizReports.Core.Rendering;
using EbizReports.Core.Tables;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QColor = QuestPDF.Infrastructure.Color;

namespace EbizReports.Rendering;

/// <summary>Render ReportDefinition ra PDF bằng QuestPDF. Hỗ trợ band free-form theo toạ độ mm.</summary>
public sealed class QuestPdfReportRenderer : IReportRenderer
{
    public RenderFormat Format => RenderFormat.Pdf;

    private static bool _fontsLoaded;
    private static readonly object FontLock = new();

    public byte[] Render(ReportDefinition definition, ReportData data, ReportRenderOptions? options = null)
    {
        return Build(definition, data, options).GeneratePdf();
    }

    /// <summary>Render trang đầu ra PNG (dùng cho thumbnail / kiểm tra trực quan).</summary>
    public byte[] RenderFirstPagePng(ReportDefinition definition, ReportData data, ReportRenderOptions? options = null)
    {
        return Build(definition, data, options).GenerateImages().First();
    }

    /// <summary>Render tất cả trang ra PNG (mỗi phần tử = ảnh 1 trang) — dùng cho xem trước WYSIWYG.</summary>
    public IReadOnlyList<byte[]> RenderPagesPng(ReportDefinition definition, ReportData data, ReportRenderOptions? options = null)
    {
        return Build(definition, data, options).GenerateImages().ToList();
    }

    private Document Build(ReportDefinition definition, ReportData data, ReportRenderOptions? options)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        if (Environment.GetEnvironmentVariable("EBIZ_QPDF_DEBUG") == "1")
            QuestPDF.Settings.EnableDebugging = true;
        EnsureFonts(options);

        var ctx = new RenderCtx(options, options?.FontFamily ?? "Lato");
        var paper = definition.Paper;
        var (pw, ph) = paper.Resolve();
        ctx = new RenderCtx(ctx.Options, ctx.Font)
        {
            AvailableWidthMm = pw - paper.Margins.Left - paper.Margins.Right,
        };
        var allRows = data.Details;

        var pageHeader = definition.Band(BandKind.PageHeader);
        var pageFooter = definition.Band(BandKind.PageFooter);

        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size((float)pw, (float)ph, Unit.Millimetre);
                page.MarginHorizontal((float)paper.Margins.Left, Unit.Millimetre);
                page.MarginVertical((float)paper.Margins.Top, Unit.Millimetre);
                page.DefaultTextStyle(t => t.FontFamily(ctx.Font).FontSize(9));

                if (pageHeader != null)
                    page.Header().Element(e => RenderBand(e, pageHeader, Scope(data, allRows, null), ctx));

                if (pageFooter != null)
                    page.Footer().Element(e => RenderBand(e, pageFooter, Scope(data, allRows, null), ctx));

                page.Content().Column(col =>
                {
                    col.Spacing(0);
                    RenderReportBands(col, definition, data, ctx);
                });
            });
        });
    }

    /// <summary>
    /// Vẽ các band nội dung (bỏ page header/footer) theo thứ tự — dùng chung cho trang chính
    /// và cho báo cáo con (item Subreport).
    /// </summary>
    private void RenderReportBands(ColumnDescriptor col, ReportDefinition definition, ReportData data, RenderCtx ctx)
    {
        var groups = ReportGrouping.Build(definition, data);
        var allRows = data.Details;

        var reportHeader = definition.Band(BandKind.ReportHeader);
        var reportFooter = definition.Band(BandKind.ReportFooter);
        var groupHeader = definition.Band(BandKind.GroupHeader);
        var groupFooter = definition.Band(BandKind.GroupFooter);
        var detail = definition.Band(BandKind.Detail);

        // Chỉ giữ nhóm khi báo cáo thật sự có khai báo nhóm (tránh giữ cả báo cáo thành 1 khối).
        var keepGroups = definition.Grouping.Count > 0 && definition.Grouping.Any(g => g.KeepTogether);

        void RenderGroup(ColumnDescriptor target, ReportGroupData group)
        {
            RenderBandItem(target, groupHeader, Scope(data, group.Rows, group.KeyValues), ctx);

            if (detail != null)
            {
                var anchor = group.Rows.Count > 0 ? group.Rows[0] : null;
                foreach (var row in group.Rows)
                    target.Item().Element(e => RenderBand(e, detail, Scope(data, group.Rows, row), ctx,
                        skipGroupTables: !ReferenceEquals(row, anchor)));
            }

            RenderBandItem(target, groupFooter, Scope(data, group.Rows, group.KeyValues), ctx);
        }

        RenderBandItem(col, reportHeader, Scope(data, allRows, null), ctx);

        foreach (var group in groups)
        {
            if (keepGroups)
            {
                // Giữ header + dòng + footer của nhóm trên cùng một trang nếu vừa.
                col.Item().PreventPageBreak().Column(gc =>
                {
                    gc.Spacing(0);
                    RenderGroup(gc, group);
                });
            }
            else
            {
                RenderGroup(col, group);
            }
        }

        RenderBandItem(col, reportFooter, Scope(data, allRows, null), ctx);
    }

    /// <summary>Vẽ một band (nếu có), tôn trọng cờ <see cref="ReportBand.PageBreakBefore"/>.</summary>
    private void RenderBandItem(ColumnDescriptor col, ReportBand? band, EvalScope scope, RenderCtx ctx)
    {
        if (band is null)
            return;

        if (band.PageBreakBefore)
            col.Item().PageBreak();

        col.Item().Element(e => RenderBand(e, band, scope, ctx));
    }

    /// <summary>Render toàn bộ nội dung một report vào container bất kỳ (dùng cho báo cáo con).</summary>
    private void RenderReportInto(IContainer container, ReportDefinition definition, ReportData data, RenderCtx ctx)
    {
        container.Column(col =>
        {
            col.Spacing(0);
            RenderReportBands(col, definition, data, ctx);
        });
    }

    private static EvalScope Scope(ReportData data, IReadOnlyList<Dictionary<string, object?>> rows,
        IReadOnlyDictionary<string, object?>? row) => new()
    {
        Data = data,
        Rows = rows,
        Row = row,
    };

    private void RenderBand(IContainer container, ReportBand band, EvalScope scope, RenderCtx ctx,
        bool skipGroupTables = false)
    {
        var c = container.Height(Mm(band.Height));
        if (band.Items.Count == 0)
            return;

        // Bỏ các item không thoả điều kiện hiển thị (VisibleWhen).
        var items = band.Items.Where(i => IsVisible(i, scope)).ToList();
        // Bảng phạm vi "nhóm" nằm trong Detail chỉ vẽ cho dòng đầu của nhóm (tránh lặp N lần).
        if (skipGroupTables)
            items = items.Where(i => !IsGroupScopedTable(i)).ToList();
        if (items.Count == 0)
            return;

        c.Layers(layers =>
        {
            var first = true;
            foreach (var item in items)
            {
                var it = item;
                if (first)
                {
                    first = false;
                    layers.PrimaryLayer().Element(e => RenderItem(e, it, scope, ctx));
                }
                else
                {
                    layers.Layer().Element(e => RenderItem(e, it, scope, ctx));
                }
            }
        });
    }

    /// <summary>Item có thoả điều kiện hiển thị không (lỗi biểu thức → coi như hiện, tránh mất nội dung).</summary>
    private static bool IsVisible(ReportItem item, EvalScope scope)
    {
        if (string.IsNullOrWhiteSpace(item.VisibleWhen))
            return true;
        try
        {
            return ToBool(ExpressionCompiler.Evaluate(item.VisibleWhen, scope));
        }
        catch
        {
            return true;
        }
    }

    /// <summary>Áp các luật định dạng có điều kiện lên kiểu gốc của item.</summary>
    private static ItemStyle ResolveStyle(ReportItem item, EvalScope scope)
    {
        if (item.StyleRules.Count == 0)
            return item.Style;

        var s = item.Style.Clone();
        foreach (var rule in item.StyleRules)
        {
            if (string.IsNullOrWhiteSpace(rule.When))
                continue;

            bool match;
            try
            {
                match = ToBool(ExpressionCompiler.Evaluate(rule.When, scope));
            }
            catch
            {
                continue;
            }
            if (!match)
                continue;

            if (rule.Bold is { } b) s.Bold = b;
            if (rule.Italic is { } it) s.Italic = it;
            if (rule.Size is { } sz) s.Size = sz;
            if (!string.IsNullOrWhiteSpace(rule.Color)) s.Color = rule.Color;
            if (!string.IsNullOrWhiteSpace(rule.Background)) s.Background = rule.Background;
            if (rule.Border is { } bd) s.Border = bd;
        }
        return s;
    }

    /// <summary>Diễn giải giá trị biểu thức thành điều kiện đúng/sai.</summary>
    private static bool ToBool(object? value) => value switch
    {
        null => false,
        bool b => b,
        string s => s.Length > 0
                    && !s.Equals("false", StringComparison.OrdinalIgnoreCase)
                    && s != "0",
        decimal d => d != 0,
        double d => d != 0,
        float f => f != 0,
        int i => i != 0,
        long l => l != 0,
        _ => true,
    };

    private void RenderItem(IContainer container, ReportItem item, EvalScope scope, RenderCtx ctx)
    {
        if (item.Kind == ItemKind.Subreport)
        {
            // Báo cáo con tự quyết chiều cao (chảy trong band); chỉ áp vị trí + bề rộng.
            var subW = ClampWidth(item.W, item.X, ctx.AvailableWidthMm);
            var sc = container
                .AlignLeft()
                .AlignTop()
                .PaddingLeft(Mm(item.X))
                .PaddingTop(Mm(item.Y))
                .Width(Mm(subW));
            RenderSubreport(sc, item, ctx, subW);
            return;
        }

        var width = ClampWidth(item.W, item.X, ctx.AvailableWidthMm);
        var c = container
            .AlignLeft()
            .AlignTop()
            .PaddingLeft(Mm(item.X))
            .PaddingTop(Mm(item.Y))
            .Width(Mm(width))
            .Height(Mm(item.H));

        var style = ResolveStyle(item, scope);
        var align = style.Align.ToLowerInvariant();

        if (item.Kind == ItemKind.Line)
        {
            c.AlignMiddle().LineHorizontal(Math.Max(0.5f, (float)style.Size));
            return;
        }

        if (item.Kind == ItemKind.Rect)
        {
            var rc = c;
            if (style.Border)
                rc = rc.Border(0.5f);
            if (style.Background != null)
                rc = rc.Background(ToColor(style.Background));
            return;
        }

        if (item.Kind == ItemKind.Image && TryLoadImage(item.Src, out var bytes))
        {
            if (IsSvg(bytes))
                c.AlignMiddle().Svg(Encoding.UTF8.GetString(bytes)).FitArea();
            else
                c.AlignMiddle().Image(bytes).FitArea();
            return;
        }

        if (item.Kind == ItemKind.PageNumber)
        {
            RenderPageNumber(c, item, align, style);
            return;
        }

        if (item.Kind == ItemKind.Chart)
        {
            var pts = ChartBuilder.Aggregate(item.Chart, scope.Data, scope.Rows);
            var svg = ChartBuilder.BuildSvg(item.Chart, pts, item.W, item.H);
            c.Svg(svg).FitArea();
            return;
        }

        if (item.Kind == ItemKind.Table)
        {
            RenderTable(c, item, scope);
            return;
        }

        if (Symbology.IsSymbology(item.Kind))
        {
            var value = ResolveValue(item, scope);
            var aspect = item.H > 0.1 ? item.W / item.H : 2.5;
            var sym = Symbology.Encode(item.Kind, item.Symbology, value, aspect);
            if (sym != null)
            {
                var showText = item.Kind == ItemKind.Barcode && (item.Symbology?.ShowText ?? false);
                if (showText)
                {
                    var textMm = Math.Clamp(item.H * 0.30, 2.0, 4.0);
                    var svgMm = Math.Max(1.5, item.H - textMm - 0.4);
                    c.Column(col =>
                    {
                        col.Spacing(0);
                        col.Item().Height(Mm(svgMm)).Svg(sym.Svg).FitArea();
                        col.Item().AlignCenter().Text(t =>
                            t.Span(sym.Value).FontSize((float)Math.Max(5, style.Size - 1)));
                    });
                }
                else
                {
                    c.Svg(sym.Svg).FitArea();
                }
                return;
            }
            // không mã hoá được → in giá trị dạng chữ
        }

        var text = BuildText(item, scope);
        var target = style.Border ? c.Border(0.4f).Padding(1) : c;
        if (!string.IsNullOrWhiteSpace(style.Background))
            target = target.Background(ToColor(style.Background));

        target = align switch
        {
            "center" => target.AlignCenter(),
            "right" => target.AlignRight(),
            _ => target.AlignLeft(),
        };

        target.AlignMiddle().Text(t =>
        {
            var span = t.Span(text).FontSize((float)style.Size);
            if (style.Bold) span.Bold();
            if (style.Italic) span.Italic();
            if (!string.IsNullOrWhiteSpace(style.Color)) span.FontColor(ToColor(style.Color));
        });
    }

    private void RenderTable(IContainer container, ReportItem item, EvalScope scope)
    {
        var spec = item.Table ?? new TableSpec();
        var rows = TableHelper.RowsFor(item.Table, scope.Rows, scope.Data);
        var cols = spec.Columns.Count > 0 ? spec.Columns : new List<TableColumn>();

        if (cols.Count == 0)
        {
            // fallback: cột theo key dòng đầu (không sửa vào definition)
            var first = rows.FirstOrDefault();
            if (first != null)
            {
                var w = item.W / Math.Max(1, first.Count);
                foreach (var key in first.Keys)
                    cols.Add(new TableColumn { Header = key, Binding = key, Width = w });
            }
        }
        if (cols.Count == 0)
            return;

        var rowHmm = Math.Max(2.5, spec.RowHeight);
        var headerH = spec.ShowHeader ? rowHmm : 0;
        var footerH = spec.Footer ? rowHmm : 0;
        var capacity = (int)Math.Floor((item.H - headerH - footerH) / rowHmm);
        if (capacity < 0)
            capacity = 0;
        var limit = capacity;
        if (spec.MaxRows > 0)
            limit = Math.Min(limit, spec.MaxRows);
        var dataRows = rows.Take(limit).ToList();

        var fontSize = (float)Math.Max(4, Math.Min(ResolveStyle(item, scope).Size, rowHmm * 2.8));

        container.Table(table =>
        {
            table.ColumnsDefinition(cd =>
            {
                foreach (var col in cols)
                    cd.RelativeColumn((float)Math.Max(1, col.Width));
            });

            void Cell(IContainer cell, string text, string a, bool bold, string? bg)
            {
                var cc = cell.Height(Mm(rowHmm));
                if (spec.Borders)
                    cc = cc.Border(0.4f).BorderColor(Colors.Grey.Medium);
                if (!string.IsNullOrWhiteSpace(bg))
                    cc = cc.Background(ToColor(bg));
                cc = cc.PaddingHorizontal(1).AlignMiddle();
                cc = a switch
                {
                    "center" => cc.AlignCenter(),
                    "right" => cc.AlignRight(),
                    _ => cc.AlignLeft(),
                };
                cc.Text(t =>
                {
                    var span = t.Span(text).FontSize(fontSize);
                    if (bold)
                        span.Bold();
                });
            }

            if (spec.ShowHeader)
            {
                table.Header(h =>
                {
                    foreach (var col in cols)
                        h.Cell().Element(c => Cell(c, col.Header, "center", true, spec.HeaderBackground));
                });
            }

            for (var r = 0; r < dataRows.Count; r++)
            {
                var row = dataRows[r];
                var bg = spec.Zebra && r % 2 == 1 ? "#F5F5F5" : null;
                foreach (var col in cols)
                {
                    var text = TableHelper.CellText(col, scope.Data, rows, row);
                    var a = col.Align;
                    table.Cell().Element(c => Cell(c, text, a, false, bg));
                }
            }

            if (spec.Footer)
            {
                table.Footer(f =>
                {
                    foreach (var col in cols)
                    {
                        var text = TableHelper.FooterText(col, scope.Data, rows);
                        var a = col.Align;
                        f.Cell().Element(c => Cell(c, text, a, true, "#FAFAFA"));
                    }
                });
            }
        });
    }

    private const int MaxSubreportDepth = 4;

    /// <summary>Bảng dùng phạm vi "nhóm" (mặc định) — chỉ vẽ 1 lần cho mỗi nhóm khi nằm trong Detail.</summary>
    private static bool IsGroupScopedTable(ReportItem item) =>
        item.Kind == ItemKind.Table &&
        (item.Table is null || !item.Table.Scope.Equals("all", StringComparison.OrdinalIgnoreCase));

    /// <summary>Giới hạn bề rộng item theo vùng khả dụng (khi render báo cáo con).</summary>
    private static double ClampWidth(double width, double x, double? available)
    {
        if (available is not { } avail || avail <= 0)
            return width;
        return Math.Max(1, Math.Min(width, avail - x));
    }

    /// <summary>Vẽ item báo cáo con: tra định nghĩa + dữ liệu qua resolver rồi render nội dung.</summary>
    private void RenderSubreport(IContainer container, ReportItem item, RenderCtx ctx, double widthMm)
    {
        var spec = item.Subreport ?? new SubreportSpec();
        var code = spec.ReportCode;

        if (ctx.Depth >= MaxSubreportDepth)
        {
            Placeholder(container, $"Subreport lồng quá sâu: {code}");
            return;
        }
        if (string.IsNullOrWhiteSpace(code))
        {
            Placeholder(container, "Subreport: chưa chọn báo cáo con");
            return;
        }

        var resolution = ctx.Options?.SubreportResolver?.Invoke(spec);
        if (resolution is null)
        {
            Placeholder(container, $"Subreport: không tải được \"{code}\"");
            return;
        }

        var target = container;
        var available = widthMm;
        if (spec.ShowBorder)
        {
            target = target.Border(0.4f).BorderColor(Colors.Grey.Medium);
            available = Math.Max(1, widthMm - 0.5); // chừa chỗ cho viền
        }

        RenderReportInto(target, resolution.Definition, resolution.Data, ctx.Nested(available));
    }

    private static void Placeholder(IContainer container, string text)
    {
        container
            .Border(0.4f).BorderColor(Colors.Grey.Medium)
            .Padding(2)
            .Text(t => t.Span(text).FontSize(7).Italic().FontColor(Colors.Grey.Medium));
    }

    private void RenderPageNumber(IContainer container, ReportItem item, string align, ItemStyle style)
    {
        var template = item.Text ?? item.Expression ?? "Trang {Page}/{Pages}";

        var c = align switch
        {
            "center" => container.AlignCenter(),
            "right" => container.AlignRight(),
            _ => container.AlignLeft(),
        };

        c.AlignMiddle().Text(t =>
        {
            t.DefaultTextStyle(s => s.FontSize((float)style.Size).FontColor(ToColor(style.Color)));
            var parts = SplitTemplate(template);
            foreach (var (isToken, value, token) in parts)
            {
                if (!isToken)
                {
                    t.Span(value);
                }
                else if (token == "Page")
                {
                    t.CurrentPageNumber();
                }
                else if (token == "Pages")
                {
                    t.TotalPages();
                }
                else
                {
                    t.Span(value);
                }
            }
        });
    }

    private static string BuildText(ReportItem item, EvalScope scope)
    {
        switch (item.Kind)
        {
            case ItemKind.Text:
                return Interpolate(item.Text ?? "", scope);
            case ItemKind.Field:
                return ValueFormatter.Format(scope.Data.Resolve(item.Binding ?? "", scope.Row), item.Format);
            case ItemKind.Expression:
                return ValueFormatter.Format(ExpressionCompiler.Evaluate(item.Expression ?? "", scope), item.Format);
            case ItemKind.Barcode:
            case ItemKind.QrCode:
                return ResolveValue(item, scope);
            default:
                return "";
        }
    }

    /// <summary>Giá trị của item theo nguồn dữ liệu (Field / Expression / Text có nội suy).</summary>
    private static string ResolveValue(ReportItem item, EvalScope scope)
    {
        if (!string.IsNullOrWhiteSpace(item.Binding))
            return ValueFormatter.Format(scope.Data.Resolve(item.Binding!, scope.Row), item.Format);
        if (!string.IsNullOrWhiteSpace(item.Expression))
            return ValueFormatter.Format(ExpressionCompiler.Evaluate(item.Expression!, scope), item.Format);
        return Interpolate(item.Text ?? "", scope);
    }

    /// <summary>Thay {biểu thức} trong chuỗi tĩnh bằng giá trị đã tính.</summary>
    private static string Interpolate(string template, EvalScope scope)
    {
        if (string.IsNullOrEmpty(template) || !template.Contains('{'))
            return template;

        var sb = new System.Text.StringBuilder();
        var i = 0;
        while (i < template.Length)
        {
            var open = template.IndexOf('{', i);
            if (open < 0)
            {
                sb.Append(template, i, template.Length - i);
                break;
            }
            var close = template.IndexOf('}', open + 1);
            if (close < 0)
            {
                sb.Append(template, i, template.Length - i);
                break;
            }
            sb.Append(template, i, open - i);
            var expr = template.Substring(open + 1, close - open - 1).Trim();
            try
            {
                sb.Append(ValueFormatter.Format(ExpressionCompiler.Evaluate(expr, scope), null));
            }
            catch
            {
                sb.Append('{').Append(expr).Append('}');
            }
            i = close + 1;
        }
        return sb.ToString();
    }

    private static List<(bool IsToken, string Value, string Token)> SplitTemplate(string template)
    {
        var list = new List<(bool, string, string)>();
        var i = 0;
        while (i < template.Length)
        {
            var open = template.IndexOf('{', i);
            if (open < 0)
            {
                list.Add((false, template[i..], ""));
                break;
            }
            if (open > i)
                list.Add((false, template[i..open], ""));
            var close = template.IndexOf('}', open + 1);
            if (close < 0)
            {
                list.Add((false, template[open..], ""));
                break;
            }
            var token = template.Substring(open + 1, close - open - 1).Trim();
            list.Add((true, "", token));
            i = close + 1;
        }
        return list;
    }

    /// <summary>Ngữ cảnh render dùng chung khi đi qua các band/item (kể cả báo cáo con lồng nhau).</summary>
    private sealed class RenderCtx
    {
        public RenderCtx(ReportRenderOptions? options, string font)
        {
            Options = options;
            Font = font;
        }

        public ReportRenderOptions? Options { get; }
        public string Font { get; }
        public int Depth { get; init; }

        /// <summary>Bề rộng khả dụng (mm) khi render trong khung báo cáo con; null = cả trang.</summary>
        public double? AvailableWidthMm { get; init; }

        public RenderCtx Nested(double? availableWidthMm) => new(Options, Font)
        {
            Depth = Depth + 1,
            AvailableWidthMm = availableWidthMm,
        };
    }

    private static bool TryLoadImage(string? src, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(src))
            return false;

        try
        {
            var comma = src.IndexOf(',');
            if (src.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0)
            {
                var header = src[..comma];
                var payload = src[(comma + 1)..];
                if (header.Contains("base64", StringComparison.OrdinalIgnoreCase))
                    bytes = Convert.FromBase64String(payload);
                else
                    bytes = Encoding.UTF8.GetBytes(Uri.UnescapeDataString(payload));
                return bytes.Length > 0;
            }
            if (File.Exists(src))
            {
                bytes = File.ReadAllBytes(src);
                return bytes.Length > 0;
            }
        }
        catch
        {
            return false;
        }
        return false;
    }

    /// <summary>Đoán nội dung ảnh là SVG (để vẽ véc-tơ thay vì ảnh raster).</summary>
    private static bool IsSvg(byte[] bytes)
    {
        var n = Math.Min(bytes.Length, 1024);
        if (n == 0)
            return false;
        var head = Encoding.UTF8.GetString(bytes, 0, n)
            .TrimStart('\uFEFF', ' ', '\t', '\r', '\n')
            .ToLowerInvariant();
        return head.StartsWith("<?xml") || head.StartsWith("<svg") || head.Contains("<svg");
    }

    private static void EnsureFonts(ReportRenderOptions? options)
    {
        lock (FontLock)
        {
            if (_fontsLoaded)
                return;

            var loaded = 0;

            // 1) font nhúng trong DLL (tự chứa)
            var asm = typeof(QuestPdfReportRenderer).Assembly;
            foreach (var name in asm.GetManifestResourceNames())
            {
                if (!name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
                    continue;
                using var s = asm.GetManifestResourceStream(name);
                if (s == null)
                    continue;
                FontManager.RegisterFont(s);
                loaded++;
            }

            // 2) font từ thư mục ngoài (nếu có) — bổ sung
            var dir = options?.FontDirectory;
            if (loaded == 0 && !string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
            {
                foreach (var file in Directory.GetFiles(dir, "*.ttf"))
                {
                    using var stream = File.OpenRead(file);
                    FontManager.RegisterFont(stream);
                    loaded++;
                }
            }

            _fontsLoaded = loaded > 0;
        }
    }

    private static float Mm(double mm) => (float)(mm * 72.0 / 25.4);

    private static QColor ToColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return Colors.Black;
        try
        {
            return Color.FromHex(hex);
        }
        catch
        {
            return Colors.Black;
        }
    }
}
