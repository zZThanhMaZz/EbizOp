using EbizReports.Core.Data;
using EbizReports.Core.Integration;
using EbizReports.Core.Model;
using EbizReports.Core.Rendering;

namespace EbizReports.Rendering.Integration;

/// <summary>
/// Điều phối nghiệp vụ render: phân quyền → nạp định nghĩa → áp tham số → lấy dữ liệu → render.
/// Tách khỏi UI để có thể gọi từ Blazor, Web API, job in, hoặc Print Agent.
/// </summary>
public sealed class ReportService
{
    private readonly IReportStore _store;
    private readonly IReportDataProvider _dataProvider;
    private readonly ReportEngine _engine;
    private readonly IReportAuthorizer _authorizer;
    private readonly IReportAuditStore? _audit;

    public ReportService(
        IReportStore store,
        IReportDataProvider dataProvider,
        ReportEngine engine,
        IReportAuthorizer? authorizer = null,
        IReportAuditStore? audit = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _authorizer = authorizer ?? new AllowAllReportAuthorizer();
        _audit = audit;
    }

    /// <summary>Ghi nhật ký (không làm hỏng luồng nghiệp vụ nếu nơi lưu lỗi).</summary>
    private void Audit(string action, string code, int? version = null, string? detail = null)
    {
        if (_audit is null)
            return;
        try
        {
            _audit.Record(new ReportAuditEntry
            {
                Action = action,
                Code = code,
                Version = version,
                User = Environment.UserName,
                Detail = detail,
            });
        }
        catch
        {
            // Nhật ký không được phép chặn thao tác chính.
        }
    }

    /// <summary>Danh sách report trong kho.</summary>
    public IReadOnlyList<ReportDefinition> List() => _store.List();

    /// <summary>Lấy report theo mã, ném <see cref="ReportNotFoundException"/> nếu không có.</summary>
    public ReportDefinition Get(string code) => _store.Get(code) ?? throw new ReportNotFoundException(code);

    /// <summary>Lấy report theo mã, trả về null nếu không có.</summary>
    public ReportDefinition? Find(string code) => _store.Get(code);

    /// <summary>Lưu định nghĩa (cần quyền thiết kế).</summary>
    public void Save(ReportDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (!_authorizer.CanDesign(definition.Code))
            throw new ReportForbiddenException(definition.Code, "design");

        _store.Save(definition);
        Audit("save", definition.Code, definition.Version);
    }

    /// <summary>Xoá định nghĩa khỏi kho (cần quyền thiết kế). Trả về true nếu có xoá.</summary>
    public bool Delete(string code)
    {
        if (!_authorizer.CanDesign(code))
            throw new ReportForbiddenException(code, "design");

        var deleted = _store.Delete(code);
        if (deleted)
            Audit("delete", code);
        return deleted;
    }

    /// <summary>
    /// Nhân bản một report thành mã mới (mặc định <c>&lt;code&gt;-copy</c>, tự tránh trùng bằng
    /// hậu tố <c>-2</c>, <c>-3</c>…). Bản sao được sao chép sâu rồi lưu vào kho.
    /// </summary>
    /// <param name="code">Mã report nguồn.</param>
    /// <param name="newCode">Mã mong muốn cho bản sao; để trống thì tự sinh.</param>
    /// <param name="newName">Tên hiển thị cho bản sao; để trống thì thêm “(bản sao)”.</param>
    public ReportDefinition Clone(string code, string? newCode = null, string? newName = null)
    {
        var source = _store.Get(code) ?? throw new ReportNotFoundException(code);
        var target = UniqueCode(string.IsNullOrWhiteSpace(newCode) ? code + "-copy" : newCode!.Trim());

        if (!_authorizer.CanDesign(target))
            throw new ReportForbiddenException(target, "design");

        var clone = ReportDefinition.FromJson(source.ToJson());
        clone.Code = target;
        clone.Name = string.IsNullOrWhiteSpace(newName) ? source.Name + " (bản sao)" : newName!.Trim();
        clone.Version = 1;

        _store.Save(clone);
        Audit("clone", target, clone.Version, $"{code} → {target}");
        return clone;
    }

    /// <summary>Bảo đảm <paramref name="baseCode"/> chưa tồn tại; nếu có thì thêm hậu tố số.</summary>
    private string UniqueCode(string baseCode)
    {
        var code = baseCode;
        for (var i = 2; _store.Get(code) is not null; i++)
            code = $"{baseCode}-{i}";
        return code;
    }

    /// <summary>Xuất toàn bộ kho thành một gói sao lưu JSON (có thể tải xuống hoặc lưu trữ).</summary>
    public string ExportBackup() => ReportBackup.FromStore(_store).ToJson();

    /// <summary>
    /// Khôi phục kho từ một gói sao lưu JSON. Khi <paramref name="overwrite"/> = false (mặc định)
    /// các mã đã tồn tại sẽ bị bỏ qua; khi true thì ghi đè. Mục lỗi không chặn các mục còn lại.
    /// </summary>
    public ReportImportResult ImportBackup(string json, bool overwrite = false)
    {
        var backup = ReportBackup.FromJson(json);
        var result = new ReportImportResult();

        foreach (var def in backup.Reports)
        {
            if (def is null || string.IsNullOrWhiteSpace(def.Code))
            {
                result.Errors.Add("(báo cáo không có mã)");
                continue;
            }

            if (!_authorizer.CanDesign(def.Code))
            {
                result.Errors.Add($"{def.Code}: không có quyền thiết kế");
                continue;
            }

            var exists = _store.Get(def.Code) is not null;
            if (exists && !overwrite)
            {
                result.Skipped.Add(def.Code);
                continue;
            }

            // Sao chép sâu để kho không dùng chung instance với gói sao lưu.
            _store.Save(ReportDefinition.FromJson(def.ToJson()));
            (exists ? result.Updated : result.Added).Add(def.Code);
        }

        Audit("import", "*", null,
            $"thêm {result.Added.Count}, cập nhật {result.Updated.Count}, bỏ qua {result.Skipped.Count}, lỗi {result.Errors.Count}");
        return result;
    }

    /// <summary>Render theo mã report (nạp định nghĩa đã lưu).</summary>
    public byte[] Render(string code, RenderFormat format,
        IReadOnlyDictionary<string, object?>? parameters = null, ReportRenderOptions? options = null)
    {
        var definition = _store.Get(code) ?? throw new ReportNotFoundException(code);
        return Render(definition, format, parameters, options);
    }

    /// <summary>Render một định nghĩa cụ thể (dùng cho designer đang sửa, chưa lưu vào kho).</summary>
    public byte[] Render(ReportDefinition definition, RenderFormat format,
        IReadOnlyDictionary<string, object?>? parameters = null, ReportRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (!_authorizer.CanRender(definition.Code))
            throw new ReportForbiddenException(definition.Code, "render");

        var (data, renderOptions) = Prepare(definition, parameters, options);
        var result = _engine.Render(definition, data, format, renderOptions);
        Audit("render", definition.Code, definition.Version, format.ToString().ToLowerInvariant());
        return result;
    }

    /// <summary>
    /// Render tất cả trang ra ảnh PNG (mỗi phần tử = 1 trang) — dùng cho xem trước WYSIWYG
    /// trong designer theo định nghĩa đang sửa (chưa cần lưu vào kho).
    /// </summary>
    public IReadOnlyList<byte[]> RenderPagesPng(ReportDefinition definition,
        IReadOnlyDictionary<string, object?>? parameters = null, ReportRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (!_authorizer.CanRender(definition.Code))
            throw new ReportForbiddenException(definition.Code, "render");

        var (data, renderOptions) = Prepare(definition, parameters, options);
        var pages = new QuestPdfReportRenderer().RenderPagesPng(definition, data, renderOptions);
        Audit("preview", definition.Code, definition.Version, $"{pages.Count} trang");
        return pages;
    }

    /// <summary>Áp tham số hiệu lực, lấy dữ liệu và gắn resolver báo cáo con cho một định nghĩa.</summary>
    private (ReportData Data, ReportRenderOptions Options) Prepare(ReportDefinition definition,
        IReadOnlyDictionary<string, object?>? parameters, ReportRenderOptions? options)
    {
        var effective = EffectiveParameters(definition, parameters);
        var data = _dataProvider.GetData(definition, effective) ?? new ReportData();
        foreach (var kv in effective)
            data.SetParam(kv.Key, kv.Value);
        return (data, WithSubreports(options, effective));
    }

    /// <summary>
    /// Bảo đảm options có resolver cho item Subreport. Nếu người gọi đã tự cấu hình resolver
    /// thì giữ nguyên; ngược lại tạo bản sao và gắn resolver dùng kho + nguồn dữ liệu hiện tại.
    /// </summary>
    private ReportRenderOptions WithSubreports(ReportRenderOptions? options,
        IReadOnlyDictionary<string, object?> parentParameters)
    {
        if (options?.SubreportResolver is not null)
            return options;

        return new ReportRenderOptions
        {
            FontDirectory = options?.FontDirectory,
            FontFamily = options?.FontFamily ?? "Lato",
            Watermark = options?.Watermark,
            SubreportResolver = spec => ResolveSubreport(spec, parentParameters),
        };
    }

    /// <summary>Giải mã report con: nạp định nghĩa, áp tham số (kế thừa nếu bật) và lấy dữ liệu.</summary>
    private SubreportResolution? ResolveSubreport(SubreportSpec spec,
        IReadOnlyDictionary<string, object?> parentParameters)
    {
        if (string.IsNullOrWhiteSpace(spec.ReportCode))
            return null;

        var child = _store.Get(spec.ReportCode);
        if (child is null || !_authorizer.CanRender(child.Code))
            return null;

        var overrides = spec.InheritParameters ? parentParameters : null;
        var effective = EffectiveParameters(child, overrides);
        var data = _dataProvider.GetData(child, effective) ?? new ReportData();
        foreach (var kv in effective)
            data.SetParam(kv.Key, kv.Value);

        return new SubreportResolution { Definition = child, Data = data };
    }

    /// <summary>
    /// Tính tham số hiệu lực: bắt đầu từ giá trị mặc định (đã chuyển đúng kiểu) của định nghĩa,
    /// rồi ghi đè bằng giá trị người dùng truyền vào lúc chạy.
    /// </summary>
    public static Dictionary<string, object?> EffectiveParameters(ReportDefinition definition,
        IReadOnlyDictionary<string, object?>? overrides = null)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (definition?.Parameters is not null)
        {
            foreach (var p in definition.Parameters)
            {
                if (string.IsNullOrWhiteSpace(p.Name))
                    continue;
                var value = ReportParameterDefaults.Convert(p);
                if (value is not null)
                    result[p.Name] = value;
            }
        }

        if (overrides is not null)
        {
            foreach (var kv in overrides)
            {
                if (!string.IsNullOrWhiteSpace(kv.Key))
                    result[kv.Key] = kv.Value;
            }
        }
        return result;
    }
}
