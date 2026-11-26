using EbizReports.Core.Integration;

namespace EbizReports.Data;

/// <summary>
/// Sổ đăng ký các kết nối CSDL do host cấu hình. Report chỉ tham chiếu theo tên,
/// nhờ đó chuỗi kết nối không lọt vào file JSON của báo cáo.
/// </summary>
public sealed class SqlConnectionRegistry
{
    private readonly Dictionary<string, SqlConnectionOptions> _map = new(StringComparer.OrdinalIgnoreCase);

    public SqlConnectionRegistry Register(SqlConnectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.Name))
            throw new ArgumentException("Tên kết nối là bắt buộc.", nameof(options));
        _map[options.Name] = options;
        return this;
    }

    public SqlConnectionOptions? Resolve(string? name) =>
        !string.IsNullOrWhiteSpace(name) && _map.TryGetValue(name!, out var o) ? o : null;

    public IReadOnlyList<SqlConnectionOptions> All() => _map.Values.ToList();

    /// <summary>Danh sách kết nối để designer hiển thị (không lộ chuỗi kết nối).</summary>
    public IReadOnlyList<ReportConnectionInfo> Describe() =>
        _map.Values
            .Select(o => new ReportConnectionInfo
            {
                Name = o.Name,
                Provider = o.Provider,
                Label = string.IsNullOrWhiteSpace(o.Label) ? $"{o.Name} ({o.Provider})" : o.Label,
            })
            .ToList();
}
