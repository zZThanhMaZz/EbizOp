using System.Text.Json;
using System.Text.Json.Serialization;

namespace EbizReports.Core.Model;

public sealed class ReportDefinition
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Group { get; set; } = "";
    public string Description { get; set; } = "";

    /// <summary>Mẫu do hệ thống cung cấp (không cho xoá/sửa ở client).</summary>
    public bool IsSystem { get; set; }

    /// <summary>Lưu bản sao mẫu tại máy client.</summary>
    public bool SaveToClient { get; set; }

    /// <summary>Dùng để in lưới số liệu (báo cáo dạng bảng lưới).</summary>
    public bool ForGridPrint { get; set; }

    public int Version { get; set; } = 1;
    public PaperSettings Paper { get; set; } = new();
    public List<ReportDataSource> DataSources { get; set; } = new();
    public List<ReportParameter> Parameters { get; set; } = new();
    public List<ReportSort> Sorting { get; set; } = new();
    public List<ReportGroup> Grouping { get; set; } = new();
    public List<ReportBand> Bands { get; set; } = new();
    public List<string> Outputs { get; set; } = new() { "pdf", "xlsx", "csv" };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static ReportDefinition FromJson(string json) =>
        JsonSerializer.Deserialize<ReportDefinition>(json, JsonOptions)
        ?? throw new InvalidOperationException("Report JSON không hợp lệ");

    public ReportBand? Band(BandKind kind) => Bands.FirstOrDefault(b => b.Kind == kind);
}

public sealed class PaperSettings
{
    public PaperSizeKind Size { get; set; } = PaperSizeKind.A4;
    public PaperOrientation Orientation { get; set; } = PaperOrientation.Portrait;
    public double WidthMm { get; set; } = 210;
    public double HeightMm { get; set; } = 297;
    public Margins Margins { get; set; } = new();

    public (double Width, double Height) Resolve()
    {
        if (Size == PaperSizeKind.Custom)
            return (WidthMm, HeightMm);

        var (w, h) = Size switch
        {
            PaperSizeKind.A4 => (210d, 297d),
            PaperSizeKind.A5 => (148d, 210d),
            PaperSizeKind.A6 => (105d, 148d),
            PaperSizeKind.Letter => (215.9d, 279.4d),
            _ => (210d, 297d),
        };
        return Orientation == PaperOrientation.Landscape ? (h, w) : (w, h);
    }
}

public sealed class Margins
{
    public double Top { get; set; } = 8;
    public double Right { get; set; } = 6;
    public double Bottom { get; set; } = 8;
    public double Left { get; set; } = 6;
}

public sealed class ReportDataSource
{
    public string Name { get; set; } = "";

    /// <summary>Kiểu nguồn: <c>service</c> (host cung cấp) hoặc <c>sql</c> (truy vấn SQL).</summary>
    public string Type { get; set; } = "service";

    /// <summary>Định danh nguồn khi Type = "service" (host tự hiểu).</summary>
    public string Ref { get; set; } = "";

    /// <summary>Tên tham số truyền vào nguồn dữ liệu (nếu khai báo).</summary>
    public List<string> Params { get; set; } = new();

    /// <summary>Tên kết nối do host đăng ký (dùng khi <see cref="Type"/> = "sql").</summary>
    public string? Connection { get; set; }

    /// <summary>Câu truy vấn SQL, có thể chứa tham số <c>@Ten</c> (dùng khi <see cref="Type"/> = "sql").</summary>
    public string? Sql { get; set; }

    /// <summary>Câu SQL dùng để thiết kế (lấy cột/dữ liệu mẫu trong designer khi báo cáo SQL khác).</summary>
    public string? DesignSql { get; set; }
}

public sealed class ReportParameter
{
    public string Name { get; set; } = "";
    public ReportParameterType Type { get; set; } = ReportParameterType.String;
    public string Label { get; set; } = "";
    public bool Required { get; set; }
    public string? Default { get; set; }

    /// <summary>Quyền cần có để dùng tham số (host tự diễn giải, ví dụ mã quyền).</summary>
    public string? Permission { get; set; }

    /// <summary>Câu SQL đổ dữ liệu cho combobox của tham số (nếu là tham số chọn).</summary>
    public string? ComboBoxSql { get; set; }

    /// <summary>Field giá trị của combobox (khoá).</summary>
    public string? ValueField { get; set; }

    /// <summary>Field hiển thị của combobox (nhãn).</summary>
    public string? DisplayField { get; set; }

    /// <summary>Các field ẩn, cách nhau dấu phẩy.</summary>
    public string? HiddenFields { get; set; }
}

public sealed class ReportSort
{
    public string Field { get; set; } = "";
    public string Dir { get; set; } = "asc";
}

public sealed class ReportGroup
{
    public string Field { get; set; } = "";
    public string Sort { get; set; } = "asc";
    public bool RepeatHeader { get; set; } = true;

    /// <summary>
    /// Giữ toàn bộ một nhóm (GroupHeader + các dòng Detail + GroupFooter) trên cùng một trang
    /// nếu vừa; nếu không vừa mới ngắt sang trang kế. Chỉ áp dụng khi báo cáo có khai báo nhóm.
    /// </summary>
    public bool KeepTogether { get; set; }
}

public sealed class ReportBand
{
    public BandKind Kind { get; set; }
    public double Height { get; set; } = 7;
    public List<ReportItem> Items { get; set; } = new();

    /// <summary>Bắt đầu trang mới trước khi vẽ band này.</summary>
    public bool PageBreakBefore { get; set; }
}

public sealed class ReportItem
{
    public ItemKind Kind { get; set; } = ItemKind.Text;
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; } = 40;
    public double H { get; set; } = 6;

    /// <summary>Nội dung tĩnh (Kind = Text).</summary>
    public string? Text { get; set; }

    /// <summary>Đường dẫn dữ liệu (Kind = Field), ví dụ "Name" hoặc "Bill.BillNo".</summary>
    public string? Binding { get; set; }

    /// <summary>Biểu thức (Kind = Expression / PageNumber).</summary>
    public string? Expression { get; set; }

    /// <summary>Format số/ngày: "N0", "0.###", "dd/MM/yyyy".</summary>
    public string? Format { get; set; }

    public ItemStyle Style { get; set; } = new();

    /// <summary>Nguồn ảnh (đường dẫn file hoặc data URI base64).</summary>
    public string? Src { get; set; }

    /// <summary>Cấu hình mã vạch / QR (Kind = Barcode | QrCode).</summary>
    public SymbologySpec? Symbology { get; set; }

    /// <summary>Cấu hình biểu đồ (Kind = Chart).</summary>
    public ChartSpec? Chart { get; set; }

    /// <summary>Cấu hình bảng lặp (Kind = Table).</summary>
    public TableSpec? Table { get; set; }

    /// <summary>Cấu hình báo cáo con (Kind = Subreport).</summary>
    public SubreportSpec? Subreport { get; set; }

    /// <summary>Id nhóm: các item cùng Group được chọn/kéo cùng nhau.</summary>
    public string? Group { get; set; }

    /// <summary>
    /// Biểu thức điều kiện hiển thị; null/rỗng = luôn hiện. Sai → bỏ item khỏi bản in.
    /// Ví dụ: <c>Qty &gt; 0</c> hoặc <c>LineTotal &lt; 0</c>.
    /// </summary>
    public string? VisibleWhen { get; set; }

    /// <summary>Luật định dạng theo điều kiện; áp lần lượt, luật sau ghi đè luật trước.</summary>
    public List<StyleRule> StyleRules { get; set; } = new();

    /// <summary>Khoá vị trí: không kéo/resize/nudge/xoá được từ canvas.</summary>
    public bool Locked { get; set; }

    /// <summary>Sao chép sâu (dùng cho nhân bản item).</summary>
    public ReportItem Clone()
    {
        var json = JsonSerializer.Serialize(this, CloneJson);
        return JsonSerializer.Deserialize<ReportItem>(json, CloneJson)!;
    }

    private static readonly JsonSerializerOptions CloneJson = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };
}

/// <summary>Cấu hình cho item biểu đồ (Kind = Chart).</summary>
public sealed class ChartSpec
{
    /// <summary>Loại biểu đồ: bar (cột) · line (đường) · pie (tròn).</summary>
    public string Type { get; set; } = "bar";

    /// <summary>Biểu thức phân loại (mỗi dòng detail cho 1 nhãn), ví dụ "Format(Ngay, 'dd/MM')".</summary>
    public string? Category { get; set; }

    /// <summary>Biểu thức giá trị trên mỗi dòng, ví dụ "TongTien".</summary>
    public string? Value { get; set; }

    /// <summary>Tổng hợp theo nhóm: sum (mặc định) · avg · count · min · max.</summary>
    public string Aggregate { get; set; } = "sum";

    /// <summary>Vị trí chú giải: none · right (mặc định) · bottom.</summary>
    public string Legend { get; set; } = "right";

    /// <summary>Hiện giá trị trên cột/điểm.</summary>
    public bool ShowValues { get; set; }

    /// <summary>Tiêu đề biểu đồ (tuỳ chọn).</summary>
    public string? Title { get; set; }

    /// <summary>Cột ngang (chỉ với Type = bar).</summary>
    public bool Horizontal { get; set; }
}

/// <summary>Cấu hình cho item bảng lặp (Kind = Table): tiêu đề cột + các dòng lấy từ dữ liệu.</summary>
public sealed class TableSpec
{
    public List<TableColumn> Columns { get; set; } = new();

    /// <summary>Chiều cao mỗi dòng (mm).</summary>
    public double RowHeight { get; set; } = 6;

    public bool ShowHeader { get; set; } = true;

    public bool Borders { get; set; } = true;

    /// <summary>Sọc xen kẽ cho dòng chẵn/lẻ.</summary>
    public bool Zebra { get; set; }

    /// <summary>Màu nền dòng tiêu đề (#RRGGBB).</summary>
    public string? HeaderBackground { get; set; } = "#EEEEEE";

    /// <summary>Hiện dòng tổng hợp ở cuối (theo Aggregate của từng cột).</summary>
    public bool Footer { get; set; }

    /// <summary>Giới hạn số dòng dữ liệu (0 = tự động theo chiều cao ô).</summary>
    public int MaxRows { get; set; }

    /// <summary>
    /// Phạm vi dữ liệu: "group" = các dòng của nhóm hiện tại (mặc định; dùng cho bảng con đặt
    /// trong GroupHeader/GroupFooter), "all" = toàn bộ dòng detail của báo cáo.
    /// </summary>
    public string Scope { get; set; } = "group";
}

public sealed class TableColumn
{
    public string Header { get; set; } = "";

    /// <summary>Bề rộng cột (mm).</summary>
    public double Width { get; set; } = 30;

    /// <summary>Đường dẫn dữ liệu theo dòng, ví dụ "Name".</summary>
    public string? Binding { get; set; }

    /// <summary>Biểu thức theo dòng (ưu tiên sau Binding).</summary>
    public string? Expression { get; set; }

    public string? Format { get; set; }

    public string Align { get; set; } = "left"; // left | center | right

    /// <summary>Tổng hợp cho dòng footer: none · sum · avg · count · min · max.</summary>
    public string Aggregate { get; set; } = "none";
}

/// <summary>Cấu hình cho item báo cáo con (Kind = Subreport).</summary>
public sealed class SubreportSpec
{
    /// <summary>Mã report con (tra trong kho định nghĩa lúc render).</summary>
    public string ReportCode { get; set; } = "";

    /// <summary>Kế thừa tham số của report cha khi render report con.</summary>
    public bool InheritParameters { get; set; } = true;

    /// <summary>Vẽ khung quanh vùng báo cáo con.</summary>
    public bool ShowBorder { get; set; }
}

/// <summary>Cấu hình cho item mã vạch / QR.</summary>
public sealed class SymbologySpec
{
    /// <summary>
    /// Với Barcode: Code128 (mặc định) · Code39 · EAN13 · EAN8 · UPCA · ITF.
    /// Với QrCode: bỏ qua.
    /// </summary>
    public string Format { get; set; } = "";

    /// <summary>Mức sửa lỗi QR: L · M (mặc định) · Q · H.</summary>
    public string Ecc { get; set; } = "M";

    /// <summary>Hiện chuỗi ký tự ngay dưới mã vạch (chỉ Barcode).</summary>
    public bool ShowText { get; set; }

    /// <summary>Vùng lặng quanh mã, theo module (0 = mặc định 10 cho barcode / 4 cho QR).</summary>
    public int QuietZone { get; set; }
}

public sealed class ItemStyle
{
    public double Size { get; set; } = 9;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public string Align { get; set; } = "left"; // left | center | right
    public string? Color { get; set; }          // #RRGGBB
    public bool Border { get; set; }
    public string? Background { get; set; }     // #RRGGBB

    public ItemStyle Clone() => new()
    {
        Size = Size,
        Bold = Bold,
        Italic = Italic,
        Align = Align,
        Color = Color,
        Border = Border,
        Background = Background,
    };
}

/// <summary>
/// Luật định dạng theo điều kiện: khi <see cref="When"/> đúng thì áp các thuộc tính được đặt
/// (thuộc tính null = giữ nguyên giá trị hiện có). Ví dụ tô đỏ dòng có thành tiền âm.
/// </summary>
public sealed class StyleRule
{
    /// <summary>Biểu thức điều kiện, ví dụ <c>LineTotal &lt; 0</c>.</summary>
    public string When { get; set; } = "";

    public bool? Bold { get; set; }
    public bool? Italic { get; set; }
    public double? Size { get; set; }
    public string? Color { get; set; }        // #RRGGBB
    public string? Background { get; set; }   // #RRGGBB
    public bool? Border { get; set; }
}
