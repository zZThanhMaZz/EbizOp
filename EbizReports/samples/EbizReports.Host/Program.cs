using EbizReports.Core.Integration;
using EbizReports.Data;
using EbizReports.Host.Components;
using EbizReports.Host.Services;
using EbizReports.Rendering;
using EbizReports.Rendering.Integration;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddSingleton<SampleReports>();
builder.Services.AddSingleton<ReportEngine>();

// ---- Lớp tích hợp plug-in: kho định nghĩa + nguồn dữ liệu + quyền + service điều phối ----
// Sổ kết nối CSDL: demo SQLite (tự tạo + seed) + tuỳ chọn SQL Server từ cấu hình.
builder.Services.AddSingleton(_ =>
{
    var registry = new SqlConnectionRegistry();

    var dbPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "ebizop-demo.db");
    var sqliteCs = DemoDatabase.EnsureCreated(dbPath);
    registry.Register(new SqlConnectionOptions
    {
        Name = "demo-sqlite",
        Provider = "sqlite",
        ConnectionString = sqliteCs,
        Label = "Demo SQLite — phiếu bán hàng",
    });

    var sqlServerCs = builder.Configuration["Reports:SqlServer:ConnectionString"];
    if (!string.IsNullOrWhiteSpace(sqlServerCs))
    {
        registry.Register(new SqlConnectionOptions
        {
            Name = "ebizop-sqlserver",
            Provider = "sqlserver",
            ConnectionString = sqlServerCs,
            Label = "EbizOp SQL Server",
        });
    }

    return registry;
});

// Nguồn dữ liệu: nếu report có datasource SQL thì chạy SQL; ngược lại dùng dữ liệu mẫu.
builder.Services.AddSingleton<IReportDataProvider>(sp =>
    new SqlReportDataProvider(
        sp.GetRequiredService<SqlConnectionRegistry>(),
        new SampleReportDataProvider(sp.GetRequiredService<SampleReports>())));
builder.Services.AddSingleton<IReportAuthorizer, AllowAllReportAuthorizer>();
builder.Services.AddSingleton<IReportAuditStore, InMemoryReportAuditStore>();
builder.Services.AddSingleton<IReportStore>(sp =>
{
    // File JSON trong App_Data/reports; seed từ SampleReports ở lần chạy đầu.
    var dir = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "reports");
    var seed = sp.GetRequiredService<SampleReports>().All();
    return new FileReportStore(dir, seed);
});
builder.Services.AddSingleton<ReportService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
