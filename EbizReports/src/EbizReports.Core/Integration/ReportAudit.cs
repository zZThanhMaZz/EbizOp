namespace EbizReports.Core.Integration;

/// <summary>Một bản ghi nhật ký thao tác trên report (thiết kế, in, sao lưu…).</summary>
public sealed class ReportAuditEntry
{
    /// <summary>Thời điểm thao tác (UTC).</summary>
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Loại thao tác: save · delete · clone · import · render · preview.</summary>
    public string Action { get; set; } = "";

    /// <summary>Mã report liên quan.</summary>
    public string Code { get; set; } = "";

    /// <summary>Phiên bản định nghĩa tại thời điểm thao tác (nếu có).</summary>
    public int? Version { get; set; }

    /// <summary>Người thực hiện (nếu host cung cấp).</summary>
    public string? User { get; set; }

    /// <summary>Chi tiết tự do: định dạng in, mã nguồn→đích khi nhân bản, số lượng khi khôi phục…</summary>
    public string? Detail { get; set; }
}

/// <summary>
/// Nơi ghi nhật ký thao tác report. Có thể thay bằng bảng DB / Serilog khi tích hợp EbizOp;
/// mặc định dùng <see cref="InMemoryReportAuditStore"/>.
/// </summary>
public interface IReportAuditStore
{
    void Record(ReportAuditEntry entry);

    /// <summary>Các bản ghi mới nhất trước, tối đa <paramref name="max"/> mục.</summary>
    IReadOnlyList<ReportAuditEntry> Recent(int max = 50);
}

/// <summary>Nhật ký trong bộ nhớ: thread-safe, tự cắt bớt khi vượt sức chứa.</summary>
public sealed class InMemoryReportAuditStore : IReportAuditStore
{
    private readonly object _sync = new();
    private readonly Queue<ReportAuditEntry> _entries = new();
    private readonly int _capacity;

    public InMemoryReportAuditStore(int capacity = 500) => _capacity = Math.Max(1, capacity);

    public void Record(ReportAuditEntry entry)
    {
        if (entry is null)
            return;

        lock (_sync)
        {
            _entries.Enqueue(entry);
            while (_entries.Count > _capacity)
                _entries.Dequeue();
        }
    }

    public IReadOnlyList<ReportAuditEntry> Recent(int max = 50)
    {
        if (max <= 0)
            return Array.Empty<ReportAuditEntry>();

        lock (_sync)
        {
            var take = Math.Min(max, _entries.Count);
            var list = new List<ReportAuditEntry>(take);
            // Queue là FIFO; lấy từ cuối để có "mới nhất trước".
            var arr = _entries.ToArray();
            for (var i = arr.Length - 1; i >= 0 && list.Count < take; i--)
                list.Add(arr[i]);
            return list;
        }
    }

    /// <summary>Số bản ghi hiện có (dùng cho kiểm thử / chẩn đoán).</summary>
    public int Count
    {
        get { lock (_sync) return _entries.Count; }
    }
}