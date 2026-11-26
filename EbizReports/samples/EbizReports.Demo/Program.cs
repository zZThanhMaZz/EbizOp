using System.Text;
using EbizReports.Core.Charts;
using EbizReports.Core.Data;
using EbizReports.Core.Expressions;
using EbizReports.Core.Integration;
using EbizReports.Core.Model;
using EbizReports.Core.Rendering;
using EbizReports.Core.Tables;
using EbizReports.Data;
using EbizReports.Rendering;
using EbizReports.Rendering.Integration;
using Microsoft.Data.Sqlite;

Console.OutputEncoding = Encoding.UTF8;

var outDir = Path.Combine(Directory.GetCurrentDirectory(), "out");
Directory.CreateDirectory(outDir);

var engine = new ReportEngine();
var options = new ReportRenderOptions(); // font Lato đã nhúng trong DLL

Console.WriteLine("=== EbizReports · P0 demo ===");
Console.WriteLine($"Output: {outDir}");
Console.WriteLine();

// ---------------------------------------------------------------- 1) HÓA ĐƠN
var invoice = BuildInvoice();
var invoiceData = InvoiceData();

// JSON round-trip (chứng minh template lưu/đọc được)
var json = invoice.ToJson();
File.WriteAllText(Path.Combine(outDir, "hoa-don.json"), json, new UTF8Encoding(false));
var reloaded = ReportDefinition.FromJson(json);
Console.WriteLine($"Định nghĩa hóa đơn: {json.Length:N0} bytes JSON, round-trip OK = {reloaded.Code == invoice.Code}");

RenderTo(engine, invoice, invoiceData, outDir, "hoa-don", options);
var previewPng = new QuestPdfReportRenderer().RenderFirstPagePng(invoice, invoiceData, options);
File.WriteAllBytes(Path.Combine(outDir, "hoa-don-p1.png"), previewPng);
Console.WriteLine($"  {"hoa-don-p1 (png)",-24} {previewPng.Length,7:N0}B");

// ---------------------------------------------------------------- 2) BÁO CÁO NHÓM
var sales = BuildSalesByGroup();
var salesData = SalesData();
RenderTo(engine, sales, salesData, outDir, "ban-hang-theo-nhom", options);

Console.WriteLine();
Console.WriteLine("Xong. Mở file .pdf/.xlsx/.csv trong thư mục out để kiểm tra.");
Console.WriteLine();
ParameterSelfTest(engine, invoice, options, outDir);
ChartSelfTest(engine, options, outDir);
TableSelfTest(engine, options, outDir);
IntegrationSelfTest(engine, options, outDir);
SubreportSelfTest(engine, options, outDir);
ConditionalFormatSelfTest(engine, options, outDir);
ImageSelfTest(engine, options, outDir, previewPng);
SortingSelfTest(engine, options, outDir);
FieldCatalogSelfTest();
PngPagesSelfTest(options, outDir);
GroupTableSelfTest(options, outDir);
BackupRestoreSelfTest(engine, outDir);
ExpressionBuilderSelfTest();
PaginationSelfTest(options, outDir);
AuditSelfTest(engine, options);
SqlDataSourceSelfTest(engine, options, outDir);
SymbologySelfTest();

static void RenderTo(ReportEngine engine, ReportDefinition def, ReportData data, string dir, string baseName,
    ReportRenderOptions options)
{
    var pdf = engine.Render(def, data, RenderFormat.Pdf, options);
    File.WriteAllBytes(Path.Combine(dir, baseName + ".pdf"), pdf);

    var xlsx = engine.Render(def, data, RenderFormat.Xlsx, options);
    File.WriteAllBytes(Path.Combine(dir, baseName + ".xlsx"), xlsx);

    var csv = engine.Render(def, data, RenderFormat.Csv, options);
    File.WriteAllBytes(Path.Combine(dir, baseName + ".csv"), csv);

    Console.WriteLine($"  {baseName,-24} pdf={pdf.Length,7:N0}B  xlsx={xlsx.Length,6:N0}B  csv={csv.Length,6:N0}B");
}

static ReportDefinition BuildInvoice()
{
    var def = new ReportDefinition
    {
        Code = "hoa-don-ban-hang",
        Name = "Hóa đơn bán hàng",
        Paper = new PaperSettings
        {
            Size = PaperSizeKind.A5,
            Orientation = PaperOrientation.Portrait,
            Margins = new Margins { Top = 8, Right = 6, Bottom = 8, Left = 6 },
        },
        Outputs = new List<string> { "pdf", "xlsx", "csv" },
        Parameters =
        {
            new ReportParameter
            {
                Name = "TieuDe",
                Type = ReportParameterType.String,
                Label = "Tiêu đề hóa đơn",
                Default = "HÓA ĐƠN BÁN HÀNG",
            },
        },
    };

    def.Bands.Add(new ReportBand
    {
        Kind = BandKind.PageHeader,
        Height = 30,
        Items =
        {
            new ReportItem { Kind = ItemKind.Text, X = 0, Y = 0, W = 80, H = 7, Text = "{StoreName}", Style = new ItemStyle { Size = 14, Bold = true } },
            new ReportItem { Kind = ItemKind.Text, X = 0, Y = 7, W = 80, H = 5, Text = "{StoreAddress}", Style = new ItemStyle { Size = 7 } },
            new ReportItem { Kind = ItemKind.Text, X = 0, Y = 12, W = 80, H = 5, Text = "ĐT: {StorePhone}", Style = new ItemStyle { Size = 7 } },
            new ReportItem { Kind = ItemKind.Text, X = 0, Y = 19, W = 136, H = 6, Text = "{TieuDe}", Style = new ItemStyle { Size = 12, Bold = true, Align = "center" } },
            new ReportItem { Kind = ItemKind.Text, X = 86, Y = 0, W = 50, H = 5, Text = "Số: {BillNo}", Style = new ItemStyle { Size = 8, Align = "right" } },
            new ReportItem { Kind = ItemKind.Expression, X = 86, Y = 5, W = 50, H = 5, Expression = "Now()", Format = "dd/MM/yyyy HH:mm", Style = new ItemStyle { Size = 7, Align = "right" } },
            new ReportItem { Kind = ItemKind.Text, X = 0, Y = 26, W = 136, H = 4, Text = "Khách hàng: {CustomerName}   ·   Thu ngân: {CashierName}", Style = new ItemStyle { Size = 8 } },
        },
    });

    def.Bands.Add(new ReportBand
    {
        Kind = BandKind.ReportHeader,
        Height = 9,
        Items =
        {
            new ReportItem { Kind = ItemKind.Text, X = 0, Y = 1, W = 62, H = 6, Text = "Tên hàng", Style = new ItemStyle { Size = 8, Bold = true } },
            new ReportItem { Kind = ItemKind.Text, X = 62, Y = 1, W = 14, H = 6, Text = "SL", Style = new ItemStyle { Size = 8, Bold = true, Align = "center" } },
            new ReportItem { Kind = ItemKind.Text, X = 76, Y = 1, W = 28, H = 6, Text = "Đơn giá", Style = new ItemStyle { Size = 8, Bold = true, Align = "right" } },
            new ReportItem { Kind = ItemKind.Text, X = 104, Y = 1, W = 32, H = 6, Text = "Thành tiền", Style = new ItemStyle { Size = 8, Bold = true, Align = "right" } },
            new ReportItem { Kind = ItemKind.Line, X = 0, Y = 8, W = 136, H = 1, Style = new ItemStyle { Size = 0.6 } },
        },
    });

    def.Bands.Add(new ReportBand
    {
        Kind = BandKind.Detail,
        Height = 6,
        Items =
        {
            new ReportItem { Kind = ItemKind.Field, X = 0, Y = 0, W = 62, H = 6, Binding = "Name", Style = new ItemStyle { Size = 8 } },
            new ReportItem { Kind = ItemKind.Field, X = 62, Y = 0, W = 14, H = 6, Binding = "Qty", Format = "0.###", Style = new ItemStyle { Size = 8, Align = "center" } },
            new ReportItem { Kind = ItemKind.Field, X = 76, Y = 0, W = 28, H = 6, Binding = "UnitPrice", Format = "N0", Style = new ItemStyle { Size = 8, Align = "right" } },
            new ReportItem { Kind = ItemKind.Field, X = 104, Y = 0, W = 32, H = 6, Binding = "LineTotal", Format = "N0", Style = new ItemStyle { Size = 8, Align = "right" } },
        },
    });

    def.Bands.Add(new ReportBand
    {
        Kind = BandKind.ReportFooter,
        Height = 46,
        Items =
        {
            new ReportItem { Kind = ItemKind.Line, X = 0, Y = 2, W = 136, H = 1, Style = new ItemStyle { Size = 0.6 } },
            new ReportItem { Kind = ItemKind.Text, X = 76, Y = 4, W = 28, H = 6, Text = "Khuyến mãi", Style = new ItemStyle { Size = 8, Align = "right" } },
            new ReportItem { Kind = ItemKind.Field, X = 104, Y = 4, W = 32, H = 6, Binding = "DiscountTotal", Format = "N0", Style = new ItemStyle { Size = 8, Align = "right" } },
            new ReportItem { Kind = ItemKind.Text, X = 76, Y = 10, W = 28, H = 6, Text = "TỔNG CỘNG", Style = new ItemStyle { Size = 10, Bold = true, Align = "right" } },
            new ReportItem { Kind = ItemKind.Expression, X = 104, Y = 10, W = 32, H = 6, Expression = "Sum(LineTotal) - DiscountTotal", Format = "N0", Style = new ItemStyle { Size = 10, Bold = true, Align = "right" } },
            new ReportItem
            {
                Kind = ItemKind.QrCode, X = 0, Y = 16, W = 26, H = 26, Text = "{BillNo}",
                Symbology = new SymbologySpec { Ecc = "M" },
            },
            new ReportItem
            {
                Kind = ItemKind.Barcode, X = 32, Y = 20, W = 104, H = 12, Text = "{BillNo}",
                Symbology = new SymbologySpec { Format = "Code128", ShowText = true },
            },
            new ReportItem { Kind = ItemKind.Text, X = 0, Y = 42, W = 136, H = 4, Text = "Cảm ơn quý khách — hẹn gặp lại!", Style = new ItemStyle { Size = 7, Align = "center" } },
        },
    });

    def.Bands.Add(new ReportBand
    {
        Kind = BandKind.PageFooter,
        Height = 6,
        Items =
        {
            new ReportItem { Kind = ItemKind.PageNumber, X = 0, Y = 0, W = 136, H = 6, Text = "Trang {Page}/{Pages}", Style = new ItemStyle { Size = 7, Align = "center", Color = "#888888" } },
        },
    });

    return def;
}

static ReportData InvoiceData()
{
    var data = new ReportData()
        .SetField("StoreName", "KÍNH MẮT EBIZOP")
        .SetField("StoreAddress", "123 Lê Lợi, Q1, TP.HCM")
        .SetField("StorePhone", "028 3825 0000")
        .SetField("BillNo", "#B-1123-0001")
        .SetField("CustomerName", "Nguyễn Văn A")
        .SetField("CashierName", "Lan")
        .SetField("DiscountTotal", 20000);

    data.AddRow(("Name", "Gọng Titan Nam T-01"), ("Qty", 1m), ("UnitPrice", 850000), ("LineTotal", 850000));
    data.AddRow(("Name", "Tròng đổi màu 1.56"), ("Qty", 2m), ("UnitPrice", 320000), ("LineTotal", 640000));
    data.AddRow(("Name", "Khăn lau + hộp"), ("Qty", 1m), ("UnitPrice", 50000), ("LineTotal", 50000));
    return data;
}

static ReportDefinition BuildSalesByGroup()
{
    var def = new ReportDefinition
    {
        Code = "ban-hang-theo-nhom",
        Name = "Bán hàng theo nhóm",
        Paper = new PaperSettings
        {
            Size = PaperSizeKind.A4,
            Orientation = PaperOrientation.Landscape,
            Margins = new Margins { Top = 10, Right = 8, Bottom = 10, Left = 8 },
        },
        Grouping = { new ReportGroup { Field = "GroupName", Sort = "asc" } },
        Parameters =
        {
            new ReportParameter
            {
                Name = "TuNgay",
                Type = ReportParameterType.Date,
                Label = "Từ ngày",
                Default = "2026-01-01",
            },
            new ReportParameter
            {
                Name = "DenNgay",
                Type = ReportParameterType.Date,
                Label = "Đến ngày",
                Default = "2026-03-31",
            },
        },
    };

    def.Bands.Add(new ReportBand
    {
        Kind = BandKind.PageHeader,
        Height = 10,
        Items =
        {
            new ReportItem { Kind = ItemKind.Text, X = 0, Y = 0, W = 150, H = 7, Text = "BÁO CÁO BÁN HÀNG THEO NHÓM", Style = new ItemStyle { Size = 13, Bold = true } },
            new ReportItem { Kind = ItemKind.Text, X = 155, Y = 2, W = 24, H = 5, Text = "Từ ngày", Style = new ItemStyle { Size = 8, Align = "right" } },
            new ReportItem { Kind = ItemKind.Expression, X = 181, Y = 0, W = 46, H = 7, Expression = "TuNgay", Format = "dd/MM/yyyy", Style = new ItemStyle { Size = 10, Align = "right" } },
            new ReportItem { Kind = ItemKind.Text, X = 229, Y = 2, W = 14, H = 5, Text = "đến", Style = new ItemStyle { Size = 8, Align = "center" } },
            new ReportItem { Kind = ItemKind.Expression, X = 245, Y = 0, W = 36, H = 7, Expression = "DenNgay", Format = "dd/MM/yyyy", Style = new ItemStyle { Size = 10, Align = "right" } },
        },
    });

    def.Bands.Add(new ReportBand
    {
        Kind = BandKind.GroupHeader,
        Height = 8,
        Items =
        {
            new ReportItem { Kind = ItemKind.Text, X = 0, Y = 1, W = 80, H = 6, Text = "Nhóm: {GroupName}", Style = new ItemStyle { Size = 10, Bold = true, Color = "#1E5AA8" } },
        },
    });

    def.Bands.Add(new ReportBand
    {
        Kind = BandKind.Detail,
        Height = 6,
        Items =
        {
            new ReportItem { Kind = ItemKind.Field, X = 4, Y = 0, W = 120, H = 6, Binding = "Name", Style = new ItemStyle { Size = 8 } },
            new ReportItem { Kind = ItemKind.Field, X = 130, Y = 0, W = 20, H = 6, Binding = "Qty", Format = "0.###", Style = new ItemStyle { Size = 8, Align = "center" } },
            new ReportItem { Kind = ItemKind.Field, X = 190, Y = 0, W = 40, H = 6, Binding = "LineTotal", Format = "N0", Style = new ItemStyle { Size = 8, Align = "right" } },
        },
    });

    def.Bands.Add(new ReportBand
    {
        Kind = BandKind.GroupFooter,
        Height = 7,
        Items =
        {
            new ReportItem { Kind = ItemKind.Text, X = 130, Y = 0, W = 60, H = 6, Text = "Cộng nhóm:", Style = new ItemStyle { Size = 8, Bold = true, Align = "right" } },
            new ReportItem { Kind = ItemKind.Expression, X = 190, Y = 0, W = 40, H = 6, Expression = "Sum(LineTotal)", Format = "N0", Style = new ItemStyle { Size = 8, Bold = true, Align = "right" } },
        },
    });

    def.Bands.Add(new ReportBand
    {
        Kind = BandKind.ReportFooter,
        Height = 74,
        Items =
        {
            new ReportItem { Kind = ItemKind.Line, X = 0, Y = 1, W = 260, H = 1, Style = new ItemStyle { Size = 0.8 } },
            new ReportItem { Kind = ItemKind.Text, X = 130, Y = 3, W = 60, H = 6, Text = "TỔNG DOANH THU:", Style = new ItemStyle { Size = 10, Bold = true, Align = "right" } },
            new ReportItem { Kind = ItemKind.Expression, X = 190, Y = 3, W = 40, H = 6, Expression = "Sum(LineTotal)", Format = "N0", Style = new ItemStyle { Size = 10, Bold = true, Align = "right" } },
            new ReportItem
            {
                Kind = ItemKind.Chart, X = 0, Y = 12, W = 260, H = 60,
                Chart = new ChartSpec
                {
                    Type = "bar", Category = "Name", Value = "LineTotal", Aggregate = "sum",
                    Legend = "none", ShowValues = true, Title = "Doanh thu theo sản phẩm",
                },
            },
        },
    });

    return def;
}

static ReportDefinition BuildChartReport(string type) => new()
{
    Code = "chart-demo-" + type,
    Name = "Biểu đồ demo",
    Paper = new PaperSettings { Size = PaperSizeKind.A4, Orientation = PaperOrientation.Landscape },
    Bands =
    {
        new ReportBand
        {
            Kind = BandKind.ReportHeader,
            Height = 135,
            Items =
            {
                new ReportItem { Kind = ItemKind.Text, X = 0, Y = 0, W = 270, H = 8, Text = "Biểu đồ: " + type, Style = new ItemStyle { Size = 13, Bold = true } },
                new ReportItem
                {
                    Kind = ItemKind.Chart, X = 0, Y = 12, W = 270, H = 118,
                    Chart = new ChartSpec
                    {
                        Type = type, Category = "GroupName", Value = "LineTotal", Aggregate = "sum",
                        Legend = type == "pie" ? "bottom" : "right",
                        ShowValues = type != "line",
                        Title = "Doanh thu theo nhóm",
                    },
                },
            },
        },
    },
};

static ReportDefinition BuildTableReport() => new()
{
    Code = "table-demo",
    Name = "Bảng demo",
    Paper = new PaperSettings { Size = PaperSizeKind.A4, Orientation = PaperOrientation.Portrait },
    Bands =
    {
        new ReportBand
        {
            Kind = BandKind.ReportHeader,
            Height = 100,
            Items =
            {
                new ReportItem { Kind = ItemKind.Text, X = 0, Y = 0, W = 198, H = 8, Text = "TOP SẢN PHẨM BÁN CHẠY", Style = new ItemStyle { Size = 14, Bold = true } },
                new ReportItem
                {
                    Kind = ItemKind.Table, X = 0, Y = 12, W = 198, H = 84,
                    Table = new TableSpec
                    {
                        ShowHeader = true,
                        Borders = true,
                        Zebra = true,
                        Footer = true,
                        RowHeight = 8,
                        HeaderBackground = "#1E5AA8",
                        Columns =
                        {
                            new TableColumn { Header = "Tên hàng", Binding = "Name", Width = 96 },
                            new TableColumn { Header = "Nhóm", Binding = "GroupName", Width = 46 },
                            new TableColumn { Header = "SL", Binding = "Qty", Width = 18, Align = "center", Aggregate = "sum" },
                            new TableColumn { Header = "Doanh thu", Binding = "LineTotal", Width = 38, Align = "right", Aggregate = "sum", Format = "N0" },
                        },
                    },
                },
            },
        },
    },
};

static ReportData SalesData()
{
    var data = new ReportData();
    void Add(string group, string name, decimal qty, int price) =>
        data.AddRow(("GroupName", group), ("Name", name), ("Qty", qty), ("LineTotal", (int)(qty * price)));

    Add("Gọng", "Gọng nhựa trẻ em", 3, 180000);
    Add("Gọng", "Gọng Titan Nam T-01", 2, 850000);
    Add("Gọng", "Gọng khoan nữ K-12", 1, 1200000);
    Add("Tròng", "Tròng đổi màu 1.56", 5, 320000);
    Add("Tròng", "Tròng chống ánh sáng xanh", 4, 450000);
    Add("Phụ kiện", "Khăn lau", 12, 15000);
    Add("Phụ kiện", "Hộp đựng", 6, 35000);
    return data;
}

static void ParameterSelfTest(ReportEngine engine, ReportDefinition invoice, ReportRenderOptions options, string outDir)
{
    Console.WriteLine("=== Kiểm chứng tham số ===");

    // 1) engine tự áp giá trị mặc định khi dữ liệu chưa cung cấp tham số
    var d1 = InvoiceData();
    Console.WriteLine($"  trước render: {d1.Parameters.Count} tham số");
    engine.Render(invoice, d1, RenderFormat.Pdf, options);
    var applied = string.Join(", ", d1.Parameters.OrderBy(k => k.Key).Select(kv => $"{kv.Key}={kv.Value}"));
    Console.WriteLine($"  engine áp mặc định: {applied}");

    // 2) tham số dùng được trong biểu thức / nội suy
    var scope = new EvalScope { Data = d1 };
    var title = ExpressionCompiler.EvaluateText("TieuDe", scope);
    Console.WriteLine($"  EvaluateText(\"TieuDe\") = \"{title}\"  ({(title == "HÓA ĐƠN BÁN HÀNG" ? "OK" : "FAIL")})");

    // 3) ghi đè tham số rồi render PNG để xem tiêu đề thay đổi
    var d2 = InvoiceData().SetParam("TieuDe", "PHIẾU THANH TOÁN");
    var png = new QuestPdfReportRenderer().RenderFirstPagePng(invoice, d2, options);
    File.WriteAllBytes(Path.Combine(outDir, "hoa-don-thamso.png"), png);
    Console.WriteLine($"  ghi đè TieuDe = \"PHIẾU THANH TOÁN\" → hoa-don-thamso.png ({png.Length:N0}B)");

    // 4) tham số kiểu ngày (DateTime) + format
    var s = SalesData();
    engine.Render(BuildSalesByGroup(), s, RenderFormat.Pdf, options);
    s.Parameters.TryGetValue("TuNgay", out var tu);
    Console.WriteLine($"  kiểu ngày: TuNgay={tu} ({tu?.GetType().Name}) → \"{ValueFormatter.Format(tu, "dd/MM/yyyy")}\"");
    Console.WriteLine();
}

static void ChartSelfTest(ReportEngine engine, ReportRenderOptions options, string outDir)
{
    Console.WriteLine("=== Kiểm chứng biểu đồ (tổng hợp + SVG + render) ===");
    var data = SalesData();
    var rows = data.Details;

    // 1) tổng hợp sum(LineTotal) theo nhóm
    var spec = new ChartSpec { Type = "bar", Category = "GroupName", Value = "LineTotal", Aggregate = "sum" };
    var pts = ChartBuilder.Aggregate(spec, data, rows);
    Console.WriteLine("  tổng hợp: " + string.Join("  ", pts.Select(p => $"{p.Category}={p.Value:N0}")));

    var expect = new Dictionary<string, double>
    {
        ["Gọng"] = 3_440_000, ["Tròng"] = 3_400_000, ["Phụ kiện"] = 390_000,
    };
    var okAgg = pts.Count == 3 &&
        pts.All(p => expect.TryGetValue(p.Category, out var e) && Math.Abs(p.Value - e) < 0.5);
    Console.WriteLine($"  {(okAgg ? "OK  " : "FAIL")}  tổng hợp khớp giá trị mong đợi (3 nhóm)");

    // 2) count theo nhóm
    var pc = ChartBuilder.Aggregate(new ChartSpec { Category = "GroupName", Aggregate = "count" }, data, rows);
    Console.WriteLine("  count: " + string.Join("  ", pc.Select(p => $"{p.Category}={p.Value:0}")));

    // 3) SVG cho 3 loại
    foreach (var t in new[] { "bar", "line", "pie" })
    {
        var sp = new ChartSpec { Type = t, Title = "Doanh thu theo nhóm", Legend = t == "pie" ? "bottom" : "right", ShowValues = true };
        var svg = ChartBuilder.BuildSvg(sp, pts, 120, 60);
        var okSvg = svg.StartsWith("<svg") && svg.EndsWith("</svg>") && (svg.Contains("<rect") || svg.Contains("<path"));
        Console.WriteLine($"  {(okSvg ? "OK  " : "FAIL")}  svg {t,-4} {svg.Length,5}B");
    }

    // 4) render PNG thật (3 loại) để kiểm tra trực quan
    foreach (var t in new[] { "bar", "line", "pie" })
    {
        var png = new QuestPdfReportRenderer().RenderFirstPagePng(BuildChartReport(t), SalesData(), options);
        File.WriteAllBytes(Path.Combine(outDir, $"chart-{t}.png"), png);
        Console.WriteLine($"  {($"chart-{t}.png"),-20} {png.Length,7:N0}B");
    }
    Console.WriteLine();
}

static void TableSelfTest(ReportEngine engine, ReportRenderOptions options, string outDir)
{
    Console.WriteLine("=== Kiểm chứng bảng (item Bảng) ===");
    var data = SalesData();
    var rows = data.Details;

    var nameCol = new TableColumn { Header = "Tên hàng", Binding = "Name" };
    var c1 = TableHelper.CellText(nameCol, data, rows, rows[0]);
    Console.WriteLine($"  ô dòng 1 (Name) = \"{c1}\"  ({(c1 == "Gọng nhựa trẻ em" ? "OK" : "FAIL")})");

    var qtyCol = new TableColumn { Header = "SL", Binding = "Qty", Aggregate = "sum", Format = "N0" };
    var ft = TableHelper.FooterText(qtyCol, data, rows);
    Console.WriteLine($"  tổng SL = \"{ft}\"  ({(ft == "33" ? "OK" : "FAIL")})");

    var moneyCol = new TableColumn { Header = "Doanh thu", Binding = "LineTotal", Aggregate = "sum", Format = "N0" };
    var fm = TableHelper.FooterText(moneyCol, data, rows);
    Console.WriteLine($"  tổng doanh thu = \"{fm}\"  ({(fm == "7.230.000" ? "OK" : "FAIL")})");

    var avg = TableHelper.FooterText(new TableColumn { Binding = "LineTotal", Aggregate = "avg", Format = "N0" }, data, rows);
    var cnt = TableHelper.FooterText(new TableColumn { Binding = "LineTotal", Aggregate = "count", Format = "N0" }, data, rows);
    Console.WriteLine($"  avg doanh thu = \"{avg}\" · count = \"{cnt}\"");

    var png = new QuestPdfReportRenderer().RenderFirstPagePng(BuildTableReport(), SalesData(), options);
    File.WriteAllBytes(Path.Combine(outDir, "table-demo.png"), png);
    Console.WriteLine($"  {"table-demo.png",-20} {png.Length,7:N0}B");
    Console.WriteLine();
}

static void IntegrationSelfTest(ReportEngine engine, ReportRenderOptions options, string outDir)
{
    Console.WriteLine("=== Kiểm chứng lớp tích hợp (ReportService) ===");

    var invoice = BuildInvoice();
    var sales = BuildSalesByGroup();
    var store = new InMemoryReportStore(new[] { invoice, sales });
    var provider = new DelegateReportDataProvider((def, _) =>
        def.Code == "ban-hang-theo-nhom" ? SalesData() : InvoiceData());
    var service = new ReportService(store, provider, engine);

    var list = store.List();
    Console.WriteLine("  store.List() = " + string.Join(", ", list.Select(d => d.Code)));
    Console.WriteLine($"  {(list.Count == 2 ? "OK  " : "FAIL")}  liệt kê report trong kho");
    Console.WriteLine($"  {(service.Find("hoa-don-ban-hang")?.Code == invoice.Code ? "OK  " : "FAIL")}  Get/Find theo mã");

    var pdf = service.Render("hoa-don-ban-hang", RenderFormat.Pdf, null, options);
    File.WriteAllBytes(Path.Combine(outDir, "service-hoa-don.pdf"), pdf);
    Console.WriteLine($"  {(pdf.Length > 1000 ? "OK  " : "FAIL")}  Render qua service → pdf {pdf.Length:N0}B");

    var eff = ReportService.EffectiveParameters(invoice);
    Console.WriteLine("  tham số hiệu lực (mặc định): " + string.Join(", ", eff.Select(kv => $"{kv.Key}={kv.Value}")));
    var pdf2 = service.Render(invoice, RenderFormat.Pdf,
        new Dictionary<string, object?> { ["TieuDe"] = "PHIẾU THANH TOÁN" }, options);
    Console.WriteLine($"  {(pdf2.Length > 1000 ? "OK  " : "FAIL")}  Render với ghi đè tham số → {pdf2.Length:N0}B");

    // mã sai
    try
    {
        service.Render("khong-ton-tai", RenderFormat.Pdf, null, options);
        Console.WriteLine("  FAIL  mã sai phải ném ReportNotFoundException");
    }
    catch (ReportNotFoundException ex)
    {
        Console.WriteLine($"  OK    mã sai → ReportNotFoundException ({ex.Code})");
    }

    // phân quyền: chặn cả render lẫn design
    var deny = new ReportService(store, provider, engine, new DenyAllAuthorizer());
    try
    {
        deny.Render("hoa-don-ban-hang", RenderFormat.Pdf, null, options);
        Console.WriteLine("  FAIL  thiếu quyền render phải bị chặn");
    }
    catch (ReportForbiddenException ex)
    {
        Console.WriteLine($"  OK    chặn render → ReportForbiddenException ({ex.Action})");
    }
    try
    {
        deny.Save(invoice);
        Console.WriteLine("  FAIL  thiếu quyền design phải bị chặn");
    }
    catch (ReportForbiddenException ex)
    {
        Console.WriteLine($"  OK    chặn lưu → ReportForbiddenException ({ex.Action})");
    }

    // kho file: ghi → đọc lại
    var dir = Path.Combine(outDir, "store");
    if (Directory.Exists(dir))
        Directory.Delete(dir, true);
    new FileReportStore(dir, new[] { invoice, sales }).Save(invoice);
    var reloaded = new FileReportStore(dir).Get("hoa-don-ban-hang");
    Console.WriteLine($"  {(File.Exists(Path.Combine(dir, "hoa-don-ban-hang.json")) ? "OK  " : "FAIL")}  FileReportStore ghi JSON ({dir})");
    Console.WriteLine($"  {(reloaded?.Code == invoice.Code && reloaded.Name == invoice.Name ? "OK  " : "FAIL")}  FileReportStore đọc lại từ đĩa");
    Console.WriteLine();
}

static ReportDefinition BuildSummaryReport() => new()
{
    Code = "summary-demo",
    Name = "Tổng hợp (có báo cáo con)",
    Paper = new PaperSettings { Size = PaperSizeKind.A4, Orientation = PaperOrientation.Portrait },
    Bands =
    {
        new ReportBand
        {
            Kind = BandKind.PageHeader,
            Height = 16,
            Items =
            {
                new ReportItem { Kind = ItemKind.Text, X = 0, Y = 0, W = 198, H = 8, Text = "BÁO CÁO TỔNG HỢP", Style = new ItemStyle { Size = 14, Bold = true } },
            },
        },
        new ReportBand
        {
            Kind = BandKind.ReportFooter,
            Height = 120,
            Items =
            {
                new ReportItem
                {
                    Kind = ItemKind.Subreport, X = 0, Y = 0, W = 198, H = 118,
                    Subreport = new SubreportSpec
                    {
                        ReportCode = "table-demo",
                        InheritParameters = true,
                        ShowBorder = true,
                    },
                },
            },
        },
    },
};

static void SubreportSelfTest(ReportEngine engine, ReportRenderOptions options, string outDir)
{
    Console.WriteLine("=== Kiểm chứng báo cáo con (Subreport) ===");

    var child = BuildTableReport();
    var childData = SalesData();
    var parent = BuildSummaryReport();
    var parentData = SalesData();

    var calls = 0;
    var resolverOptions = new ReportRenderOptions
    {
        FontDirectory = options.FontDirectory,
        FontFamily = options.FontFamily,
        SubreportResolver = spec =>
        {
            calls++;
            return string.Equals(spec.ReportCode, child.Code, StringComparison.OrdinalIgnoreCase)
                ? new SubreportResolution { Definition = child, Data = childData }
                : null;
        },
    };

    var pdf = engine.Render(parent, parentData, RenderFormat.Pdf, resolverOptions);
    File.WriteAllBytes(Path.Combine(outDir, "subreport-demo.pdf"), pdf);
    var callsAfterPdf = calls;
    var png = new QuestPdfReportRenderer().RenderFirstPagePng(parent, parentData, resolverOptions);
    File.WriteAllBytes(Path.Combine(outDir, "subreport-demo.png"), png);
    Console.WriteLine($"  {(callsAfterPdf == 1 ? "OK  " : "FAIL")}  resolver được gọi đúng 1 lần mỗi lượt render (calls={callsAfterPdf})");
    Console.WriteLine($"  {(pdf.Length > 2000 ? "OK  " : "FAIL")}  PDF có nội dung ({pdf.Length:N0}B)");
    Console.WriteLine($"  subreport-demo.png   {png.Length:N0}B");

    // báo cáo con không tồn tại → in placeholder, không ném
    var missing = engine.Render(parent, parentData, RenderFormat.Pdf, new ReportRenderOptions
    {
        FontDirectory = options.FontDirectory,
        SubreportResolver = _ => null,
    });
    Console.WriteLine($"  {(missing.Length > 2000 ? "OK  " : "FAIL")}  thiếu báo cáo con → placeholder, vẫn render ({missing.Length:N0}B)");

    // qua ReportService: tự nối resolver từ kho + nguồn dữ liệu
    var store = new InMemoryReportStore(new[] { parent, child });
    var provider = new DelegateReportDataProvider((def, _) => def.Code == child.Code ? childData : parentData);
    var service = new ReportService(store, provider, engine);
    var viaService = service.Render(parent.Code, RenderFormat.Pdf, null, options);
    File.WriteAllBytes(Path.Combine(outDir, "subreport-service.pdf"), viaService);
    Console.WriteLine($"  {(viaService.Length > 2000 ? "OK  " : "FAIL")}  ReportService tự nối resolver từ kho ({viaService.Length:N0}B)");

    // thiếu quyền render báo cáo con → placeholder, không ném
    var denyService = new ReportService(new InMemoryReportStore(new[] { parent, child }), provider, engine,
        new ChildDenyAuthorizer(child.Code));
    var denied = denyService.Render(parent.Code, RenderFormat.Pdf, null, options);
    Console.WriteLine($"  {(denied.Length > 2000 ? "OK  " : "FAIL")}  thiếu quyền báo cáo con → placeholder ({denied.Length:N0}B)");

    Console.WriteLine();
}

static ReportDefinition BuildCondReport(bool withCondition, bool withRule) => new()
{
    Code = "cond-demo",
    Name = "Định dạng có điều kiện",
    Paper = new PaperSettings { Size = PaperSizeKind.A5, Orientation = PaperOrientation.Portrait },
    Bands =
    {
        new ReportBand
        {
            Kind = BandKind.ReportHeader,
            Height = 10,
            Items =
            {
                new ReportItem { Kind = ItemKind.Text, X = 0, Y = 0, W = 136, H = 8, Text = "ĐỊNH DẠNG CÓ ĐIỀU KIỆN", Style = new ItemStyle { Size = 13, Bold = true } },
            },
        },
        new ReportBand
        {
            Kind = BandKind.Detail,
            Height = 7,
            Items =
            {
                new ReportItem
                {
                    Kind = ItemKind.Field, X = 0, Y = 0, W = 76, H = 6, Binding = "Name",
                    VisibleWhen = withCondition ? "Qty > 2" : null,
                    StyleRules = withRule
                        ? new List<StyleRule>
                        {
                            new() { When = "Qty > 5", Color = "#C62828", Bold = true },
                        }
                        : new List<StyleRule>(),
                },
                new ReportItem
                {
                    Kind = ItemKind.Expression, X = 76, Y = 0, W = 40, H = 6, Expression = "LineTotal", Format = "N0",
                    Style = new ItemStyle { Align = "right" },
                    StyleRules = withRule
                        ? new List<StyleRule>
                        {
                            new() { When = "LineTotal >= 3000000", Color = "#C62828" },
                        }
                        : new List<StyleRule>(),
                },
            },
        },
    },
};

static void ConditionalFormatSelfTest(ReportEngine engine, ReportRenderOptions options, string outDir)
{
    Console.WriteLine("=== Kiểm chứng định dạng có điều kiện (VisibleWhen + StyleRule) ===");

    var baseline = engine.Render(BuildCondReport(false, false), SalesData(), RenderFormat.Pdf, options);
    var hiddenOnly = engine.Render(BuildCondReport(true, false), SalesData(), RenderFormat.Pdf, options);
    var conditional = engine.Render(BuildCondReport(true, true), SalesData(), RenderFormat.Pdf, options);
    File.WriteAllBytes(Path.Combine(outDir, "cond-format.pdf"), conditional);

    var png = new QuestPdfReportRenderer().RenderFirstPagePng(BuildCondReport(true, true), SalesData(), options);
    File.WriteAllBytes(Path.Combine(outDir, "cond-format.png"), png);

    // VisibleWhen "Qty > 2" ẩn 2/7 dòng → ít item hơn baseline (so cùng cấu hình, không có luật màu).
    Console.WriteLine($"  baseline {baseline.Length:N0}B · ẩn bớt {hiddenOnly.Length:N0}B · có luật màu {conditional.Length:N0}B · png {png.Length:N0}B");
    Console.WriteLine($"  {(hiddenOnly.Length < baseline.Length ? "OK  " : "FAIL")}  VisibleWhen ẩn bớt item → PDF nhỏ hơn");
    Console.WriteLine($"  {(conditional.Length != hiddenOnly.Length ? "OK  " : "FAIL")}  StyleRule đổi định dạng → PDF khác bản không luật");
    Console.WriteLine();
}

static void ImageSelfTest(ReportEngine engine, ReportRenderOptions options, string outDir, byte[] seedPng)
{
    Console.WriteLine("=== Kiểm chứng ảnh / logo (SVG nhúng + PNG data URI + nguồn lỗi) ===");

    var seedPath = Path.Combine(outDir, "image-source.png");
    File.WriteAllBytes(seedPath, seedPng);
    var pngDataUri = "data:image/png;base64," + Convert.ToBase64String(seedPng);

    const string svg = """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 160 80">
          <rect width="160" height="80" rx="10" fill="#1565C0"/>
          <circle cx="46" cy="32" r="16" fill="none" stroke="#FFFFFF" stroke-width="6"/>
          <circle cx="96" cy="32" r="16" fill="none" stroke="#FFFFFF" stroke-width="6"/>
          <path d="M62 32h18" stroke="#FFFFFF" stroke-width="6"/>
          <text x="80" y="70" font-family="Arial, sans-serif" font-size="18" font-weight="bold"
                fill="#FFFFFF" text-anchor="middle">EbizOp</text>
        </svg>
        """;
    var svgDataUri = "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));

    var withImages = engine.Render(BuildImageReport(svgDataUri, pngDataUri), new ReportData(), RenderFormat.Pdf, options);
    var empty = engine.Render(BuildImageReport("", ""), new ReportData(), RenderFormat.Pdf, options);
    var filePath = engine.Render(BuildImageReport(svgDataUri, seedPath), new ReportData(), RenderFormat.Pdf, options);
    File.WriteAllBytes(Path.Combine(outDir, "image-demo.pdf"), withImages);

    var png = new QuestPdfReportRenderer().RenderFirstPagePng(BuildImageReport(svgDataUri, pngDataUri), new ReportData(), options);
    File.WriteAllBytes(Path.Combine(outDir, "image-demo.png"), png);

    Console.WriteLine($"  có ảnh {withImages.Length:N0}B · không ảnh {empty.Length:N0}B · đường dẫn tệp {filePath.Length:N0}B · png {png.Length:N0}B");
    Console.WriteLine($"  {(withImages.Length > empty.Length ? "OK  " : "FAIL")}  SVG + PNG nhúng làm PDF lớn hơn (ảnh được vẽ)");
    Console.WriteLine($"  {(filePath.Length > 2000 ? "OK  " : "FAIL")}  nguồn là đường dẫn tệp vẫn render (không lỗi)");
    Console.WriteLine($"  {(png.Length > 5000 ? "OK  " : "FAIL")}  xuất PNG trang đầu thành công");
    Console.WriteLine();
}

static ReportDefinition BuildImageReport(string svgSrc, string rasterSrc) => new()
{
    Code = "image-demo",
    Name = "Ảnh / logo",
    Paper = new PaperSettings { Size = PaperSizeKind.A5, Orientation = PaperOrientation.Portrait },
    Bands =
    {
        new ReportBand
        {
            Kind = BandKind.ReportHeader,
            Height = 75,
            Items =
            {
                new ReportItem { Kind = ItemKind.Text, X = 0, Y = 0, W = 136, H = 7, Text = "ẢNH / LOGO", Style = new ItemStyle { Size = 14, Bold = true } },

                // SVG nhúng (véc-tơ)
                new ReportItem { Kind = ItemKind.Image, X = 0, Y = 9, W = 50, H = 25, Src = svgSrc },
                new ReportItem { Kind = ItemKind.Text, X = 0, Y = 34, W = 50, H = 5, Text = "SVG (data URI)", Style = new ItemStyle { Size = 7, Align = "center" } },

                // PNG nhúng (raster)
                new ReportItem { Kind = ItemKind.Image, X = 58, Y = 9, W = 30, H = 30, Src = rasterSrc },
                new ReportItem { Kind = ItemKind.Text, X = 58, Y = 39, W = 30, H = 5, Text = "PNG (data URI)", Style = new ItemStyle { Size = 7, Align = "center" } },

                // Nguồn lỗi: phải bị bỏ qua an toàn, không làm hỏng trang.
                new ReportItem { Kind = ItemKind.Image, X = 0, Y = 48, W = 40, H = 20, Src = "khong-ton-tai.png" },
                new ReportItem { Kind = ItemKind.Text, X = 0, Y = 68, W = 50, H = 5, Text = "nguồn lỗi → bỏ qua", Style = new ItemStyle { Size = 7, Align = "center", Color = "#C62828" } },
            },
        },
    },
};

static void SortingSelfTest(ReportEngine engine, ReportRenderOptions options, string outDir)
{
    Console.WriteLine("=== Kiểm chứng sắp xếp & nhóm dữ liệu ===");

    var def = new ReportDefinition
    {
        Code = "sort-demo",
        Name = "Sắp xếp & nhóm",
        Paper = new PaperSettings { Size = PaperSizeKind.A5, Orientation = PaperOrientation.Portrait },
        Sorting = { new ReportSort { Field = "LineTotal", Dir = "desc" } },
        Grouping = { new ReportGroup { Field = "GroupName", Sort = "asc", RepeatHeader = true } },
        Bands =
        {
            new ReportBand
            {
                Kind = BandKind.ReportHeader,
                Height = 7,
                Items =
                {
                    new ReportItem { Kind = ItemKind.Text, X = 0, Y = 0, W = 136, H = 7, Text = "SẮP XẾP & NHÓM", Style = new ItemStyle { Size = 11, Bold = true } },
                },
            },
            new ReportBand
            {
                Kind = BandKind.GroupHeader,
                Height = 6,
                Items =
                {
                    new ReportItem { Kind = ItemKind.Field, X = 0, Y = 0, W = 136, H = 6, Binding = "GroupName", Style = new ItemStyle { Size = 9, Bold = true } },
                },
            },
            new ReportBand
            {
                Kind = BandKind.Detail,
                Height = 6,
                Items =
                {
                    new ReportItem { Kind = ItemKind.Field, X = 0, Y = 0, W = 90, H = 6, Binding = "Name", Style = new ItemStyle { Size = 8 } },
                    new ReportItem { Kind = ItemKind.Field, X = 90, Y = 0, W = 46, H = 6, Binding = "LineTotal", Format = "N0", Style = new ItemStyle { Size = 8, Align = "right" } },
                },
            },
            new ReportBand
            {
                Kind = BandKind.GroupFooter,
                Height = 6,
                Items =
                {
                    new ReportItem { Kind = ItemKind.Expression, X = 0, Y = 0, W = 90, H = 6, Expression = "'Tổng nhóm'", Style = new ItemStyle { Size = 8, Bold = true } },
                    new ReportItem { Kind = ItemKind.Expression, X = 90, Y = 0, W = 46, H = 6, Expression = "Sum(LineTotal)", Format = "N0", Style = new ItemStyle { Size = 8, Align = "right", Bold = true } },
                },
            },
        },
    };

    var data = SalesData();
    var groups = ReportGrouping.Build(def, data);

    var eachGroupDesc = groups.All(g =>
    {
        var totals = g.Rows.Select(r => Convert.ToDecimal(r["LineTotal"])).ToList();
        return totals.SequenceEqual(totals.OrderByDescending(x => x));
    });
    var groupKeys = groups.Select(g => g.KeyValues?["GroupName"]?.ToString() ?? "").ToList();
    var keysSorted = groupKeys.SequenceEqual(groupKeys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase));
    var totalRows = groups.Sum(g => g.Rows.Count);

    Console.WriteLine($"  {groups.Count} nhóm · {totalRows} dòng · thứ tự: {string.Join(", ", groupKeys)}");
    Console.WriteLine($"  {(eachGroupDesc ? "OK  " : "FAIL")}  trong mỗi nhóm, LineTotal giảm dần (Sorting)");
    Console.WriteLine($"  {(keysSorted ? "OK  " : "FAIL")}  các nhóm tăng dần theo GroupName (Grouping)");
    Console.WriteLine($"  {(totalRows == data.Details.Count ? "OK  " : "FAIL")}  không mất / không lặp dòng");

    var pdf = engine.Render(def, data, RenderFormat.Pdf, options);
    var png = new QuestPdfReportRenderer().RenderFirstPagePng(def, data, options);
    File.WriteAllBytes(Path.Combine(outDir, "sort-group.pdf"), pdf);
    File.WriteAllBytes(Path.Combine(outDir, "sort-group.png"), png);
    Console.WriteLine($"  PDF {pdf.Length:N0}B · PNG {png.Length:N0}B");
    Console.WriteLine();
}

static void FieldCatalogSelfTest()
{
    Console.WriteLine("=== Kiểm chứng bảng trường dữ liệu (data panel) ===");

    var def = new ReportDefinition
    {
        Code = "field-demo",
        Name = "Data panel",
        Parameters =
        {
            new ReportParameter { Name = "TuNgay", Type = ReportParameterType.Date, Label = "Từ ngày" },
            new ReportParameter { Name = "TuNgay2", Type = ReportParameterType.Date, Label = "Bỏ trống" },
        },
    };

    var data = new ReportData()
        .SetField("StoreName", "EbizOp")
        .SetParam("TuNgay", new DateTime(2026, 1, 1))
        .AddRow(("GroupName", "Gọng"), ("Name", "Gọng nhựa"), ("Qty", 3m))
        .AddRow(("GroupName", "Tròng"), ("Name", "Tròng đổi màu"), ("Qty", 2m), ("LineTotal", 640000));

    var fields = ReportFieldCatalog.Discover(def, data);

    string Cat(string name) => fields.FirstOrDefault(f => f.Name == name)?.Category ?? "(none)";
    string? Samp(string name) => fields.FirstOrDefault(f => f.Name == name)?.Sample;

    var detailOk = Cat("GroupName") == "detail" && Cat("Name") == "detail"
                   && Cat("Qty") == "detail" && Cat("LineTotal") == "detail";
    var fieldOk = Cat("StoreName") == "field" && Samp("StoreName") == "EbizOp";
    var paramOk = Cat("TuNgay") == "param" && Cat("TuNgay2") == "param";
    var sampleOk = Samp("LineTotal") is { Length: > 0 };   // 640.000 (vi-VN)
    var noDup = fields.Select(f => f.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == fields.Count;
    // LineTotal chỉ có ở dòng 2 nhưng vẫn phải xuất hiện (hợp nhất khoá mọi dòng)
    var unionOk = fields.Any(f => f.Name == "LineTotal" && f.Category == "detail");

    Console.WriteLine($"  {fields.Count} trường: {string.Join(", ", fields.Select(f => $"{f.Name}({f.Category})"))}");
    Console.WriteLine($"  {(detailOk ? "OK  " : "FAIL")}  4 khoá detail được hợp nhất từ mọi dòng");
    Console.WriteLine($"  {(fieldOk ? "OK  " : "FAIL")}  field đơn có giá trị mẫu (StoreName = \"{Samp("StoreName")}\")");
    Console.WriteLine($"  {(paramOk ? "OK  " : "FAIL")}  tham số gồm cả giá trị và khai báo chưa có dữ liệu");
    Console.WriteLine($"  {(sampleOk ? "OK  " : "FAIL")}  giá trị mẫu của LineTotal = \"{Samp("LineTotal")}\"");
    Console.WriteLine($"  {(unionOk ? "OK  " : "FAIL")}  LineTotal (chỉ ở dòng 2) vẫn xuất hiện");
    Console.WriteLine($"  {(noDup ? "OK  " : "FAIL")}  không trùng tên trường");
    Console.WriteLine();
}

static void PngPagesSelfTest(ReportRenderOptions options, string outDir)
{
    Console.WriteLine("=== Kiểm chứng xem trước WYSIWYG (nhiều trang PNG) ===");

    var def = new ReportDefinition
    {
        Code = "png-pages",
        Name = "Danh sách dài",
        Paper = new PaperSettings { Size = PaperSizeKind.A4, Orientation = PaperOrientation.Portrait },
        Bands =
        {
            new ReportBand
            {
                Kind = BandKind.PageHeader,
                Height = 8,
                Items =
                {
                    new ReportItem { Kind = ItemKind.Text, X = 0, Y = 0, W = 198, H = 8, Text = "DANH SÁCH DÀI", Style = new ItemStyle { Size = 12, Bold = true } },
                },
            },
            new ReportBand
            {
                Kind = BandKind.Detail,
                Height = 6,
                Items =
                {
                    new ReportItem { Kind = ItemKind.Field, X = 0, Y = 0, W = 20, H = 6, Binding = "STT", Style = new ItemStyle { Size = 9 } },
                    new ReportItem { Kind = ItemKind.Field, X = 20, Y = 0, W = 178, H = 6, Binding = "Ten", Style = new ItemStyle { Size = 9 } },
                },
            },
        },
    };

    var data = new ReportData();
    for (var i = 1; i <= 150; i++)
        data.AddRow(("STT", i), ("Ten", $"Dòng số {i}"));

    var renderer = new QuestPdfReportRenderer();
    var pages = renderer.RenderPagesPng(def, data, options);

    var onePage = new ReportDefinition
    {
        Code = "png-one",
        Paper = new PaperSettings { Size = PaperSizeKind.A5, Orientation = PaperOrientation.Portrait },
        Bands =
        {
            new ReportBand
            {
                Kind = BandKind.Detail,
                Height = 6,
                Items = { new ReportItem { Kind = ItemKind.Text, X = 0, Y = 0, W = 100, H = 6, Text = "Một trang" } },
            },
        },
    };
    var single = renderer.RenderPagesPng(onePage, new ReportData(), options);

    var sample = pages[0];
    var isPng = sample.Length > 8 && sample[0] == 0x89 && sample[1] == 0x50 && sample[2] == 0x4E && sample[3] == 0x47;

    Console.WriteLine($"  150 dòng → {pages.Count} trang · report ngắn → {single.Count} trang");
    Console.WriteLine($"  {(pages.Count > 1 ? "OK  " : "FAIL")}  report dài tách thành nhiều trang");
    Console.WriteLine($"  {(single.Count == 1 ? "OK  " : "FAIL")}  report ngắn chỉ 1 trang");
    Console.WriteLine($"  {(isPng ? "OK  " : "FAIL")}  ảnh là PNG hợp lệ (chữ ký 89 50 4E 47)");
    File.WriteAllBytes(Path.Combine(outDir, "preview-page1.png"), pages[0]);
    File.WriteAllBytes(Path.Combine(outDir, "preview-page2.png"), pages[1]);
    Console.WriteLine();
}

static void GroupTableSelfTest(ReportRenderOptions options, string outDir)
{
    Console.WriteLine("=== Kiểm chứng bảng con theo nhóm (GroupHeader + Scope) ===");

    var data = SalesData();
    var groupDef = new ReportDefinition
    {
        Code = "gt-groups",
        Grouping = { new ReportGroup { Field = "GroupName", Sort = "asc" } },
    };
    var groups = ReportGrouping.Build(groupDef, data);
    var counts = groups.Select(g => g.Rows.Count).ToList();
    var ctx = groups[0].Rows;

    var pickGroup = TableHelper.RowsFor(new TableSpec { Scope = "group" }, ctx, data);
    var pickDefault = TableHelper.RowsFor(null, ctx, data);
    var pickAll = TableHelper.RowsFor(new TableSpec { Scope = "all" }, ctx, data);

    Console.WriteLine($"  {groups.Count} nhóm · số dòng mỗi nhóm: {string.Join(", ", counts)}");
    Console.WriteLine($"  {(counts.SequenceEqual(new[] { 3, 2, 2 }) ? "OK  " : "FAIL")}  chia đúng 3 nhóm (3, 2, 2 dòng)");
    Console.WriteLine($"  {(ReferenceEquals(pickGroup, ctx) && pickGroup.Count == 3 ? "OK  " : "FAIL")}  Scope=group → đúng 3 dòng của nhóm đầu");
    Console.WriteLine($"  {(pickDefault.Count == ctx.Count ? "OK  " : "FAIL")}  Scope mặc định cũng là group");
    Console.WriteLine($"  {(pickAll.Count == data.Details.Count ? "OK  " : "FAIL")}  Scope=all → toàn bộ {data.Details.Count} dòng");

    // ---- đo mức lặp: bảng trong Detail chỉ vẽ 1 lần cho nhóm, không vẽ lại mỗi dòng ----
    var renderer = new QuestPdfReportRenderer();

    ReportData Rows4()
    {
        var d = new ReportData();
        for (var i = 1; i <= 4; i++)
            d.AddRow(("Name", $"Dòng {i}"), ("Qty", i), ("LineTotal", i * 1000));
        return d;
    }

    ReportItem Table() => new()
    {
        Kind = ItemKind.Table, X = 0, Y = 0, W = 190, H = 48,
        Table = new TableSpec
        {
            Scope = "group", ShowHeader = true, Borders = true, Footer = true,
            Columns =
            {
                new TableColumn { Header = "Tên", Binding = "Name", Width = 60 },
                new TableColumn { Header = "SL", Binding = "Qty", Align = "center", Width = 20, Aggregate = "sum", Format = "N0" },
                new TableColumn { Header = "Tiền", Binding = "LineTotal", Align = "right", Width = 40, Aggregate = "sum", Format = "N0" },
            },
        },
    };

    ReportDefinition Small(BandKind? tableBand)
    {
        var d = new ReportDefinition
        {
            Code = "gt-small", Name = "gt-small",
            Paper = new PaperSettings { Size = PaperSizeKind.A4, Orientation = PaperOrientation.Portrait },
        };
        if (tableBand == BandKind.ReportHeader)
            d.Bands.Add(new ReportBand { Kind = BandKind.ReportHeader, Height = 48, Items = { Table() } });
        var detail = new ReportBand { Kind = BandKind.Detail, Height = 48 };
        detail.Items.Add(new ReportItem { Kind = ItemKind.Field, X = 0, Y = 0, W = 190, H = 6, Binding = "Name", Style = new ItemStyle { Size = 9 } });
        if (tableBand == BandKind.Detail)
            detail.Items.Add(Table());
        d.Bands.Add(detail);
        return d;
    }

    var baseSize = renderer.Render(Small(null), Rows4(), options).Length;
    var headerSize = renderer.Render(Small(BandKind.ReportHeader), Rows4(), options).Length;
    var detailSize = renderer.Render(Small(BandKind.Detail), Rows4(), options).Length;
    var dHeader = headerSize - baseSize;
    var dDetail = detailSize - baseSize;

    Console.WriteLine($"  base {baseSize:N0}B · header+bảng {headerSize:N0}B (Δ{dHeader:N0}) · detail+bảng {detailSize:N0}B (Δ{dDetail:N0})");
    Console.WriteLine($"  {(dDetail > 0 ? "OK  " : "FAIL")}  bảng đặt trong Detail vẫn được vẽ");
    Console.WriteLine($"  {(dDetail < dHeader * 2 ? "OK  " : "FAIL")}  bảng trong Detail KHÔNG lặp mỗi dòng (chỉ 1 lần cho nhóm)");

    // ---- ảnh trực quan: mỗi nhóm có bảng con riêng ----
    var vis = new ReportDefinition
    {
        Code = "gt-vis",
        Name = "Bảng con theo nhóm",
        Paper = new PaperSettings { Size = PaperSizeKind.A4, Orientation = PaperOrientation.Portrait },
        Grouping = { new ReportGroup { Field = "GroupName", Sort = "asc" } },
        Bands =
        {
            new ReportBand
            {
                Kind = BandKind.ReportHeader, Height = 9,
                Items = { new ReportItem { Kind = ItemKind.Text, X = 0, Y = 0, W = 190, H = 9, Text = "BẢNG CON THEO NHÓM", Style = new ItemStyle { Size = 13, Bold = true } } },
            },
            new ReportBand
            {
                Kind = BandKind.GroupHeader, Height = 50,
                Items =
                {
                    new ReportItem { Kind = ItemKind.Text, X = 0, Y = 0, W = 190, H = 6, Text = "Nhóm: {GroupName}", Style = new ItemStyle { Size = 10, Bold = true, Color = "#1E5AA8" } },
                    new ReportItem
                    {
                        Kind = ItemKind.Table, X = 0, Y = 7, W = 190, H = 42,
                        Table = new TableSpec
                        {
                            Scope = "group", ShowHeader = true, Borders = true, Zebra = true, Footer = true,
                            Columns =
                            {
                                new TableColumn { Header = "Sản phẩm", Binding = "Name", Width = 100 },
                                new TableColumn { Header = "SL", Binding = "Qty", Align = "center", Width = 30, Aggregate = "sum", Format = "N0" },
                                new TableColumn { Header = "Thành tiền", Binding = "LineTotal", Align = "right", Width = 60, Aggregate = "sum", Format = "N0" },
                            },
                        },
                    },
                },
            },
        },
    };

    var visPdf = renderer.Render(vis, data, options);
    var visPng = renderer.RenderFirstPagePng(vis, data, options);
    File.WriteAllBytes(Path.Combine(outDir, "group-table.pdf"), visPdf);
    File.WriteAllBytes(Path.Combine(outDir, "group-table.png"), visPng);
    Console.WriteLine($"  bảng con theo nhóm: PDF {visPdf.Length:N0}B · PNG {visPng.Length:N0}B");
    Console.WriteLine();
}

static void BackupRestoreSelfTest(ReportEngine engine, string outDir)
{
    Console.WriteLine("=== Kiểm chứng sao lưu / khôi phục / nhân bản kho ===");

    ReportDefinition Make(string code, string name) => new()
    {
        Code = code,
        Name = name,
        Bands =
        {
            new ReportBand
            {
                Kind = BandKind.Detail, Height = 6,
                Items = { new ReportItem { Kind = ItemKind.Text, X = 0, Y = 0, W = 100, H = 6, Text = name } },
            },
        },
    };

    var provider = new DelegateReportDataProvider((_, _) => new ReportData());
    var store = new InMemoryReportStore(new[] { Make("bc-a", "Báo cáo A"), Make("bc-b", "Báo cáo B") });
    var service = new ReportService(store, provider, engine);

    // ---- xuất sao lưu ----
    var bundle = service.ExportBackup();
    var parsed = ReportBackup.FromJson(bundle);
    File.WriteAllText(Path.Combine(outDir, "reports-backup.json"), bundle);
    Console.WriteLine($"  gói sao lưu: schema v{parsed.SchemaVersion} · {parsed.Reports.Count} báo cáo · {bundle.Length:N0}B");
    Console.WriteLine($"  {(parsed.SchemaVersion == ReportBackup.CurrentSchema ? "OK  " : "FAIL")}  schema đúng");
    Console.WriteLine($"  {(parsed.Reports.Count == 2 ? "OK  " : "FAIL")}  gói chứa đủ 2 báo cáo");

    // ---- khôi phục vào kho rỗng ----
    var store2 = new InMemoryReportStore();
    var service2 = new ReportService(store2, provider, engine);
    var r1 = service2.ImportBackup(bundle);
    Console.WriteLine($"  khôi phục kho rỗng: {r1.Summary}");
    Console.WriteLine($"  {(r1.Added.Count == 2 && store2.List().Count == 2 ? "OK  " : "FAIL")}  thêm mới 2 báo cáo");

    // ---- khôi phục lần 2, không ghi đè → bỏ qua ----
    var r2 = service2.ImportBackup(bundle);
    Console.WriteLine($"  khôi phục lại (không ghi đè): {r2.Summary}");
    Console.WriteLine($"  {(r2.Skipped.Count == 2 && r2.Added.Count == 0 && r2.Updated.Count == 0 ? "OK  " : "FAIL")}  bỏ qua vì đã tồn tại");

    // ---- sửa tại chỗ rồi khôi phục có ghi đè → khôi phục nội dung gốc ----
    var local = store2.Get("bc-a")!;
    local.Name = "ĐÃ SỬA";
    store2.Save(local);
    var r3 = service2.ImportBackup(bundle, overwrite: true);
    Console.WriteLine($"  khôi phục có ghi đè: {r3.Summary}");
    Console.WriteLine($"  {(r3.Updated.Count == 2 && store2.Get("bc-a")!.Name == "Báo cáo A" ? "OK  " : "FAIL")}  ghi đè khôi phục nội dung gốc");

    // ---- nhân bản ----
    var clone1 = service.Clone("bc-a");
    var clone2 = service.Clone("bc-a");
    Console.WriteLine($"  nhân bản: “{clone1.Code}” / “{clone2.Code}” · {clone1.Name}");
    Console.WriteLine($"  {(clone1.Code == "bc-a-copy" ? "OK  " : "FAIL")}  bản sao đầu tiên = bc-a-copy");
    Console.WriteLine($"  {(clone2.Code == "bc-a-copy-2" ? "OK  " : "FAIL")}  bản sao thứ hai = bc-a-copy-2 (tự tránh trùng)");
    Console.WriteLine($"  {(clone1.Name == "Báo cáo A (bản sao)" ? "OK  " : "FAIL")}  tên mặc định thêm “(bản sao)”");
    Console.WriteLine($"  {(clone1.Version == 1 ? "OK  " : "FAIL")}  phiên bản bản sao = 1");
    Console.WriteLine($"  {(store.List().Count == 4 ? "OK  " : "FAIL")}  kho có 4 báo cáo sau 2 lần nhân bản");

    // sao chép sâu: sửa bản sao không ảnh hưởng bản gốc
    clone1.Bands[0].Items[0].Text = "ĐÃ ĐỔI Ở BẢN SAO";
    store.Save(clone1);
    var originalText = store.Get("bc-a")!.Bands[0].Items[0].Text;
    Console.WriteLine($"  {(originalText == "Báo cáo A" ? "OK  " : "FAIL")}  nhân bản sao chép sâu (bản gốc không đổi)");

    // nhân bản với mã/tên chỉ định
    var named = service.Clone("bc-b", "bc-b-v2", "Báo cáo B (v2)");
    Console.WriteLine($"  {(named.Code == "bc-b-v2" && named.Name == "Báo cáo B (v2)" ? "OK  " : "FAIL")}  nhân bản với mã/tên chỉ định");

    // ---- kiểm tra lỗi ----
    try
    {
        ReportBackup.FromJson("{\"SchemaVersion\":99,\"Reports\":[]}");
        Console.WriteLine("  FAIL  schema mới hơn phải bị từ chối");
    }
    catch (InvalidOperationException)
    {
        Console.WriteLine("  OK    từ chối gói sao lưu có schema mới hơn");
    }

    try
    {
        ReportBackup.FromJson("{ khong-phai-json }");
        Console.WriteLine("  FAIL  JSON hỏng phải bị từ chối");
    }
    catch (InvalidOperationException)
    {
        Console.WriteLine("  OK    từ chối JSON hỏng");
    }

    try
    {
        service.Clone("khong-ton-tai");
        Console.WriteLine("  FAIL  nhân bản mã sai phải ném ReportNotFoundException");
    }
    catch (ReportNotFoundException)
    {
        Console.WriteLine("  OK    nhân bản mã sai → ReportNotFoundException");
    }

    Console.WriteLine();
}

static void ExpressionBuilderSelfTest()
{
    Console.WriteLine("=== Kiểm chứng Expression builder (gợi ý / kiểm tra / xem trước) ===");

    var funcs = ExpressionInspector.Functions;
    Console.WriteLine($"  {funcs.Count} hàm: {string.Join(", ", funcs.Select(f => f.Name))}");
    Console.WriteLine($"  {(funcs.Count >= 18 ? "OK  " : "FAIL")}  danh mục hàm đầy đủ");
    foreach (var name in new[] { "Sum", "Avg", "Count", "Iif", "Format", "Today", "Page" })
        Console.WriteLine($"  {(funcs.Any(f => f.Name == name) ? "OK  " : "FAIL")}  có hàm {name}");

    void Chk(string expr, bool expectOk)
    {
        var r = ExpressionInspector.Validate(expr);
        var ok = r.Ok == expectOk;
        Console.WriteLine($"  {(ok ? "OK  " : "FAIL")}  Validate(\"{expr}\") → {(r.Ok ? "hợp lệ" : "lỗi: " + r.Error)}");
    }
    Chk("Sum(LineTotal) + 1", true);
    Chk("Iif(Qty > 0, 'có', 'không')", true);
    Chk("Sum(", false);
    Chk("Sum(1, 2)", false);
    Chk("Foo(1)", false);
    Chk("1 +", false);

    var data = new ReportData();
    data.AddRow(("Name", "Ao"), ("Qty", 1m), ("LineTotal", 1000m));
    data.AddRow(("Name", "Quan"), ("Qty", 2m), ("LineTotal", 2000m));
    data.AddRow(("Name", "Giay"), ("Qty", 3m), ("LineTotal", 3000m));

    void Eval(string expr, string expected)
    {
        var r = ExpressionInspector.Evaluate(expr, data);
        var ok = r.Ok && r.Value == expected;
        var shown = r.Ok ? r.Value : "lỗi: " + r.Error;
        Console.WriteLine($"  {(ok ? "OK  " : "FAIL")}  {expr} = {shown}{(ok ? "" : $" (mong đợi {expected})")}");
    }
    Eval("Sum(LineTotal)", "6.000");
    Eval("Count()", "3");
    Eval("Avg(Qty)", "2");
    Eval("Max(LineTotal)", "3.000");
    Eval("Min(LineTotal)", "1.000");
    Eval("Iif(Sum(LineTotal) > 5000, 'lớn', 'nhỏ')", "lớn");
    Eval("Upper(Name)", "AO");
    Eval("Round(LineTotal / 3, 0)", "333");
    Eval("LineTotal * 2", "2.000");
    Eval("Format(LineTotal, 'N0')", "1.000");
    Eval("Len(Name)", "2");

    var err = ExpressionInspector.Evaluate("Foo(1)", data);
    Console.WriteLine($"  {(!err.Ok && err.Error is not null ? "OK  " : "FAIL")}  biểu thức sai → báo lỗi ({err.Error})");

    // dữ liệu mẫu dựng từ danh sách trường + tham số mặc định
    var def = new ReportDefinition
    {
        Code = "expr-sample",
        Parameters = { new ReportParameter { Name = "TuNgay", Type = ReportParameterType.Date, Default = "2026-01-01" } },
    };
    var fields = new List<ReportFieldInfo>
    {
        new() { Name = "Name", Category = "detail", Sample = "Áo sơ mi" },
        new() { Name = "Qty", Category = "detail", Sample = "5" },
        new() { Name = "LineTotal", Category = "detail", Sample = "1.250.000" },
        new() { Name = "StoreName", Category = "field", Sample = "CH Trung tâm" },
    };
    var sample = ExpressionInspector.SampleData(def, fields);
    var s1 = ExpressionInspector.Evaluate("Sum(LineTotal)", sample);
    var s2 = ExpressionInspector.Evaluate("Name + ' @ ' + StoreName", sample);
    var s3 = ExpressionInspector.Evaluate("TuNgay", sample);
    Console.WriteLine($"  {(s1.Value == "1.250.000" ? "OK  " : "FAIL")}  mẫu Sum(LineTotal) = {s1.Value} (số vi-VN)");
    Console.WriteLine($"  {(s2.Value == "Áo sơ mi @ CH Trung tâm" ? "OK  " : "FAIL")}  mẫu ghép dòng+field = {s2.Value}");
    Console.WriteLine($"  {(s3.Value != null && s3.Value.StartsWith("01/01/2026") ? "OK  " : "FAIL")}  mẫu tham số TuNgay = {s3.Value}");
    Console.WriteLine();
}

static void PaginationSelfTest(ReportRenderOptions options, string outDir)
{
    Console.WriteLine("=== Kiểm chứng phân trang & keep-together ===");

    const int groupCount = 5;
    const int rowsPerGroup = 13; // 9mm header + 13 × 7mm = 100mm mỗi nhóm

    ReportDefinition Build(bool keepTogether, bool footerBreak) => new()
    {
        Code = "paging",
        Name = "Phân trang",
        Paper = new PaperSettings
        {
            Size = PaperSizeKind.A4,
            Orientation = PaperOrientation.Portrait,
            Margins = new Margins { Top = 10, Right = 8, Bottom = 10, Left = 8 },
        },
        Grouping = { new ReportGroup { Field = "Nhom", KeepTogether = keepTogether } },
        Bands =
        {
            new ReportBand
            {
                Kind = BandKind.GroupHeader,
                Height = 9,
                Items =
                {
                    new ReportItem { Kind = ItemKind.Text, X = 0, Y = 0, W = 120, H = 9, Text = "Nhóm {Nhom}", Style = new ItemStyle { Size = 10, Bold = true } },
                },
            },
            new ReportBand
            {
                Kind = BandKind.Detail,
                Height = 7,
                Items =
                {
                    new ReportItem { Kind = ItemKind.Field, X = 0, Y = 0, W = 120, H = 7, Binding = "Ten", Style = new ItemStyle { Size = 8 } },
                },
            },
            new ReportBand
            {
                Kind = BandKind.ReportFooter,
                Height = 8,
                PageBreakBefore = footerBreak,
                Items =
                {
                    new ReportItem { Kind = ItemKind.Text, X = 0, Y = 0, W = 120, H = 8, Text = "HẾT", Style = new ItemStyle { Size = 9, Bold = true } },
                },
            },
        },
    };

    var data = new ReportData();
    for (var g = 1; g <= groupCount; g++)
        for (var r = 1; r <= rowsPerGroup; r++)
            data.AddRow(("Nhom", $"N{g}"), ("Ten", $"Nhóm {g} · dòng {r}"));

    var renderer = new QuestPdfReportRenderer();
    var flow = renderer.RenderPagesPng(Build(false, false), data, options);
    var kept = renderer.RenderPagesPng(Build(true, false), data, options);
    var footerBreak = renderer.RenderPagesPng(Build(false, true), data, options);

    Console.WriteLine($"  không giữ nhóm → {flow.Count} trang · giữ nhóm → {kept.Count} trang · ngắt trang trước footer → {footerBreak.Count} trang");
    Console.WriteLine($"  {(flow.Count >= 2 ? "OK  " : "FAIL")}  báo cáo dài tách nhiều trang");
    Console.WriteLine($"  {(kept.Count > flow.Count ? "OK  " : "FAIL")}  keep-together đẩy nhóm không vừa sang trang mới (+{kept.Count - flow.Count} trang)");
    Console.WriteLine($"  {(footerBreak.Count == flow.Count + 1 ? "OK  " : "FAIL")}  PageBreakBefore thêm đúng 1 trang");

    File.WriteAllBytes(Path.Combine(outDir, "paging-keep-together-p1.png"), kept[0]);
    if (kept.Count > 1)
        File.WriteAllBytes(Path.Combine(outDir, "paging-keep-together-p2.png"), kept[1]);
    Console.WriteLine();
}

static void AuditSelfTest(ReportEngine engine, ReportRenderOptions options)
{
    Console.WriteLine("=== Kiểm chứng nhật ký thao tác (audit) ===");

    var def = new ReportDefinition
    {
        Code = "audit-a",
        Name = "Audit A",
        Bands =
        {
            new ReportBand
            {
                Kind = BandKind.Detail,
                Height = 6,
                Items = { new ReportItem { Kind = ItemKind.Text, X = 0, Y = 0, W = 50, H = 6, Text = "x" } },
            },
        },
    };

    var store = new InMemoryReportStore(new[] { def });
    var audit = new InMemoryReportAuditStore(capacity: 50);
    var provider = new DelegateReportDataProvider((_, _) => new ReportData());
    var service = new ReportService(store, provider, engine, null, audit);

    service.Save(def);                                          // save
    var clone = service.Clone("audit-a");                       // clone → audit-a-copy
    service.Render("audit-a", RenderFormat.Pdf, null, options); // render
    service.Delete(clone.Code);                                 // delete

    var recent = audit.Recent(10);
    var actions = string.Join(" > ", recent.Select(e => e.Action));
    Console.WriteLine($"  thao tác (mới nhất trước): {actions}");
    Console.WriteLine($"  {(recent.Count == 4 ? "OK  " : "FAIL")}  ghi đủ 4 thao tác save/clone/render/delete");
    Console.WriteLine($"  {(actions == "delete > render > clone > save" ? "OK  " : "FAIL")}  đúng thứ tự mới nhất trước");
    Console.WriteLine($"  {(recent.All(e => !string.IsNullOrWhiteSpace(e.Code)) ? "OK  " : "FAIL")}  mọi mục đều có mã report");
    var cloneEntry = recent.FirstOrDefault(e => e.Action == "clone");
    Console.WriteLine($"  {(cloneEntry?.Detail?.Contains("audit-a → audit-a-copy") == true ? "OK  " : "FAIL")}  chi tiết clone: nguồn → đích");
    Console.WriteLine($"  {(recent.All(e => !string.IsNullOrWhiteSpace(e.User)) ? "OK  " : "FAIL")}  có ghi người dùng");

    // khôi phục: ghi 1 mục "import" kèm tóm tắt
    service.ImportBackup(service.ExportBackup(), overwrite: true);
    var importEntry = audit.Recent(1).FirstOrDefault();
    Console.WriteLine($"  {(importEntry?.Action == "import" ? "OK  " : "FAIL")}  khôi phục → ghi mục import");
    Console.WriteLine($"  {(importEntry?.Detail?.Contains("thêm") == true ? "OK  " : "FAIL")}  chi tiết import có tóm tắt ({importEntry?.Detail})");

    // sức chứa: chỉ giữ N mục mới nhất
    var small = new InMemoryReportAuditStore(capacity: 2);
    for (var i = 0; i < 5; i++)
        small.Record(new ReportAuditEntry { Action = "a" + i, Code = "c" + i });
    var kept = small.Recent(10);
    Console.WriteLine($"  {((small.Count == 2 && kept.Count == 2) ? "OK  " : "FAIL")}  kho nhật ký cắt bớt theo sức chứa (2 mục)");
    Console.WriteLine($"  {((kept.Count == 2 && kept[0].Action == "a4" && kept[1].Action == "a3") ? "OK  " : "FAIL")}  giữ các mục mới nhất");
    Console.WriteLine();
}

static void SqlDataSourceSelfTest(ReportEngine engine, ReportRenderOptions options, string outDir)
{
    Console.WriteLine("=== Kiểm chứng nguồn dữ liệu SQL ===");

    var dbPath = Path.Combine(outDir, "sql-demo.db");
    if (File.Exists(dbPath))
        File.Delete(dbPath);
    var cs = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();

    using (var db = new SqliteConnection(cs))
    {
        db.Open();
        using (var create = db.CreateCommand())
        {
            create.CommandText = "CREATE TABLE HoaDon (Ma TEXT, Ngay TEXT, Ten TEXT, Tien REAL);";
            create.ExecuteNonQuery();
        }

        var rows = new (string Ma, string Ngay, string Ten, double Tien)[]
        {
            ("HD-01", "2026-01-15", "Hóa đơn tháng 1", 100000),
            ("HD-02", "2026-02-05", "Hóa đơn tháng 2a", 200000),
            ("HD-03", "2026-02-20", "Hóa đơn tháng 2b", 300000),
            ("HD-04", "2026-03-10", "Hóa đơn tháng 3", 400000),
            ("HD-05", "2026-04-01", "Hóa đơn tháng 4", 500000),
        };

        using var ins = db.CreateCommand();
        ins.CommandText = "INSERT INTO HoaDon (Ma, Ngay, Ten, Tien) VALUES ($ma, $ngay, $ten, $tien)";
        var pMa = ins.CreateParameter(); pMa.ParameterName = "$ma"; ins.Parameters.Add(pMa);
        var pNgay = ins.CreateParameter(); pNgay.ParameterName = "$ngay"; ins.Parameters.Add(pNgay);
        var pTen = ins.CreateParameter(); pTen.ParameterName = "$ten"; ins.Parameters.Add(pTen);
        var pTien = ins.CreateParameter(); pTien.ParameterName = "$tien"; ins.Parameters.Add(pTien);
        foreach (var r in rows)
        {
            pMa.Value = r.Ma; pNgay.Value = r.Ngay; pTen.Value = r.Ten; pTien.Value = r.Tien;
            ins.ExecuteNonQuery();
        }
    }

    const string sql = "SELECT Ma, Ngay, Ten, Tien FROM HoaDon " +
                       "WHERE Ngay >= @TuNgay AND Ngay <= @DenNgay ORDER BY Ngay";

    var registry = new SqlConnectionRegistry().Register(new SqlConnectionOptions
    {
        Name = "demo",
        Provider = "sqlite",
        ConnectionString = cs,
    });

    var def = new ReportDefinition
    {
        Code = "sql-demo",
        Name = "Hóa đơn theo khoảng ngày",
        Paper = new PaperSettings { Size = PaperSizeKind.A4, Orientation = PaperOrientation.Portrait },
        Parameters =
        {
            new ReportParameter { Name = "TuNgay", Type = ReportParameterType.Date, Default = "2026-02-01" },
            new ReportParameter { Name = "DenNgay", Type = ReportParameterType.Date, Default = "2026-02-28" },
        },
        DataSources =
        {
            new ReportDataSource { Name = "HoaDon", Type = "sql", Connection = "demo", Sql = sql },
        },
        Bands =
        {
            new ReportBand
            {
                Kind = BandKind.PageHeader,
                Height = 12,
                Items =
                {
                    new ReportItem { Kind = ItemKind.Text, X = 0, Y = 0, W = 180, H = 8, Text = "HÓA ĐƠN THEO KHOẢNG NGÀY", Style = new ItemStyle { Size = 12, Bold = true } },
                },
            },
            new ReportBand
            {
                Kind = BandKind.Detail,
                Height = 7,
                Items =
                {
                    new ReportItem { Kind = ItemKind.Field, X = 0, Y = 0, W = 30, H = 7, Binding = "Ma", Style = new ItemStyle { Size = 8 } },
                    new ReportItem { Kind = ItemKind.Field, X = 32, Y = 0, W = 70, H = 7, Binding = "Ten", Style = new ItemStyle { Size = 8 } },
                    new ReportItem { Kind = ItemKind.Field, X = 110, Y = 0, W = 40, H = 7, Binding = "Tien", Format = "N0", Style = new ItemStyle { Size = 8, Align = "right" } },
                },
            },
        },
    };

    var effective = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
    {
        ["TuNgay"] = new DateTime(2026, 2, 1),
        ["DenNgay"] = new DateTime(2026, 2, 28),
    };

    var provider = new SqlReportDataProvider(registry);
    var data = provider.GetData(def, effective);
    foreach (var kv in effective)
        data.SetParam(kv.Key, kv.Value);

    var first = data.Details.FirstOrDefault();
    Console.WriteLine($"  {data.Details.Count} dòng trong khoảng 01/02–28/02");
    Console.WriteLine($"  {(data.Details.Count == 2 ? "OK  " : "FAIL")}  lọc theo @TuNgay / @DenNgay");
    Console.WriteLine($"  {(first is not null && (string?)first["Ma"] == "HD-02" ? "OK  " : "FAIL")}  sắp xếp theo ngày (ORDER BY)");
    Console.WriteLine($"  {(first is not null && data.Resolve("Ten", first) is string && (string?)first["Ten"] == "Hóa đơn tháng 2a" ? "OK  " : "FAIL")}  ánh xạ cột → field");

    var probe = SqlDataSourceExecutor.Probe(registry.Resolve("demo"), sql, effective, maxRows: 20);
    Console.WriteLine($"  {(probe.Ok ? "OK  " : "FAIL")}  “Chạy thử” trả kết quả ({probe.RowCount} dòng, {probe.ElapsedMs} ms)");
    Console.WriteLine($"  {(probe.Ok && probe.Columns.SequenceEqual(new[] { "Ma", "Ngay", "Ten", "Tien" }) ? "OK  " : "FAIL")}  lấy đúng danh sách cột: {string.Join(", ", probe.Columns)}");
    Console.WriteLine($"  {(probe.Ok && probe.RowCount == 2 && probe.Rows.Count == 2 ? "OK  " : "FAIL")}  dòng mẫu khớp số dòng");

    var bad = SqlDataSourceExecutor.Probe(registry.Resolve("demo"), "SELECT * FROM KhongTonTai", effective);
    Console.WriteLine($"{(!bad.Ok && !string.IsNullOrWhiteSpace(bad.Error) ? "  OK  " : "  FAIL")}  SQL sai → trả lỗi (không ném ngoại lệ)");

    var fallback = new SqlReportDataProvider(registry, new DelegateReportDataProvider((_, _) =>
    {
        var d = new ReportData();
        d.AddRow(("X", 1));
        return d;
    }));
    var fb = fallback.GetData(new ReportDefinition { Code = "no-sql" }, new Dictionary<string, object?>());
    Console.WriteLine($"  {(fb.Details.Count == 1 ? "OK  " : "FAIL")}  report không có datasource sql → dùng nguồn dự phòng");

    var pdf = engine.Render(def, data, RenderFormat.Pdf, options);
    File.WriteAllBytes(Path.Combine(outDir, "sql-datasource.pdf"), pdf);
    Console.WriteLine($"  {(pdf.Length > 800 ? "OK  " : "FAIL")}  render PDF từ dữ liệu SQL ({pdf.Length:N0}B) → out/sql-datasource.pdf");
    Console.WriteLine();
}

static void SymbologySelfTest()
{
    Console.WriteLine("=== Kiểm chứng mã vạch / QR (mã hoá → raster → giải mã lại) ===");
    var cases = new (ItemKind Kind, SymbologySpec? Spec, string Value, string Label)[]
    {
        (ItemKind.Barcode, new SymbologySpec { Format = "Code128" }, "#B-1123-0001", "Code128"),
        (ItemKind.Barcode, new SymbologySpec { Format = "Code39" }, "ABC-12345", "Code39"),
        (ItemKind.Barcode, new SymbologySpec { Format = "EAN13" }, "8935001100010", "EAN13"),
        (ItemKind.QrCode, new SymbologySpec { Ecc = "M" }, "https://ebizop.vn/hd/B-1123-0001", "QR (M) URL"),
        (ItemKind.QrCode, new SymbologySpec { Ecc = "H" }, "HÓA ĐƠN KÍNH MẮT EBIZOP", "QR (H) UTF-8"),
        (ItemKind.QrCode, new SymbologySpec { Ecc = "L" }, "1234567890", "QR (L) số"),
    };

    var allOk = true;
    foreach (var (kind, spec, value, label) in cases)
    {
        var r = SymbologyVerifier.Verify(kind, spec, value);
        var svg = Symbology.Encode(kind, spec, value);
        allOk &= r.Ok;
        Console.WriteLine($"  {(r.Ok ? "OK  " : "FAIL")}  {label,-14} modules={svg?.ModulesWide}x{svg?.ModulesHigh}  svg={svg?.Svg.Length ?? 0}B  {(r.Ok ? "" : r.Detail)}");
    }
    Console.WriteLine(allOk ? "  => Tất cả mã vạch/QR giải mã lại khớp." : "  => CÓ LỖI kiểm chứng!");
}

/// <summary>Authorizer từ chối tất cả — dùng để kiểm chứng nhánh phân quyền.</summary>
sealed class DenyAllAuthorizer : IReportAuthorizer
{
    public bool CanRender(string reportCode) => false;
    public bool CanDesign(string reportCode) => false;
}

/// <summary>Từ chối render đúng một mã (báo cáo con) — kiểm chứng fallback placeholder.</summary>
sealed class ChildDenyAuthorizer : IReportAuthorizer
{
    private readonly string _denied;

    public ChildDenyAuthorizer(string denied) => _denied = denied;

    public bool CanRender(string reportCode) =>
        !string.Equals(reportCode, _denied, StringComparison.OrdinalIgnoreCase);

    public bool CanDesign(string reportCode) => true;
}
