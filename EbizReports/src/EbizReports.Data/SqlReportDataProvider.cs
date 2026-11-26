using EbizReports.Core.Data;
using EbizReports.Core.Integration;
using EbizReports.Core.Model;

namespace EbizReports.Data;

/// <summary>
/// Nguồn dữ liệu cho report dựa trên SQL: nếu định nghĩa có <see cref="ReportDataSource"/> kiểu
/// <c>sql</c> thì chạy truy vấn tương ứng; ngược lại chuyển tiếp cho <paramref name="fallback"/>
/// (thường là nguồn dữ liệu mẫu/dịch vụ của host).
/// </summary>
public sealed class SqlReportDataProvider : IReportDataProvider
{
    private readonly SqlConnectionRegistry _registry;
    private readonly IReportDataProvider? _fallback;

    public SqlReportDataProvider(SqlConnectionRegistry registry, IReportDataProvider? fallback = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _fallback = fallback;
    }

    public ReportData GetData(ReportDefinition definition, IReadOnlyDictionary<string, object?> parameters)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var sqlSource = definition.DataSources.FirstOrDefault(d =>
            string.Equals(d.Type, "sql", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(d.Sql));

        if (sqlSource is null)
            return _fallback?.GetData(definition, parameters) ?? new ReportData();

        var conn = _registry.Resolve(sqlSource.Connection)
            ?? throw new InvalidOperationException(
                $"Không tìm thấy kết nối “{sqlSource.Connection}” cho nguồn “{sqlSource.Name}”.");

        return SqlDataSourceExecutor.Execute(conn, sqlSource.Sql, parameters);
    }
}
