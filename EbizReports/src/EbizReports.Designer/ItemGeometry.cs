namespace EbizReports.Designer;

/// <summary>Toạ độ mm mà JS gửi về sau khi kéo-thả / resize một item.</summary>
public sealed class ItemGeometry
{
    public int Band { get; set; }
    public int Idx { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; }
    public double H { get; set; }
}
