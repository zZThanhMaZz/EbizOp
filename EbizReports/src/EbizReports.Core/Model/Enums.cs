namespace EbizReports.Core.Model;

public enum BandKind
{
    ReportHeader,
    PageHeader,
    GroupHeader,
    Detail,
    GroupFooter,
    PageFooter,
    ReportFooter,
}

public enum ItemKind
{
    Text,
    Field,
    Expression,
    PageNumber,
    Line,
    Rect,
    Image,
    Barcode,
    QrCode,
    Chart,
    Table,
    Subreport,
}

public enum AggregateKind
{
    None,
    Sum,
    Avg,
    Count,
    Min,
    Max,
}

public enum ReportParameterType
{
    String,
    Int,
    Decimal,
    Date,
    Bool,
}

public enum PaperSizeKind
{
    A4,
    A5,
    A6,
    Letter,
    Custom,
}

public enum PaperOrientation
{
    Portrait,
    Landscape,
}

public enum RenderFormat
{
    Pdf,
    Xlsx,
    Csv,
}
