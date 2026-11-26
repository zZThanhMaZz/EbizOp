using EbizReports.Core.Model;

namespace EbizReports.Core.Integration;

/// <summary>Kho in-memory (giữ nguyên instance đã nạp). Dùng cho test, demo, hoặc cache tạm.</summary>
public sealed class InMemoryReportStore : IReportStore
{
    private readonly Dictionary<string, ReportDefinition> _map = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _order = new();

    public InMemoryReportStore(IEnumerable<ReportDefinition>? seed = null)
    {
        if (seed is null)
            return;
        foreach (var d in seed)
            Save(d);
    }

    public IReadOnlyList<ReportDefinition> List() => _order.Select(c => _map[c]).ToList();

    public ReportDefinition? Get(string code) =>
        !string.IsNullOrWhiteSpace(code) && _map.TryGetValue(code, out var d) ? d : null;

    public void Save(ReportDefinition definition)
    {
        if (definition is null || string.IsNullOrWhiteSpace(definition.Code))
            throw new ArgumentException("ReportDefinition.Code là bắt buộc.", nameof(definition));

        if (!_map.ContainsKey(definition.Code))
            _order.Add(definition.Code);
        _map[definition.Code] = definition;
    }

    public bool Delete(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || !_map.Remove(code))
            return false;
        _order.RemoveAll(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase));
        return true;
    }
}
