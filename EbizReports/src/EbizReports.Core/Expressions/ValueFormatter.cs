using System.Globalization;

namespace EbizReports.Core.Expressions;

public static class ValueFormatter
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static string Format(object? value, string? format)
    {
        if (value == null)
            return "";

        if (value is bool b)
            return b ? "Có" : "Không";

        if (!string.IsNullOrWhiteSpace(format))
        {
            if (value is DateTime dt)
                return dt.ToString(format, CultureInfo.InvariantCulture);

            if (IsNumeric(value))
            {
                var d = ToDecimal(value);
                try
                {
                    return d.ToString(format, Vi);
                }
                catch (FormatException)
                {
                    return d.ToString(Vi);
                }
            }
        }

        return value switch
        {
            DateTime d2 => d2.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
            decimal dec => dec.ToString("N0", Vi),
            double db => db.ToString("N0", Vi),
            int i => i.ToString("N0", Vi),
            long l => l.ToString("N0", Vi),
            _ => value.ToString() ?? "",
        };
    }

    public static bool IsNumeric(object v) =>
        v is decimal or double or float or int or long or short or byte;

    public static decimal ToDecimal(object v) => v switch
    {
        decimal d => d,
        double db => (decimal)db,
        float f => (decimal)f,
        int i => i,
        long l => l,
        short s => s,
        byte b => b,
        _ => 0m,
    };
}
