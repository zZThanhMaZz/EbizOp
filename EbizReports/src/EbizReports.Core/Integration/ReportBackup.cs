using System.Text.Json;
using System.Text.Json.Serialization;
using EbizReports.Core.Model;

namespace EbizReports.Core.Integration;

/// <summary>
/// Gói sao lưu toàn bộ kho report dưới dạng **một** file JSON. Dùng cho xuất/nhập kho
/// (backup/restore) và chuyển bộ báo cáo giữa các môi trường mà không phụ thuộc cấu trúc lưu trữ.
/// </summary>
public sealed class ReportBackup
{
    /// <summary>Phiên bản lược đồ gói sao lưu (tăng khi cấu trúc thay đổi không tương thích).</summary>
    public const int CurrentSchema = 1;

    public int SchemaVersion { get; set; } = CurrentSchema;

    /// <summary>Thời điểm xuất (UTC).</summary>
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Toàn bộ định nghĩa report trong kho.</summary>
    public List<ReportDefinition> Reports { get; set; } = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static ReportBackup FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("Tệp sao lưu rỗng.");

        ReportBackup? backup;
        try
        {
            backup = JsonSerializer.Deserialize<ReportBackup>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Tệp sao lưu không phải JSON hợp lệ: " + ex.Message, ex);
        }

        if (backup is null)
            throw new InvalidOperationException("Tệp sao lưu không hợp lệ.");

        if (backup.SchemaVersion > CurrentSchema)
            throw new InvalidOperationException(
                $"Gói sao lưu dùng lược đồ v{backup.SchemaVersion}, bản hiện tại chỉ hỗ trợ tới v{CurrentSchema}.");

        backup.Reports ??= new List<ReportDefinition>();
        return backup;
    }

    /// <summary>Chụp toàn bộ report hiện có trong kho thành một gói sao lưu.</summary>
    public static ReportBackup FromStore(IReportStore store)
    {
        if (store is null)
            throw new ArgumentNullException(nameof(store));

        return new ReportBackup { Reports = store.List().ToList() };
    }
}

/// <summary>Kết quả khôi phục kho từ một gói sao lưu.</summary>
public sealed class ReportImportResult
{
    /// <summary>Mã các report được thêm mới.</summary>
    public List<string> Added { get; } = new();

    /// <summary>Mã các report bị ghi đè (khi bật overwrite).</summary>
    public List<string> Updated { get; } = new();

    /// <summary>Mã các report bị bỏ qua vì đã tồn tại (khi tắt overwrite).</summary>
    public List<string> Skipped { get; } = new();

    /// <summary>Các mục lỗi (kèm lý do) — không chặn các mục còn lại.</summary>
    public List<string> Errors { get; } = new();

    /// <summary>Tổng số mục đã xử lý (thêm + ghi đè + bỏ qua).</summary>
    public int Total => Added.Count + Updated.Count + Skipped.Count;

    /// <summary>Mô tả ngắn để hiển thị cho người dùng.</summary>
    public string Summary =>
        $"Thêm {Added.Count}, cập nhật {Updated.Count}, bỏ qua {Skipped.Count}, lỗi {Errors.Count}.";
}