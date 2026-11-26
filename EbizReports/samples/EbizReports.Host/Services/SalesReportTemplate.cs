using EbizReports.Core.Model;

namespace EbizReports.Host.Services;

/// <summary>
/// Bộ khung báo cáo "Danh sách phiếu bán hàng" dùng khi bấm “Tạo báo cáo mới”.
/// Chỉ dựng bố cục + tham số ngày; nguồn dữ liệu SQL do người dùng khai báo trong designer.
/// </summary>
public static class SalesReportTemplate
{
    public static ReportDefinition Build(string code, string name) => new()
    {
        Code = code,
        Name = string.IsNullOrWhiteSpace(name) ? "Danh sách phiếu bán hàng" : name,
        Paper = new PaperSettings
        {
            Size = PaperSizeKind.A4,
            Orientation = PaperOrientation.Portrait,
            Margins = new Margins { Top = 12, Right = 6, Bottom = 10, Left = 6 },
        },
        Parameters =
        {
            new ReportParameter { Name = "TuNgay", Type = ReportParameterType.Date, Label = "Từ ngày", Default = "2026-01-01" },
            new ReportParameter { Name = "DenNgay", Type = ReportParameterType.Date, Label = "Đến ngày", Default = "2026-03-31" },
        },
        Bands =
        {
            new ReportBand
            {
                Kind = BandKind.PageHeader,
                Height = 16,
                Items =
                {
                    new ReportItem { Kind = ItemKind.Text, X = 0, Y = 0, W = 198, H = 8, Text = "DANH SÁCH PHIẾU BÁN HÀNG", Style = new ItemStyle { Size = 14, Bold = true } },
                    new ReportItem { Kind = ItemKind.Text, X = 0, Y = 9, W = 32, H = 5, Text = "Từ ngày:", Style = new ItemStyle { Size = 9, Align = "right" } },
                    new ReportItem { Kind = ItemKind.Expression, X = 34, Y = 8, W = 36, H = 6, Expression = "TuNgay", Format = "dd/MM/yyyy", Style = new ItemStyle { Size = 9 } },
                    new ReportItem { Kind = ItemKind.Text, X = 74, Y = 9, W = 22, H = 5, Text = "đến ngày:", Style = new ItemStyle { Size = 9, Align = "right" } },
                    new ReportItem { Kind = ItemKind.Expression, X = 98, Y = 8, W = 36, H = 6, Expression = "DenNgay", Format = "dd/MM/yyyy", Style = new ItemStyle { Size = 9 } },
                },
            },
            new ReportBand
            {
                Kind = BandKind.ReportHeader,
                Height = 8,
                Items =
                {
                    new ReportItem { Kind = ItemKind.Text, X = 0, Y = 1, W = 28, H = 6, Text = "Mã phiếu", Style = new ItemStyle { Size = 8, Bold = true } },
                    new ReportItem { Kind = ItemKind.Text, X = 28, Y = 1, W = 24, H = 6, Text = "Ngày bán", Style = new ItemStyle { Size = 8, Bold = true, Align = "center" } },
                    new ReportItem { Kind = ItemKind.Text, X = 52, Y = 1, W = 56, H = 6, Text = "Khách hàng", Style = new ItemStyle { Size = 8, Bold = true } },
                    new ReportItem { Kind = ItemKind.Text, X = 108, Y = 1, W = 30, H = 6, Text = "Nhân viên", Style = new ItemStyle { Size = 8, Bold = true } },
                    new ReportItem { Kind = ItemKind.Text, X = 138, Y = 1, W = 32, H = 6, Text = "Trạng thái", Style = new ItemStyle { Size = 8, Bold = true, Align = "center" } },
                    new ReportItem { Kind = ItemKind.Text, X = 170, Y = 1, W = 28, H = 6, Text = "Tổng tiền", Style = new ItemStyle { Size = 8, Bold = true, Align = "right" } },
                    new ReportItem { Kind = ItemKind.Line, X = 0, Y = 7, W = 198, H = 1, Style = new ItemStyle { Size = 0.6 } },
                },
            },
            new ReportBand
            {
                Kind = BandKind.Detail,
                Height = 7,
                Items =
                {
                    new ReportItem { Kind = ItemKind.Field, X = 0, Y = 0, W = 28, H = 7, Binding = "MaPhieu", Style = new ItemStyle { Size = 8 } },
                    new ReportItem { Kind = ItemKind.Field, X = 28, Y = 0, W = 24, H = 7, Binding = "NgayBan", Format = "dd/MM/yyyy", Style = new ItemStyle { Size = 8, Align = "center" } },
                    new ReportItem { Kind = ItemKind.Field, X = 52, Y = 0, W = 56, H = 7, Binding = "KhachHang", Style = new ItemStyle { Size = 8 } },
                    new ReportItem { Kind = ItemKind.Field, X = 108, Y = 0, W = 30, H = 7, Binding = "NhanVien", Style = new ItemStyle { Size = 8 } },
                    new ReportItem { Kind = ItemKind.Field, X = 138, Y = 0, W = 32, H = 7, Binding = "TrangThai", Style = new ItemStyle { Size = 8, Align = "center" } },
                    new ReportItem { Kind = ItemKind.Field, X = 170, Y = 0, W = 28, H = 7, Binding = "TongTien", Format = "N0", Style = new ItemStyle { Size = 8, Align = "right" } },
                },
            },
            new ReportBand
            {
                Kind = BandKind.ReportFooter,
                Height = 12,
                Items =
                {
                    new ReportItem { Kind = ItemKind.Line, X = 0, Y = 1, W = 198, H = 1, Style = new ItemStyle { Size = 0.8 } },
                    new ReportItem { Kind = ItemKind.Text, X = 110, Y = 3, W = 58, H = 7, Text = "TỔNG CỘNG", Style = new ItemStyle { Size = 10, Bold = true, Align = "right" } },
                    new ReportItem { Kind = ItemKind.Expression, X = 170, Y = 3, W = 28, H = 7, Expression = "Sum(TongTien)", Format = "N0", Style = new ItemStyle { Size = 10, Bold = true, Align = "right" } },
                },
            },
            new ReportBand
            {
                Kind = BandKind.PageFooter,
                Height = 6,
                Items =
                {
                    new ReportItem { Kind = ItemKind.PageNumber, X = 0, Y = 0, W = 198, H = 6, Text = "Trang {Page}/{Pages}", Style = new ItemStyle { Size = 7, Align = "center", Color = "#888888" } },
                },
            },
        },
    };
}
