using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using EbizReports.Core.Data;
using EbizReports.Core.Integration;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace EbizReports.Data;

/// <summary>
/// Chạy câu truy vấn SQL trên một kết nối (<c>sqlite</c> hoặc <c>sqlserver</c>) và trả về dữ liệu
/// dạng <see cref="ReportData"/>. Dùng ADO.NET thuần (<see cref="DbProviderFactory"/>) nên không
/// phụ thuộc ORM. Tham số trong SQL viết dạng <c>@Ten</c> và được bind từ tham số của báo cáo.
/// </summary>
public static class SqlDataSourceExecutor
{
    private static readonly Regex ParamToken =
        new(@"@([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);

    private static DbProviderFactory Factory(string provider) =>
        provider?.Trim().ToLowerInvariant() switch
        {
            "sqlite" => SqliteFactory.Instance,
            _ => SqlClientFactory.Instance,
        };

    private static DbConnection Open(SqlConnectionOptions conn)
    {
        var factory = Factory(conn.Provider);
        var db = factory.CreateConnection()
            ?? throw new InvalidOperationException($"Không tạo được kết nối cho nhà cung cấp “{conn.Provider}”.");
        db.ConnectionString = conn.ConnectionString;
        db.Open();
        return db;
    }

    /// <summary>Chạy thử: trả về danh sách cột + tối đa <paramref name="maxRows"/> dòng mẫu.</summary>
    public static SqlProbeResult Probe(SqlConnectionOptions? conn, string? sql,
        IReadOnlyDictionary<string, object?> parameters, int maxRows = 100)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            if (conn is null)
                return new SqlProbeResult { Ok = false, Error = "Chưa chọn kết nối dữ liệu." };

            var (columns, rows, total) = Run(conn, sql, parameters, maxRows);
            return new SqlProbeResult
            {
                Ok = true,
                Columns = columns,
                Rows = rows,
                RowCount = total,
                ElapsedMs = sw.ElapsedMilliseconds,
            };
        }
        catch (Exception ex)
        {
            return new SqlProbeResult { Ok = false, Error = ex.Message, ElapsedMs = sw.ElapsedMilliseconds };
        }
    }

    /// <summary>Lấy toàn bộ dữ liệu cho render.</summary>
    public static ReportData Execute(SqlConnectionOptions conn, string? sql,
        IReadOnlyDictionary<string, object?> parameters)
    {
        var (_, rows, _) = Run(conn, sql, parameters, int.MaxValue);
        var data = new ReportData();
        foreach (var row in rows)
            data.AddRow(row);
        return data;
    }

    private static (IReadOnlyList<string> Columns, List<Dictionary<string, object?>> Rows, int Total) Run(
        SqlConnectionOptions conn, string? sql, IReadOnlyDictionary<string, object?> parameters, int maxRows)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new InvalidOperationException("Câu truy vấn SQL đang trống.");

        using var db = Open(conn);
        using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = 60;

        // Chỉ bind những tham số thực sự xuất hiện trong câu SQL (tránh lỗi tham số thừa).
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in ParamToken.Matches(sql))
            used.Add(m.Groups[1].Value);

        foreach (var name in used)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = "@" + name;
            p.Value = parameters.TryGetValue(name, out var v) && v is not null
                ? Normalize(v, conn.Provider)
                : DBNull.Value;
            cmd.Parameters.Add(p);
        }

        var columns = new List<string>();
        var rows = new List<Dictionary<string, object?>>();
        var total = 0;

        using var reader = cmd.ExecuteReader();
        for (var i = 0; i < reader.FieldCount; i++)
            columns.Add(reader.GetName(i));

        while (reader.Read())
        {
            total++;
            if (rows.Count >= maxRows)
                continue; // vẫn đếm tổng số dòng
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
                row[columns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }

        return (columns, rows, total);
    }

    private static object Normalize(object value, string provider)
    {
        // SQLite không có kiểu ngày thật; cột ngày được lưu dạng TEXT ISO "yyyy-MM-dd".
        // Ép DateTime về đúng định dạng đó để so sánh chuỗi (>=, <=) chính xác.
        if (string.Equals(provider, "sqlite", StringComparison.OrdinalIgnoreCase) && value is DateTime dt)
            return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return value;
    }
}
