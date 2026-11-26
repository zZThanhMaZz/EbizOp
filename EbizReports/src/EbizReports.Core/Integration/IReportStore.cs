using EbizReports.Core.Model;

namespace EbizReports.Core.Integration;

/// <summary>
/// Kho lưu định nghĩa report. Module chỉ phụ thuộc interface này, nên ứng dụng host có thể
/// cài đặt bằng in-memory, file JSON, hay bảng trong CSDL mà không phải sửa phần render.
/// </summary>
public interface IReportStore
{
    /// <summary>Danh sách report (giữ nguyên thứ tự đã thêm/lưu).</summary>
    IReadOnlyList<ReportDefinition> List();

    /// <summary>Lấy theo mã (không phân biệt hoa/thường), null nếu không có.</summary>
    ReportDefinition? Get(string code);

    /// <summary>Thêm mới hoặc cập nhật theo <see cref="ReportDefinition.Code"/>.</summary>
    void Save(ReportDefinition definition);

    /// <summary>Xoá theo mã, trả về true nếu có xoá.</summary>
    bool Delete(string code);
}
