using System.Text;
using EbizReports.Core.Model;

namespace EbizReports.Core.Integration;

/// <summary>
/// Lưu mỗi report thành một file JSON trong thư mục. Có cache in-memory để giữ nguyên instance
/// đã nạp (giống <see cref="InMemoryReportStore"/>) nhưng vẫn bền vững qua các lần chạy.
/// Lần đầu, các report trong <c>seed</c> chưa có file sẽ được ghi ra.
/// </summary>
public sealed class FileReportStore : IReportStore
{
    private readonly string _dir;
    private readonly List<ReportDefinition> _seed;
    private readonly Dictionary<string, ReportDefinition> _map = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _order = new();
    private bool _loaded;

    public FileReportStore(string directory, IEnumerable<ReportDefinition>? seed = null)
    {
        _dir = directory ?? throw new ArgumentNullException(nameof(directory));
        _seed = seed?.ToList() ?? new List<ReportDefinition>();
    }

    public IReadOnlyList<ReportDefinition> List()
    {
        EnsureLoaded();
        return _order.Select(c => _map[c]).ToList();
    }

    public ReportDefinition? Get(string code)
    {
        EnsureLoaded();
        return !string.IsNullOrWhiteSpace(code) && _map.TryGetValue(code, out var d) ? d : null;
    }

    public void Save(ReportDefinition definition)
    {
        if (definition is null || string.IsNullOrWhiteSpace(definition.Code))
            throw new ArgumentException("ReportDefinition.Code là bắt buộc.", nameof(definition));

        EnsureLoaded();
        if (!_map.ContainsKey(definition.Code))
            _order.Add(definition.Code);
        _map[definition.Code] = definition;

        Directory.CreateDirectory(_dir);
        File.WriteAllText(PathOf(definition.Code), definition.ToJson(), new UTF8Encoding(false));
    }

    public bool Delete(string code)
    {
        EnsureLoaded();
        if (string.IsNullOrWhiteSpace(code) || !_map.Remove(code))
            return false;

        _order.RemoveAll(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase));
        try
        {
            var path = PathOf(code);
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // bỏ qua lỗi xoá file (ví dụ đang bị khoá)
        }
        return true;
    }

    private void EnsureLoaded()
    {
        if (_loaded)
            return;
        _loaded = true;

        Directory.CreateDirectory(_dir);
        foreach (var file in Directory.EnumerateFiles(_dir, "*.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var def = ReportDefinition.FromJson(File.ReadAllText(file));
                if (!string.IsNullOrWhiteSpace(def.Code) && !_map.ContainsKey(def.Code))
                {
                    _map[def.Code] = def;
                    _order.Add(def.Code);
                }
            }
            catch
            {
                // bỏ qua file JSON hỏng để không chặn cả kho
            }
        }

        // seed những report còn thiếu (ghi ra đĩa)
        foreach (var def in _seed)
        {
            if (!string.IsNullOrWhiteSpace(def.Code) && !_map.ContainsKey(def.Code))
                Save(def);
        }
    }

    private string PathOf(string code) => Path.Combine(_dir, SafeFileName(code) + ".json");

    private static string SafeFileName(string code)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = code.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }
}
