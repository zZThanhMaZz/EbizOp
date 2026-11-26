namespace EbizReports.Core.Integration;

/// <summary>Lỗi cấp tích hợp khi điều phối render/lưu report.</summary>
public class ReportIntegrationException : Exception
{
    public ReportIntegrationException(string message) : base(message) { }
}

/// <summary>Không tìm thấy report theo mã.</summary>
public sealed class ReportNotFoundException : ReportIntegrationException
{
    public string Code { get; }

    public ReportNotFoundException(string code)
        : base($"Không tìm thấy report \"{code}\".") => Code = code;
}

/// <summary>Không đủ quyền thực hiện hành động (render/design) trên report.</summary>
public sealed class ReportForbiddenException : ReportIntegrationException
{
    public string Code { get; }

    /// <summary>Hành động bị chặn: "render" hoặc "design".</summary>
    public string Action { get; }

    public ReportForbiddenException(string code, string action)
        : base($"Không có quyền {action} report \"{code}\".")
    {
        Code = code;
        Action = action;
    }
}
