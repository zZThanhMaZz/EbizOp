using System.Globalization;
using System.Text;
using EbizReports.Core.Model;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.QrCode.Internal;

namespace EbizReports.Rendering;

/// <summary>Kết quả mã hoá một ký hiệu (barcode/QR) dưới dạng SVG vector.</summary>
public sealed class SymbologyResult
{
    public required string Svg { get; init; }
    public required string Value { get; init; }
    public required string Format { get; init; }
    public int ModulesWide { get; init; }
    public int ModulesHigh { get; init; }
}

/// <summary>
/// Tạo mã vạch (Code128, …) và QRCode dưới dạng SVG vector thuần.
/// Dùng ZXing.Net để mã hoá (thuần managed), không cần thư viện ảnh/System.Drawing.
/// </summary>
public static class Symbology
{
    public static bool IsSymbology(ItemKind kind) => kind is ItemKind.Barcode or ItemKind.QrCode;

    /// <summary>
    /// Mã hoá <paramref name="value"/> thành SVG.
    /// <paramref name="aspect"/> = W/H của ô item, dùng để giữ tỉ lệ cho barcode 1D.
    /// </summary>
    public static SymbologyResult? Encode(ItemKind kind, SymbologySpec? spec, string? value, double aspect = 2.5)
    {
        if (!TryEncodeMatrix(kind, spec, value, out var on, out var w, out var h, out var fmt, out var v))
            return null;

        var isQr = kind == ItemKind.QrCode;
        var quiet = QuietOf(spec, isQr);
        var qx = quiet;
        var qy = isQr ? quiet : 0;

        double vbH, cellH;
        if (isQr)
        {
            vbH = w;        // QR: ô vuông
            cellH = 1;
        }
        else
        {
            var totalW = w + 2.0 * qx;
            var a = aspect > 0.05 ? aspect : 2.5;
            vbH = totalW / a;   // khớp tỉ lệ ô item → lấp đầy khi FitArea
            cellH = vbH;
        }

        var svg = BuildSvg(on, w, h, qx, qy, vbH, cellH, stretch: !isQr);
        return new SymbologyResult
        {
            Svg = svg,
            Value = value!,
            Format = fmt,
            ModulesWide = w,
            ModulesHigh = h,
        };
    }

    private static int QuietOf(SymbologySpec? spec, bool isQr)
    {
        if (spec is { QuietZone: > 0 })
            return spec.QuietZone;
        return isQr ? 4 : 10;
    }

    internal static bool TryEncodeMatrix(ItemKind kind, SymbologySpec? spec, string? value,
        out bool[,] on, out int w, out int h, out string fmtName, out string cleanValue)
    {
        on = new bool[0, 0];
        w = h = 0;
        fmtName = "";
        cleanValue = (value ?? "").Trim();

        if (cleanValue.Length == 0)
            return false;

        try
        {
            var hints = new Dictionary<EncodeHintType, object>
            {
                [EncodeHintType.MARGIN] = 0,
                [EncodeHintType.CHARACTER_SET] = "UTF-8",
            };

            BitMatrix matrix;
            if (kind == ItemKind.QrCode)
            {
                hints[EncodeHintType.ERROR_CORRECTION] = Ecc(spec?.Ecc);
                matrix = new QRCodeWriter().encode(cleanValue, BarcodeFormat.QR_CODE, 1, 1, hints);
                fmtName = "QR-" + (spec?.Ecc ?? "M").ToUpperInvariant();
            }
            else
            {
                var fmt = MapFormat(spec?.Format);
                matrix = new MultiFormatWriter().encode(cleanValue, fmt, 1, 1, hints);
                fmtName = fmt.ToString().Replace('_', '-');
            }

            return Normalize(matrix, out on, out w, out h);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Chuyển BitMatrix về ma trận module (bỏ mọi hệ số phóng đại nếu có).</summary>
    private static bool Normalize(BitMatrix m, out bool[,] on, out int w, out int h)
    {
        on = new bool[0, 0];
        w = h = 0;
        if (m.Width <= 0 || m.Height <= 0)
            return false;

        var scale = DetectScale(m);
        w = m.Width / scale;
        h = m.Height / scale;
        if (w <= 0 || h <= 0)
            return false;

        var arr = new bool[h, w];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                arr[y, x] = m[x * scale, y * scale];

        on = arr;
        return true;
    }

    private static int DetectScale(BitMatrix m)
    {
        var g = 0;
        for (var y = 0; y < m.Height; y++)
        {
            var prev = m[0, y];
            var run = 1;
            for (var x = 1; x < m.Width; x++)
            {
                var v = m[x, y];
                if (v == prev)
                {
                    run++;
                }
                else
                {
                    if (prev)
                        g = Gcd(g, run);
                    prev = v;
                    run = 1;
                }
            }
            if (prev)
                g = Gcd(g, run);
        }
        return g <= 0 ? 1 : g;
    }

    private static int Gcd(int a, int b)
    {
        while (b != 0)
        {
            (a, b) = (b, a % b);
        }
        return Math.Abs(a);
    }

    private static BarcodeFormat MapFormat(string? f) => (f ?? "").Trim().ToLowerInvariant() switch
    {
        "code39" or "code-39" => BarcodeFormat.CODE_39,
        "ean13" or "ean-13" => BarcodeFormat.EAN_13,
        "ean8" or "ean-8" => BarcodeFormat.EAN_8,
        "upca" or "upc-a" => BarcodeFormat.UPC_A,
        "itf" => BarcodeFormat.ITF,
        _ => BarcodeFormat.CODE_128,
    };

    private static ErrorCorrectionLevel Ecc(string? level) => (level ?? "M").Trim().ToUpperInvariant() switch
    {
        "L" => ErrorCorrectionLevel.L,
        "Q" => ErrorCorrectionLevel.Q,
        "H" => ErrorCorrectionLevel.H,
        _ => ErrorCorrectionLevel.M,
    };

    private static string BuildSvg(bool[,] on, int w, int h, int qx, int qy, double vbH, double cellH, bool stretch)
    {
        var totalW = w + 2.0 * qx;
        var totalH = vbH + 2.0 * qy;

        var sb = new StringBuilder(256 + w * h / 8);
        sb.Append("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 ")
          .Append(F(totalW)).Append(' ').Append(F(totalH)).Append('\'');
        if (stretch)
            sb.Append(" preserveAspectRatio='none'");
        sb.Append(" shape-rendering='crispEdges'>");
        sb.Append("<rect x='0' y='0' width='").Append(F(totalW))
          .Append("' height='").Append(F(totalH)).Append("' fill='#ffffff'/>");
        sb.Append("<g fill='#000000' transform='translate(").Append(F(qx)).Append(' ').Append(F(qy)).Append(")'>");

        for (var y = 0; y < h; y++)
        {
            var x = 0;
            while (x < w)
            {
                if (!on[y, x])
                {
                    x++;
                    continue;
                }
                var x0 = x;
                while (x < w && on[y, x])
                    x++;
                var run = x - x0;
                sb.Append("<rect x='").Append(x0)
                  .Append("' y='").Append(F(y * cellH))
                  .Append("' width='").Append(run)
                  .Append("' height='").Append(F(cellH)).Append("'/>");
            }
        }

        sb.Append("</g></svg>");
        return sb.ToString();
    }

    private static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
}
