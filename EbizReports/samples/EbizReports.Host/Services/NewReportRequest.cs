namespace EbizReports.Host.Services;

/// <summary>Yêu cầu tạo báo cáo mới trả về từ hộp thoại.</summary>
public sealed record NewReportRequest(string Code, string Name);
