using EbizReports.Core.Model;
using ZXing;

namespace EbizReports.Rendering;

/// <summary>
/// Kiểm chứng vòng tròn cho mã vạch/QR: mã hoá → raster hoá ma trận module → giải mã lại.
/// Dùng cho self-test (Demo) chứ không tham gia vào quá trình render.
/// </summary>
public static class SymbologyVerifier
{
    public sealed record Result(bool Ok, string Expected, string? Decoded, string Detail);

    public static Result Verify(ItemKind kind, SymbologySpec? spec, string value, int scale = 4, int quiet = 8)
    {
        if (!Symbology.TryEncodeMatrix(kind, spec, value, out var on, out var w, out var h, out _, out _))
            return new Result(false, value, null, "encode thất bại");

        var iw = (w + 2 * quiet) * scale;
        var ih = (h + 2 * quiet) * scale;
        var rgb = new byte[iw * ih * 3];
        Array.Fill(rgb, (byte)255);

        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                if (!on[y, x])
                    continue;
                for (var dy = 0; dy < scale; dy++)
                {
                    var py = (quiet + y) * scale + dy;
                    for (var dx = 0; dx < scale; dx++)
                    {
                        var px = (quiet + x) * scale + dx;
                        var i = (py * iw + px) * 3;
                        rgb[i] = rgb[i + 1] = rgb[i + 2] = 0;
                    }
                }
            }
        }

        var reader = new BarcodeReaderGeneric();
        var res = reader.Decode(rgb, iw, ih, RGBLuminanceSource.BitmapFormat.RGB24);
        var ok = res != null && string.Equals(res.Text, value, StringComparison.Ordinal);
        return new Result(ok, value, res?.Text, res == null ? "giải mã trả về null" : $"format={res.BarcodeFormat}");
    }
}
