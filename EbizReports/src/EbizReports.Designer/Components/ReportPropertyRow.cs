namespace EbizReports.Designer.Components;

/// <summary>Kiểu trình soạn giá trị của một hàng thuộc tính.</summary>
public enum PropertyKind
{
    Text,
    Number,
    Bool,
    Enum,
    Color,
    ReadOnly,
}

/// <summary>
/// Một hàng trong lưới thuộc tính kiểu DevExpress: tên + giá trị, có phân nhóm và ô tìm kiếm.
/// <see cref="Value"/> là ảnh chụp lúc dựng; <see cref="Apply"/> ghi giá trị trở lại model.
/// </summary>
public sealed class PropertyRow
{
    /// <summary>Định danh duy nhất (dùng khi không có <see cref="EditKey"/>).</summary>
    public string Key { get; init; } = "";

    public string Label { get; init; } = "";

    /// <summary>Nhóm hiển thị (Bố cục, Dữ liệu, Giao diện, …).</summary>
    public string Category { get; init; } = "Khác";

    public PropertyKind Kind { get; init; } = PropertyKind.Text;

    public object? Value { get; init; }

    /// <summary>Danh sách lựa chọn khi <see cref="Kind"/> = <see cref="PropertyKind.Enum"/>.</summary>
    public IReadOnlyList<string>? Options { get; init; }

    public string? Placeholder { get; init; }

    /// <summary>Chú thích khi rê chuột.</summary>
    public string? Hint { get; init; }

    /// <summary>Khoá gộp lịch sử hoàn tác (xem <c>EditKey</c> của designer).</summary>
    public string? EditKey { get; init; }

    /// <summary>Màu xem trước cho hàng <see cref="PropertyKind.Color"/> (mặc định lấy <see cref="Value"/>).</summary>
    public string? Swatch { get; init; }

    public bool ReadOnly { get; init; }

    /// <summary>Ghi giá trị mới trở lại model.</summary>
    public Action<object?>? Apply { get; init; }

    public void Set(object? value) => Apply?.Invoke(value);
}
