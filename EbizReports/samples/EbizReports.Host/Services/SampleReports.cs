using EbizReports.Core.Data;
using EbizReports.Core.Model;

namespace EbizReports.Host.Services;

/// <summary>Nguồn report + dữ liệu mẫu cho demo (thay bằng ReportService của EbizOp khi tích hợp).</summary>
public sealed class SampleReports
{
    private readonly List<ReportDefinition> _definitions;

    public SampleReports()
    {
        _definitions = new List<ReportDefinition>
        {
            BuildInvoice(), BuildSalesByGroup(), BuildTopProducts(), BuildSummary(),
        };
    }

    public IReadOnlyList<ReportDefinition> All() => _definitions;

    public ReportDefinition Get(string code) =>
        _definitions.FirstOrDefault(d => string.Equals(d.Code, code, StringComparison.OrdinalIgnoreCase))
        ?? _definitions[0];

    public ReportData GetData(string code) => code switch
    {
        "ban-hang-theo-nhom" => SalesData(),
        "top-san-pham" => SalesData(),
        "bao-cao-tong-hop" => SalesData(),
        _ => InvoiceData(),
    };

    // ---------------------------------------------------------------- ảnh / logo mẫu

    /// <summary>Logo dạng SVG (nhúng thẳng vào definition, không cần tệp ngoài).</summary>
    private const string LogoSvg = """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 160 80">
          <rect width="160" height="80" rx="10" fill="#1565C0"/>
          <circle cx="46" cy="32" r="16" fill="none" stroke="#FFFFFF" stroke-width="6"/>
          <circle cx="96" cy="32" r="16" fill="none" stroke="#FFFFFF" stroke-width="6"/>
          <path d="M62 32h18" stroke="#FFFFFF" stroke-width="6"/>
          <text x="80" y="70" font-family="Arial, sans-serif" font-size="18" font-weight="bold"
                fill="#FFFFFF" text-anchor="middle">EbizOp</text>
        </svg>
        """;

    private static string SvgDataUri(string svg) =>
        "data:image/svg+xml;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svg));

    // ---------------------------------------------------------------- hóa đơn
    private static ReportDefinition BuildInvoice()
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

    private static ReportData InvoiceData()
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

    // ---------------------------------------------------------------- báo cáo nhóm
    private static ReportDefinition BuildSalesByGroup()
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
                new ReportItem
                {
                    Kind = ItemKind.Field, X = 130, Y = 0, W = 20, H = 6, Binding = "Qty", Format = "0.###",
                    Style = new ItemStyle { Size = 8, Align = "center" },
                    StyleRules =
                    {
                        // Số lượng lớn: in đậm + nền cam nhạt.
                        new StyleRule { When = "Qty >= 5", Bold = true, Background = "#FFF3E0" },
                    },
                },
                new ReportItem
                {
                    Kind = ItemKind.Field, X = 190, Y = 0, W = 40, H = 6, Binding = "LineTotal", Format = "N0",
                    Style = new ItemStyle { Size = 8, Align = "right" },
                    StyleRules =
                    {
                        // Thành tiền từ 1 triệu: tô đỏ + in đậm.
                        new StyleRule { When = "LineTotal >= 1000000", Bold = true, Color = "#C62828" },
                    },
                },
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
                        Type = "bar",
                        Category = "Name",
                        Value = "LineTotal",
                        Aggregate = "sum",
                        Legend = "none",
                        ShowValues = true,
                        Title = "Doanh thu theo sản phẩm",
                    },
                },
            },
        });

        return def;
    }

    // ---------------------------------------------------------------- top sản phẩm (item Bảng)
    private static ReportDefinition BuildTopProducts()
    {
        var def = new ReportDefinition
        {
            Code = "top-san-pham",
            Name = "Top sản phẩm bán chạy",
            Paper = new PaperSettings
            {
                Size = PaperSizeKind.A4,
                Orientation = PaperOrientation.Portrait,
                Margins = new Margins { Top = 12, Right = 6, Bottom = 10, Left = 6 },
            },
        };

        def.Bands.Add(new ReportBand
        {
            Kind = BandKind.PageHeader,
            Height = 14,
            Items =
            {
                new ReportItem { Kind = ItemKind.Text, X = 0, Y = 0, W = 198, H = 8, Text = "TOP SẢN PHẨM BÁN CHẠY", Style = new ItemStyle { Size = 14, Bold = true } },
                new ReportItem { Kind = ItemKind.Text, X = 0, Y = 8, W = 198, H = 5, Text = "Danh sách sản phẩm kèm số lượng và doanh thu", Style = new ItemStyle { Size = 8, Color = "#757575" } },
            },
        });

        def.Bands.Add(new ReportBand
        {
            Kind = BandKind.ReportHeader,
            Height = 72,
            Items =
            {
                new ReportItem
                {
                    Kind = ItemKind.Table, X = 0, Y = 0, W = 198, H = 70,
                    Table = new TableSpec
                    {
                        ShowHeader = true,
                        Borders = true,
                        Zebra = true,
                        Footer = true,
                        RowHeight = 7,
                        HeaderBackground = "#1E5AA8",
                        Columns =
                        {
                            new TableColumn { Header = "Tên hàng", Binding = "Name", Width = 96 },
                            new TableColumn { Header = "Nhóm", Binding = "GroupName", Width = 46 },
                            new TableColumn { Header = "SL", Binding = "Qty", Width = 18, Align = "center", Aggregate = "sum", Format = "N0" },
                            new TableColumn { Header = "Doanh thu", Binding = "LineTotal", Width = 38, Align = "right", Aggregate = "sum", Format = "N0" },
                        },
                    },
                },
            },
        });

        return def;
    }

    // ---------------------------------------------------------------- báo cáo tổng hợp (item Subreport)
    private static ReportDefinition BuildSummary()
    {
        var def = new ReportDefinition
        {
            Code = "bao-cao-tong-hop",
            Name = "Báo cáo tổng hợp (có báo cáo con)",
            Paper = new PaperSettings
            {
                Size = PaperSizeKind.A4,
                Orientation = PaperOrientation.Portrait,
                Margins = new Margins { Top = 12, Right = 6, Bottom = 10, Left = 6 },
            },
        };

        def.Bands.Add(new ReportBand
        {
            Kind = BandKind.PageHeader,
            Height = 16,
            Items =
            {
                new ReportItem { Kind = ItemKind.Text, X = 0, Y = 0, W = 160, H = 8, Text = "BÁO CÁO TỔNG HỢP", Style = new ItemStyle { Size = 14, Bold = true } },
                new ReportItem { Kind = ItemKind.Text, X = 0, Y = 8, W = 160, H = 5, Text = "Trang bìa + báo cáo con nhúng bên dưới", Style = new ItemStyle { Size = 8, Color = "#757575" } },
                new ReportItem
                {
                    Kind = ItemKind.Image, X = 166, Y = 0, W = 32, H = 16,
                    Src = SvgDataUri(LogoSvg),
                },
            },
        });

        def.Bands.Add(new ReportBand
        {
            Kind = BandKind.ReportFooter,
            Height = 100,
            Items =
            {
                new ReportItem
                {
                    Kind = ItemKind.Subreport, X = 0, Y = 0, W = 198, H = 98,
                    Subreport = new SubreportSpec
                    {
                        ReportCode = "top-san-pham",
                        InheritParameters = true,
                        ShowBorder = true,
                    },
                },
            },
        });

        return def;
    }

    private static ReportData SalesData()
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
}
