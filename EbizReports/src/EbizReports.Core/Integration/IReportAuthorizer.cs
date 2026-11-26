namespace EbizReports.Core.Integration;

/// <summary>Kiểm tra quyền trên report (ví dụ <c>report:render</c> / <c>report:design</c>).</summary>
public interface IReportAuthorizer
{
    /// <summary>Được phép render/in report này?</summary>
    bool CanRender(string reportCode);

    /// <summary>Được phép thiết kế/sửa report này?</summary>
    bool CanDesign(string reportCode);
}

/// <summary>Mặc định: cho phép tất cả (dùng cho demo/test; ứng dụng thật nên cài đặt theo phân quyền).</summary>
public sealed class AllowAllReportAuthorizer : IReportAuthorizer
{
    public bool CanRender(string reportCode) => true;

    public bool CanDesign(string reportCode) => true;
}
