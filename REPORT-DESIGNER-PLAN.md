# Kế hoạch: Report Designer tự xây cho EbizOp (Mức 2)

> Quyết định đã chốt: **Giữ MudBlazor 9.9.0** · **Tự xây report designer kéo-thả** · **Không có source, chỉ có bản build**
> Stack mục tiêu: .NET 9 · Blazor Server · MudBlazor · QuestPDF (PDF) · MiniExcel (XLSX/CSV)
> Lưu ý license: QuestPDF Community chỉ miễn phí khi tổ chức dùng có doanh thu < 1 triệu USD/năm.

---

## 0. Ràng buộc & hệ quả

| Ràng buộc | Hệ quả |
|---|---|
| Không có source `.sln/.csproj` | Không sửa trực tiếp app. Phải xây **module độc lập** rồi tích hợp khi có source. |
| App chạy net9.0 | Thư viện target `net9.0` (đa target `net8.0;net9.0` nếu muốn chạy demo bằng runtime có sẵn). |
| Máy dev có SDK 8.0.428 và 10.0.302, **không có SDK 9** | Build `net9.0` bằng SDK 10 (cần ref pack `Microsoft.NETCore.App.Ref 9.x`), hoặc demo host target `net8.0`/`net10.0` để chạy ngay. |
| App dùng QuestPDF + MiniExcel | Renderer dùng lại chính 2 thư viện này để output khớp với app hiện tại. |
| Có sẵn font `LatoFont\` trong build | Dùng cho PDF tiếng Việt (QuestPDF phải nhúng font, không dựa font hệ thống). |

---

## 1. Kiến trúc solution đề xuất (độc lập, plug-in được)

```
EbizReports.sln
├─ src/EbizReports.Core            (net9.0)  model + expression + aggregate + validate
├─ src/EbizReports.Rendering       (net9.0)  IReportRenderer: QuestPdf / Excel / Csv / Html
├─ src/EbizReports.Designer        (net9.0)  Razor Class Library (MudBlazor) - designer UI
├─ src/EbizReports.Storage.EfCore  (net9.0)  bảng report_definitions / templates / runs
└─ samples/EbizReports.Host        (net8.0|net10.0)  Blazor Server demo để chạy thử
```

Khi có source EbizOp chỉ cần: thêm 4 package ref + `services.AddEbizReports()` + 1 trang Blazor + 1 migration bảng.

---

## 2. Model dữ liệu — `ReportDefinition` (V1)

```jsonc
{
  "code": "hoa-don-ban-hang",
  "name": "Hóa đơn bán hàng",
  "version": 1,
  "paper": { "size": "A5", "orientation": "Portrait", "unit": "mm",
             "margins": { "top": 8, "right": 6, "bottom": 8, "left": 6 } },
  "dataSources": [
    { "name": "Bill",    "type": "service", "ref": "ReportService.BillInfo", "params": ["billId"] },
    { "name": "Items",   "type": "query",   "ref": "sales_bill_items",     "params": ["billId"] }
  ],
  "parameters": [
    { "name": "billId", "type": "int",     "label": "Mã hóa đơn", "required": true },
    { "name": "from",   "type": "date",    "label": "Từ ngày",    "default": "today-30d" },
    { "name": "to",     "type": "date",    "label": "Đến ngày",   "default": "today" }
  ],
  "sorting": [ { "field": "Items.Name", "dir": "asc" } ],
  "grouping": [ { "field": "Items.GroupName", "repeatHeader": true } ],
  "bands": [
    { "kind": "PageHeader", "height": 22, "items": [
        { "kind": "Text",  "x": 0,  "y": 0,  "w": 70, "h": 8,
          "text": "{Store.Name}", "style": { "size": 12, "bold": true } },
        { "kind": "Image", "x": 70, "y": 0,  "w": 30, "h": 18, "src": "{Store.Logo}" }
    ]},
    { "kind": "GroupHeader", "height": 8, "items": [
        { "kind": "Expression", "x": 0, "y": 0, "w": 60, "h": 6,
          "expression": "Nhom: {Fields.GroupName}" }
    ]},
    { "kind": "Detail", "height": 7, "items": [
        { "kind": "Field", "x": 0,  "y": 0, "w": 50, "h": 6, "binding": { "path": "Items.Name" } },
        { "kind": "Field", "x": 50, "y": 0, "w": 15, "h": 6, "binding": { "path": "Items.Qty" } },
        { "kind": "Field", "x": 65, "y": 0, "w": 20, "h": 6, "binding": { "path": "Items.UnitPrice" },
          "format": "N0" }
    ]},
    { "kind": "GroupFooter", "height": 7, "items": [
        { "kind": "Expression", "x": 50, "y": 0, "w": 35, "h": 6,
          "expression": "Cong: {Sum(Items.LineTotal):N0}" }
    ]},
    { "kind": "PageFooter", "height": 8, "items": [
        { "kind": "PageNumber", "x": 0, "y": 0, "w": 100, "h": 6, "text": "Trang {Page}/{Pages}" }
    ]}
  ],
  "outputs": ["pdf", "xlsx", "csv", "html"]
}
```

**Item kinds:** `Text`, `Field`, `Expression`, `Image`, `Barcode`, `QrCode`, `Line`, `Rect`, `Table`, `Chart`, `Subreport`, `PageNumber`.
**Band kinds:** `ReportHeader`, `PageHeader`, `GroupHeader`, `Detail`, `GroupFooter`, `PageFooter`, `ReportFooter`.
**Aggregate scope:** `none | group | report`.

---

## 3. Expression engine (an toàn, không eval code động)

- Cú pháp: `{Fields.TenKhach}`, `{Sum(Items.LineTotal)}`, `{IIF(Fields.Qty > 0, "Có", "Không")}`.
- Parser tự viết (Pratt) → AST → compile sang `Func<EvalContext, object>`; **không** dùng CodeDom/Reflection.Emit để tránh rủi ro bảo mật.
- Hàm hỗ trợ: `Sum, Avg, Count, Min, Max, Round, Abs, IIF, Upper, Lower, Trim, Substring, Concat, Format, Today, Now, DateDiff`.
- Format tiền/ngày theo `vi-VN`: `N0`, `dd/MM/yyyy`.
- Validate lúc lưu: field phải tồn tại trong datasource schema, không cho gọi hàm lạ.

---

## 4. DesignTime vs Runtime

| | Design time | Run time |
|---|---|---|
| Dữ liệu | sample/mock (10 dòng) hoặc data thật giới hạn | data thật theo parameter |
| Renderer | HtmlRenderer (nhanh để preview) | QuestPdf / Excel / Csv |
| Mục đích | kéo-thả, canh lề | in, xuất file, gửi máy in |

---

## 5. Designer UX (MudBlazor)

- **Layout 3 cột:** Toolbox (trái) · Canvas + ruler (giữa) · Property panel + Data panel (phải).
- **Toolbar:** Lưu, Preview, Export, Page setup, Zoom, Undo/Redo, Test với dữ liệu thật.
- **Canvas:** bands kéo giãn chiều cao, item kéo-thả + 8 handle resize, snap grid (mm), multi-select, align tools, ruler mm.
- **Data panel:** liệt kê field theo datasource, kéo field vào canvas để tạo `Field` item.
- **Expression builder:** chèn field + hàm + format, có xem trước kết quả.
- **Undo/Redo:** command stack trên model (JSON patch), không snapshot toàn bộ.
- **Preview:** nếu HtmlRenderer không đủ khớp PDF → render PDF rồi nhúng viewer (pdf.js) để WYSIWYG.

---

## 6. Rendering

- **QuestPdfRenderer:** map band → container; `GroupHeader/Footer` → lặp theo group; aggregate tính trước (precompute thành dictionary) để tránh đếm lại; table item → QuestPDF `Table`.
- **ExcelRenderer (MiniExcel):** chỉ hỗ trợ `Detail/Group/Header/Footer` dạng grid (flatten). Item phi grid (line/image/barcode) bỏ qua hoặc ghi chú.
- **CsvRenderer:** flatten giống Excel.
- **HtmlRenderer:** dùng cho preview trong Blazor.
- **Bỏ phạm vi V1:** export DOCX (rất khó, ROI thấp).

---

## 7. Tích hợp vào EbizOp (khi có source)

1. `services.AddEbizReports(o => o.UseQuestPdf().UseMiniExcel())`.
2. Tạo `IReportDataSource` adapter trỏ vào `ReportService`/`OpDbContext` sẵn có.
3. Trang `/settings/report-designer` (Blazor) + quyền mới `report:design` (thêm vào `Permissions.All` + `Roles`/`Role.Matrix`).
4. Bảng DB: `report_definitions`, `report_runs` (migration trong `EnsureFinanceSchemaAsync` hoặc migration EF riêng).
5. Template mặc định: hóa đơn bán hàng, tem barcode, phiếu nhập, phiếu thu/chi, báo cáo doanh thu.
6. In: dùng `PrintAgent`/`sales_print_jobs` sẵn có — report PDF đẩy qua cùng cơ chế.

---

## 8. Lộ trình & ước lượng

| Giai đoạn | Nội dung | Thời lượng |
|---|---|---|
| **P0** | Core model + JSON schema + expression engine + QuestPdf/Excel renderer cho report 1 datasource (header/detail/footer, aggregate) + demo host | ~1 tuần |
| **P1** | Designer UI: bands, toolbox, kéo-thả, resize, property grid, save/load JSON, preview HTML/PDF | 2–3 tuần |
| **P2** | Grouping/sorting, parameters, table/chart/barcode/QR, undo/redo, ruler/zoom, binding từ service | 3–4 tuần |
| **P3** | Tích hợp EbizOp (khi có source): datasource adapters, trang designer, phân quyền, template mặc định, in qua agent | 2–3 tuần |
| **P4** | Hardening: font VN, phân trang, keep-together, subreport, subreport/parameter nâng cao, audit | 2–4 tuần |

**Tổng: ~2–4 tháng cho bản dùng được** (đúng như đánh giá Mức 2).

### Rủi ro chính
- **Font tiếng Việt trong PDF**: phải nhúng font (Lato đã có sẵn).
- **Phân trang / group break / keep-together**: là phần khó nhất của engine.
- **WYSIWYG giữa designer và PDF thật**: HTML preview có thể lệch → fallback pdf.js.
- **Expression an toàn**: tự viết parser, không eval động.

---

## 9. Bước tiếp theo

Bắt đầu bằng **P0**: scaffold solution `EbizReports.sln` với Core + Rendering + demo Host, và dựng 1 template mẫu (hóa đơn bán hàng) render ra PDF + XLSX để chứng minh end-to-end. Sau đó mới làm designer UI (P1).

---

## 10. Trạng thái thực hiện

### ✅ P0 — HOÀN THÀNH (đã build + verify)

Solution tạo tại `EbizReports\`, đã chạy thật và render đúng:

```
EbizReports.sln
├─ src/EbizReports.Core        (net8.0;net9.0)  model + expression engine + grouping
├─ src/EbizReports.Rendering   (net8.0;net9.0)  QuestPDF / MiniExcel / CSV (font Lato nhúng)
└─ samples/EbizReports.Demo    (net8.0)         console demo
```

Chạy demo: `dotnet run --project samples/EbizReports.Demo` → ghi vào `out/`.

Kết quả verify:
| File | Nội dung |
|---|---|
| `hoa-don.pdf/.xlsx/.csv` | hóa đơn A5: header, detail, tổng, số trang, tiếng Việt đúng dấu |
| `ban-hang-theo-nhom.pdf/.xlsx/.csv` | A4 ngang: group header/footer + `Sum()` theo nhóm |
| `hoa-don.json` | định nghĩa report JSON (round-trip OK) |

Đã hoạt động: ReportDefinition JSON · expression engine (Sum/Avg/Count/Min/Max/IIF/Format/... ) · 7 loại band · item Text/Field/Expression/PageNumber/Line/Rect/Image · grouping + sort + aggregate · interpolation `{Field}` · renderer PDF/XLSX/CSV.

Ghi chú: demo hiện là **console**; Blazor host chuyển sang P1 cùng designer UI (không cần cho việc verify engine).

### ⏭ P1 — bước 1: HOÀN THÀNH (đã build + kiểm thử bằng browser)

Thêm 2 project:
```
src/EbizReports.Designer    (net8.0;net9.0)  Razor Class Library — component ReportDesigner (MudBlazor)
samples/EbizReports.Host    (net8.0)         Blazor Server host — chạy `dotnet run --project samples/EbizReports.Host`
```
Chạy: `http://localhost:5199` (hoặc `/designer/ban-hang-theo-nhom`). Legend đã kiểm thử tự động (Playwright/Chrome headless):
- chọn item trên cây & canvas → property panel hiện X/Y/W/H + nội dung/binding/expression/format + style
- sửa X=40mm → item dịch đúng (0px → 151.18px); sửa W=70mm → rộng đúng (→ 264.57px)
- thêm/xoá item (23 → 24 → 23); đổi căn lề
- LƯU JSON tải file `hoa-don-ban-hang.json` (X=40,W=70,align=center có trong file); MỞ JSON nạp lại đổi tên hiển thị
- XEM PDF mở viewer ngay trong trang, **phản ánh chỉnh sửa đang thao tác**

Lưu ý kỹ thuật: phải chạy ở môi trường **Development** để `_content/*` (MudBlazor CSS/JS + designer.css) được phục vụ; `launchSettings.json` đã đặt sẵn.

### ⏭ P1 — bước 2: HOÀN THÀNH (đã build + kiểm thử bằng browser)

Thêm khối tương tác tách rời để giữ mượt (không round-trip .NET mỗi `pointermove`):
```
src/EbizReports.Designer/wwwroot/designer.js   ES module — pointer drag/resize, snap, phím tắt
src/EbizReports.Designer/ItemGeometry.cs       DTO toạ độ mm JS → .NET
```
JS xử lý `pointerdown/move/up` trên canvas, cập nhật `left/top/width/height` trực tiếp; khi nhả chuột mới gọi
`OnGeometryCommitted(IEnumerable<ItemGeometry>)` để ghi vào model. Kết quả Playwright (có assertion):

- **ruler** ngang+dọc (A5: 15 nhãn), nền **lưới** bật/tắt, scale 3.779 px/mm đúng
- **kéo-thả** +15mm/+10mm → model X/Y đổi đúng, ô property X hiện đúng (15)
- **resize** cạnh đông +12mm → W 80→92; cạnh nam +6mm → H 7→13; đủ 8 tay cầm
- **undo/redo**: Ctrl+Z lần lượt H 13→7, W 92→80; Ctrl+Y → W 104 (đúng thứ tự)
- **nudge** ArrowRight → X +1mm
- **multi-select** Ctrl+click → "2 item" (2 `.rd-item.sel`); Esc → "chưa chọn"
- **z-order** "Lên đầu" → item thành phần tử cuối trong band (idx 6/7)
- **snap off** kéo 7.4mm → giữ đúng 7.40mm (không làm tròn)
- **Lưu JSON** chứa toạ độ vừa kéo (X=38.4, W=104)

### ⏭ P1 — bước 3: HOÀN THÀNH (Barcode + QRCode, build + kiểm thử)

Thêm khoá SVG tự sinh (dùng ZXing.Net để mã hoá, không kéo System.Drawing/ảnh):
```
src/EbizReports.Core/Model/ReportDefinition.cs      SymbologySpec + ReportItem.Symbology + Clone()
src/EbizReports.Rendering/Symbology/Symbology.cs     encode → ma trận module → SVG vector
src/EbizReports.Rendering/Symbology/SymbologyVerifier.cs  encode → raster → decode (self-test)
```
Kết quả:
- `Items`: Barcode (Code128/Code39/EAN13/EAN8/UPCA/ITF) + QrCode (ECC L/M/Q/H), đặt tuyệt đối như mọi item
- vẽ `IContainer.Svg(...)` → vector, sắc nét; tuỳ chọn hiện số dưới barcode + quiet zone
- designer: thêm Barcode/QRCode vào toolbox; xem trước SVG thật trên canvas; panel sửa định dạng/ECC/hiện số/vùng lặng; **Ctrl+D** nhân bản
- XLSX/CSV: xuất giá trị mã dạng chữ

**Kiểm chứng tự động** (`dotnet run --project samples/EbizReports.Demo`):
```
OK  Code128   modules=156x1
OK  Code39    modules=142x1
OK  EAN13     modules=95x1     (đúng 95 module chuẩn)
OK  QR (M)    modules=29x29
OK  QR (H)    modules=33x33    (UTF-8 tiếng Việt)
OK  QR (L)    modules=21x21
=> Tất cả mã vạch/QR giải mã lại khớp.
```
Và trong designer (Playwright): 2 symbol SVG khi mở; thêm QR/Barcode → 4; Ctrl+D → 5 rồi undo → 4; panel QR có ECC/Vùng lặng; panel Barcode có Định dạng/Hiện số; Xem PDF hiện iframe.

### ⏭ P1 — bước 4: HOÀN THÀNH (hoàn thiện designer, build + kiểm thử)

```
src/EbizReports.Designer/Components/ReportDesigner.razor  band mgmt + clipboard + align/distribute
src/EbizReports.Designer/wwwroot/designer.js              alignment guides + Ctrl+C/X/V/A
src/EbizReports.Designer/wwwroot/designer.css             .rd-guides, .rd-bandctl, .rd-align
```
Kết quả:
- **Band**: thêm (nút cho từng loại còn thiếu), nhân bản, xoá, đổi thứ tự lên/xuống
- **Clipboard**: Ctrl+C/Ctrl+X/Ctrl+V (+ nút Sao chép/Cắt/Dán); dán lệch 3 mm mỗi lần
- **Ctrl+A**: chọn tất cả item trong band đang chọn
- **Căn chỉnh** (≥2 item): trái/giữa ngang/phải/trên/giữa dọc/dưới
- **Rải đều** (≥3 item): ngang/dọc, giữ mép đầu–cuối, chia khoảng cách bằng nhau
- **Đường dóng** khi kéo: mép/giữa item khác (cùng band) + mép/giữa trang, bắt dính vector (ưu tiên hơn lưới); công tắc **Dóng**

**Kiểm chứng Playwright** (`designer.test8.cjs`): tất cả PASS —
```
OK  duplicate band +1 / remove band -1 / move down đổi thứ tự / move up khôi phục
OK  add band (+GroupHeader) +1
OK  Ctrl+A chọn 7 item; align left → X = [0,0,...]; align top → Y = [0,0,...]
OK  Căn chỉnh (4) + nút Rải ngang/dọc; rải ngang → X = 0,62,76,104
OK  Ctrl+C→Ctrl+V +1; Ctrl+V +1; Ctrl+X -1; Ctrl+V khôi phục; nút Sao chép/Dán +1; nút Cắt -1
OK  kéo item tới giữa trang → 2 đường dóng dọc; thả → xoá sạch
```

### ⏭ P1 — bước 5: HOÀN THÀNH (tham số / parameters, build + kiểm thử)

```
src/EbizReports.Core/Model/ReportParameterDefaults.cs      FromString + Apply (áp mặc định)
src/EbizReports.Rendering/ReportEngine.cs                  áp mặc định trước khi render
src/EbizReports.Designer/Components/ReportDesigner.razor   mục "Tham số" (định nghĩa) + "Tham số khi chạy"
src/EbizReports.Designer/wwwroot/designer.css              .rd-plist, .rd-pnode, .rd-pval
samples/EbizReports.Host/.../SampleReports.cs              TieuDe (String) · TuNgay/DenNgay (Date)
samples/EbizReports.Host/.../Home.razor                    RenderPreview(def, values) → SetParam
samples/EbizReports.Demo/Program.cs                        ParameterSelfTest
```
Kết quả:
- **Định nghĩa tham số**: tên, nhãn, kiểu (String/Int/Decimal/Date/Bool), bắt buộc, mặc định; thêm/xoá/sửa trong designer (có hoàn tác)
- **Panel “Tham số khi chạy”**: ô nhập theo kiểu — text / số / **date picker** / checkbox; điền sẵn giá trị mặc định
- **Áp mặc định ở engine** (`ReportParameterDefaults.Apply`): report render được cả khi không có UI nhập
- **Xem PDF** truyền giá trị tham số; **chặn + cảnh báo** khi thiếu tham số bắt buộc
- Tham số tra cứu trong Field/Expression/nội suy `{}` theo thứ tự: dòng → field → tham số

**Kiểm chứng**:
- Demo (`ParameterSelfTest`): `engine áp mặc định: TieuDe=HÓA ĐƠN BÁN HÀNG`; `EvaluateText("TieuDe")` OK;
  ghi đè `TieuDe="PHIẾU THANH TOÁN"` → `hoa-don-thamso.png` (đã xem, tiêu đề đổi đúng);
  `TuNgay` là `DateTime` → format `dd/MM/yyyy` = `01/01/2026`.
- Playwright (`designer.test9.cjs`): 20/20 PASS —
```
OK  1 param node (String) · name TieuDe · run-value điền sẵn "HÓA ĐƠN BÁN HÀNG"
OK  + Tham số → 2; đổi tên → NhanVien; ô chạy mới xuất hiện
OK  Ctrl+Z hoàn tác đổi tên rồi hoàn tác thêm → 1
OK  Xem PDF → iframe hiện (blob), tiêu đề trong PDF = "PHIẾU THANH TOÁN"
OK  tick Bắt buộc + xoá giá trị → alert "Thiếu tham số bắt buộc: Tiêu đề hóa đơn"
OK  xoá tham số → 0; Ctrl+Z khôi phục → 1
OK  báo cáo nhóm: 2 tham số kiểu Date, date picker điền sẵn 01/01/2026
```

### ⏭ P1 — bước 6: HOÀN THÀNH (nhóm item & khoá vị trí, build + kiểm thử)

```
src/EbizReports.Core/Model/ReportDefinition.cs             + Group, Locked (per item)
src/EbizReports.Designer/Components/ReportDesigner.razor   mục "Nhóm & khoá" + cờ Locked + data-group/locked
src/EbizReports.Designer/wwwroot/designer.js               chọn/kéo theo nhóm · bỏ qua item khoá · Ctrl+G / Ctrl+Shift+G
src/EbizReports.Designer/wwwroot/designer.css              .rd-item.locked, .rd-lock, .rd-badge
```
Kết quả:
- **Nhóm**: gán item vào nhóm (Ctrl+G hoặc nút **Nhóm**); click 1 item thuộc nhóm → chọn **cả nhóm**; kéo 1 item → **cả nhóm dịch chuyển cùng delta**
- **Bỏ nhóm**: Ctrl+Shift+G hoặc nút **Bỏ nhóm**
- **Khoá vị trí**: item khoá không kéo/resize/nudge/xoá từ canvas; hiện **🔒**, ẩn tay cầm resize; nút **Khoá đã chọn** / **Mở khoá**
- Cây cấu trúc hiện nhãn **NHÓM** cho item có group; `Group`/`Locked` lưu trong JSON definition

**Kiểm chứng Playwright** (`designer.test10.cjs`): 17/17 PASS —
```
OK  gán cùng group id (ec612d8f) · cây hiện badge "nhóm"
OK  Escape bỏ chọn · click item nhóm → chọn cả nhóm (2)
OK  kéo nhóm → d0=(11,11) d1=(11,11) (dịch chuyển bằng nhau)
OK  bỏ nhóm → id rỗng · Ctrl+Z khôi phục nhóm + vị trí
OK  khoá → .locked, ẩn tay cầm resize, không kéo được, bỏ qua nudge mũi tên
OK  Mở khoá → kéo lại bình thường
OK  Ctrl+G nhóm (7 item) · Ctrl+Shift+G bỏ nhóm
```

### ⏭ P1 — bước 7: HOÀN THÀNH (biểu đồ / chart, build + kiểm thử)

```
src/EbizReports.Core/Model/Enums.cs                     + ItemKind.Chart
src/EbizReports.Core/Model/ReportDefinition.cs          + ChartSpec (type/category/value/aggregate/legend/...)
src/EbizReports.Core/Charts/ChartBuilder.cs             tổng hợp dữ liệu + dựng SVG (bar/line/pie)
src/EbizReports.Rendering/QuestPdfReportRenderer.cs     RenderItem: Chart → SVG FitArea
src/EbizReports.Designer/Components/ReportDesigner.razor nút "Biểu đồ" + panel Chart + preview dữ liệu mẫu
src/EbizReports.Designer/wwwroot/designer.css           .rd-chart
samples/EbizReports.Host/.../SampleReports.cs           biểu đồ trong ReportFooter báo cáo nhóm
samples/EbizReports.Demo/Program.cs                     ChartSelfTest + BuildChartReport
```
Kết quả:
- **Loại**: cột (bar) · đường (line) · tròn (pie) — SVG vector tự dựng (không thư viện vẽ)
- **Dữ liệu**: Category × Value theo dòng detail; tổng hợp Sum/Avg/Count/Min/Max
- **Tuỳ chọn**: Tiêu đề · Chú giải (không/phải/dưới) · Hiện giá trị; trục + lưới + nhãn rút gọn K/M
- **Designer**: preview bằng dữ liệu mẫu; **Xem PDF** dùng dữ liệu thật; XLSX/CSV bỏ qua biểu đồ

**Kiểm chứng**:
- Demo (`ChartSelfTest`): tổng hợp `Sum(LineTotal)` theo nhóm = `Gọng 3.440.000 · Tròng 3.400.000 · Phụ kiện 390.000` (OK);
  `count` = 3/2/2; SVG bar/line/pie hợp lệ; render `chart-bar.png` / `chart-line.png` / `chart-pie.png` (đã xem).
- Playwright (`designer.test11.cjs`): 13/13 PASS —
```
OK  thêm item Biểu đồ → canvas có .rd-chart svg; cây hiện Chart; preview bar có <rect>
OK  panel Chart: Loại · Category · Value (đủ trường)
OK  đổi Loại → line có <polyline>; → pie có <path>; Ctrl+Z quay lại (bỏ <path>)
OK  báo cáo nhóm: ReportFooter có Chart; Xem PDF → iframe blob (biểu đồ thật cuối báo cáo)
```

### ⏭ P1 — bước 8: HOÀN THÀNH (bảng / table, build + kiểm thử)

```
src/EbizReports.Core/Model/Enums.cs                     + ItemKind.Table
src/EbizReports.Core/Model/ReportDefinition.cs          + TableSpec / TableColumn
src/EbizReports.Core/Tables/TableHelper.cs              giá trị ô + dòng tổng (sum/avg/count/min/max)
src/EbizReports.Rendering/QuestPdfReportRenderer.cs     RenderItem: Table → QuestPDF Table
src/EbizReports.Designer/Components/ReportDesigner.razor nút "Bảng" + panel cột + preview khung bảng
src/EbizReports.Designer/wwwroot/designer.css           .rd-table/.rd-tbl/.rd-col
samples/EbizReports.Host/.../SampleReports.cs           báo cáo mẫu "top-san-pham" (bảng ở ReportHeader)
samples/EbizReports.Demo/Program.cs                     TableSelfTest + BuildTableReport
```
Kết quả:
- **Cột**: Tiêu đề · Binding/Expression · Format · Canh lề · Rộng (mm); thêm/xoá/đổi thứ tự trong designer
- **Dữ liệu**: các dòng detail của band chứa item; **dòng tổng** theo cột (Sum/Avg/Count/Min/Max)
- **Tuỳ chọn**: Tiêu đề (lặp khi sang trang) · Viền · Sọc xen kẽ · Màu nền tiêu đề · MaxRows
- **Designer**: preview khung bảng; **Xem PDF** dùng dữ liệu thật; XLSX/CSV bỏ qua item bảng

**Kiểm chứng**:
- Demo (`TableSelfTest`): ô dòng 1 `Name = "Gọng nhựa trẻ em"`; tổng `SL = 33`; tổng `Doanh thu = 7.230.000`;
  `avg = 1.032.857`; render `table-demo.png` (đã xem — tiêu đề xanh, sọc, viền, dòng tổng đúng).
- Playwright (`designer.test12.cjs`): 14/14 PASS —
```
OK  thêm item Bảng → canvas có .rd-table table.rd-tbl (3 cột mặc định); cây hiện Table
OK  panel: "Cột (3)" + 3 card cột; mỗi card ≥ 4 trường nhập
OK  Thêm cột → 4; Ctrl+Z → 3
OK  sửa Tiêu đề cột #1 → preview <th> đổi ngay ("Sản phẩm")
OK  top-san-pham: ReportHeader có Table; Xem PDF → iframe blob (bảng thật)
```

### ⏭ P1 — bước 9: HOÀN THÀNH (lớp tích hợp plug-in, build + kiểm thử)

```
src/EbizReports.Core/Integration/IReportStore.cs            kho định nghĩa (List/Get/Save/Delete)
src/EbizReports.Core/Integration/InMemoryReportStore.cs     kho in-memory
src/EbizReports.Core/Integration/FileReportStore.cs         kho file JSON (mỗi report 1 file, có cache)
src/EbizReports.Core/Integration/IReportDataProvider.cs      nguồn dữ liệu + DelegateReportDataProvider
src/EbizReports.Core/Integration/IReportAuthorizer.cs        quyền render/design + AllowAllReportAuthorizer
src/EbizReports.Core/Integration/ReportIntegrationException.cs  ReportNotFound/Forbidden
src/EbizReports.Rendering/Integration/ReportService.cs       điều phối render/lưu + EffectiveParameters
samples/EbizReports.Host/Services/SampleReportDataProvider.cs adapter SampleReports → IReportDataProvider
samples/EbizReports.Host/Program.cs                          DI: store (file) + provider + authorizer + service
samples/EbizReports.Host/Components/Pages/Home.razor         nút "Lưu vào kho"; preview qua ReportService
samples/EbizReports.Demo/Program.cs                          IntegrationSelfTest
```
Kết quả:
- **Kho định nghĩa**: `IReportStore` với hai bản dựng sẵn (in-memory, file JSON bền vững).
- **Nguồn dữ liệu**: `IReportDataProvider` — cắm adapter bất kỳ (demo dùng `DelegateReportDataProvider`).
- **Phân quyền**: `IReportAuthorizer` (`CanRender`/`CanDesign`) — mặc định cho phép tất cả.
- **Điều phối**: `ReportService.Render(code|definition, format, params)` — phân quyền → nạp → áp
  tham số mặc định + ghi đè → lấy dữ liệu → render; `Save` cần quyền design.
- Host: `FileReportStore` (`App_Data/reports`) seed từ `SampleReports`; nút **Lưu vào kho** trên designer.

**Kiểm chứng**:
- Demo (`IntegrationSelfTest`): list 2; `Find` đúng; render PDF qua service (33,738B); ghi đè tham số (33,768B);
  mã sai → `ReportNotFoundException`; `DenyAllAuthorizer` chặn render + lưu → `ReportForbiddenException`;
  `FileReportStore` ghi `hoa-don-ban-hang.json` rồi đọc lại khớp.
- Playwright (`designer.test13.cjs`): 10/10 PASS —
```
OK  layout: nội dung nằm dưới app bar (top >= 48)  top=60
OK  danh sách report từ kho (3)
OK  designer tải được (canvas + cây cấu trúc)
OK  cả 3 report trong kho mở được
OK  nút "Lưu vào kho" có mặt
OK  lưu vào kho báo thành công  Đã lưu "top-san-pham" vào kho.
OK  kho ghi 3 file JSON: ban-hang-theo-nhom.json,hoa-don-ban-hang.json,top-san-pham.json
OK  có file top-san-pham.json
OK  file JSON chứa item Bảng (TableSpec)
OK  Xem PDF qua ReportService (blob)
```
- Sửa kèm lỗi layout Host: app bar đè thanh công cụ vì `pa-3` dùng `padding !important`
  → `MudMainContent Style="padding: 60px 12px 12px 12px"`.

### ⏭ P1 — bước 10: HOÀN THÀNH (báo cáo con / Subreport, build + kiểm thử)

```
src/EbizReports.Core/Model/Enums.cs                         ItemKind.Subreport
src/EbizReports.Core/Model/ReportDefinition.cs              SubreportSpec + ReportItem.Subreport
src/EbizReports.Core/Rendering/IReportRenderer.cs           SubreportResolver + SubreportResolution
src/EbizReports.Rendering/QuestPdfReportRenderer.cs         RenderReportInto + RenderSubreport (chặn đệ quy, kẹp bề rộng)
src/EbizReports.Rendering/Integration/ReportService.cs      tự nối resolver (kho + dữ liệu + quyền) + Delete
src/EbizReports.Designer/Components/ReportDesigner.razor    công cụ Subreport, preview, panel chọn (AvailableReports)
src/EbizReports.Designer/wwwroot/designer.css               khối .rd-sub (nét đứt)
samples/EbizReports.Host/Services/SampleReports.cs          báo cáo mẫu bao-cao-tong-hop
samples/EbizReports.Host/Components/Pages/Home.razor        nút Xoá khỏi kho (xác nhận) + truyền AvailableReports
samples/EbizReports.Demo/Program.cs                         SubreportSelfTest
```
Kết quả:
- **Model**: `ItemKind.Subreport` + `SubreportSpec` (`ReportCode`, `InheritParameters`, `ShowBorder`).
- **Hợp đồng render**: `ReportRenderOptions.SubreportResolver` trả `SubreportResolution` (định nghĩa + dữ liệu),
  renderer không tự truy cập kho → dùng được ngoài host.
- **Renderer**: `RenderReportInto` vẽ nội dung report con trong khung item (bỏ page header/footer); chặn đệ quy
  (tối đa 4 tầng); kẹp bề rộng theo vùng khả dụng; thiếu report/quyền → placeholder thay vì ném.
- **ReportService**: tự nối resolver từ kho + nguồn dữ liệu + phân quyền, kế thừa tham số report cha; thêm `Delete`.
- **Designer**: công cụ **Subreport**, preview khối nét đứt, panel chọn báo cáo con (host truyền `AvailableReports`).
- **Host**: báo cáo mẫu `bao-cao-tong-hop` nhúng `top-san-pham`; nút **Xoá khỏi kho** có hộp thoại xác nhận.

**Kiểm chứng**:
- Demo (`SubreportSelfTest`): resolver gọi đúng 1 lần/lượt; PDF 28,612B; thiếu báo cáo con → placeholder 17,406B;
  `ReportService` tự nối resolver (28,612B); thiếu quyền report con → placeholder 17,406B.
- Playwright (`designer.test14.cjs`, 11/11 PASS):
```
OK  kho có 4 report (gồm bao-cao-tong-hop)
OK  canvas có khối báo cáo con (1)
OK  báo cáo con trỏ tới top-san-pham
OK  thêm được item báo cáo con mới (2)
OK  panel hiện mô tả báo cáo con
OK  có bộ chọn báo cáo con riêng (1)
OK  bộ chọn báo cáo con liệt kê các report
OK  đổi báo cáo con → canvas cập nhật
OK  xuất JSON chứa cấu hình Subreport
OK  Xem PDF render được báo cáo con (blob)
OK  huỷ xác nhận xoá → kho không đổi
```
- Sửa kèm: `Placeholder` từng dùng `.Height(6)` (6pt) với font 7pt → QuestPDF ném "conflicting size
  constraints"; nay để text tự chảy trong khung có viền + padding.

### ⏭ P1 — bước 11: HOÀN THÀNH (định dạng có điều kiện, build + kiểm thử)

```
src/EbizReports.Core/Model/ReportDefinition.cs            ReportItem.VisibleWhen + StyleRules; StyleRule; ItemStyle.Clone
src/EbizReports.Rendering/QuestPdfReportRenderer.cs       IsVisible + ResolveStyle (theo scope) + nền cho text
src/EbizReports.Designer/Components/ReportDesigner.razor   mục "Định dạng có điều kiện" + badge ĐK
src/EbizReports.Designer/wwwroot/designer.css              .rd-cond / .rd-rule
samples/EbizReports.Host/Services/SampleReports.cs         luật mẫu cho ban-hang-theo-nhom
samples/EbizReports.Demo/Program.cs                        ConditionalFormatSelfTest
```
Kết quả:
- **Model**: `VisibleWhen` (ẩn item khi biểu thức sai) và `StyleRules` (đổi Đậm/Nghiêng/Cỡ/Màu/Nền/Khung).
- **Renderer**: lọc item ẩn ở `RenderBand`; `ResolveStyle` áp luật khớp theo `EvalScope` (dòng detail);
  lỗi biểu thức → bỏ qua an toàn (VisibleWhen lỗi = vẫn hiện); nền `Background` nay áp cho cả item chữ.
- **Designer**: ô *Hiện khi*, danh sách luật thêm/xoá (nút Đậm/Nghiêng/Khung + màu chữ/nền), badge **ĐK**.
- **Mẫu**: `ban-hang-theo-nhom` — Qty ≥ 5 tô nền cam, thành tiền ≥ 1 triệu đỏ + đậm.

**Kiểm chứng**:
- Demo (`ConditionalFormatSelfTest`): baseline 21.350B; `VisibleWhen "Qty > 2"` → 20.685B (nhỏ hơn, đúng ẩn);
  thêm luật màu → 22.504B (khác bản không luật); ảnh `cond-format.png` đúng trực quan.
- Playwright (`designer.test15.cjs`, 10/10 PASS):
```
OK  canvas hiện badge ĐK cho item có luật (2)
OK  panel có mục "Định dạng có điều kiện"
OK  luật mẫu "LineTotal >= 1000000" hiện trong panel
OK  nút "Thêm luật" thêm luật mới (1 → 2)
OK  JSON xuất có StyleRules của item Thành tiền (rules=2)
OK  JSON xuất có VisibleWhen "Qty >= 3"
OK  Xem PDF render với định dạng điều kiện (blob)
```

### ⏭ P1 — bước 12: HOÀN THÀNH (ảnh / logo, build + kiểm thử)

```
src/EbizReports.Rendering/QuestPdfReportRenderer.cs       Image: SVG (.Svg) + raster (.Image); data URI base64/utf8; nguồn lỗi an toàn
src/EbizReports.Designer/Components/ReportDesigner.razor   nút "Ảnh", ô Nguồn ảnh, tải tệp → base64, xem trước
src/EbizReports.Designer/wwwroot/designer.css              .rd-img / .rd-img-prev / .rd-file
samples/EbizReports.Host/Services/SampleReports.cs         logo SVG nhúng cho bao-cao-tong-hop
samples/EbizReports.Demo/Program.cs                        ImageSelfTest
```
Kết quả:
- **Renderer**: `ItemKind.Image` vẽ **SVG véc-tơ** (phát hiện `<svg>`) hoặc **raster** (PNG/JPEG) qua `QuestPDF.Image`;
  nguồn là data URI (base64 hoặc utf8) hay đường dẫn tệp; ảnh vừa khung, giữ tỷ lệ; nguồn hỏng bị bỏ qua.
- **Designer**: nút **Ảnh** (mặc định 40×24 mm), ô *Nguồn ảnh* (dán data URI / đường dẫn), **tải tệp** (nhúng base64,
  tối đa 6 MB), xem trước; canvas hiện ảnh hoặc huy hiệu 🖼 + tên tệp.
- **Mẫu**: `bao-cao-tong-hop` có logo EbizOp (SVG kính + chữ) ở góc phải PageHeader.
- XLSX/CSV bỏ qua item ảnh (vẫn xuất phẳng theo cột).

**Kiểm chứng**:
- Demo (`ImageSelfTest`): có ảnh 49.418B > không ảnh 16.023B; nguồn đường dẫn tệp render OK; nguồn lỗi bị bỏ qua;
  ảnh `image-demo.png` đúng trực quan (logo SVG + PNG nhúng + dòng "nguồn lỗi → bỏ qua").
- Playwright (`designer.test16.cjs`, 11/11 PASS):
```
OK  canvas hiện ảnh logo mẫu (SVG nhúng)
OK  cây cấu trúc có item Image
OK  panel có ô "Nguồn ảnh" + ảnh xem trước
OK  nút "Ảnh" thêm item vào band (1 → 2)
OK  dán nguồn → item đang chọn hiện ảnh
OK  JSON xuất có Kind=Image + Src data URI
OK  Xem PDF render bản có ảnh (blob)
```

### ⏭ P1 — bước 13: HOÀN THÀNH (sắp xếp & nhóm dữ liệu, build + kiểm thử)

```
src/EbizReports.Core/Rendering/ReportGrouping.cs     áp Sorting (ổn định) + sắp trong từng nhóm
src/EbizReports.Designer/Components/ReportDesigner.razor   panel "Sắp xếp" + "Nhóm dữ liệu" (thêm/xoá/đổi thứ tự)
src/EbizReports.Designer/wwwroot/designer.css        .rd-key / .rd-key-act
samples/EbizReports.Demo/Program.cs                  SortingSelfTest
```
Kết quả:
- **Engine**: `ReportDefinition.Sorting` trước đây khai báo nhưng **chưa hề được dùng**; nay được áp thật
  (sắp xếp ổn định, so sánh số/ngày/chuỗi). Có `Grouping` → nhóm liền nhau theo field nhóm, rồi **trong mỗi nhóm**
  theo `Sorting`; không `Grouping` → sắp toàn bộ dòng. XLSX/CSV hưởng lợi vì dùng chung `ReportGrouping.Build`.
- **Designer**: panel trái thêm **Sắp xếp** và **Nhóm dữ liệu** (trường + chiều, thêm/xoá/đổi thứ tự, *Lặp tiêu đề nhóm*).
- **Demo**: `SortingSelfTest` (nhóm `Gọng, Phụ kiện, Tròng`; trong nhóm `LineTotal` giảm dần; đủ 7 dòng).

**Kiểm chứng**:
- Demo (`SortingSelfTest`): 3 nhóm · 7 dòng; cả 3 kiểm tra OK; `sort-group.png` / `sort-group.pdf`.
- Playwright (`designer.test17.cjs`, 15/15 PASS):
```
OK  mẫu hiện "Nhóm dữ liệu (1)" = GroupName
OK  "+ Sắp xếp" thêm dòng, điền LineTotal, chọn Giảm
OK  "+ Nhóm" thêm nhóm Name
OK  JSON Sorting=[{LineTotal, desc}] + Grouping=[GroupName, Name]
OK  Xem PDF render báo cáo nhóm + sắp xếp (blob)
```

### ⏭ P1 — bước 14: HOÀN THÀNH (bảng trường dữ liệu / data panel, build + kiểm thử)

```
src/EbizReports.Core/Data/ReportFieldCatalog.cs            hợp nhất trường từ mọi dòng + field + tham số (+ giá trị mẫu)
src/EbizReports.Designer/Components/ReportDesigner.razor   panel "Trường dữ liệu", lọc, bấm/kéo-thả tạo Field
src/EbizReports.Designer/wwwroot/designer.css              .rd-fields / .rd-field
samples/EbizReports.Host/Components/Pages/Home.razor       nạp AvailableFields từ IReportDataProvider
samples/EbizReports.Demo/Program.cs                        FieldCatalogSelfTest
```
Kết quả:
- **Core**: `ReportFieldCatalog.Discover(definition, data)` liệt kê trường khả dụng: hợp nhất khoá của **mọi**
  dòng detail (trường chỉ có ở dòng sau vẫn xuất hiện), cộng field đơn và tham số; kèm `Category`
  (detail/field/param) và giá trị mẫu.
- **Designer**: panel trái **Trường dữ liệu (n)** — chip màu theo loại + ô tìm kiếm; **bấm** chip hoặc
  **kéo-thả** vào band để tạo item `Field` bind đúng trường (chưa chọn band → dùng `Detail`; item xếp dưới item thấp nhất).
- **Host**: nạp trường từ `IReportDataProvider` (mẫu) → khi ghép EbizOp chỉ đổi provider, designer không đổi.

**Kiểm chứng**:
- Demo (`FieldCatalogSelfTest`): 7 trường `GroupName, Name, Qty, LineTotal (dòng)`, `StoreName (đơn)`,
  `TuNgay, TuNgay2 (TS)`; `LineTotal` chỉ ở dòng 2 vẫn được hợp nhất; mẫu `= "640.000"`; không trùng tên.
- Playwright (`designer.test18.cjs`, 16/16 PASS):
```
OK  panel hiện "Trường dữ liệu (6)" với chip GroupName/Name/Qty/LineTotal (dòng) + TuNgay/DenNgay (TS)
OK  lọc "Line" → còn đúng chip LineTotal; xoá lọc khôi phục đủ
OK  bấm chip LineTotal → thêm 1 item vào band
OK  kéo-thả chip Qty vào band Detail → thêm 1 item
OK  JSON: +1 Field(Binding=LineTotal) và +1 Field(Binding=Qty, band=Detail)
```

### ⏭ P1 — bước 15: HOÀN THÀNH (xem trước WYSIWYG nhiều trang, build + kiểm thử)

```
src/EbizReports.Rendering/QuestPdfReportRenderer.cs        RenderPagesPng (tất cả trang → PNG)
src/EbizReports.Rendering/Integration/ReportService.cs     RenderPagesPng + tách Prepare() dùng chung
src/EbizReports.Designer/Components/ReportDesigner.razor   overlay xem trước: trang, zoom, vừa khổ, phím tắt
src/EbizReports.Designer/wwwroot/designer.js               elementWidth (đo khung cho "vừa khổ")
src/EbizReports.Designer/wwwroot/designer.css              .rd-pvw* (overlay + thanh công cụ + khung cuộn)
samples/EbizReports.Host/Components/Pages/Home.razor       RenderPreviewPages = Reports.RenderPagesPng
samples/EbizReports.Demo/Program.cs                        PngPagesSelfTest
```
Kết quả:
- **Engine/Service**: `RenderPagesPng` render **mọi trang** ra ảnh PNG; `ReportService` tái dùng phân quyền,
  tham số hiệu lực, dữ liệu và resolver báo cáo con (tách helper `Prepare`).
- **Designer**: nút **Xem trước** mở overlay hiển thị từng trang dạng ảnh (đúng 100% bản in, không cần pdf.js);
  điều hướng trang (đầu/trước/sau/cuối), **zoom** −/+ (25–400%), **100%**, **Vừa khổ** (đo bề rộng khung qua JS),
  phím `←/→`, `+/−`, `Esc`. Bề rộng trang suy từ khổ giấy mm→px nên zoom chính xác, không phụ thuộc trình duyệt.
- Giữ nút **Xem PDF** (iframe) cho bản véc-tơ.

**Kiểm chứng**:
- Demo (`PngPagesSelfTest`): 150 dòng → **4 trang**, report ngắn → **1 trang**, ảnh PNG hợp lệ.
- Playwright (`designer.test19.cjs`, 13/13 PASS):
```
OK  mở overlay .rd-pvw; ảnh là PNG data URI; "Trang 1/1"
OK  bề rộng ≈1122px (A4 ngang) → zoom in 125% rộng hơn; 100% / Vừa khổ đúng
OK  nút Trang trước/sau bị khoá khi 1 trang
OK  Esc và nút X đều đóng overlay
```

### ⏭ P1 — bước 16: HOÀN THÀNH (bảng con theo nhóm)

```
src/EbizReports.Core/Model/ReportDefinition.cs      TableSpec.Scope ("group" | "all")
src/EbizReports.Core/Tables/TableHelper.cs           RowsFor(spec, contextRows, data)
src/EbizReports.Rendering/QuestPdfReportRenderer.cs  RenderTable dùng RowsFor; Detail chỉ vẽ bảng nhóm 1 lần/nhóm
src/EbizReports.Designer/Components/ReportDesigner.razor  nút "Bảng nhóm" + combobox "Phạm vi dữ liệu"
samples/EbizReports.Demo/Program.cs                  GroupTableSelfTest
```
Kết quả:
- **Model**: `TableSpec.Scope = "group"` (mặc định, dòng của nhóm hiện tại) | `"all"` (toàn bộ dòng).
- **Engine**: `TableHelper.RowsFor` chọn bộ dòng theo phạm vi; bảng nằm trong `Detail` chỉ vẽ **1 lần/nhóm**
  (tránh lặp N lần); bảng trong `GroupHeader/GroupFooter` vẽ theo từng nhóm.
- **Designer**: nút **Bảng nhóm** tự tạo/chọn `GroupHeader`, thêm bảng (3 cột mẫu, dòng tổng), đặt dưới item sẵn có
  và nới chiều cao band; panel phải có combobox **Phạm vi dữ liệu**.

**Kiểm chứng**:
- Demo (`GroupTableSelfTest`): 3 nhóm (3, 2, 2 dòng); `Scope=group` → 3 dòng nhóm đầu, `Scope=all` → 7 dòng;
  bảng trong Detail **không lặp** (Δdetail ≈ Δheader); xuất `group-table.png`.
- Playwright (`designer.test20.cjs`, 13/13 PASS):
```
OK  bấm "Bảng nhóm" → GroupHeader +1 Table (Scope=group, 3 cột, có dòng tổng)
OK  band GroupHeader nới cao 57mm; panel có "Phạm vi dữ liệu"
OK  đổi phạm vi → JSON Scope=all; canvas hiển thị khối bảng .rd-tbl
```

### ⏭ P1 — bước 17: HOÀN THÀNH (sao lưu / khôi phục / nhân bản)

```
src/EbizReports.Core/Integration/ReportBackup.cs      gói sao lưu + ReportImportResult (schema, exportedAt, reports)
src/EbizReports.Rendering/Integration/ReportService.cs  ExportBackup / ImportBackup / Clone / UniqueCode
samples/EbizReports.Host/Components/Pages/Home.razor     nút Nhân bản / Sao lưu kho / Khôi phục + DesignerKey
samples/EbizReports.Host/wwwroot/js/backup.js            tải file bằng Blob
samples/EbizReports.Host/Components/App.razor            nạp backup.js
samples/EbizReports.Demo/Program.cs                      BackupRestoreSelfTest
```
Kết quả:
- **Core**: `ReportBackup` = 1 file JSON (`SchemaVersion`, `ExportedAt`, `Reports[]`), có kiểm tra lược đồ;
  `ReportImportResult` tổng hợp thêm/cập nhật/bỏ qua/lỗi.
- **Service**: `ExportBackup`, `ImportBackup(json, overwrite)` (mặc định bỏ qua mã trùng), `Clone(code, newCode?, newName?)`
  sao chép sâu + tự tránh trùng mã. Tất cả ở tầng service, không phụ thuộc UI.
- **Host**: nút **Nhân bản** (lưu bản gốc rồi chuyển sang bản sao), **Sao lưu kho** (tải `reports-backup-*.json`),
  **Khôi phục** (đọc file, hỏi ghi đè); `DesignerKey` ép dựng lại designer sau khi nạp lại.

**Kiểm chứng**:
- Demo (`BackupRestoreSelfTest`): khôi phục kho rỗng (thêm 2), khôi phục lại (bỏ qua 2), ghi đè khôi phục gốc;
  nhân bản `bc-a-copy` / `bc-a-copy-2` (tên “(bản sao)”, `Version=1`, sao chép sâu); từ chối schema v99 / JSON hỏng.
- Playwright (`designer.test21.cjs`, 17/17 PASS):
```
OK  nhân bản → URL /designer/bc-clone-test; designer hiển thị mã bản sao; giữ item
OK  sao lưu → JSON schema 1, ≥5 báo cáo, có bc-clone-test, có ExportedAt
OK  khôi phục → "Thêm 1, cập nhật 5, bỏ qua 0, lỗi 0"; mở được bc-import-test
OK  sao lưu lại → kho có 6 báo cáo (đã ghi xuống store)
```

### ⏭ P1 — bước 18: HOÀN THÀNH (expression builder có xem trước)

```
src/EbizReports.Core/Expressions/ExpressionInspector.cs      danh mục 18 hàm + Validate + Evaluate + SampleData
src/EbizReports.Designer/Components/ExpressionBuilder.razor  ô nhập + Kiểm tra + Xem kết quả + chip chèn hàm/trường
src/EbizReports.Designer/Components/ReportDesigner.razor     dùng builder cho Expression / VisibleWhen / rule.When / cột bảng
src/EbizReports.Designer/wwwroot/designer.css                .rd-expr* (chip, thông báo ok/err)
samples/EbizReports.Host/Components/Pages/Home.razor          PreviewExpression (dữ liệu thật + tham số hiệu dụng)
samples/EbizReports.Demo/Program.cs                           ExpressionBuilderSelfTest
```
Kết quả:
- **Inspector**: `Functions` (18 hàm kèm chữ ký/mô tả/snippet/số đối số); `Validate` bắt lỗi cú pháp **và**
  hàm lạ/sai số đối số; `Evaluate` tính thử (mặc định dùng dòng đầu); `SampleData` dựng `ReportData` mẫu
  từ trường + giá trị mặc định của tham số.
- **Component**: `ExpressionBuilder` nhận `Value/ValueChanged/Definition/Fields/Preview/Label/Compact`;
  chip hàm chèn snippet, chip trường/tham số chèn tên; thông báo `ok`/`err`.
- **Tích hợp**: áp dụng ở 4 chỗ nhập biểu thức (Expression item, Hiện khi, StyleRule.Khi, cột bảng Expression);
  Host cung cấp `PreviewExpression` chạy trên dữ liệu thật (kèm tham số hiệu dụng) — fallback về dữ liệu mẫu.

**Kiểm chứng**:
- Demo (`ExpressionBuilderSelfTest`, 30 mục, 0 FAIL): danh mục hàm, `Validate` hợp lệ/sai, `Sum/Avg/Count/`
  `Min/Max/Iif/Upper/Round/Format/Len`, dữ liệu mẫu vi-VN + ghép dòng/field/param.
- Playwright (`designer.test22.cjs`, 14/14 PASS):
```
OK  chọn item Expression → panel có builder (.rd-expr) + nút Kiểm tra / Xem kết quả
OK  18 chip hàm + chip trường LineTotal
OK  Xem kết quả Sum(LineTotal) = 7.230.000 (dữ liệu thật)
OK  Kiểm tra "Sum(LineTotal)" → hợp lệ; "Sum(" → báo lỗi kèm vị trí
OK  chip Count() chèn đúng; chip trường nối vào biểu thức
OK  Lưu JSON chứa biểu thức vừa nhập
```

> Lưu ý kỹ thuật: tham số kiểu `string` của component phải gán bằng **biểu thức có `@`**
> (`Value="@_item.Expression"`), nếu viết `Value="_item.Expression"` Razor sẽ hiểu là **chuỗi hằng**.

### ⏭ P1 — bước 19: HOÀN THÀNH (phân trang & keep-together)

```
src/EbizReports.Core/Model/ReportDefinition.cs        ReportGroup.KeepTogether + ReportBand.PageBreakBefore
src/EbizReports.Rendering/QuestPdfReportRenderer.cs   PreventPageBreak()/PageBreak() + RenderBandItem()
src/EbizReports.Designer/Components/ReportDesigner.razor  checkbox "Giữ nhóm trên một trang" / "Ngắt trang trước band"
samples/EbizReports.Demo/Program.cs                   PaginationSelfTest
```
Kết quả:
- **Model**: `ReportGroup.KeepTogether` (giữ cả nhóm trên một trang nếu vừa) và
  `ReportBand.PageBreakBefore` (ép sang trang mới trước band).
- **Renderer**: tách `RenderBandItem` xử lý `PageBreakBefore`; khi bật keep-together, mỗi nhóm được bọc
  `col.Item().PreventPageBreak().Column(...)`. Chỉ áp dụng khi báo cáo có khai báo nhóm.
- **Designer**: 2 checkbox tương ứng trong panel Nhóm / panel Band.

**Kiểm chứng**:
- Demo (`PaginationSelfTest`, 3 mục OK): 5 nhóm × 100 mm → không giữ **2 trang**, giữ nhóm **3 trang**;
  `PageBreakBefore` trên ReportFooter thêm đúng **1 trang**.
- Playwright (`designer.test23.cjs`, 13/13 PASS): 2 cờ mặc định `false`; bật → JSON `KeepTogether=true`,
  `PageBreakBefore=true`; xem trước WYSIWYG vẫn render (Trang 1/2).

### ⏭ P1 — bước 20: HOÀN THÀNH (nhật ký thao tác / audit)

```
src/EbizReports.Core/Integration/ReportAudit.cs       ReportAuditEntry + IReportAuditStore + InMemoryReportAuditStore
src/EbizReports.Rendering/Integration/ReportService.cs  ghi audit save/delete/clone/import/render/preview
samples/EbizReports.Host/Program.cs                    đăng ký InMemoryReportAuditStore
samples/EbizReports.Host/Components/AuditDialog.razor   hộp thoại bảng nhật ký
samples/EbizReports.Host/Components/Pages/Home.razor    nút "Nhật ký"
samples/EbizReports.Demo/Program.cs                    AuditSelfTest
```
Kết quả:
- **Core**: `ReportAuditEntry` (thời gian, thao tác, mã, phiên bản, người dùng, chi tiết); `IReportAuditStore`
  pluggable; `InMemoryReportAuditStore` thread-safe, cắt theo sức chứa, trả mới-nhất-trước.
- **Service**: tự ghi nhật ký cho `save`, `delete`, `clone`, `import`, `render`, `preview`; lỗi nơi lưu
  không chặn thao tác chính (nuốt lỗi có chủ đích).
- **Host**: nút **Nhật ký** mở `AuditDialog` liệt kê 100 mục gần nhất.

**Kiểm chứng**:
- Demo (`AuditSelfTest`, 9 mục OK): chuỗi `delete > render > clone > save` đúng thứ tự; có mã + người dùng;
  `clone` ghi “nguồn → đích”; `import` ghi tóm tắt; cắt theo sức chứa, giữ mục mới nhất.
- Playwright (`designer.test24.cjs`, 8/8 PASS): nhật ký ban đầu trống → xem trước → thấy dòng
  `preview · ban-hang-theo-nhom · 1 · PC · 1 trang`.

### ⏭ P1 — bước 21: HOÀN THÀNH (nguồn dữ liệu SQL: SQLite + SQL Server)

```
src/EbizReports.Data/EbizReports.Data.csproj            (net8.0;net9.0) Microsoft.Data.Sqlite + Microsoft.Data.SqlClient
src/EbizReports.Data/SqlConnectionOptions.cs            thông tin kết nối (host giữ)
src/EbizReports.Data/SqlConnectionRegistry.cs           sổ đăng ký + Describe() cho designer
src/EbizReports.Data/SqlDataSourceExecutor.cs           Probe()/Execute() + bind @ThamSo
src/EbizReports.Data/SqlReportDataProvider.cs           IReportDataProvider (sql → fallback)
src/EbizReports.Core/Model/ReportDefinition.cs          ReportDataSource.Connection + .Sql (Type "service"|"sql")
src/EbizReports.Core/Integration/ReportSql.cs           ReportConnectionInfo + SqlProbeResult
src/EbizReports.Designer/Components/ReportDesigner.razor  panel "Nguồn dữ liệu" + Connections/TestDataSource
samples/EbizReports.Host/Services/DemoDatabase.cs       SQLite demo (bảng PhieuBanHang) + seed
samples/EbizReports.Host/Services/SalesReportTemplate.cs  bộ khung "Danh sách phiếu bán hàng"
samples/EbizReports.Host/Components/NewReportDialog.razor nút "Tạo báo cáo mới"
samples/EbizReports.Demo/Program.cs                     SqlDataSourceSelfTest
```
Kết quả:
- **Data**: chạy SQL qua ADO.NET thuần (`DbProviderFactory`) trên **SQLite** hoặc **SQL Server**; tham số `@Ten`
  trong câu SQL được bind từ tham số báo cáo; `Probe` trả cột + dòng mẫu (nuốt lỗi → `Ok=false`).
- **Provider**: `SqlReportDataProvider` ưu tiên datasource `sql`, nếu không có thì chuyển tiếp nguồn dự phòng.
- **Designer**: panel **Nguồn dữ liệu** (thêm/xoá, chọn kết nối, ô nhập SQL, nút **Chạy thử**); cột trả về tự
  đổ vào **Trường dữ liệu**.
- **Host**: nút **Tạo báo cáo mới** sinh bộ khung có tham số Từ/Đến ngày; CSDL demo SQLite tự tạo + seed;
  tuỳ chọn SQL Server qua `Reports:SqlServer:ConnectionString`.

**Kiểm chứng**:
- Demo (`SqlDataSourceSelfTest`): lọc theo `@TuNgay`/`@DenNgay`, `ORDER BY`, ánh xạ cột, “Chạy thử”, SQL sai báo
  lỗi, fallback, render PDF → `out/sql-datasource.pdf`.
- Playwright (`flow.test25.cjs`, 16/16 PASS): tạo báo cáo mới → thêm nguồn → chọn `Demo SQLite` → nhập SQL →
  Chạy thử (13 dòng · 6 cột) → cột vào panel → xem trước (Trang 1/1) → lưu kho.
- Video: `out/huong-dan-nguon-du-lieu-sql.mp4`.

### ⏭ P1 — bước 22: HOÀN THÀNH (giao diện giống DevExpress Reports — phần 1–6/6)

Danh sách ưu tiên (làm tuần tự): 1) Property Grid · 2) Toolbox · 3) Bố cục 3 panel + kéo giãn band ·
4) Thước mm + zoom + snap · 5) Menu chuột phải + Copy/Paste + căn chỉnh · 6) Thanh công cụ dạng tab (ribbon).

```
src/EbizReports.Designer/Components/ReportPropertyRow.cs     model hàng thuộc tính (Kind/Category/Options/Apply)
src/EbizReports.Designer/Components/ReportPropertyGrid.razor lưới thuộc tính: tìm kiếm + nhóm gấp/mở
src/EbizReports.Designer/Components/ReportDesigner.razor     - Toolbox (rd-tb), tab phải (rd-rtab), band header + tay kéo
                                                             - PropertyRows() cho Báo cáo/Band/Item; OnBandHeightCommitted()
                                                             - thanh zoom (.rd-zoombar): SetZoom/ZoomBy/ZoomFitAsync
                                                             - menu chuột phải (.rd-ctx) + OnContextMenu/OnCloseContextMenu
                                                             - ribbon (.rd-ribbon): _ribbonTab + RibbonBold/Italic/... + SetStyleAsync
src/EbizReports.Designer/wwwroot/designer.css                .rpg*, .rd-toolbox/.rd-tb, .rd-rtabs/.rd-rtab, .rd-bandhead/.rd-band-rz,
                                                             .rd-cscroll (vùng cuộn), .rd-zoombar/.rd-z* (thanh zoom),
                                                             .rd-ctx* (menu chuột phải), .rd-ribbon/.rd-rtab2/.rd-rgroup/.rd-rbtn
src/EbizReports.Designer/wwwroot/designer.js                 kéo giãn chiều cao band (mode "bandh" → OnBandHeightCommitted);
                                                             chuột phải → OnContextMenu (chặn menu mặc định)
```
Kết quả:
- **1) Property Grid**: hàng kiểu DevExpress (tên bên trái, trình soạn bên phải) với ô tìm kiếm và nhóm gấp/mở;
  nhóm **Band** (loại, chiều cao, ngắt trang, số điều khiển) khi chọn band, nhóm **Bố cục/Dữ liệu/Giao diện/Hành vi** khi chọn item.
- **2) Toolbox**: lưới icon 12 điều khiển thay cho dãy nút “Thêm vào band” cũ.
- **3) 3 panel**: Trái = Hộp công cụ + Report Explorer (Cấu trúc) + Band; Giữa = canvas + thước; Phải = tab
  **Trường dữ liệu** / **Thuộc tính**. Band có header (loại + mm) và **tay kéo** ở đáy để đổi chiều cao.
- **4) Thước + Zoom**: thước mm ngang/dọc; thanh zoom góc dưới phải (`−`/`+`, nhãn %, preset 50/75/100/125/150/200%,
  **Vừa khung**). Dùng CSS `zoom` trên `.rd-stage` nên thước, lưới, chữ và item cùng phóng; JS `computeScale()`
  đo lại px/mm từ trang nên kéo/thả và kéo giãn band vẫn đúng ở mọi mức zoom (snap 1 mm giữ nguyên).
- **5) Menu chuột phải**: JS chặn `contextmenu` trên vùng thiết kế → gọi `OnContextMenu(band, idx, x, y)`; item
  được chuột phải tự chọn. Menu dựng bằng HTML (`.rd-ctx`): Cắt/Sao chép/Dán/Nhân bản/Xoá (kèm `Ctrl+X/C/V/D`),
  **Căn chỉnh** 6 nút (dùng lại `AlignAsync`), **Phân bố** ngang/dọc (dùng lại `DistributeAsync`),
  Nhóm/Bỏ nhóm, Khoá/Mở khoá vị trí; đóng bằng click ra ngoài hoặc `Esc`.
- **6) Ribbon dạng tab**: thanh công cụ chia 3 tab **Trang chủ / Bố cục / Định dạng** (`_ribbonTab`).
  *Trang chủ* = Hoàn tác/Làm lại + Lưu/Mở JSON + Xem PDF/Xem trước; *Bố cục* = Snap + bước + Lưới + Dóng + Vừa khung;
  *Định dạng* = Đậm/Nghiêng/Khung + Cỡ chữ + Canh lề + Màu chữ, áp cho **1 hoặc nhiều item** đang chọn qua `SetStyleAsync`
  (dùng `PushEdit` cho Undo, đồng bộ với Property Grid).

**Kiểm chứng**: Playwright `designer.test28.cjs` (14/14), `designer.test29.cjs` (18/18), `designer.test30.cjs`
(26/26 — menu chuột phải, Phân bố −24/40 → 8/8 mm giữ nguyên độ rộng, Canh trên cùng Y, Khoá/Mở khoá, Esc),
`designer.test31.cjs` (24/24 — ribbon 3 tab, tab mặc định Trang chủ, Bố cục có Snap/Lưới/Dóng, Định dạng áp
Đậm/Nghiêng/Canh lề/Cỡ chữ/Màu chữ và đồng bộ Property Grid, áp nhiều item bằng `Ctrl+A`);
các bộ cũ vẫn xanh (`designer.test[11,12,14,15,16,18,20,23,26,27].cjs`, `flow.test25.cjs`); Demo self-test 0 FAIL; build 0/0.

### ⏭ P1 — bước 23: HOÀN THÀNH (3 tab cấp cao: Thông số · Thiết kế mẫu · Xem trước)

Tách dữ liệu/tham số/SQL khỏi phần vẽ, giống cửa sổ "MAU BAO CAO" của EbizOp. Thêm tab cấp cao (`_mainTab`):

- **Tab Thông số** (`_mainTab = "params"`, `_sqlTab` cho 2 tab con SQL): **Thông tin báo cáo** (Mã · Nhóm ·
  Tên báo cáo · Diễn giải + cờ *Là mẫu của hệ thống* / *Lưu tại máy client* / *Dùng để in lưới số liệu*);
  lưới **Các tham số cho báo cáo** (`.rd-pgrid`: TT/R, Tham số, Tên tham số, Kiểu DL, Quyền, Câu SQL cho
  combobox, Field giá trị, Field hiển thị, Các Fields ẩn, Mặc định; thêm/xoá dòng); **Nguồn dữ liệu** + 2 tab
  con **Câu SQL dùng báo cáo** / **Câu SQL dùng để thiết kế** (Chạy thử); **Sắp xếp**, **Nhóm dữ liệu**.
- **Tab Thiết kế mẫu** (mặc định): toàn bộ phần vẽ (ribbon, toolbox, cấu trúc/band, canvas + thước + zoom,
  tab phải Trường dữ liệu / Thuộc tính).
- **Tab Xem trước**: render WYSIWYG trong tab (chuyển trang, zoom, Vừa khổ, Tạo xem trước); overlay `.rd-pvw`
  trước đây vẫn dùng cho nút *Xem trước* trên ribbon, chỉ ẩn khi đang ở tab Xem trước.

Model bổ sung: `ReportDefinition.Group/Description/IsSystem/SaveToClient/ForGridPrint`;
`ReportParameter.Permission/ComboBoxSql/ValueField/DisplayField/HiddenFields`; `ReportDataSource.DesignSql`.

```
src/EbizReports.Core/Model/ReportDefinition.cs               + metadata báo cáo, + cột tham số, + DesignSql
src/EbizReports.Designer/Components/ReportDesigner.razor     3 tab (.rd-mtab), lưới tham số (.rd-pgrid), 2 tab con SQL,
                                                             tab Xem trước; metadata/param helpers; EditKey.Meta
src/EbizReports.Designer/wwwroot/designer.css                .rd-mtabs/.rd-mtab, .rd-params/.rd-pscard/.rd-meta/.rd-in,
                                                             .rd-pgrid*, .rd-sqltabs/.rd-stab, .rd-prevtab
```

**Kiểm chứng**: Playwright `designer.test32.cjs` (29/29 PASS — 3 tab, metadata, lưới tham số, nguồn dữ liệu +
SQL 2 tab con, tab Xem trước); các bộ cũ đã cập nhật luồng vẫn xanh
(`designer.test[11,12,14,15,16,18,20,23,26,27,28,29,30,31].cjs`, `flow.test25.cjs`); build 0/0.
Ảnh: `out/dx-07-tab-thongso.png`, `out/dx-07-tab-preview.png`.

### ⏭ P3 — tích hợp EbizOp (kế tiếp, cần source)
Datasource adapter thật, phân quyền `report:design`, bảng DB (`report_definitions`, `report_runs`),
template mặc định, in qua Print Agent; gắn `IReportAuditStore` vào DB/audit trail thật.
